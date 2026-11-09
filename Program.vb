
Imports System
Imports System.IO
Imports System.Globalization
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.VisualBasic.FileIO

' Represents one fund price record
Public Class FundPrice

    Public Property ISIN As String
    Public Property ValuationDate As DateTime
    Public Property NAV As Decimal
    Public Property Currency As String

End Class

Module Program

    Sub Main()

        Dim folderPath As String = "C:\Users\wwwpu\OneDrive\Desktop\Visual Studio 2022\Projects\FundPriceProcessor\FundPriceProcessor\ProfileData"

        Dim files() As String =
            Directory.GetFiles(folderPath, "202610*.csv").
            OrderBy(Function(f) Path.GetFileName(f)).
            ToArray()

        Dim records As New List(Of FundPrice)

        Dim seenRecords As New HashSet(Of String)(
            StringComparer.OrdinalIgnoreCase)

        Dim rejectedRecords As Integer = 0

        Dim dateFormats As String() = {
            "dd/MM/yyyy",
            "M/d/yyyy",
            "MM/dd/yyyy"
        }

        Console.WriteLine("FUND PERFORMANCE PROCESSOR")
        Console.WriteLine("===========================")

        ' Process each CSV file
        For Each filePath As String In files

            Console.WriteLine()
            Console.WriteLine("Reading: " &
                              Path.GetFileName(filePath))

            Using parser As New TextFieldParser(filePath)

                parser.TextFieldType = FieldType.Delimited
                parser.SetDelimiters(",")
                parser.HasFieldsEnclosedInQuotes = True
                parser.TrimWhiteSpace = False

                Dim headers() As String = parser.ReadFields()

                Dim columns As New Dictionary(Of String, Integer)(
                    StringComparer.OrdinalIgnoreCase)

                For i As Integer = 0 To headers.Length - 1
                    columns(headers(i).Trim()) = i
                Next

                Dim rowNumber As Integer = 1

                While Not parser.EndOfData

                    rowNumber += 1

                    Try

                        Dim fields() As String = parser.ReadFields()

                        ' Check field count
                        If fields.Length <> headers.Length Then

                            rejectedRecords += 1

                            Console.WriteLine(
                                $"Rejected row {rowNumber}: Invalid field count")

                            Continue While

                        End If

                        ' Extract required fields
                        Dim isin As String =
                            fields(columns("ISIN")).Trim()

                        Dim dateText As String =
                            fields(columns("Valuation_Date")).Trim()

                        Dim navText As String =
                            fields(columns("NAV_per_unit")).Trim()

                        Dim currency As String =
                            fields(columns("QUOTCurrency")).Trim()

                        ' Validate ISIN and currency presence
                        If String.IsNullOrWhiteSpace(isin) OrElse
                           String.IsNullOrWhiteSpace(currency) Then

                            rejectedRecords += 1
                            Continue While

                        End If

                        ' Parse valuation date
                        Dim valuationDate As DateTime

                        If Not DateTime.TryParseExact(
                            dateText,
                            dateFormats,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            valuationDate) Then

                            rejectedRecords += 1
                            Continue While

                        End If

                        ' Parse NAV
                        Dim nav As Decimal

                        If Not Decimal.TryParse(
                            navText,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            nav) OrElse nav <= 0D Then

                            rejectedRecords += 1
                            Continue While

                        End If

                        ' Prevent duplicate records
                        Dim recordKey As String =
                            isin & "|" &
                            valuationDate.ToString("yyyy-MM-dd") & "|" &
                            currency

                        If Not seenRecords.Add(recordKey) Then

                            rejectedRecords += 1
                            Console.WriteLine(
                                $"Duplicate record: {recordKey}")

                            Continue While

                        End If

                        ' Store validated record
                        records.Add(New FundPrice With {
                            .ISIN = isin,
                            .ValuationDate = valuationDate,
                            .NAV = nav,
                            .Currency = currency
                        })

                    Catch ex As MalformedLineException

                        rejectedRecords += 1

                        Console.WriteLine(
                            $"Malformed record at row {rowNumber}")

                    End Try

                End While

            End Using

        Next

        Console.WriteLine()
        Console.WriteLine("Files processed: " & files.Length)
        Console.WriteLine("Valid records: " & records.Count)
        Console.WriteLine("Rejected records: " & rejectedRecords)

        ' Calculate performance
        Console.WriteLine()
        Console.WriteLine("========== FUND PERFORMANCE ==========")

        Dim fundGroups = records.
            GroupBy(Function(r) r.ISIN & "|" & r.Currency).
            OrderBy(Function(g) g.Key)

        For Each fundGroup In fundGroups

            Dim prices = fundGroup.
                OrderBy(Function(r) r.ValuationDate).
                ToList()

            Dim firstPrice As FundPrice = prices.First()
            Dim lastPrice As FundPrice = prices.Last()

            Dim percentageReturn As Decimal =
                ((lastPrice.NAV - firstPrice.NAV) /
                firstPrice.NAV) * 100D

            Console.WriteLine()
            Console.WriteLine("ISIN: " & firstPrice.ISIN)
            Console.WriteLine("Currency: " & firstPrice.Currency)

            Console.WriteLine(
                "Starting NAV: " &
                firstPrice.NAV.ToString("N4"))

            Console.WriteLine(
                "Ending NAV: " &
                lastPrice.NAV.ToString("N4"))

            Console.WriteLine(
                "Start date: " &
                firstPrice.ValuationDate.ToString("dd/MM/yyyy"))

            Console.WriteLine(
                "End date: " &
                lastPrice.ValuationDate.ToString("dd/MM/yyyy"))

            Console.WriteLine(
                "Observations: " & prices.Count)

            Console.WriteLine(
                "NAV Return: " &
                percentageReturn.ToString("N2") & "%")

        Next

        Console.WriteLine()
        Console.WriteLine("Performance calculation completed.")

        Console.ReadKey()

    End Sub

End Module