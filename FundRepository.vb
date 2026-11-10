
Imports Microsoft.Data.SqlClient
Imports System.Data

Public Class FundPriceMatch

    Public Property ISIN As String
    Public Property FundName As String
    Public Property Currency As String
    Public Property Price As Decimal
    Public Property UpdateDate As DateTime
    Public Property MappingStatus As String

End Class


Public Class FundRepository

    Private ReadOnly connectionString As String =
        "Server=localhost;Database=ProfileFundPractice;Integrated Security=True;TrustServerCertificate=True;"


    ' ==========================================
    ' EXERCISE 5: RETRIEVE AND MATCH FUND PRICES
    ' ==========================================

    Public Function GetFundPriceMatches() As List(Of FundPriceMatch)

        Dim results As New List(Of FundPriceMatch)

        Dim query As String =
            "SELECT " &
            "dp.ISIN, " &
            "f.FullFundName, " &
            "dp.Currency, " &
            "dp.Price, " &
            "dp.UpdateDate, " &
            "CASE WHEN f.ISIN IS NULL THEN 'UNMATCHED' ELSE 'MATCHED' END AS MappingStatus " &
            "FROM dbo.DailyPricesUpdate AS dp " &
            "LEFT JOIN dbo.Funds AS f ON dp.ISIN = f.ISIN " &
            "ORDER BY dp.UpdateDate DESC, dp.ISIN;"

        Using connection As New SqlConnection(connectionString)

            Using command As New SqlCommand(query, connection)

                connection.Open()

                Using reader As SqlDataReader = command.ExecuteReader()

                    Dim isinIndex = reader.GetOrdinal("ISIN")
                    Dim fundNameIndex = reader.GetOrdinal("FullFundName")
                    Dim currencyIndex = reader.GetOrdinal("Currency")
                    Dim priceIndex = reader.GetOrdinal("Price")
                    Dim dateIndex = reader.GetOrdinal("UpdateDate")
                    Dim statusIndex = reader.GetOrdinal("MappingStatus")

                    While reader.Read()

                        Dim record As New FundPriceMatch With {
                            .ISIN = reader.GetString(isinIndex),
                            .FundName = If(
                                reader.IsDBNull(fundNameIndex),
                                "UNKNOWN FUND",
                                reader.GetString(fundNameIndex)
                            ),
                            .Currency = reader.GetString(currencyIndex),
                            .Price = reader.GetDecimal(priceIndex),
                            .UpdateDate = reader.GetDateTime(dateIndex),
                            .MappingStatus = reader.GetString(statusIndex)
                        }

                        results.Add(record)

                    End While

                End Using

            End Using

        End Using

        Return results

    End Function


    ' ==========================================
    ' EXERCISE 6: IMPORT FUND PRICES
    ' ==========================================

    Public Function ImportFundPrices(
        prices As List(Of FundPrice)
    ) As ImportSummary

        Dim summary As New ImportSummary()

        ' CHECK 1: Validate input collection

        If prices Is Nothing OrElse prices.Count = 0 Then
            Return summary
        End If

        ' CHECK 2: Validate all records BEFORE
        ' opening a database transaction.

        Dim incomingKeys As New HashSet(Of String)(
            StringComparer.OrdinalIgnoreCase)

        For Each price As FundPrice In prices

            If price Is Nothing Then
                Throw New ArgumentException(
                    "Import contains a null record.")
            End If

            If String.IsNullOrWhiteSpace(price.ISIN) OrElse
               price.ISIN.Trim().Length > 12 Then

                Throw New ArgumentException(
                    "Invalid ISIN supplied.")
            End If

            If String.IsNullOrWhiteSpace(price.QuoteCurrency) OrElse
               price.QuoteCurrency.Trim().Length <> 3 Then

                Throw New ArgumentException(
                    "Invalid currency supplied.")
            End If

            If price.NAVPerUnit <= 0D Then

                Throw New ArgumentException(
                    "NAV must be greater than zero.")
            End If

            ' Build unique business key
            Dim recordKey As String =
                price.ISIN.Trim().ToUpperInvariant() & "|" &
                price.QuoteCurrency.Trim().ToUpperInvariant() & "|" &
                price.ValuationDate.ToString("yyyy-MM-dd")

            If Not incomingKeys.Add(recordKey) Then

                Throw New ArgumentException(
                    "Duplicate record found in incoming batch: " &
                    recordKey)

            End If

        Next


        ' CHECK 3: Open database connection

        Using connection As New SqlConnection(connectionString)

            connection.Open()

            ' CHECK 4: Start transaction
            Using transaction As SqlTransaction =
                connection.BeginTransaction(IsolationLevel.Serializable)

                Try

                    For Each price As FundPrice In prices

                        Dim isin As String =
                            price.ISIN.Trim().ToUpperInvariant()

                        Dim currency As String =
                            price.QuoteCurrency.Trim().ToUpperInvariant()

                        ' CHECK 5: Determine whether
                        ' the business key already exists.

                        Dim existsQuery As String =
                            "SELECT 1 " &
                            "FROM dbo.DailyPricesUpdate WITH (UPDLOCK, HOLDLOCK) " &
                            "WHERE ISIN = @ISIN " &
                            "AND Currency = @Currency " &
                            "AND UpdateDate = @UpdateDate;"

                        Dim recordExists As Boolean

                        Using command As New SqlCommand(
                            existsQuery, connection, transaction)

                            command.Parameters.Add(
                                "@ISIN", SqlDbType.VarChar, 12).Value = isin

                            command.Parameters.Add(
                                "@Currency", SqlDbType.VarChar, 3).Value = currency

                            command.Parameters.Add(
                                "@UpdateDate", SqlDbType.Date).Value =
                                price.ValuationDate.Date

                            recordExists =
                                command.ExecuteScalar() IsNot Nothing

                        End Using


                        If recordExists Then

                            ' CHECK 6: Update existing record

                            Dim updateQuery As String =
                                "UPDATE dbo.DailyPricesUpdate " &
                                "SET Price = @Price " &
                                "WHERE ISIN = @ISIN " &
                                "AND Currency = @Currency " &
                                "AND UpdateDate = @UpdateDate;"

                            Using command As New SqlCommand(
                                updateQuery, connection, transaction)

                                AddPriceParameters(command, price)

                                command.ExecuteNonQuery()

                            End Using

                            summary.UpdatedRecords += 1

                        Else

                            ' CHECK 7: Insert new record

                            Dim insertQuery As String =
                                "INSERT INTO dbo.DailyPricesUpdate " &
                                "(ISIN, Currency, Price, UpdateDate) " &
                                "VALUES " &
                                "(@ISIN, @Currency, @Price, @UpdateDate);"

                            Using command As New SqlCommand(
                                insertQuery, connection, transaction)

                                AddPriceParameters(command, price)

                                command.ExecuteNonQuery()

                            End Using

                            summary.InsertedRecords += 1

                        End If

                        summary.TotalProcessed += 1

                    Next


                    ' CHECK 8: Commit only after
                    ' every database operation succeeds.

                    transaction.Commit()

                Catch ex As Exception

                    ' CHECK 9: Undo all changes if
                    ' any database operation fails.

                    Try
                        transaction.Rollback()

                    Catch rollbackException As Exception

                        ' Preserve the original database exception.
                        ' Rollback failure should not hide the original error.

                    End Try

                    ' Record the failed import attempt.
                    summary.FailedRecords = 1

                    ' No records were successfully committed.
                    summary.InsertedRecords = 0
                    summary.UpdatedRecords = 0

                    Throw New Exception(
                        "Fund price import failed. All database changes were rolled back.",
                        ex
                    )

                End Try

            End Using

        End Using

        Return summary

    End Function


    ' ==========================================
    ' SHARED SQL PARAMETER METHOD
    ' ==========================================

    Private Sub AddPriceParameters(
        command As SqlCommand,
        price As FundPrice)

        command.Parameters.Add(
            "@ISIN", SqlDbType.VarChar, 12).Value =
            price.ISIN.Trim().ToUpperInvariant()

        command.Parameters.Add(
            "@Currency", SqlDbType.VarChar, 3).Value =
            price.QuoteCurrency.Trim().ToUpperInvariant()

        Dim priceParameter As SqlParameter =
            command.Parameters.Add("@Price", SqlDbType.Decimal)

        priceParameter.Precision = 18
        priceParameter.Scale = 6
        priceParameter.Value = price.NAVPerUnit

        command.Parameters.Add(
            "@UpdateDate", SqlDbType.Date).Value =
            price.ValuationDate.Date

    End Sub

End Class
