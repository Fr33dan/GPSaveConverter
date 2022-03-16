using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GPSaveConverter
{
    public partial class CreditsForm : Form
    {
        public CreditsForm()
        {
            InitializeComponent();

            this.creditsTextBox.LoadFile(new MemoryStream(GPSaveConverter.Properties.Resources.Credits), RichTextBoxStreamType.RichText);
        }

        private void CreditsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void creditsTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", e.LinkText);
        }
    }
}
