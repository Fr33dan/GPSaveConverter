namespace GPSaveConverter
{
    partial class PreferencesForm
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
            this.logLevelComboBox = new System.Windows.Forms.ComboBox();
            this.logLevelLabel = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.allowNetworkCheckbox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // logLevelComboBox
            // 
            this.logLevelComboBox.FormattingEnabled = true;
            this.logLevelComboBox.Location = new System.Drawing.Point(139, 12);
            this.logLevelComboBox.Name = "logLevelComboBox";
            this.logLevelComboBox.Size = new System.Drawing.Size(121, 21);
            this.logLevelComboBox.TabIndex = 0;
            // 
            // logLevelLabel
            // 
            this.logLevelLabel.AutoSize = true;
            this.logLevelLabel.Location = new System.Drawing.Point(76, 15);
            this.logLevelLabel.Name = "logLevelLabel";
            this.logLevelLabel.Size = new System.Drawing.Size(57, 13);
            this.logLevelLabel.TabIndex = 1;
            this.logLevelLabel.Text = "Log Level:";
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(185, 62);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 2;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // allowNetworkCheckbox
            // 
            this.allowNetworkCheckbox.AutoSize = true;
            this.allowNetworkCheckbox.Location = new System.Drawing.Point(76, 39);
            this.allowNetworkCheckbox.Name = "allowNetworkCheckbox";
            this.allowNetworkCheckbox.Size = new System.Drawing.Size(184, 17);
            this.allowNetworkCheckbox.TabIndex = 3;
            this.allowNetworkCheckbox.Text = "Allow Internet Game Info Sources";
            this.allowNetworkCheckbox.UseVisualStyleBackColor = true;
            // 
            // PreferencesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(272, 98);
            this.Controls.Add(this.allowNetworkCheckbox);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.logLevelLabel);
            this.Controls.Add(this.logLevelComboBox);
            this.Name = "PreferencesForm";
            this.Text = "Preferences";
            this.Icon = GPSaveConverter.Properties.Resources.Icon;
            this.Load += new System.EventHandler(this.PreferencesForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox logLevelComboBox;
        private System.Windows.Forms.Label logLevelLabel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.CheckBox allowNetworkCheckbox;
    }
}