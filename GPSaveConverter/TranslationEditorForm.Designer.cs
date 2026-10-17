namespace GPSaveConverter
{
    partial class TranslationEditorForm
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
            this.introLabel = new System.Windows.Forms.Label();
            this.xboxSideLabel = new System.Windows.Forms.Label();
            this.container1Label = new System.Windows.Forms.Label();
            this.container1Box = new System.Windows.Forms.TextBox();
            this.container2Label = new System.Windows.Forms.Label();
            this.container2Box = new System.Windows.Forms.TextBox();
            this.blobLabel = new System.Windows.Forms.Label();
            this.blobBox = new System.Windows.Forms.TextBox();
            this.pcSideLabel = new System.Windows.Forms.Label();
            this.fileLabel = new System.Windows.Forms.Label();
            this.fileBox = new System.Windows.Forms.TextBox();
            this.previewLabel = new System.Windows.Forms.Label();
            this.previewList = new System.Windows.Forms.ListBox();
            this.summaryLabel = new System.Windows.Forms.Label();
            this.problemLabel = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // introLabel
            //
            this.introLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.introLabel.Location = new System.Drawing.Point(12, 9);
            this.introLabel.Name = "introLabel";
            this.introLabel.Size = new System.Drawing.Size(536, 52);
            this.introLabel.TabIndex = 0;
            //
            // xboxSideLabel
            //
            this.xboxSideLabel.AutoSize = true;
            this.xboxSideLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.xboxSideLabel.Location = new System.Drawing.Point(12, 66);
            this.xboxSideLabel.Name = "xboxSideLabel";
            this.xboxSideLabel.Size = new System.Drawing.Size(52, 13);
            this.xboxSideLabel.TabIndex = 1;
            this.xboxSideLabel.Text = "On Xbox";
            //
            // container1Label
            //
            this.container1Label.AutoSize = true;
            this.container1Label.Location = new System.Drawing.Point(24, 89);
            this.container1Label.Name = "container1Label";
            this.container1Label.Size = new System.Drawing.Size(85, 13);
            this.container1Label.TabIndex = 2;
            this.container1Label.Text = "Container name:";
            //
            // container1Box
            //
            this.container1Box.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.container1Box.Location = new System.Drawing.Point(170, 86);
            this.container1Box.Name = "container1Box";
            this.container1Box.Size = new System.Drawing.Size(378, 20);
            this.container1Box.TabIndex = 3;
            this.container1Box.TextChanged += new System.EventHandler(this.nameBox_TextChanged);
            //
            // container2Label
            //
            this.container2Label.AutoSize = true;
            this.container2Label.Location = new System.Drawing.Point(24, 115);
            this.container2Label.Name = "container2Label";
            this.container2Label.Size = new System.Drawing.Size(123, 13);
            this.container2Label.TabIndex = 4;
            this.container2Label.Text = "Second container name:";
            //
            // container2Box
            //
            this.container2Box.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.container2Box.Location = new System.Drawing.Point(170, 112);
            this.container2Box.Name = "container2Box";
            this.container2Box.Size = new System.Drawing.Size(378, 20);
            this.container2Box.TabIndex = 5;
            this.container2Box.TextChanged += new System.EventHandler(this.nameBox_TextChanged);
            //
            // blobLabel
            //
            this.blobLabel.AutoSize = true;
            this.blobLabel.Location = new System.Drawing.Point(24, 141);
            this.blobLabel.Name = "blobLabel";
            this.blobLabel.Size = new System.Drawing.Size(59, 13);
            this.blobLabel.TabIndex = 6;
            this.blobLabel.Text = "Blob name:";
            //
            // blobBox
            //
            this.blobBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.blobBox.Location = new System.Drawing.Point(170, 138);
            this.blobBox.Name = "blobBox";
            this.blobBox.Size = new System.Drawing.Size(378, 20);
            this.blobBox.TabIndex = 7;
            this.blobBox.TextChanged += new System.EventHandler(this.nameBox_TextChanged);
            //
            // pcSideLabel
            //
            this.pcSideLabel.AutoSize = true;
            this.pcSideLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.pcSideLabel.Location = new System.Drawing.Point(12, 170);
            this.pcSideLabel.Name = "pcSideLabel";
            this.pcSideLabel.Size = new System.Drawing.Size(181, 13);
            this.pcSideLabel.TabIndex = 8;
            this.pcSideLabel.Text = "On this PC, in the save folder";
            //
            // fileLabel
            //
            this.fileLabel.AutoSize = true;
            this.fileLabel.Location = new System.Drawing.Point(24, 193);
            this.fileLabel.Name = "fileLabel";
            this.fileLabel.Size = new System.Drawing.Size(55, 13);
            this.fileLabel.TabIndex = 9;
            this.fileLabel.Text = "File name:";
            //
            // fileBox
            //
            this.fileBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileBox.Location = new System.Drawing.Point(170, 190);
            this.fileBox.Name = "fileBox";
            this.fileBox.Size = new System.Drawing.Size(378, 20);
            this.fileBox.TabIndex = 10;
            this.fileBox.TextChanged += new System.EventHandler(this.nameBox_TextChanged);
            //
            // previewLabel
            //
            this.previewLabel.AutoSize = true;
            this.previewLabel.Location = new System.Drawing.Point(12, 222);
            this.previewLabel.Name = "previewLabel";
            this.previewLabel.Size = new System.Drawing.Size(130, 13);
            this.previewLabel.TabIndex = 11;
            this.previewLabel.Text = "With the files listed now:";
            //
            // previewList
            //
            this.previewList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.previewList.FormattingEnabled = true;
            this.previewList.HorizontalScrollbar = true;
            this.previewList.IntegralHeight = false;
            this.previewList.Location = new System.Drawing.Point(12, 240);
            this.previewList.Name = "previewList";
            this.previewList.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.previewList.Size = new System.Drawing.Size(536, 147);
            this.previewList.TabIndex = 12;
            //
            // summaryLabel
            //
            this.summaryLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.summaryLabel.Location = new System.Drawing.Point(12, 392);
            this.summaryLabel.Name = "summaryLabel";
            this.summaryLabel.Size = new System.Drawing.Size(536, 16);
            this.summaryLabel.TabIndex = 13;
            //
            // problemLabel
            //
            this.problemLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.problemLabel.ForeColor = System.Drawing.Color.Firebrick;
            this.problemLabel.Location = new System.Drawing.Point(12, 410);
            this.problemLabel.Name = "problemLabel";
            this.problemLabel.Size = new System.Drawing.Size(536, 30);
            this.problemLabel.TabIndex = 14;
            //
            // okButton
            //
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Location = new System.Drawing.Point(392, 446);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 15;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(473, 446);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 16;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            //
            // TranslationEditorForm
            //
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(560, 481);
            this.Controls.Add(this.introLabel);
            this.Controls.Add(this.xboxSideLabel);
            this.Controls.Add(this.container1Label);
            this.Controls.Add(this.container1Box);
            this.Controls.Add(this.container2Label);
            this.Controls.Add(this.container2Box);
            this.Controls.Add(this.blobLabel);
            this.Controls.Add(this.blobBox);
            this.Controls.Add(this.pcSideLabel);
            this.Controls.Add(this.fileLabel);
            this.Controls.Add(this.fileBox);
            this.Controls.Add(this.previewLabel);
            this.Controls.Add(this.previewList);
            this.Controls.Add(this.summaryLabel);
            this.Controls.Add(this.problemLabel);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(480, 400);
            this.Name = "TranslationEditorForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "File Translation";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label introLabel;
        private System.Windows.Forms.Label xboxSideLabel;
        private System.Windows.Forms.Label container1Label;
        private System.Windows.Forms.TextBox container1Box;
        private System.Windows.Forms.Label container2Label;
        private System.Windows.Forms.TextBox container2Box;
        private System.Windows.Forms.Label blobLabel;
        private System.Windows.Forms.TextBox blobBox;
        private System.Windows.Forms.Label pcSideLabel;
        private System.Windows.Forms.Label fileLabel;
        private System.Windows.Forms.TextBox fileBox;
        private System.Windows.Forms.Label previewLabel;
        private System.Windows.Forms.ListBox previewList;
        private System.Windows.Forms.Label summaryLabel;
        private System.Windows.Forms.Label problemLabel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}
