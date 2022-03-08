namespace GPSaveConverter
{
    partial class Form1
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
            this.packagesListBox = new System.Windows.Forms.ListBox();
            this.insertButton = new System.Windows.Forms.Button();
            this.selectAllButton = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.packagesLabel = new System.Windows.Forms.Label();
            this.profileLabel = new System.Windows.Forms.Label();
            this.steamFilesLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.xboxFilesTable = new System.Windows.Forms.DataGridView();
            this.File = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastEdit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.steamFilesTable = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.xboxFilesTable)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.steamFilesTable)).BeginInit();
            this.SuspendLayout();
            // 
            // packagesListBox
            // 
            this.packagesListBox.FormattingEnabled = true;
            this.packagesListBox.Location = new System.Drawing.Point(12, 28);
            this.packagesListBox.Name = "packagesListBox";
            this.packagesListBox.Size = new System.Drawing.Size(221, 82);
            this.packagesListBox.TabIndex = 0;
            this.packagesListBox.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // insertButton
            // 
            this.insertButton.Location = new System.Drawing.Point(12, 186);
            this.insertButton.Name = "insertButton";
            this.insertButton.Size = new System.Drawing.Size(75, 23);
            this.insertButton.TabIndex = 4;
            this.insertButton.Text = "Insert Files";
            this.insertButton.UseVisualStyleBackColor = true;
            this.insertButton.Click += new System.EventHandler(this.insertButton_Click);
            // 
            // selectAllButton
            // 
            this.selectAllButton.Location = new System.Drawing.Point(156, 187);
            this.selectAllButton.Name = "selectAllButton";
            this.selectAllButton.Size = new System.Drawing.Size(75, 23);
            this.selectAllButton.TabIndex = 6;
            this.selectAllButton.Text = "Select All";
            this.selectAllButton.UseVisualStyleBackColor = true;
            this.selectAllButton.Click += new System.EventHandler(this.selectAllButton_Click);
            // 
            // listBox1
            // 
            this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(247, 28);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(219, 82);
            this.listBox1.TabIndex = 7;
            // 
            // packagesLabel
            // 
            this.packagesLabel.AutoSize = true;
            this.packagesLabel.Location = new System.Drawing.Point(12, 12);
            this.packagesLabel.Name = "packagesLabel";
            this.packagesLabel.Size = new System.Drawing.Size(129, 13);
            this.packagesLabel.TabIndex = 8;
            this.packagesLabel.Text = "Potential Xbox Packages:";
            // 
            // profileLabel
            // 
            this.profileLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.profileLabel.AutoSize = true;
            this.profileLabel.Location = new System.Drawing.Point(244, 12);
            this.profileLabel.Name = "profileLabel";
            this.profileLabel.Size = new System.Drawing.Size(72, 13);
            this.profileLabel.TabIndex = 9;
            this.profileLabel.Text = "Steam Profile:";
            // 
            // steamFilesLabel
            // 
            this.steamFilesLabel.AutoSize = true;
            this.steamFilesLabel.Location = new System.Drawing.Point(236, 223);
            this.steamFilesLabel.Name = "steamFilesLabel";
            this.steamFilesLabel.Size = new System.Drawing.Size(92, 13);
            this.steamFilesLabel.TabIndex = 10;
            this.steamFilesLabel.Text = "Steam Save Files:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 223);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "Xbox Save Files:";
            // 
            // xboxFilesTable
            // 
            this.xboxFilesTable.AllowUserToAddRows = false;
            this.xboxFilesTable.AllowUserToDeleteRows = false;
            this.xboxFilesTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.xboxFilesTable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.File,
            this.LastEdit});
            this.xboxFilesTable.Location = new System.Drawing.Point(12, 239);
            this.xboxFilesTable.Name = "xboxFilesTable";
            this.xboxFilesTable.ReadOnly = true;
            this.xboxFilesTable.Size = new System.Drawing.Size(195, 199);
            this.xboxFilesTable.TabIndex = 13;
            // 
            // File
            // 
            this.File.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.File.HeaderText = "File";
            this.File.Name = "File";
            this.File.ReadOnly = true;
            // 
            // LastEdit
            // 
            this.LastEdit.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.LastEdit.HeaderText = "Last Modified";
            this.LastEdit.Name = "LastEdit";
            this.LastEdit.ReadOnly = true;
            this.LastEdit.Width = 95;
            // 
            // steamFilesTable
            // 
            this.steamFilesTable.AllowUserToAddRows = false;
            this.steamFilesTable.AllowUserToDeleteRows = false;
            this.steamFilesTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.steamFilesTable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2});
            this.steamFilesTable.Location = new System.Drawing.Point(239, 239);
            this.steamFilesTable.Name = "steamFilesTable";
            this.steamFilesTable.ReadOnly = true;
            this.steamFilesTable.Size = new System.Drawing.Size(227, 199);
            this.steamFilesTable.TabIndex = 14;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridViewTextBoxColumn1.HeaderText = "File";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewTextBoxColumn2.HeaderText = "Last Modified";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 95;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(478, 450);
            this.Controls.Add(this.steamFilesTable);
            this.Controls.Add(this.xboxFilesTable);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.steamFilesLabel);
            this.Controls.Add(this.profileLabel);
            this.Controls.Add(this.packagesLabel);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.selectAllButton);
            this.Controls.Add(this.insertButton);
            this.Controls.Add(this.packagesListBox);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.xboxFilesTable)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.steamFilesTable)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox packagesListBox;
        private System.Windows.Forms.Button insertButton;
        private System.Windows.Forms.Button selectAllButton;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Label packagesLabel;
        private System.Windows.Forms.Label profileLabel;
        private System.Windows.Forms.Label steamFilesLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridView xboxFilesTable;
        private System.Windows.Forms.DataGridViewTextBoxColumn File;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastEdit;
        private System.Windows.Forms.DataGridView steamFilesTable;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
    }
}

