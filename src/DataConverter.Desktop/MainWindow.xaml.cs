using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DataConverter.Core;
using DataConverter.Core.Csv;
using DataConverter.Core.Excel;
using DataConverter.Core.Sqlite;
using Microsoft.Win32;

namespace DataConverter.Desktop;

/// <summary>
/// Two tabs: open a CSV or Excel file, preview it, save it in the other format or import it into SQLite;
/// and browse the tables of a SQLite database and export them. All conversion logic lives in DataConverter.Core.
/// </summary>
public partial class MainWindow : Window
{
    private const int PreviewRows = 500;
    private const string NoSplit = "(one table)";

    private static readonly (string Label, char? Value)[] Delimiters =
    [
        ("Auto-detect", null), ("Comma ,", ','), ("Semicolon ;", ';'), ("Tab", '\t'), ("Pipe |", '|'),
    ];

    private static readonly (string Label, IfTableExists Value)[] IfExistsChoices =
    [
        ("Stop", IfTableExists.Fail), ("Replace it", IfTableExists.Replace), ("Add the rows", IfTableExists.Append),
    ];

    private string? _sourcePath;
    private TabularData? _source;
    private string? _databasePath;
    private bool _loadingSource;

    public MainWindow()
    {
        InitializeComponent();
        DelimiterPicker.ItemsSource = Delimiters.Select(d => d.Label).ToList();
        DelimiterPicker.SelectedIndex = 0;
        OutputDelimiterPicker.ItemsSource = Delimiters.Skip(1).Select(d => d.Label).ToList();
        OutputDelimiterPicker.SelectedIndex = 0;
        IfExistsPicker.ItemsSource = IfExistsChoices.Select(c => c.Label).ToList();
        IfExistsPicker.SelectedIndex = 0;
        SplitPicker.ItemsSource = new[] { NoSplit };
        SplitPicker.SelectedIndex = 0;
        SheetPicker.Visibility = Visibility.Collapsed;
        DelimiterPicker.Visibility = Visibility.Collapsed;
    }

    /// <summary>Opens a data file in the first tab or a database in the second, depending on its extension.</summary>
    public void OpenPath(string path)
    {
        try
        {
            if (DataFiles.KindOf(path) == DataFileKind.Sqlite)
            {
                Tabs.SelectedItem = BrowseTab;
                LoadDatabase(path);
            }
            else
            {
                Tabs.SelectedItem = ConvertTab;
                LoadSource(path, resetOptions: true);
            }
        }
        catch (DataFormatException ex)
        {
            ShowError(ex.Message);
        }
    }

    // ----------------------------------------------------------------- Convert & import

    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CSV or Excel files (*.csv;*.tsv;*.txt;*.xlsx;*.xlsm)|*.csv;*.tsv;*.txt;*.xlsx;*.xlsm" };
        if (dialog.ShowDialog(this) == true)
        {
            OpenPath(dialog.FileName);
        }
    }

    private void SourceOption_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_loadingSource && _sourcePath is not null && ReferenceEquals(e.OriginalSource, sender))
        {
            LoadSource(_sourcePath, resetOptions: false);
        }
    }

    private void LoadSource(string path, bool resetOptions)
    {
        _loadingSource = true;
        try
        {
            var kind = DataFiles.KindOf(path);
            if (resetOptions)
            {
                DelimiterPicker.SelectedIndex = 0;
                SheetPicker.ItemsSource = kind == DataFileKind.Excel ? ExcelFiles.SheetNames(path) : null;
                SheetPicker.SelectedIndex = kind == DataFileKind.Excel ? 0 : -1;
                TableNameInput.Text = Path.GetFileNameWithoutExtension(path);
            }

            SheetPicker.Visibility = kind == DataFileKind.Excel ? Visibility.Visible : Visibility.Collapsed;
            DelimiterPicker.Visibility = kind == DataFileKind.Csv ? Visibility.Visible : Visibility.Collapsed;

            var delimiter = Delimiters[Math.Max(0, DelimiterPicker.SelectedIndex)].Value;
            var data = DataFiles.Load(path, SheetPicker.SelectedItem as string, delimiter);

            _sourcePath = path;
            _source = data;
            SourceText.Text = path;
            ShowTable(PreviewGrid, data.Take(PreviewRows));

            var detail = kind == DataFileKind.Csv
                ? $"delimiter {DescribeDelimiter(delimiter ?? CsvFormat.DetectDelimiter(TextFiles.Read(path, out _)))}"
                : $"sheet \"{SheetPicker.SelectedItem}\"";
            SourceInfo.Text = $"{data.RowCount:N0} rows, {data.ColumnCount} columns ({detail})" +
                (data.RowCount > PreviewRows ? $". Showing the first {PreviewRows}." : ".");

            var previousSplit = SplitPicker.SelectedItem as string;
            SplitPicker.ItemsSource = new[] { NoSplit }.Concat(data.Headers).ToList();
            SplitPicker.SelectedItem = data.Headers.Contains(previousSplit) ? previousSplit : NoSplit;

            SaveExcelButton.IsEnabled = SaveCsvButton.IsEnabled = ImportButton.IsEnabled = data.ColumnCount > 0;
            ShowStatus($"Opened {Path.GetFileName(path)}.");
        }
        catch (Exception ex) when (ex is DataFormatException or IOException or UnauthorizedAccessException)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _loadingSource = false;
        }
    }

    private async void SaveExcelButton_Click(object sender, RoutedEventArgs e) => await SaveSourceAs(".xlsx", "Excel workbook (*.xlsx)|*.xlsx");

    private async void SaveCsvButton_Click(object sender, RoutedEventArgs e) => await SaveSourceAs(".csv", "CSV file (*.csv)|*.csv");

    private async Task SaveSourceAs(string extension, string filter)
    {
        if (_source is null || _sourcePath is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = filter,
            FileName = Path.GetFileNameWithoutExtension(_sourcePath) + extension,
            InitialDirectory = Path.GetDirectoryName(_sourcePath),
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var data = _source;
        var target = dialog.FileName;
        var delimiter = Delimiters[OutputDelimiterPicker.SelectedIndex + 1].Value ?? ',';
        await Run(() => DataFiles.Save(data, target, delimiter, Path.GetFileNameWithoutExtension(target)),
            $"Saved {data.RowCount:N0} rows to {target}.");
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_source is null || _sourcePath is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Choose or create a SQLite database",
            Filter = "SQLite database (*.db;*.sqlite;*.sqlite3)|*.db;*.sqlite;*.sqlite3",
            FileName = Path.GetFileNameWithoutExtension(_sourcePath) + ".db",
            InitialDirectory = Path.GetDirectoryName(_sourcePath),
            OverwritePrompt = false,
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var split = SplitPicker.SelectedItem as string;
        var options = new ImportOptions(
            TableNameInput.Text,
            split == NoSplit ? null : split,
            IfExistsChoices[Math.Max(0, IfExistsPicker.SelectedIndex)].Value,
            InferTypesInput.IsChecked == true);
        var data = _source;
        var database = dialog.FileName;

        ImportResult? result = null;
        await Run(() => result = SqliteImporter.Import(data, database, options), null);
        if (result is not null)
        {
            ShowStatus($"Imported {result.TotalRows:N0} rows into {result.Tables.Count} table(s) in {Path.GetFileName(database)}. " +
                "It is open in the Browse tab.", success: true);
            LoadDatabase(database);
        }
    }

    // ----------------------------------------------------------------- Browse database

    private void OpenDatabaseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "SQLite database (*.db;*.sqlite;*.sqlite3)|*.db;*.sqlite;*.sqlite3|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) == true)
        {
            Tabs.SelectedItem = BrowseTab;
            LoadDatabase(dialog.FileName);
        }
    }

    private void LoadDatabase(string path)
    {
        try
        {
            var tables = SqliteDatabase.ListTables(path);
            _databasePath = path;
            DatabaseText.Text = path;
            TableList.ItemsSource = tables;
            TableList.SelectedIndex = tables.Count > 0 ? 0 : -1;
            if (tables.Count == 0)
            {
                ShowTable(TableGrid, null);
                TableTitle.Text = "This database has no tables.";
                TableInfo.Text = string.Empty;
            }
        }
        catch (DataFormatException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void TableList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = TableList.SelectedItem as TableInfo;
        ExportCsvButton.IsEnabled = ExportExcelButton.IsEnabled = selected is not null;
        if (selected is null || _databasePath is null)
        {
            return;
        }

        try
        {
            var data = SqliteDatabase.ReadTable(_databasePath, selected.Name, PreviewRows);
            TableTitle.Text = selected.Name;
            ShowTable(TableGrid, data);
            TableInfo.Text = $"{selected.Rows:N0} rows, {data.ColumnCount} columns" +
                (selected.Rows > PreviewRows ? $". Showing the first {PreviewRows}." : ".");
        }
        catch (DataFormatException ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void ExportCsvButton_Click(object sender, RoutedEventArgs e) => await ExportTable(".csv", "CSV file (*.csv)|*.csv");

    private async void ExportExcelButton_Click(object sender, RoutedEventArgs e) => await ExportTable(".xlsx", "Excel workbook (*.xlsx)|*.xlsx");

    private async Task ExportTable(string extension, string filter)
    {
        if (TableList.SelectedItem is not TableInfo table || _databasePath is null)
        {
            return;
        }

        var dialog = new SaveFileDialog { Filter = filter, FileName = table.Name + extension };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var database = _databasePath;
        var target = dialog.FileName;
        await Run(() => DataFiles.Save(SqliteDatabase.ReadTable(database, table.Name), target, ',', table.Name),
            $"Exported {table.Rows:N0} rows from \"{table.Name}\" to {target}.");
    }

    // ----------------------------------------------------------------- Shared

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
        {
            OpenPath(files[0]);
        }
    }

    /// <summary>Runs slow work off the UI thread, with the busy bar showing and errors reported in the status line.</summary>
    private async Task Run(Action work, string? successMessage)
    {
        BusyBar.Visibility = Visibility.Visible;
        IsEnabled = false;
        try
        {
            await Task.Run(work);
            if (successMessage is not null)
            {
                ShowStatus(successMessage, success: true);
            }
        }
        catch (Exception ex) when (ex is DataFormatException or IOException or UnauthorizedAccessException)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsEnabled = true;
            BusyBar.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>Shows a table in a grid. Columns bind by position, so any header text (dots, brackets) is safe.</summary>
    private static void ShowTable(DataGrid grid, TabularData? data)
    {
        grid.Columns.Clear();
        grid.ItemsSource = null;
        if (data is null)
        {
            return;
        }

        for (var i = 0; i < data.ColumnCount; i++)
        {
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = data.Headers[i],
                Binding = new Binding($"[{i}]"),
                MaxWidth = 320,
            });
        }

        grid.ItemsSource = data.Rows;
    }

    private static string DescribeDelimiter(char delimiter) => delimiter switch
    {
        ',' => "comma",
        ';' => "semicolon",
        '\t' => "tab",
        '|' => "pipe",
        _ => $"'{delimiter}'",
    };

    private void ShowStatus(string message, bool success = false)
    {
        StatusText.Text = message;
        StatusText.SetResourceReference(TextBlock.ForegroundProperty, success ? "AppSuccessBrush" : "AppForegroundBrush");
    }

    private void ShowError(string message)
    {
        StatusText.Text = message;
        StatusText.SetResourceReference(TextBlock.ForegroundProperty, "AppErrorBrush");
    }
}
