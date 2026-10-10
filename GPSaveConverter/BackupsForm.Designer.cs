namespace GPSaveConverter
{
    partial class BackupsForm
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
            this.backupsListView = new System.Windows.Forms.ListView();
            this.dateColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.sideColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.madeBeforeColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.sizeColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.detailsLabel = new System.Windows.Forms.Label();
            this.restoreButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            this.openFolderButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.keptLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // backupsListView
            //
            this.backupsListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.backupsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.dateColumn,
            this.sideColumn,
            this.madeBeforeColumn,
            this.sizeColumn});
            this.backupsListView.FullRowSelect = true;
            this.backupsListView.HideSelection = false;
            this.backupsListView.Location = new System.Drawing.Point(12, 12);
            this.backupsListView.MultiSelect = false;
            this.backupsListView.Name = "backupsListView";
            this.backupsListView.Size = new System.Drawing.Size(660, 199);
            this.backupsListView.TabIndex = 0;
            this.backupsListView.UseCompatibleStateImageBehavior = false;
            this.backupsListView.View = System.Windows.Forms.View.Details;
            this.backupsListView.SelectedIndexChanged += new System.EventHandler(this.backupsListView_SelectedIndexChanged);
            //
            // dateColumn
            //
            this.dateColumn.Text = "Date";
            this.dateColumn.Width = 140;
            //
            // sideColumn
            //
            this.sideColumn.Text = "Backup of";
            this.sideColumn.Width = 100;
            //
            // madeBeforeColumn
            //
            this.madeBeforeColumn.Text = "Made before";
            this.madeBeforeColumn.Width = 320;
            //
            // sizeColumn
            //
            this.sizeColumn.Text = "Size";
            this.sizeColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.sizeColumn.Width = 75;
            //
            // detailsLabel
            //
            this.detailsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.detailsLabel.AutoEllipsis = true;
            this.detailsLabel.Location = new System.Drawing.Point(12, 219);
            this.detailsLabel.Name = "detailsLabel";
            this.detailsLabel.Size = new System.Drawing.Size(660, 45);
            this.detailsLabel.TabIndex = 1;
            //
            // restoreButton
            //
            this.restoreButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.restoreButton.Location = new System.Drawing.Point(12, 272);
            this.restoreButton.Name = "restoreButton";
            this.restoreButton.Size = new System.Drawing.Size(90, 23);
            this.restoreButton.TabIndex = 2;
            this.restoreButton.Text = "Restore";
            this.restoreButton.UseVisualStyleBackColor = true;
            this.restoreButton.Click += new System.EventHandler(this.restoreButton_Click);
            //
            // deleteButton
            //
            this.deleteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.deleteButton.Location = new System.Drawing.Point(108, 272);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(90, 23);
            this.deleteButton.TabIndex = 3;
            this.deleteButton.Text = "Delete";
            this.deleteButton.UseVisualStyleBackColor = true;
            this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
            //
            // openFolderButton
            //
            this.openFolderButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.openFolderButton.Location = new System.Drawing.Point(204, 272);
            this.openFolderButton.Name = "openFolderButton";
            this.openFolderButton.Size = new System.Drawing.Size(90, 23);
            this.openFolderButton.TabIndex = 4;
            this.openFolderButton.Text = "Open Folder";
            this.openFolderButton.UseVisualStyleBackColor = true;
            this.openFolderButton.Click += new System.EventHandler(this.openFolderButton_Click);
            //
            // closeButton
            //
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(582, 272);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(90, 23);
            this.closeButton.TabIndex = 5;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            //
            // keptLabel
            //
            this.keptLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.keptLabel.AutoEllipsis = true;
            this.keptLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.keptLabel.Location = new System.Drawing.Point(12, 304);
            this.keptLabel.Name = "keptLabel";
            this.keptLabel.Size = new System.Drawing.Size(660, 13);
            this.keptLabel.TabIndex = 6;
            //
            // BackupsForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(684, 326);
            this.Controls.Add(this.keptLabel);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.openFolderButton);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.restoreButton);
            this.Controls.Add(this.detailsLabel);
            this.Controls.Add(this.backupsListView);
            this.Icon = global::GPSaveConverter.Properties.Resources.Icon;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(520, 300);
            this.Name = "BackupsForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Backups";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView backupsListView;
        private System.Windows.Forms.ColumnHeader dateColumn;
        private System.Windows.Forms.ColumnHeader sideColumn;
        private System.Windows.Forms.ColumnHeader madeBeforeColumn;
        private System.Windows.Forms.ColumnHeader sizeColumn;
        private System.Windows.Forms.Label detailsLabel;
        private System.Windows.Forms.Button restoreButton;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.Button openFolderButton;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Label keptLabel;
    }
}
