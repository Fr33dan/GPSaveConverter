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
            this.containerFileDisplayBox = new System.Windows.Forms.RichTextBox();
            this.sourceFileListBox = new System.Windows.Forms.ListBox();
            this.insertButton = new System.Windows.Forms.Button();
            this.indexTextBox = new System.Windows.Forms.RichTextBox();
            this.selectAllButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // packagesListBox
            // 
            this.packagesListBox.FormattingEnabled = true;
            this.packagesListBox.Location = new System.Drawing.Point(12, 12);
            this.packagesListBox.Name = "packagesListBox";
            this.packagesListBox.Size = new System.Drawing.Size(221, 95);
            this.packagesListBox.TabIndex = 0;
            this.packagesListBox.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // containerFileDisplayBox
            // 
            this.containerFileDisplayBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.containerFileDisplayBox.Location = new System.Drawing.Point(12, 215);
            this.containerFileDisplayBox.Name = "containerFileDisplayBox";
            this.containerFileDisplayBox.Size = new System.Drawing.Size(454, 223);
            this.containerFileDisplayBox.TabIndex = 2;
            this.containerFileDisplayBox.Text = "";
            // 
            // sourceFileListBox
            // 
            this.sourceFileListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sourceFileListBox.FormattingEnabled = true;
            this.sourceFileListBox.Location = new System.Drawing.Point(239, 12);
            this.sourceFileListBox.Name = "sourceFileListBox";
            this.sourceFileListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.sourceFileListBox.Size = new System.Drawing.Size(227, 199);
            this.sourceFileListBox.TabIndex = 3;
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
            // indexTextBox
            // 
            this.indexTextBox.Location = new System.Drawing.Point(12, 110);
            this.indexTextBox.Name = "indexTextBox";
            this.indexTextBox.Size = new System.Drawing.Size(221, 70);
            this.indexTextBox.TabIndex = 5;
            this.indexTextBox.Text = "";
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
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(478, 450);
            this.Controls.Add(this.selectAllButton);
            this.Controls.Add(this.indexTextBox);
            this.Controls.Add(this.insertButton);
            this.Controls.Add(this.sourceFileListBox);
            this.Controls.Add(this.containerFileDisplayBox);
            this.Controls.Add(this.packagesListBox);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox packagesListBox;
        private System.Windows.Forms.RichTextBox containerFileDisplayBox;
        private System.Windows.Forms.ListBox sourceFileListBox;
        private System.Windows.Forms.Button insertButton;
        private System.Windows.Forms.RichTextBox indexTextBox;
        private System.Windows.Forms.Button selectAllButton;
    }
}

