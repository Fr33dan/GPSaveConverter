namespace GPSaveConverter
{
    partial class SaveFileConverterForm
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
            this.components = new System.ComponentModel.Container();
            this.profileListBox = new System.Windows.Forms.ListBox();
            this.packagesLabel = new System.Windows.Forms.Label();
            this.profileLabel = new System.Windows.Forms.Label();
            this.nonXboxFilesLabel = new System.Windows.Forms.Label();
            this.xboxFileLabel = new System.Windows.Forms.Label();
            this.xboxFilesTable = new System.Windows.Forms.DataGridView();
            this.File = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastEdit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nonXboxFilesTable = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FullPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.basePanel = new System.Windows.Forms.TableLayoutPanel();
            this.packagesBasePanel = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.viewXboxFilesButton = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.viewNonXboxFileButton = new System.Windows.Forms.Button();
            this.panel4 = new System.Windows.Forms.Panel();
            this.centerControlsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.moveAllToXboxButton = new System.Windows.Forms.Button();
            this.moveSelectionFromXboxButton = new System.Windows.Forms.Button();
            this.moveAllFromXboxButton = new System.Windows.Forms.Button();
            this.moveSelectionToXboxButton = new System.Windows.Forms.Button();
            this.foldersToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.infoStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.packagesDataGridView = new System.Windows.Forms.DataGridView();
            this.GameIcon = new System.Windows.Forms.DataGridViewImageColumn();
            this.GameName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.packagesScrollPanel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.xboxFilesTable)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nonXboxFilesTable)).BeginInit();
            this.basePanel.SuspendLayout();
            this.packagesBasePanel.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel4.SuspendLayout();
            this.centerControlsPanel.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.packagesDataGridView)).BeginInit();
            this.packagesScrollPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // profileListBox
            // 
            this.profileListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.profileListBox.Enabled = false;
            this.profileListBox.FormattingEnabled = true;
            this.profileListBox.Location = new System.Drawing.Point(3, 19);
            this.profileListBox.Name = "profileListBox";
            this.profileListBox.Size = new System.Drawing.Size(404, 82);
            this.profileListBox.TabIndex = 7;
            this.profileListBox.SelectedIndexChanged += new System.EventHandler(this.profileListBox_SelectedIndexChanged);
            // 
            // packagesLabel
            // 
            this.packagesLabel.AutoSize = true;
            this.packagesLabel.Location = new System.Drawing.Point(3, 3);
            this.packagesLabel.Name = "packagesLabel";
            this.packagesLabel.Size = new System.Drawing.Size(129, 13);
            this.packagesLabel.TabIndex = 8;
            this.packagesLabel.Text = "Potential Xbox Packages:";
            // 
            // profileLabel
            // 
            this.profileLabel.AutoSize = true;
            this.profileLabel.Location = new System.Drawing.Point(3, 3);
            this.profileLabel.Name = "profileLabel";
            this.profileLabel.Size = new System.Drawing.Size(154, 13);
            this.profileLabel.TabIndex = 9;
            this.profileLabel.Text = "Non-Xbox Profile: (if applicable)";
            // 
            // nonXboxFilesLabel
            // 
            this.nonXboxFilesLabel.AutoSize = true;
            this.nonXboxFilesLabel.Location = new System.Drawing.Point(3, 15);
            this.nonXboxFilesLabel.Name = "nonXboxFilesLabel";
            this.nonXboxFilesLabel.Size = new System.Drawing.Size(109, 13);
            this.nonXboxFilesLabel.TabIndex = 10;
            this.nonXboxFilesLabel.Text = "Non-Xbox Save Files:";
            // 
            // xboxFileLabel
            // 
            this.xboxFileLabel.AutoSize = true;
            this.xboxFileLabel.Location = new System.Drawing.Point(3, 15);
            this.xboxFileLabel.Name = "xboxFileLabel";
            this.xboxFileLabel.Size = new System.Drawing.Size(86, 13);
            this.xboxFileLabel.TabIndex = 12;
            this.xboxFileLabel.Text = "Xbox Save Files:";
            // 
            // xboxFilesTable
            // 
            this.xboxFilesTable.AllowUserToAddRows = false;
            this.xboxFilesTable.AllowUserToDeleteRows = false;
            this.xboxFilesTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.xboxFilesTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.xboxFilesTable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.File,
            this.LastEdit});
            this.xboxFilesTable.Location = new System.Drawing.Point(3, 31);
            this.xboxFilesTable.Name = "xboxFilesTable";
            this.xboxFilesTable.ReadOnly = true;
            this.xboxFilesTable.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.xboxFilesTable.Size = new System.Drawing.Size(403, 273);
            this.xboxFilesTable.TabIndex = 13;
            // 
            // File
            // 
            this.File.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.File.DataPropertyName = "EncodedPath";
            this.File.HeaderText = "File";
            this.File.Name = "File";
            this.File.ReadOnly = true;
            // 
            // LastEdit
            // 
            this.LastEdit.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.LastEdit.DataPropertyName = "Timestamp";
            this.LastEdit.HeaderText = "Last Modified";
            this.LastEdit.Name = "LastEdit";
            this.LastEdit.ReadOnly = true;
            this.LastEdit.Width = 95;
            // 
            // nonXboxFilesTable
            // 
            this.nonXboxFilesTable.AllowUserToAddRows = false;
            this.nonXboxFilesTable.AllowUserToDeleteRows = false;
            this.nonXboxFilesTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nonXboxFilesTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.nonXboxFilesTable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.FullPath,
            this.dataGridViewTextBoxColumn2});
            this.nonXboxFilesTable.Location = new System.Drawing.Point(0, 31);
            this.nonXboxFilesTable.Name = "nonXboxFilesTable";
            this.nonXboxFilesTable.ReadOnly = true;
            this.nonXboxFilesTable.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.nonXboxFilesTable.Size = new System.Drawing.Size(407, 273);
            this.nonXboxFilesTable.TabIndex = 14;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridViewTextBoxColumn1.DataPropertyName = "RelativePath";
            this.dataGridViewTextBoxColumn1.HeaderText = "File";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // FullPath
            // 
            this.FullPath.DataPropertyName = "FilePath";
            this.FullPath.HeaderText = "Full Path";
            this.FullPath.Name = "FullPath";
            this.FullPath.ReadOnly = true;
            this.FullPath.Visible = false;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewTextBoxColumn2.DataPropertyName = "Timestamp";
            this.dataGridViewTextBoxColumn2.HeaderText = "Last Modified";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 95;
            // 
            // basePanel
            // 
            this.basePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.basePanel.ColumnCount = 3;
            this.basePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.basePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.basePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.basePanel.Controls.Add(this.packagesBasePanel, 0, 0);
            this.basePanel.Controls.Add(this.panel2, 0, 1);
            this.basePanel.Controls.Add(this.panel1, 2, 1);
            this.basePanel.Controls.Add(this.panel4, 2, 0);
            this.basePanel.Controls.Add(this.centerControlsPanel, 1, 1);
            this.basePanel.Location = new System.Drawing.Point(12, 12);
            this.basePanel.Name = "basePanel";
            this.basePanel.RowCount = 2;
            this.basePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 28.40376F));
            this.basePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 71.59624F));
            this.basePanel.Size = new System.Drawing.Size(863, 424);
            this.basePanel.TabIndex = 15;
            // 
            // packagesBasePanel
            // 
            this.packagesBasePanel.Controls.Add(this.packagesScrollPanel);
            this.packagesBasePanel.Controls.Add(this.packagesLabel);
            this.packagesBasePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.packagesBasePanel.Location = new System.Drawing.Point(0, 0);
            this.packagesBasePanel.Margin = new System.Windows.Forms.Padding(0);
            this.packagesBasePanel.Name = "packagesBasePanel";
            this.packagesBasePanel.Size = new System.Drawing.Size(406, 120);
            this.packagesBasePanel.TabIndex = 16;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.viewXboxFilesButton);
            this.panel2.Controls.Add(this.xboxFileLabel);
            this.panel2.Controls.Add(this.xboxFilesTable);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 120);
            this.panel2.Margin = new System.Windows.Forms.Padding(0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(406, 304);
            this.panel2.TabIndex = 16;
            // 
            // viewXboxFilesButton
            // 
            this.viewXboxFilesButton.Enabled = false;
            this.viewXboxFilesButton.Location = new System.Drawing.Point(279, 3);
            this.viewXboxFilesButton.Name = "viewXboxFilesButton";
            this.viewXboxFilesButton.Size = new System.Drawing.Size(124, 22);
            this.viewXboxFilesButton.TabIndex = 14;
            this.viewXboxFilesButton.Text = "Explore Xbox Files";
            this.viewXboxFilesButton.UseVisualStyleBackColor = true;
            this.viewXboxFilesButton.Click += new System.EventHandler(this.viewXboxFilesButton_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.viewNonXboxFileButton);
            this.panel1.Controls.Add(this.nonXboxFilesTable);
            this.panel1.Controls.Add(this.nonXboxFilesLabel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(456, 120);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(407, 304);
            this.panel1.TabIndex = 0;
            // 
            // viewNonXboxFileButton
            // 
            this.viewNonXboxFileButton.Enabled = false;
            this.viewNonXboxFileButton.Location = new System.Drawing.Point(280, 3);
            this.viewNonXboxFileButton.Name = "viewNonXboxFileButton";
            this.viewNonXboxFileButton.Size = new System.Drawing.Size(124, 22);
            this.viewNonXboxFileButton.TabIndex = 15;
            this.viewNonXboxFileButton.Text = "Explore non-Xbox Files";
            this.viewNonXboxFileButton.UseVisualStyleBackColor = true;
            this.viewNonXboxFileButton.Click += new System.EventHandler(this.viewNonXboxFileButton_Click);
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.profileLabel);
            this.panel4.Controls.Add(this.profileListBox);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(456, 0);
            this.panel4.Margin = new System.Windows.Forms.Padding(0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(407, 120);
            this.panel4.TabIndex = 17;
            // 
            // centerControlsPanel
            // 
            this.centerControlsPanel.ColumnCount = 1;
            this.centerControlsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerControlsPanel.Controls.Add(this.moveAllToXboxButton, 0, 2);
            this.centerControlsPanel.Controls.Add(this.moveSelectionFromXboxButton, 0, 3);
            this.centerControlsPanel.Controls.Add(this.moveAllFromXboxButton, 0, 4);
            this.centerControlsPanel.Controls.Add(this.moveSelectionToXboxButton, 0, 1);
            this.centerControlsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centerControlsPanel.Location = new System.Drawing.Point(409, 123);
            this.centerControlsPanel.Name = "centerControlsPanel";
            this.centerControlsPanel.RowCount = 6;
            this.centerControlsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.00001F));
            this.centerControlsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.centerControlsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.centerControlsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.centerControlsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.centerControlsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.centerControlsPanel.Size = new System.Drawing.Size(44, 298);
            this.centerControlsPanel.TabIndex = 18;
            // 
            // moveAllToXboxButton
            // 
            this.moveAllToXboxButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.moveAllToXboxButton.Location = new System.Drawing.Point(8, 123);
            this.moveAllToXboxButton.Name = "moveAllToXboxButton";
            this.moveAllToXboxButton.Size = new System.Drawing.Size(27, 23);
            this.moveAllToXboxButton.TabIndex = 1;
            this.moveAllToXboxButton.Text = "<<";
            this.moveAllToXboxButton.UseVisualStyleBackColor = true;
            this.moveAllToXboxButton.Click += new System.EventHandler(this.moveAllToXboxButton_Click);
            // 
            // moveSelectionFromXboxButton
            // 
            this.moveSelectionFromXboxButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.moveSelectionFromXboxButton.Location = new System.Drawing.Point(8, 152);
            this.moveSelectionFromXboxButton.Name = "moveSelectionFromXboxButton";
            this.moveSelectionFromXboxButton.Size = new System.Drawing.Size(27, 23);
            this.moveSelectionFromXboxButton.TabIndex = 2;
            this.moveSelectionFromXboxButton.Text = ">";
            this.moveSelectionFromXboxButton.UseVisualStyleBackColor = true;
            this.moveSelectionFromXboxButton.Click += new System.EventHandler(this.moveSelectionFromXboxButton_Click);
            // 
            // moveAllFromXboxButton
            // 
            this.moveAllFromXboxButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.moveAllFromXboxButton.Location = new System.Drawing.Point(8, 181);
            this.moveAllFromXboxButton.Name = "moveAllFromXboxButton";
            this.moveAllFromXboxButton.Size = new System.Drawing.Size(27, 23);
            this.moveAllFromXboxButton.TabIndex = 3;
            this.moveAllFromXboxButton.Text = ">>";
            this.moveAllFromXboxButton.UseVisualStyleBackColor = true;
            this.moveAllFromXboxButton.Click += new System.EventHandler(this.moveAllFromXboxButton_Click);
            // 
            // moveSelectionToXboxButton
            // 
            this.moveSelectionToXboxButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.moveSelectionToXboxButton.Location = new System.Drawing.Point(8, 94);
            this.moveSelectionToXboxButton.Name = "moveSelectionToXboxButton";
            this.moveSelectionToXboxButton.Size = new System.Drawing.Size(27, 23);
            this.moveSelectionToXboxButton.TabIndex = 0;
            this.moveSelectionToXboxButton.Text = "<";
            this.moveSelectionToXboxButton.UseVisualStyleBackColor = true;
            this.moveSelectionToXboxButton.Click += new System.EventHandler(this.moveSelectionToXboxButton_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.infoStatusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 439);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(887, 22);
            this.statusStrip1.TabIndex = 16;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // infoStatusLabel
            // 
            this.infoStatusLabel.Name = "infoStatusLabel";
            this.infoStatusLabel.Size = new System.Drawing.Size(88, 17);
            this.infoStatusLabel.Text = "infoStatusLabel";
            // 
            // packagesDataGridView
            // 
            this.packagesDataGridView.AllowUserToAddRows = false;
            this.packagesDataGridView.AllowUserToDeleteRows = false;
            this.packagesDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.packagesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.packagesDataGridView.ColumnHeadersVisible = false;
            this.packagesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.GameIcon,
            this.GameName});
            this.packagesDataGridView.Location = new System.Drawing.Point(0, 0);
            this.packagesDataGridView.MultiSelect = false;
            this.packagesDataGridView.Name = "packagesDataGridView";
            this.packagesDataGridView.ReadOnly = true;
            this.packagesDataGridView.RowHeadersVisible = false;
            this.packagesDataGridView.RowTemplate.Height = 75;
            this.packagesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.packagesDataGridView.Size = new System.Drawing.Size(397, 98);
            this.packagesDataGridView.TabIndex = 9;
            this.packagesDataGridView.Click += new System.EventHandler(this.packagesDataGridView_Click);
            // 
            // GameIcon
            // 
            this.GameIcon.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.GameIcon.DataPropertyName = "GameIcon";
            this.GameIcon.Frozen = true;
            this.GameIcon.HeaderText = "Icon";
            this.GameIcon.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.GameIcon.Name = "GameIcon";
            this.GameIcon.ReadOnly = true;
            this.GameIcon.Width = 75;
            // 
            // GameName
            // 
            this.GameName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.GameName.DataPropertyName = "Name";
            this.GameName.HeaderText = "Name";
            this.GameName.Name = "GameName";
            this.GameName.ReadOnly = true;
            // 
            // packagesScrollPanel
            // 
            this.packagesScrollPanel.AutoScroll = true;
            this.packagesScrollPanel.Controls.Add(this.packagesDataGridView);
            this.packagesScrollPanel.Location = new System.Drawing.Point(6, 19);
            this.packagesScrollPanel.Name = "packagesScrollPanel";
            this.packagesScrollPanel.Size = new System.Drawing.Size(400, 98);
            this.packagesScrollPanel.TabIndex = 9;
            // 
            // SaveFileConverterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(887, 461);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.basePanel);
            this.Name = "SaveFileConverterForm";
            this.Text = "Xbox Save File Converter";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.xboxFilesTable)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nonXboxFilesTable)).EndInit();
            this.basePanel.ResumeLayout(false);
            this.packagesBasePanel.ResumeLayout(false);
            this.packagesBasePanel.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.centerControlsPanel.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.packagesDataGridView)).EndInit();
            this.packagesScrollPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox profileListBox;
        private System.Windows.Forms.Label packagesLabel;
        private System.Windows.Forms.Label profileLabel;
        private System.Windows.Forms.Label nonXboxFilesLabel;
        private System.Windows.Forms.Label xboxFileLabel;
        private System.Windows.Forms.DataGridView xboxFilesTable;
        private System.Windows.Forms.DataGridView nonXboxFilesTable;
        private System.Windows.Forms.DataGridViewTextBoxColumn File;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastEdit;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn FullPath;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.TableLayoutPanel basePanel;
        private System.Windows.Forms.Panel packagesBasePanel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.TableLayoutPanel centerControlsPanel;
        private System.Windows.Forms.Button moveSelectionToXboxButton;
        private System.Windows.Forms.Button moveAllToXboxButton;
        private System.Windows.Forms.Button moveSelectionFromXboxButton;
        private System.Windows.Forms.Button moveAllFromXboxButton;
        private System.Windows.Forms.ToolTip foldersToolTip;
        private System.Windows.Forms.Button viewXboxFilesButton;
        private System.Windows.Forms.Button viewNonXboxFileButton;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel infoStatusLabel;
        private System.Windows.Forms.DataGridView packagesDataGridView;
        private System.Windows.Forms.DataGridViewImageColumn GameIcon;
        private System.Windows.Forms.DataGridViewTextBoxColumn GameName;
        private System.Windows.Forms.Panel packagesScrollPanel;
    }
}

