
Option Strict On
Option Explicit On

Imports System.IO
Imports System.Globalization
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.VisualBasic.FileIO
Imports System.Text.RegularExpressions

Module Program

    Sub Main()

        Console.WriteLine("FUND PERFORMANCE PROCESSOR")
        Console.WriteLine("===========================")
        Console.WriteLine()

        ' --------------------------------------------------
        ' CONFIGURATION
        ' --------------------------------------------------

        Dim folderPath As String = "C:\Users\wwwpu\OneDrive\Desktop\Visual Studio 2022\Projects\FundPriceProcessor\FundPriceProcessor\ProfileData"

        If Not Directory.Exists(folderPath) Then

            Console.WriteLine("ERROR: Folder does not exist:")
            Console.WriteLine(folderPath)

            Return

        End If

        Dim files As String() =
            Directory.GetFiles(folderPath, "*.csv").
            OrderBy(Function(f) f).
            ToArray()

        If files.Length = 0 Then

            Console.WriteLine("No CSV files found.")
            Return

        End If

        ' --------------------------------------------------
        ' CSV COLUMN DEFINITIONS
        ' --------------------------------------------------

        Dim requiredColumns As String() = {
            "ISIN",
            "Valuation_Date",
            "NAV_per_unit",
            "QUOTCurrency",
            "BASECurrency",
            "Unrounded_Base_NAV"
        }

        ' --------------------------------------------------
        ' DATA COLLECTION
        ' --------------------------------------------------

        Dim records As New List(Of FundPrice)

        Dim validRecords As Integer = 0
        Dim rejectedRecords As Integer = 0
        Dim duplicateRecords As Integer = 0
        Dim filesProcessed As Integer = 0

        Dim seenRecords As New HashSet(Of String)(
            StringComparer.OrdinalIgnoreCase
        )

        ' --------------------------------------------------
        ' PROCESS ALL CSV FILES
        ' --------------------------------------------------

        For Each filePath As String In files

            Console.WriteLine("Reading: " &
                              Path.GetFileName(filePath))

            Try

                Using parser As New TextFieldParser(filePath)

                    parser.TextFieldType = FieldType.Delimited
                    parser.SetDelimiters(",")
                    parser.HasFieldsEnclosedInQuotes = True
                    parser.TrimWhiteSpace = False

                    If parser.EndOfData Then
                        Continue For
                    End If

                    ' Read header row.
                    Dim headers As String() = parser.ReadFields()

                    If headers Is Nothing Then
                        Continue For
                    End If

                    ' Build header-to-column lookup.
                    Dim columnIndexes As New Dictionary(Of String, Integer)(
                        StringComparer.OrdinalIgnoreCase
                    )

                    For i As Integer = 0 To headers.Length - 1

                        Dim headerName As String =
                            headers(i).Trim().TrimStart(ChrW(&HFEFF))

                        If Not columnIndexes.ContainsKey(headerName) Then
                            columnIndexes.Add(headerName, i)
                        End If

                    Next

                    ' Verify required columns.
                    Dim missingColumns As New List(Of String)

                    For Each columnName As String In requiredColumns

                        If Not columnIndexes.ContainsKey(columnName) Then
                            missingColumns.Add(columnName)
                        End If

                    Next

                    If missingColumns.Count > 0 Then

                        Console.WriteLine(
                            "File rejected. Missing columns: " &
                            String.Join(", ", missingColumns)
                        )

                        Continue For

                    End If

                    filesProcessed += 1

                    ' Read data rows.
                    Dim rowNumber As Integer = 1

                    While Not parser.EndOfData

                        rowNumber += 1

                        Dim fields As String() = Nothing

                        Try

                            fields = parser.ReadFields()

                        Catch ex As MalformedLineException

                            rejectedRecords += 1

                            Console.WriteLine(
                                "Rejected row " & rowNumber &
                                ": Malformed CSV line."
                            )

                            Continue While

                        End Try

                        If fields Is Nothing Then
                            Continue While
                        End If

                        ' Validate field count.
                        If fields.Length <> headers.Length Then

                            rejectedRecords += 1

                            Console.WriteLine(
                                "Rejected row " & rowNumber &
                                ": Invalid field count. Expected " &
                                headers.Length & ", received " &
                                fields.Length
                            )

                            Continue While

                        End If

                        ' Extract fields using header names.
                        Dim isin As String =
                            fields(columnIndexes("ISIN")).Trim()

                        Dim dateText As String =
                            fields(columnIndexes("Valuation_Date")).Trim()

                        Dim navText As String =
                            fields(columnIndexes("NAV_per_unit")).Trim()

                        Dim quoteCurrency As String =
                            fields(columnIndexes("QUOTCurrency")).Trim().ToUpperInvariant()

                        Dim baseCurrency As String =
                            fields(columnIndexes("BASECurrency")).Trim().ToUpperInvariant()

                        Dim baseNAVText As String =
                            fields(columnIndexes("Unrounded_Base_NAV")).Trim()

                        ' Validate ISIN.
                        If Not Regex.IsMatch(
                            isin,
                            "^[A-Z]{2}[A-Z0-9]{10}$"
                        ) Then

                            rejectedRecords += 1

                            Console.WriteLine(
                                "Rejected row " & rowNumber &
                                ": Invalid ISIN."
                            )

                            Continue While

                        End If

                        ' Validate valuation date.
                        Dim valuationDate As DateTime

                        Dim acceptedDateFormats As String() = {
                            "dd/MM/yyyy",
                            "M/d/yyyy",
                            "yyyy-MM-dd"
                        }

                        If Not DateTime.TryParseExact(
                            dateText,
                            acceptedDateFormats,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            valuationDate
                        ) Then

                            rejectedRecords += 1

                            Console.WriteLine(
                                "Rejected row " & rowNumber &
                                ": Invalid valuation date."
                            )

                            Continue While

                        End If

                        ' Validate NAV per unit.
                        Dim navPerUnit As Decimal

                        If Not Decimal.TryParse(
                            navText,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            navPerUnit
                        ) OrElse navPerUnit <= 0D Then

                            rejectedRecords += 1

                            Console.WriteLine(
                                "Rejected row " & rowNumber &
                                ": Invalid NAV per unit."
                            )

                            Continue While

                        End If

                        ' Validate base NAV.
                        Dim baseNAV As Decimal

                        If Not Decimal.TryParse(
                            baseNAVText,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            baseNAV
                        ) OrElse baseNAV <= 0D Then

                            rejectedRecords += 1

                            Console.WriteLine(
                                "Rejected row " & rowNumber &
                                ": Invalid base NAV."
                            )

                            Continue While

                        End If

                        ' Validate currencies.
                        Dim allowedCurrencies As String() = {
                            "USD", "EUR", "GBP", "CAD",
                            "SEK", "NOK", "CHF", "ZAR"
                        }

                        If Not allowedCurrencies.Contains(quoteCurrency) OrElse
                           Not allowedCurrencies.Contains(baseCurrency) Then

                            rejectedRecords += 1

                            Console.WriteLine(
                                "Rejected row " & rowNumber &
                                ": Unsupported currency."
                            )

                            Continue While

                        End If

                        ' Prevent duplicate ISIN/date combinations.
                        Dim duplicateKey As String =
                            isin & "|" &
                            quoteCurrency & "|" &
                            valuationDate.ToString("yyyyMMdd")

                        If Not seenRecords.Add(duplicateKey) Then

                            duplicateRecords += 1

                            Console.WriteLine(
                                "Duplicate record: " & duplicateKey
                            )

                            Continue While

                        End If

                        ' Create the record.
                        Dim fundRecord As New FundPrice With {
                            .ISIN = isin,
                            .ValuationDate = valuationDate,
                            .NAVPerUnit = navPerUnit,
                            .QuoteCurrency = quoteCurrency,
                            .BaseCurrency = baseCurrency,
                            .BaseNAV = baseNAV
                        }

                        records.Add(fundRecord)
                        validRecords += 1

                    End While

                End Using

            Catch ex As Exception

                Console.WriteLine(
                    "File processing error: " & ex.Message
                )

            End Try

            Console.WriteLine()

        Next

        ' --------------------------------------------------
        ' PROCESSING SUMMARY
        ' --------------------------------------------------

        Console.WriteLine("========== PROCESSING SUMMARY ==========")

        Console.WriteLine("Files processed: " & filesProcessed)
        Console.WriteLine("Valid records: " & validRecords)
        Console.WriteLine("Rejected records: " & rejectedRecords)
        Console.WriteLine("Duplicate records: " & duplicateRecords)

        Console.WriteLine()

        If records.Count = 0 Then

            Console.WriteLine("No valid records available.")
            Return

        End If

        ' --------------------------------------------------
        ' COLLECT HISTORICAL USD/ZAR EXCHANGE RATES
        ' --------------------------------------------------

        Dim fxRates As New Dictionary(Of DateTime, Decimal)

        Dim usdDates As List(Of DateTime) =
            records.
            Where(Function(r) r.BaseCurrency = "USD").
            Select(Function(r) r.ValuationDate.Date).
            Distinct().
            OrderBy(Function(d) d).
            ToList()

        If usdDates.Count > 0 Then

            Console.WriteLine("========== USD/ZAR RATES ==========")

            Console.WriteLine(
                "Enter verified historical rates."
            )

            Console.WriteLine(
                "Format: ZAR per 1 USD"
            )

            Console.WriteLine()

            For Each valuationDate As DateTime In usdDates

                Dim exchangeRate As Decimal = 0D
                Dim validRate As Boolean = False

                While Not validRate

                    Console.Write(
                        valuationDate.ToString("dd/MM/yyyy") &
                        " USD/ZAR: "
                    )

                    Dim rateInput As String =
                        Console.ReadLine()

                    If Decimal.TryParse(
                        rateInput,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        exchangeRate
                    ) AndAlso exchangeRate > 0D Then

                        validRate = True

                    Else

                        Console.WriteLine(
                            "Invalid rate. Enter a positive number, " &
                            "for example 18.25."
                        )

                    End If

                End While

                fxRates(valuationDate.Date) = exchangeRate

            Next

        End If

        Console.WriteLine()

        ' --------------------------------------------------
        ' CALCULATE FUND PERFORMANCE IN ZAR
        ' --------------------------------------------------

        Console.WriteLine("========== ZAR FUND PERFORMANCE ==========")

        Dim fundGroups = records.
            GroupBy(Function(r) r.ISIN).
            OrderBy(Function(g) g.Key)

        For Each fundGroup In fundGroups

            Dim orderedRecords As List(Of FundPrice) =
                fundGroup.
                OrderBy(Function(r) r.ValuationDate).
                ToList()

            Dim firstPrice As FundPrice = orderedRecords.First()
            Dim lastPrice As FundPrice = orderedRecords.Last()

            ' This exercise assumes a fund keeps the same
            ' base currency throughout the reporting period.
            Dim baseCurrency As String = firstPrice.BaseCurrency

            If orderedRecords.Any(
                Function(r) r.BaseCurrency <> baseCurrency
            ) Then

                Console.WriteLine(
                    "Skipped " & fundGroup.Key &
                    ": Inconsistent base currencies."
                )

                Continue For

            End If

            Dim startNAVZAR As Decimal
            Dim endNAVZAR As Decimal

            If baseCurrency = "USD" Then

                Dim startFX As Decimal
                Dim endFX As Decimal

                If Not fxRates.TryGetValue(
                    firstPrice.ValuationDate.Date,
                    startFX
                ) OrElse
                   Not fxRates.TryGetValue(
                    lastPrice.ValuationDate.Date,
                    endFX
                ) Then

                    Console.WriteLine(
                        "Missing FX rate for " & fundGroup.Key
                    )

                    Continue For

                End If

                startNAVZAR = firstPrice.BaseNAV * startFX
                endNAVZAR = lastPrice.BaseNAV * endFX

            ElseIf baseCurrency = "ZAR" Then

                startNAVZAR = firstPrice.BaseNAV
                endNAVZAR = lastPrice.BaseNAV

            Else

                Console.WriteLine(
                    "Skipped " & fundGroup.Key &
                    ": Base currency " & baseCurrency &
                    " has no conversion configured."
                )

                Continue For

            End If

            If startNAVZAR <= 0D Then
                Continue For
            End If

            ' Calculate percentage return.
            Dim performance As Decimal =
                ((endNAVZAR - startNAVZAR) / startNAVZAR) * 100D

            Console.WriteLine()

            Console.WriteLine("ISIN: " & fundGroup.Key)

            Console.WriteLine(
                "Base currency: " & baseCurrency
            )

            Console.WriteLine(
                "Observations: " & orderedRecords.Count
            )

            Console.WriteLine(
                "Start date: " &
                firstPrice.ValuationDate.ToString("dd-MM-yyyy")
            )

            Console.WriteLine(
                "End date: " &
                lastPrice.ValuationDate.ToString("dd-MM-yyyy")
            )

            Console.WriteLine(
                "Start NAV (ZAR): " &
                startNAVZAR.ToString("N4")
            )

            Console.WriteLine(
                "End NAV (ZAR): " &
                endNAVZAR.ToString("N4")
            )

            Console.WriteLine(
                "ZAR Performance: " &
                performance.ToString("N2") & "%"
            )

        Next

        ' --------------------------------------------------
        ' EXERCISE 6: DATABASE FUND PRICE IMPORT
        ' --------------------------------------------------

        Console.WriteLine()
        Console.WriteLine("========== DATABASE FUND PRICE IMPORT ==========")

        Try

            Dim repository As New FundRepository()

            Console.WriteLine("Starting database import...")

            Dim importSummary As ImportSummary =
        repository.ImportFundPrices(records)

            Console.WriteLine()
            Console.WriteLine("========== IMPORT SUMMARY ==========")

            Console.WriteLine(
        "Total processed: " & importSummary.TotalProcessed
    )

            Console.WriteLine(
        "Inserted: " & importSummary.InsertedRecords
    )

            Console.WriteLine(
        "Updated: " & importSummary.UpdatedRecords
    )

            Console.WriteLine(
        "Failed: " & importSummary.FailedRecords
    )

            Console.WriteLine("Database transaction completed.")

        Catch ex As Exception

            Console.WriteLine()
            Console.WriteLine("DATABASE IMPORT FAILED")
            Console.WriteLine(ex.Message)

        End Try


        ' --------------------------------------------------
        ' DATABASE FUND PRICE MATCHING
        ' --------------------------------------------------

        Console.WriteLine()
        Console.WriteLine("========== DATABASE FUND PRICE MATCHING ==========")

        Try

            Dim repository As New FundRepository()

            Dim databaseRecords As List(Of FundPriceMatch) =
        repository.GetFundPriceMatches()

            For Each record As FundPriceMatch In databaseRecords

                Console.WriteLine(
            $"{record.ISIN} | " &
            $"{record.FundName} | " &
            $"{record.Currency} | " &
            $"{record.Price:N4} | " &
            $"{record.UpdateDate:dd-MM-yyyy} | " &
            $"{record.MappingStatus}"
        )

            Next

            Dim matchedCount As Integer = System.Linq.Enumerable.Count(databaseRecords, Function(x) x.MappingStatus = "MATCHED")

            Dim unmatchedCount As Integer = System.Linq.Enumerable.Count(databaseRecords, Function(x) x.MappingStatus = "UNMATCHED")

            Console.WriteLine()
            Console.WriteLine("Database records: " & databaseRecords.Count)
            Console.WriteLine("Matched: " & matchedCount)
            Console.WriteLine("Unmatched: " & unmatchedCount)

        Catch ex As Exception

            Console.WriteLine("Database processing error:")
            Console.WriteLine(ex.Message)

        End Try

        Console.WriteLine()
        Console.WriteLine("Processing complete.")

        Console.WriteLine()
        Console.WriteLine("Press ENTER to exit.")

        Console.ReadLine()

    End Sub

End Module