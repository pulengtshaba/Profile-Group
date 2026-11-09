Imports System
Imports System.IO
Imports System.Globalization
Imports System.Collections.Generic
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic.FileIO

Module Program

    Sub Main()

        Dim filePath As String = "C:\Users\wwwpu\OneDrive\Desktop\Visual Studio 2022\Projects\FundPriceProcessor\FundPriceProcessor\ProfileData\20261028.csv"

        If Not File.Exists(filePath) Then
            Console.WriteLine("File not found!")
            Return
        End If

        Dim validRecords As Integer = 0
        Dim rejectedRecords As Integer = 0
        Dim duplicateRecords As Integer = 0

        ' Supported currencies
        Dim supportedCurrencies As New HashSet(Of String)(
            StringComparer.OrdinalIgnoreCase) From {
                "USD", "EUR", "GBP", "CAD",
                "SEK", "NOK", "CHF"
            }

        ' Store previously encountered business keys
        Dim processedRecords As New HashSet(Of String)(
            StringComparer.OrdinalIgnoreCase)

        ' Accepted date formats from the supplied files
        Dim dateFormats As String() = {
            "dd/MM/yyyy",
            "M/d/yyyy",
            "MM/dd/yyyy"
        }

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

            Console.WriteLine("File: " & Path.GetFileName(filePath))
            Console.WriteLine("Columns: " & headers.Length)
            Console.WriteLine("--------------------------------")

            Dim requiredColumns() As String = {
                "ISIN",
                "Valuation_Date",
                "NAV_per_unit",
                "QUOTCurrency"
            }

            For Each column As String In requiredColumns

                If Not columns.ContainsKey(column) Then
                    Console.WriteLine(
                        "Missing column: " & column)
                    Return
                End If

            Next

            Dim rowNumber As Integer = 1

            While Not parser.EndOfData

                rowNumber += 1

                Try

                    Dim fields() As String = parser.ReadFields()

                    ' CHECK 1: Correct number of columns
                    If fields.Length <> headers.Length Then

                        rejectedRecords += 1

                        Console.WriteLine(
                            $"Row {rowNumber}: Expected {headers.Length} fields, received {fields.Length}")

                        Continue While

                    End If

                    ' Extract fields
                    Dim isin As String =
                        fields(columns("ISIN")).Trim().ToUpperInvariant()

                    Dim dateText As String =
                        fields(columns("Valuation_Date")).Trim()

                    Dim navText As String =
                        fields(columns("NAV_per_unit")).Trim()

                    Dim currency As String =
                        fields(columns("QUOTCurrency")).Trim().ToUpperInvariant()

                    ' CHECK 2: ISIN structure
                    If Not Regex.IsMatch(
                        isin,
                        "^[A-Z]{2}[A-Z0-9]{9}[0-9]$") Then

                        rejectedRecords += 1
                        Console.WriteLine(
                            $"Row {rowNumber}: Invalid ISIN structure")

                        Continue While

                    End If

                    ' CHECK 3: Valuation date
                    Dim valuationDate As DateTime

                    If Not DateTime.TryParseExact(
                        dateText,
                        dateFormats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        valuationDate) Then

                        rejectedRecords += 1
                        Console.WriteLine(
                            $"Row {rowNumber}: Invalid valuation date")

                        Continue While

                    End If

                    ' CHECK 4: NAV
                    Dim nav As Decimal

                    If Not Decimal.TryParse(
                        navText,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        nav) OrElse nav <= 0D Then

                        rejectedRecords += 1
                        Console.WriteLine(
                            $"Row {rowNumber}: Invalid NAV")

                        Continue While

                    End If

                    ' CHECK 5: Currency
                    If Not supportedCurrencies.Contains(currency) Then

                        rejectedRecords += 1
                        Console.WriteLine(
                            $"Row {rowNumber}: Unsupported currency {currency}")

                        Continue While

                    End If

                    ' CHECK 6: Duplicate business key
                    Dim duplicateKey As String =
                        isin & "|" &
                        valuationDate.ToString("yyyy-MM-dd") & "|" &
                        currency

                    If processedRecords.Contains(duplicateKey) Then

                        duplicateRecords += 1

                        Console.WriteLine(
                            $"Row {rowNumber}: Duplicate record {duplicateKey}")

                        Continue While

                    End If

                    processedRecords.Add(duplicateKey)

                    validRecords += 1

                    Console.WriteLine(
                        $"VALID: {isin} | {valuationDate:yyyy-MM-dd} | {nav} {currency}")

                Catch ex As MalformedLineException

                    rejectedRecords += 1

                    Console.WriteLine(
                        $"Row {rowNumber}: Malformed CSV record")

                End Try

            End While

        End Using

        Console.WriteLine()
        Console.WriteLine("========== VALIDATION REPORT ==========")
        Console.WriteLine("Valid records: " & validRecords)
        Console.WriteLine("Rejected records: " & rejectedRecords)
        Console.WriteLine("Duplicate records: " & duplicateRecords)

        Console.WriteLine()
        Console.WriteLine("Validation completed.")

        Console.ReadKey()

    End Sub

End Module
