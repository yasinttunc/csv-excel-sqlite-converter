# CSV, Excel and SQLite Converter

[![CI](https://github.com/yasinttunc/csv-excel-sqlite-converter/actions/workflows/ci.yml/badge.svg)](https://github.com/yasinttunc/csv-excel-sqlite-converter/actions/workflows/ci.yml)

Convert spreadsheets between **CSV**, **Excel (.xlsx)** and **SQLite**: a Windows desktop app and a cross-platform command-line tool (`dataconv`) that share one tested core library.

**C# · .NET 8 · WPF · ClosedXML · Microsoft.Data.Sqlite · xUnit · GitHub Actions**

It merges two small tools I wrote in 2023, a CSV-to-Excel converter and an Excel-to-SQLite importer, into one app. Their original code is kept in [`legacy/`](legacy) and in the first commit.

![Convert and import](docs/screenshots/convert.png)

## What it does

| Task | Desktop app | Command line |
|---|---|---|
| Preview a CSV or Excel file | Open or drop the file | `dataconv preview grades.csv` |
| CSV → Excel, Excel → CSV | *Save as Excel* / *Save as CSV* | `dataconv convert grades.csv grades.xlsx` |
| Import into SQLite | *Import into database* | `dataconv import grades.xlsx school.db` |
| One table per value of a column | *One table per value of* | `dataconv import grades.csv school.db --split-by Section` |
| List and read tables | *Browse database* tab | `dataconv tables school.db` |
| Export a table | *Export CSV* / *Export Excel* | `dataconv export school.db TKM405-41 tkm405.xlsx` |

### Details that matter with real files

- **CSV the way spreadsheets write it.** Quoted fields, doubled quotes, delimiters and line breaks inside quotes (RFC 4180). The delimiter is detected (`,` `;` tab `|`), because Turkish and many European versions of Excel save "CSV" with semicolons.
- **Encodings.** UTF-8 with or without BOM, UTF-16, and a fallback to Windows-1254 for files saved by Turkish Windows, so `Öğrenci No` and `Şube` come through intact. CSV output is UTF-8 with BOM so Excel opens it correctly.
- **Nothing silently changed.** `42` and `87.5` become number cells and INTEGER/REAL columns, but `007` and `0555 123 45 67` stay text, so leading zeros survive. Empty cells become NULL.
- **Safe SQL.** Every value is a parameter and every table and column name is quoted, so any header text works as a column name. An import runs in one transaction: it either completes or changes nothing. Existing tables are left alone unless you choose *Replace* or *Append*.
- **Clear errors.** Merged cells, missing sheets, old `.xls` files, unknown columns and table-name clashes are reported with what to do next.

## Screenshots

<table>
  <tr>
    <td><img src="docs/screenshots/import-options.png" alt="Import options with split by column"></td>
    <td><img src="docs/screenshots/browse.png" alt="Browsing the tables of a SQLite database"></td>
  </tr>
</table>

Screenshots are taken automatically by the UI smoke test on a GitHub-hosted Windows runner.

## Command line

```
dataconv convert <input.csv|.xlsx> <output.csv|.xlsx> [--sheet NAME] [--delimiter D] [--out-delimiter D]
dataconv import  <input.csv|.xlsx> <database.db> [--table NAME] [--split-by COLUMN]
                 [--if-exists fail|replace|append] [--sheet NAME] [--delimiter D] [--text-only]
dataconv tables  <database.db>
dataconv export  <database.db> <table> <output.csv|.xlsx>
dataconv sheets  <workbook.xlsx>
dataconv preview <input.csv|.xlsx> [--rows N]
```

Exit codes: `0` success, `1` the data could not be converted (the message says why), `2` wrong usage.

```
$ dataconv import samples/grades-tr.csv school.db --split-by Şube
Imported 48 rows into 4 table(s) in school.db:
  TKM405-41                          12 rows
  TKM405-42                          12 rows
  TKM435-41                          12 rows
  TKM439-41                          12 rows
Columns: Öğrenci No INTEGER, Ad Soyad TEXT, Şube TEXT, Vize INTEGER, Final INTEGER, Ortalama TEXT, Telefon TEXT, Not TEXT
```

`Ortalama` stays TEXT because the Turkish file writes decimals with a comma (`55,6`); values are stored exactly as written rather than guessed.

## Running it

Requirements: the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). The command-line tool runs on Windows, macOS and Linux; the desktop app needs Windows.

```
git clone https://github.com/yasinttunc/csv-excel-sqlite-converter.git
cd csv-excel-sqlite-converter

dotnet run --project src/DataConverter.Cli -- preview samples/grades-tr.csv
dotnet run --project src/DataConverter.Desktop          # Windows only
```

Each CI run also publishes ready-to-run builds as artifacts: the Windows app and `dataconv` for Windows, macOS (Apple silicon and Intel) and Linux, as single self-contained files that need no .NET installed.

## Project structure

```
src/
  DataConverter.Core/      CSV, Excel and SQLite logic (net8.0), no UI code
  DataConverter.Cli/       dataconv command-line tool (net8.0)
  DataConverter.Desktop/   WPF app (net8.0-windows, MaterialDesignThemes)
tests/
  DataConverter.Core.Tests/  xUnit tests for the core and the CLI
samples/                   fictional student grades: grades.csv (UTF-8, commas), grades-tr.csv (Windows-1254, semicolons)
legacy/                    the two original 2023 tools
tools/                     CI scripts (UI smoke test, content checks)
```

## Tests and CI

```
dotnet test
```

GitHub Actions runs on every push and pull request:

- **Linux:** core and CLI tests, the CLI run end to end on the sample files, content checks, and self-contained CLI builds for macOS and Linux.
- **Windows:** the full solution build and tests, then a UI Automation smoke test that opens the app with the Turkish sample CSV and with a database made by the CLI, checks the preview, the import options and the database browser, and saves screenshots. Self-contained Windows builds are uploaded as artifacts.

## What changed from the 2023 tools

| | 2023 | Now |
|---|---|---|
| Apps | Two separate Windows Forms apps | One core library used by a desktop app and a CLI |
| CSV parsing | `line.Split(',')`: broke on quoted commas and semicolon files | RFC 4180 parser with delimiter and encoding detection |
| Output | Hard-coded path to one workbook in the user's Documents folder | Save dialog / any path you give |
| SQL | Values and names pasted into SQL text (`'{value}'`) | Parameters and quoted names in one transaction |
| Split by column | Table per value, rows matched by text search across the whole row | Table per value of the chosen column only |
| Types | Every column TEXT | INTEGER, REAL or TEXT, with leading zeros kept |
| Platforms | Windows only | Desktop app on Windows; CLI on Windows, macOS and Linux |
| Tests | None | xUnit tests and a UI smoke test in CI |

## License

[MIT](LICENSE). Uses [ClosedXML](https://github.com/ClosedXML/ClosedXML), [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore) and [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit), all MIT-licensed.
