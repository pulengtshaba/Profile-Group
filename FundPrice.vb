Option Strict On
Option Explicit On


' Represents one valid fund price record.
Public Class FundPrice

    Public Property ISIN As String
    Public Property ValuationDate As DateTime
    Public Property NAVPerUnit As Decimal
    Public Property QuoteCurrency As String
    Public Property BaseCurrency As String
    Public Property BaseNAV As Decimal

End Class