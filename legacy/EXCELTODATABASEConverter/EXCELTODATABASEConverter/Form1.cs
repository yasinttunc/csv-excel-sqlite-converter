using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OfficeOpenXml;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows;



namespace EXCELTODATABASEConverter
{
    public partial class Form1 : Form
    {
        string connectionString = string.Empty;
        private List<string> TableList = new List<string>();
        string wantedColumnString = string.Empty;

        public Form1()
        {
            InitializeComponent();
            BUTTON_DB.Visible = false;
            LABELDB.Visible = false;
            TEXTBOX_DB.Visible = false;
            BUTTON_IMPORT.Visible = false;
            BUTTON_SHOW.Visible = false;
            loadPanel.Visible = false;
            

        }

        string xlsxfilename = string.Empty;
        string dbfilename = string.Empty;
        
        private void ButtonXlSX_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialogxlsx = new OpenFileDialog();
            fileDialogxlsx.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm";
            fileDialogxlsx.ShowDialog();

            xlsxfilename = fileDialogxlsx.FileName;
            
            if (xlsxfilename != string.Empty)
            {
                TEXTBOX_XLSX.Text = xlsxfilename;
                BUTTON_DB.Visible = true;
                LABELDB.Visible = true;
                TEXTBOX_DB.Visible = true;
            }
        }

        private void BUTONKEYWORD_Click(object sender, EventArgs e)
        {

            wantedColumnString = TEXTBOXKEYWORD.Text.ToString();
            TEXTBOXKEYWORD.ForeColor = Color.DarkRed;
    
        }



        private void BUTTON_DB_Click(object sender, EventArgs e)
        {
            OpenFileDialog DBfileDialog = new OpenFileDialog();
            DBfileDialog.Filter = "Database Files|*.db*";
            DBfileDialog.ShowDialog();
            dbfilename = DBfileDialog.FileName;

            if (dbfilename != string.Empty)
            {
                BUTTON_IMPORT.Visible = true;
            }
        }

        private void BUTTON_IMPORT_Click(object sender, EventArgs e)
        {


            using (var package = new ExcelPackage(new FileInfo(xlsxfilename)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var lastrow = worksheet.Dimension.End.Row;
                var lastcolumn = worksheet.Dimension.End.Column;
                var wantedColumnPlace = 0;
                var columnnames = new List<string>();
                var tablenames = new List<string>();
                var RowDatas = new List<Dictionary<string, string>>();

                foreach (var col in Enumerable.Range(1, lastcolumn))
                {

                    var cellplace = worksheet.Cells[col, 1];

                    if (cellplace.Merge)
                        {
                            MessageBox.Show("Birleştirilmiş Hücre Kullanılamaz, Lütfen Düzenleyiniz", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    if (cellplace.Value == null)
                    {
                        MessageBox.Show("İlk Sütun Boş Kullanılamaz, Lütfen Düzenleyiniz", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }


                }

                foreach (var row in Enumerable.Range(1, lastrow))
                {

                    for (int i = 1;i <= lastcolumn; i++) 
                    {
                        var cellplace = worksheet.Cells[i, row];

                        if (cellplace.Merge)
                        {
                            MessageBox.Show("Birleştirilmiş Hücre Kullanılamaz, Lütfen Düzenleyiniz", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        if(cellplace.Value == null)
                        {
                            MessageBox.Show("İlk Satır Boş Kullanılamaz, Lütfen Düzenleyiniz", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                    }
                    

                }




                loadPanel.Visible = true;
                int progress = 0;
                progressBar1.Minimum = 0;
                pleasewaitLABEL.Visible = true;

                for (int finder = 1, column = 1; worksheet.Cells[1, column].Value.ToString() != string.Empty; finder++, column++)
                {
                    if (worksheet.Cells[1, column].Value.ToString().Contains(wantedColumnString))
                    {
                        wantedColumnPlace = finder;
                        break;
                    }
                }
              

                for (int i = 2; worksheet.Cells[i, wantedColumnPlace].Value != null; i++)
                {
                    string currentCellValue = worksheet.Cells[i, wantedColumnPlace].Value.ToString();

                    if (!tablenames.Contains(currentCellValue))
                    {
                        tablenames.Add(currentCellValue);
                    }

                    else if (tablenames.Contains(currentCellValue))
                    {
                        continue;
                    }
                }

                connectionString = $"Data Source={dbfilename.Replace(@"\", "\\")}; Version=3;";


                foreach (var col in Enumerable.Range(1, lastcolumn))
                {
                    if (worksheet.Cells[1, col].Value != null)
                    {
                        columnnames.Add(worksheet.Cells[1, col].Value.ToString());
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (var i in Enumerable.Range(2, lastrow - 1))
                {
                    var rowData = new Dictionary<string, string>();

                    foreach (var col in Enumerable.Range(1, lastcolumn))
                    {
                        if (worksheet.Cells[1, col].Value != null && worksheet.Cells[i, col].Value != null)
                        {
                            var columnName = worksheet.Cells[1, col].Value.ToString();
                            var cellValue = worksheet.Cells[i, col].Value.ToString();

                            rowData[columnName] = cellValue;
                        }
                        else
                        {
                            break;
                        }
                    }

                    RowDatas.Add(rowData);
                }
                progressBar1.Maximum = (RowDatas.Count() + 1);


                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbfilename.Replace(@"\", "\\")}; Version=3;"))
                {
                    connection.Open();

                    foreach (var tableName in tablenames)
                    {
                        string createTableQuery = $"CREATE TABLE IF NOT EXISTS \"{tableName}\"  (rowid INTEGER PRIMARY KEY AUTOINCREMENT);";

                        using (SQLiteCommand CreateTableCommand = new SQLiteCommand(createTableQuery, connection))
                        {
                            CreateTableCommand.ExecuteNonQuery();
                        }
                        progress++;
                        progressBar1.Maximum = (RowDatas.Count() + tablenames.Count() + 1);
                        progressBar1.Value = progress;
                        
                    }
                   

                    foreach (var columnName in columnnames)
                    {
                        foreach (var tableName in tablenames)
                        {
                            int tablecount = 1;
                            if (tablecount <= 7)
                            {
                                string alterTableQuery = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" TEXT;";

                                using (SQLiteCommand alterTableCommand = new SQLiteCommand(alterTableQuery, connection))
                                {
                                    alterTableCommand.ExecuteNonQuery();
                                    tablecount++;
                                    progress++;
                                    progressBar1.Maximum = (RowDatas.Count() + tablenames.Count() + columnnames.Count() + 1);
                                    progressBar1.Value = progress;
                                }
                            }
                            else { break; }
                        }
                    }
                    foreach (var tableName in tablenames)
                    {
                        foreach (var rowData in RowDatas)
                        {
                            int counter = 100;
                            string tablenamececk = tableName;
                                var valuesList = new List<string>();
                                var insertvalue = rowData.Values.Select(value => $"'{value}'");
                                    valuesList.AddRange(insertvalue);
                                
                                var columnNamesList = rowData.Keys.Select(key => $"\"{key}\"");
                               
                                string insertQuery = $"INSERT INTO \"{tableName}\" ({string.Join(",", columnNamesList)}) VALUES ({string.Join(",", valuesList)});";
                                using (SQLiteCommand insertCommand = new SQLiteCommand(insertQuery, connection))
                                {
                                if (valuesList.Any(value => value.Contains(tablenamececk)))
                                {
                                    progress++;
                                    progressBar1.Maximum = (RowDatas.Count() + tablenames.Count() + columnnames.Count() + counter);
                                    counter++;
                                    progressBar1.Value = progress;
                                    insertCommand.ExecuteNonQuery();
                                }
                                else
                                {
                                    continue;
                                }

                            }
                            }

                        }
                    pleasewaitLABEL.Visible = true;
                    loadPanel.Visible = false;

                    }


                foreach (var tableName in tablenames)
                {
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        TableList.Add(tableName);
                    }
                    else { break; }
                    }
            }
            
            
            BUTTON_SHOW.Visible = true;
            MessageBox.Show("Başaralı");
        }
        private void BUTTON_SHOW_Click(object sender, EventArgs e)
        {

           

            Form2 Datagrid = new Form2(connectionString,TableList);
            Datagrid.ShowDialog(this);

        }

        private void LABEL_EXCELPATH_Click(object sender, EventArgs e)
        {

        }

        private void TEXTBOX_XLSX_TextChanged(object sender, EventArgs e)
        {

        }

        
    }
}
