# CSV, Excel and SQLite Converter

> **Status:** this commit holds my two original converter tools from 2023, as written, for reference. A merged, rewritten version is being built on top of it.

- `legacy/CSVCONVERTER`: a Windows Forms app that converted a CSV file to an Excel workbook.
- `legacy/EXCELTODATABASEConverter`: a Windows Forms app that imported an Excel sheet into SQLite, creating one table per value of a chosen column, and showed the tables in a grid.

Both target .NET Framework 4.7.2. NuGet packages are not committed, so this version is not expected to build as-is.
