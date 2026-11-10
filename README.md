# Fund Price Processor

A VB.NET console application for processing, validating, analysing, and importing daily investment fund prices from CSV files into Microsoft SQL Server.

This project simulates a financial data ingestion workflow similar to those used by financial data providers to process daily fund Net Asset Values (NAVs).

## Project Overview

Financial data providers receive fund pricing information from external sources in different formats. Before this information can be used by downstream systems, it must be validated, processed, reconciled, and stored reliably.

The Fund Price Processor demonstrates how to build a structured data-processing workflow that handles these responsibilities.

## Technology Stack

| Technology | Purpose |
|---|---|
| VB.NET | Application development |
| .NET 10 | Runtime and framework |
| Microsoft SQL Server | Relational database |
| ADO.NET | Database connectivity and data access |
| Microsoft.Data.SqlClient | SQL Server integration |
| CSV | External financial data input |
| Visual Studio | Development environment |
| Git & GitHub | Version control |

## Features

### 1. CSV Data Ingestion
- Reads multiple CSV files from a designated directory.
- Processes fund pricing records.
- Identifies columns using CSV headers.
- Supports structured CSV parsing.

### 2. Data Validation
- Validates ISIN formats.
- Validates valuation dates.
- Validates NAV values.
- Checks currency information.
- Identifies malformed records.
- Detects duplicate fund price records.
- Separates valid records from rejected records.

### 3. Fund Price Processing
- Extracts fund NAV information.
- Processes multiple valuation dates.
- Calculates percentage changes in fund prices.
- Converts fund values into ZAR using exchange-rate inputs.

### 4. Database Integration
- Connects to Microsoft SQL Server using ADO.NET.
- Retrieves fund information.
- Uses SQL joins to reconcile fund prices against registered funds.
- Identifies matched and unmatched fund records.

### 5. Transactional Database Import
- Validates records before database operations.
- Checks whether a fund price already exists.
- Inserts new fund price records.
- Updates existing fund price records.
- Uses parameterized SQL commands.
- Uses database transactions to maintain consistency.
- Rolls back changes when an import operation fails.

### 6. Import Reporting
Generates an import summary containing:

- Total records processed.
- Records inserted.
- Records updated.
- Records rejected or failed.

## Application Workflow

```text
External CSV Files
        |
        v
CSV File Processing
        |
        v
Data Extraction
        |
        v
Data Validation
        |
        +-------> Rejected Records
        |
        v
Valid Fund Price Records
        |
        v
Fund Price Processing
        |
        v
SQL Server Reconciliation
        |
        v
Database Import
        |
        v
Transactional Commit
        |
        v
Import Summary
```

## Database Structure

The practice database is named:

`ProfileFundPractice`

The main tables used in the exercises are:

### Funds

Stores registered fund information.

| Column | Description |
|---|---|
| ISIN | Fund identification number |
| FundName | Fund name |

### DailyPricesUpdate

Stores daily fund price information.

| Column | Description |
|---|---|
| ISIN | Fund identification number |
| Currency | Price currency |
| Price | Fund NAV |
| UpdateDate | Valuation date |

The intended business key for daily prices is:

`ISIN + Currency + UpdateDate`

## Project Structure

```text
FundPriceProcessor/
│
├── Program.vb
├── FundPrice.vb
├── FundRepository.vb
├── ImportSummary.vb
│
├── ProfileData/
│   ├── 20261021.csv
│   ├── 20261022.csv
│   └── ...
│
└── README.md
```

## Getting Started

### Prerequisites

- Windows
- Visual Studio 2022 or later
- .NET 10 SDK
- Microsoft SQL Server
- SQL Server Management Studio (SSMS)

### 1. Clone the Repository

```bash
https://github.com/pulengtshaba/Profile-Group.git
```

### 2. Open the Project

Open the solution in Visual Studio.

### 3. Configure the Database

Create a SQL Server database named:

```sql
CREATE DATABASE ProfileFundPractice;
```

Create the required tables and configure the connection string in `FundRepository.vb`.

Example connection string:

```vb
"Server=localhost;Database=ProfileFundPractice;Integrated Security=True;TrustServerCertificate=True;"
```

### 4. Configure CSV Input

Place the input CSV files inside the application's `ProfileData` directory.

Ensure the configured directory path in `Program.vb` points to the correct location.

### 5. Restore Dependencies and Run

```bash
dotnet restore
dotnet build
dotnet run
```

## Engineering Concepts Demonstrated

This project provides practical experience with:

- File processing and data ingestion.
- Data validation and error handling.
- Object-oriented programming.
- Repository pattern and separation of concerns.
- ADO.NET database operations.
- SQL joins and data reconciliation.
- Parameterized SQL queries.
- Database transactions.
- Insert-versus-update logic.
- Data integrity and duplicate prevention.
- Financial data processing.

## Important Notes

This is an independent learning and assessment-preparation project. It does not represent Profile's production system or actual internal database architecture.

Any sample exchange rates or financial data used for testing are illustrative and should not be treated as verified market data or investment performance.

The transactional import functionality is intended for the local practice database. Live-system testing should use a non-persistent, non-distributing execution mode.

## Future Improvements

- Implement automated unit and integration tests.
- Introduce structured application logging.
- Add configurable validation rules.
- Implement batch processing for large datasets.
- Add asynchronous database operations.
- Introduce dependency injection.
- Add automated build and test pipelines using GitHub Actions.
- Implement a dedicated dry-run mode for safe data inspection.

## Author

Puleng Tshaba

Independent software development project focused on backend development, financial data processing, database integration, and enterprise application engineering.