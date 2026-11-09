
Imports System
Imports System.IO
Imports System.Globalization
Imports Microsoft.VisualBasic.FileIO

Module Program

    Sub Main()

        Dim filePath As String =
            "C:\Users\wwwpu\OneDrive\Desktop\Visual Studio 2022\Projects\FundPriceProcessor\FundPriceProcessor\ProfileData\20261021.csv"

        If Not File.Exists(filePath) Then
            Console.WriteLine("File not found!")
            Return
        End If

        Dim validRecords As Integer = 0
        Dim rejectedRecords As Integer = 0

        Using parser As New TextFieldParser(filePath)

            parser.TextFieldType = FieldType.Delimited
            parser.SetDelimiters(",")
            parser.HasFieldsEnclosedInQuotes = True
            parser.TrimWhiteSpace = False

            ' Read CSV headers
            Dim headers() As String = parser.ReadFields()

            ' Build a header-to-column lookup
            Dim columns As New Dictionary(Of String, Integer)(
                StringComparer.OrdinalIgnoreCase)

            For i As Integer = 0 To headers.Length - 1
                columns(headers(i).Trim()) = i
            Next

            Console.WriteLine("CSV file loaded.")
            Console.WriteLine("Columns: " & headers.Length)
            Console.WriteLine("--------------------------------")

            ' Check required columns
            Dim requiredColumns() As String = {
                "ISIN",
                "Valuation_Date",
                "NAV_per_unit",
                "QUOTCurrency"
            }

            For Each column As String In requiredColumns
                If Not columns.ContainsKey(column) Then
                    Console.WriteLine(
                        "Missing required column: " & column)
                    Return
                End If
            Next

            Dim rowNumber As Integer = 1

            While Not parser.EndOfData

                rowNumber += 1

                Try

                    Dim fields() As String = parser.ReadFields()

                    ' Check column count
                    If fields.Length <> headers.Length Then

                        rejectedRecords += 1

                        Console.WriteLine(
                            $"Row {rowNumber}: Invalid field count ({fields.Length})")

                        Continue While
                    End If

                    ' Extract values using column names
                    Dim isin As String =
                        fields(columns("ISIN")).Trim()

                    Dim dateText As String =
                        fields(columns("Valuation_Date")).Trim()

                    Dim navText As String =
                        fields(columns("NAV_per_unit")).Trim()

                    Dim currency As String =
                        fields(columns("QUOTCurrency")).Trim()

                    Dim nav As Decimal

                    ' Validate NAV
                    If String.IsNullOrWhiteSpace(isin) OrElse
                       String.IsNullOrWhiteSpace(currency) OrElse
                       Not Decimal.TryParse(
                           navText,
                           NumberStyles.Number,
                           CultureInfo.InvariantCulture,
                           nav) OrElse nav <= 0D Then

                        rejectedRecords += 1
                        Console.WriteLine(
                            $"Row {rowNumber}: Invalid required data")

                        Continue While
                    End If

                    validRecords += 1

                    ' Display first five records
                    If validRecords <= 5 Then

                        Console.WriteLine()
                        Console.WriteLine("ISIN: " & isin)
                        Console.WriteLine("Date: " & dateText)
                        Console.WriteLine("NAV: " & nav)
                        Console.WriteLine("Currency: " & currency)

                    End If

                Catch ex As MalformedLineException

                    rejectedRecords += 1

                    Console.WriteLine(
                        $"Row {rowNumber}: Malformed CSV record")

                End Try

            End While

        End Using

        Console.WriteLine()
        Console.WriteLine("========== IMPORT SUMMARY ==========")
        Console.WriteLine("Valid records: " & validRecords)
        Console.WriteLine("Rejected records: " & rejectedRecords)

        Console.WriteLine()
        Console.WriteLine("Press any key to exit.")
        Console.ReadKey()

    End Sub

End Module
