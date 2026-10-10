using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GPSaveConverter
{
    /// <summary>
    /// Tells the user about an error and offers the details for a report. Opened by <see cref="ErrorReport"/>.
    /// </summary>
    public partial class ErrorForm : Form
    {
        internal ErrorForm(string summary, string message, string details)
        {
            InitializeComponent();

            this.summaryLabel.Text = summary;
            this.messageLabel.Text = message;
            this.detailsTextBox.Text = details;
        }

        private void ErrorForm_Shown(object sender, EventArgs e)
        {
            // Otherwise the details start out selected from top to bottom.
            this.closeButton.Focus();
        }

        private void copyButton_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(this.detailsTextBox.Text);
                this.copyButton.Text = "Copied";
            }
            catch (ExternalException)
            {
                // Another program is holding the clipboard. Selecting the text leaves Ctrl+C to the user.
                this.detailsTextBox.Focus();
                this.detailsTextBox.SelectAll();
            }
        }

        private void issuesButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(ErrorReport.IssuesPage);
        }
    }
}
