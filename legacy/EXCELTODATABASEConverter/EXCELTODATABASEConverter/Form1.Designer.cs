
namespace EXCELTODATABASEConverter
{
    partial class Form1
    {
        /// <summary>
        ///Gerekli tasarımcı değişkeni.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///Kullanılan tüm kaynakları temizleyin.
        /// </summary>
        ///<param name="disposing">yönetilen kaynaklar dispose edilmeliyse doğru; aksi halde yanlış.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer üretilen kod

        /// <summary>
        /// Tasarımcı desteği için gerekli metot - bu metodun 
        ///içeriğini kod düzenleyici ile değiştirmeyin.
        /// </summary>
        private void InitializeComponent()
        {
            this.ButtonXlSX = new System.Windows.Forms.Button();
            this.LABEL_EXCELPATH = new System.Windows.Forms.Label();
            this.LABELDB = new System.Windows.Forms.Label();
            this.TEXTBOX_XLSX = new System.Windows.Forms.TextBox();
            this.TEXTBOX_DB = new System.Windows.Forms.TextBox();
            this.BUTTON_DB = new System.Windows.Forms.Button();
            this.BUTTON_IMPORT = new System.Windows.Forms.Button();
            this.BUTTON_SHOW = new System.Windows.Forms.Button();
            this.loadPanel = new System.Windows.Forms.Panel();
            this.pleasewaitLABEL = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.TEXTBOXKEYWORD = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.BUTONKEYWORD = new System.Windows.Forms.Button();
            this.loadPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // ButtonXlSX
            // 
            this.ButtonXlSX.Location = new System.Drawing.Point(237, 40);
            this.ButtonXlSX.Name = "ButtonXlSX";
            this.ButtonXlSX.Size = new System.Drawing.Size(75, 23);
            this.ButtonXlSX.TabIndex = 0;
            this.ButtonXlSX.Text = "CLICK";
            this.ButtonXlSX.UseVisualStyleBackColor = true;
            this.ButtonXlSX.Click += new System.EventHandler(this.ButtonXlSX_Click);
            // 
            // LABEL_EXCELPATH
            // 
            this.LABEL_EXCELPATH.AutoSize = true;
            this.LABEL_EXCELPATH.Location = new System.Drawing.Point(75, 12);
            this.LABEL_EXCELPATH.Name = "LABEL_EXCELPATH";
            this.LABEL_EXCELPATH.Size = new System.Drawing.Size(104, 17);
            this.LABEL_EXCELPATH.TabIndex = 1;
            this.LABEL_EXCELPATH.Text = "Choose xlsx file";
            this.LABEL_EXCELPATH.Click += new System.EventHandler(this.LABEL_EXCELPATH_Click);
            // 
            // LABELDB
            // 
            this.LABELDB.AutoSize = true;
            this.LABELDB.Location = new System.Drawing.Point(74, 138);
            this.LABELDB.Name = "LABELDB";
            this.LABELDB.Size = new System.Drawing.Size(105, 17);
            this.LABELDB.TabIndex = 2;
            this.LABELDB.Text = "Choose DB File";
            // 
            // TEXTBOX_XLSX
            // 
            this.TEXTBOX_XLSX.Location = new System.Drawing.Point(11, 41);
            this.TEXTBOX_XLSX.Name = "TEXTBOX_XLSX";
            this.TEXTBOX_XLSX.Size = new System.Drawing.Size(213, 22);
            this.TEXTBOX_XLSX.TabIndex = 3;
            this.TEXTBOX_XLSX.TextChanged += new System.EventHandler(this.TEXTBOX_XLSX_TextChanged);
            // 
            // TEXTBOX_DB
            // 
            this.TEXTBOX_DB.Location = new System.Drawing.Point(11, 158);
            this.TEXTBOX_DB.Name = "TEXTBOX_DB";
            this.TEXTBOX_DB.Size = new System.Drawing.Size(214, 22);
            this.TEXTBOX_DB.TabIndex = 4;
            // 
            // BUTTON_DB
            // 
            this.BUTTON_DB.Location = new System.Drawing.Point(238, 157);
            this.BUTTON_DB.Name = "BUTTON_DB";
            this.BUTTON_DB.Size = new System.Drawing.Size(75, 23);
            this.BUTTON_DB.TabIndex = 5;
            this.BUTTON_DB.Text = "CLICK";
            this.BUTTON_DB.UseVisualStyleBackColor = true;
            this.BUTTON_DB.Click += new System.EventHandler(this.BUTTON_DB_Click);
            // 
            // BUTTON_IMPORT
            // 
            this.BUTTON_IMPORT.Location = new System.Drawing.Point(43, 186);
            this.BUTTON_IMPORT.Name = "BUTTON_IMPORT";
            this.BUTTON_IMPORT.Size = new System.Drawing.Size(242, 23);
            this.BUTTON_IMPORT.TabIndex = 6;
            this.BUTTON_IMPORT.Text = "IMPORT";
            this.BUTTON_IMPORT.UseVisualStyleBackColor = true;
            this.BUTTON_IMPORT.Click += new System.EventHandler(this.BUTTON_IMPORT_Click);
            // 
            // BUTTON_SHOW
            // 
            this.BUTTON_SHOW.Location = new System.Drawing.Point(124, 215);
            this.BUTTON_SHOW.Name = "BUTTON_SHOW";
            this.BUTTON_SHOW.Size = new System.Drawing.Size(75, 23);
            this.BUTTON_SHOW.TabIndex = 7;
            this.BUTTON_SHOW.Text = "SHOW DB";
            this.BUTTON_SHOW.UseVisualStyleBackColor = true;
            this.BUTTON_SHOW.Click += new System.EventHandler(this.BUTTON_SHOW_Click);
            // 
            // loadPanel
            // 
            this.loadPanel.Controls.Add(this.pleasewaitLABEL);
            this.loadPanel.Controls.Add(this.progressBar1);
            this.loadPanel.Location = new System.Drawing.Point(11, 32);
            this.loadPanel.Name = "loadPanel";
            this.loadPanel.Size = new System.Drawing.Size(339, 358);
            this.loadPanel.TabIndex = 8;
            // 
            // pleasewaitLABEL
            // 
            this.pleasewaitLABEL.AutoSize = true;
            this.pleasewaitLABEL.Location = new System.Drawing.Point(8, 85);
            this.pleasewaitLABEL.Name = "pleasewaitLABEL";
            this.pleasewaitLABEL.Size = new System.Drawing.Size(315, 17);
            this.pleasewaitLABEL.TabIndex = 1;
            this.pleasewaitLABEL.Text = "Please wait while convertion process continues...";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 111);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(304, 23);
            this.progressBar1.TabIndex = 0;
            // 
            // TEXTBOXKEYWORD
            // 
            this.TEXTBOXKEYWORD.Location = new System.Drawing.Point(12, 103);
            this.TEXTBOXKEYWORD.Name = "TEXTBOXKEYWORD";
            this.TEXTBOXKEYWORD.Size = new System.Drawing.Size(213, 22);
            this.TEXTBOXKEYWORD.TabIndex = 11;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(38, 83);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(186, 17);
            this.label1.TabIndex = 10;
            this.label1.Text = "Choose KeyWord for Tables";
            // 
            // BUTONKEYWORD
            // 
            this.BUTONKEYWORD.Location = new System.Drawing.Point(238, 102);
            this.BUTONKEYWORD.Name = "BUTONKEYWORD";
            this.BUTONKEYWORD.Size = new System.Drawing.Size(75, 23);
            this.BUTONKEYWORD.TabIndex = 9;
            this.BUTONKEYWORD.Text = "CLICK";
            this.BUTONKEYWORD.UseVisualStyleBackColor = true;
            this.BUTONKEYWORD.Click += new System.EventHandler(this.BUTONKEYWORD_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(328, 249);
            this.Controls.Add(this.loadPanel);
            this.Controls.Add(this.TEXTBOXKEYWORD);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.BUTONKEYWORD);
            this.Controls.Add(this.BUTTON_SHOW);
            this.Controls.Add(this.TEXTBOX_XLSX);
            this.Controls.Add(this.LABELDB);
            this.Controls.Add(this.LABEL_EXCELPATH);
            this.Controls.Add(this.ButtonXlSX);
            this.Controls.Add(this.TEXTBOX_DB);
            this.Controls.Add(this.BUTTON_IMPORT);
            this.Controls.Add(this.BUTTON_DB);
            this.Name = "Form1";
            this.Text = "Form1";
            this.loadPanel.ResumeLayout(false);
            this.loadPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonXlSX;
        private System.Windows.Forms.Label LABEL_EXCELPATH;
        private System.Windows.Forms.Label LABELDB;
        private System.Windows.Forms.TextBox TEXTBOX_XLSX;
        private System.Windows.Forms.TextBox TEXTBOX_DB;
        private System.Windows.Forms.Button BUTTON_DB;
        private System.Windows.Forms.Button BUTTON_IMPORT;
        private System.Windows.Forms.Button BUTTON_SHOW;
        private System.Windows.Forms.Panel loadPanel;
        private System.Windows.Forms.Label pleasewaitLABEL;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TextBox TEXTBOXKEYWORD;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button BUTONKEYWORD;
    }
}

