using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using ClosedXML.Excel;
using OfficeOpenXml;



namespace CSVCONVERTER
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            progressBar1.Visible = false;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
        }

       
        private void button1_Click(object sender, EventArgs e)
        {
            string destinationpath = "C:\\Users\\user\\Documents\\Book1.xlsx";
            
            int i = 0;
            string filename = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files (*.csv)|*.csv";
            openFileDialog.ShowDialog();
            filename = openFileDialog.FileName;
            if (filename != string.Empty)
            {
                progressBar1.Visible = true;
                progressBar1.Minimum = 0;

                MessageBox.Show(filename);

                using (var package = new ExcelPackage(new FileInfo(destinationpath)))
                {
                    var worksheet = package.Workbook.Worksheets.Add("a");

                    string[] csvLines = File.ReadAllLines(filename);
                    
                   
                    
                    for (int row = 1; row <= csvLines.Length; row++)
                    {
                       
                        string[] values = csvLines[row - 1].Split(',');
                        for (int col = 1; col <= values.Length; col++)
                        {
                            worksheet.Cells[row, col].Value = values[col - 1];
                        }
                        i+=1;
                        progressBar1.Step = i;
                        progressBar1.Maximum = row;


                    }

                    package.Save();
                }

                Console.WriteLine("CSV file converted to XLSX successfully.");
            }


            else { MessageBox.Show("Dosya Seçilmedi", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning); }


        }


    }
}








    