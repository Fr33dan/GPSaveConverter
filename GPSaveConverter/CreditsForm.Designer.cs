namespace GPSaveConverter
{
    partial class CreditsForm
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
            this.creditsTextBox = new GPSaveConverter.RichTextBoxFixedForFriendlyLinks();
            this.SuspendLayout();
            // 
            // creditsTextBox
            // 
            this.creditsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.creditsTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.creditsTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.creditsTextBox.Location = new System.Drawing.Point(12, 12);
            this.creditsTextBox.Name = "creditsTextBox";
            this.creditsTextBox.ReadOnly = true;
            this.creditsTextBox.Size = new System.Drawing.Size(544, 287);
            this.creditsTextBox.TabIndex = 0;
            this.creditsTextBox.Text = "";
            this.creditsTextBox.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.creditsTextBox_LinkClicked);
            // 
            // CreditsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(568, 311);
            this.Controls.Add(this.creditsTextBox);
            this.Icon = global::GPSaveConverter.Properties.Resources.Icon;
            this.Name = "CreditsForm";
            this.Text = "Credits";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CreditsForm_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private RichTextBoxFixedForFriendlyLinks creditsTextBox;
    }
}