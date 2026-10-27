
namespace EXCELTODATABASEConverter
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.DataShowGrid = new System.Windows.Forms.DataGridView();
            this.TableNamesTBLPanel = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.DataShowGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // DataShowGrid
            // 
            this.DataShowGrid.AllowUserToDeleteRows = false;
            this.DataShowGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataShowGrid.Location = new System.Drawing.Point(257, 12);
            this.DataShowGrid.Name = "DataShowGrid";
            this.DataShowGrid.RowHeadersWidth = 51;
            this.DataShowGrid.RowTemplate.Height = 24;
            this.DataShowGrid.Size = new System.Drawing.Size(713, 371);
            this.DataShowGrid.TabIndex = 0;
            // 
            // TableNamesTBLPanel
            // 
            this.TableNamesTBLPanel.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.InsetDouble;
            this.TableNamesTBLPanel.ColumnCount = 1;
            this.TableNamesTBLPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TableNamesTBLPanel.Location = new System.Drawing.Point(0, 12);
            this.TableNamesTBLPanel.Name = "TableNamesTBLPanel";
            this.TableNamesTBLPanel.RowCount = 1;
            this.TableNamesTBLPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.TableNamesTBLPanel.Size = new System.Drawing.Size(236, 371);
            this.TableNamesTBLPanel.TabIndex = 1;
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(982, 392);
            this.Controls.Add(this.TableNamesTBLPanel);
            this.Controls.Add(this.DataShowGrid);
            this.Name = "Form2";
            this.Text = "Form2";
            ((System.ComponentModel.ISupportInitialize)(this.DataShowGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView DataShowGrid;
        private System.Windows.Forms.TableLayoutPanel TableNamesTBLPanel;
    }
}