using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;

namespace EXCELTODATABASEConverter
{
    public partial class Form2 : Form
    {


        private string connectionString;
        private List<string> TableList;
        private Label lastClickedLabel = null;

        public Form2(string connectionString, List<string> TableList)
        {
            InitializeComponent();
            this.connectionString = connectionString;
            this.TableList = TableList;
            LoadTableData();
        }

        
        private void LoadTableData()
        {

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                try
                {
                    int labelYPosition = 0;

                    foreach (string tableName in TableList)
                    {
                       
                        Label dblabel = new Label();
                        dblabel.Text = tableName;
                        dblabel.AutoSize = true;
                        dblabel.Margin = new Padding(5);
                        dblabel.Cursor = Cursors.Hand;
                        dblabel.Click += Label_Click;
                        TableNamesTBLPanel.Controls.Add(dblabel, 0, labelYPosition);
                        TextBox txtb = new TextBox();

                        labelYPosition++;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }
        private void Label_Click(object sender, EventArgs e)
        {
            Label clickedLabel = (Label)sender;

            if (lastClickedLabel != null && lastClickedLabel != clickedLabel)
            {
                lastClickedLabel.ForeColor = SystemColors.ControlText;
                lastClickedLabel.Font = new Font(lastClickedLabel.Font, FontStyle.Regular);
            }

            clickedLabel.ForeColor = Color.Red;
            clickedLabel.Font = new Font(clickedLabel.Font, FontStyle.Bold);

            lastClickedLabel = clickedLabel;
            LoadData(clickedLabel.Text);

        }
        private void LoadData(string tableName)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {

                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter($"SELECT * FROM [{tableName}]", connection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    DataShowGrid.DataSource = dataTable;
                }
            }
        }


    }
}

