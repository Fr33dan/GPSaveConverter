using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GPSaveConverter
{
    public partial class PreferencesForm : Form
    {
        public PreferencesForm()
        {
            InitializeComponent();
        }

        private void PreferencesForm_Load(object sender, EventArgs e)
        {
            this.logLevelComboBox.Items.AddRange(NLog.LogLevel.AllLevels.ToArray());
            this.logLevelComboBox.SelectedIndex = Properties.Settings.Default.FileLogLevel.Ordinal;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.FileLogLevel = this.logLevelComboBox.SelectedItem as NLog.LogLevel;

            NLog.LogManager.Configuration.Variables["fileLogLevel"] = GPSaveConverter.Properties.Settings.Default.FileLogLevel.ToString();

            Properties.Settings.Default.Save();
            this.Hide();
        }
    }
}
