
Imports Microsoft.Data.SqlClient

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

                    While reader.Read()

                        Dim record As New FundPriceMatch With {
                            .ISIN = reader.GetString(0),
                            .FundName = If(reader.IsDBNull(1), "UNKNOWN FUND", reader.GetString(1)),
                            .Currency = reader.GetString(2),
                            .Price = reader.GetDecimal(3),
                            .UpdateDate = reader.GetDateTime(4),
                            .MappingStatus = reader.GetString(5)
                        }

                        results.Add(record)

                    End While

                End Using

            End Using

        End Using

        Return results

    End Function

End Class
