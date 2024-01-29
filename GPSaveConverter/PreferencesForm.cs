using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GPSaveConverter.Interfaces;

namespace GPSaveConverter
{
    public partial class PreferencesForm : Form
    {
        internal static ISettingsProvider Settings { get; set; } = new DefaultSettingsProvider();
        internal bool SkipSave = false;
        public PreferencesForm()
        {
            InitializeComponent();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.R))
            {
                this.BeginInvoke((Action)ResetSettings);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ResetSettings()
        {
            DialogResult res = MessageBox.Show(this, "Remove all local configurations? (Software will then exit)", "Are you sure?", MessageBoxButtons.YesNoCancel);
            if(res == DialogResult.Yes)
            {
                Settings.Reset();
                Settings.Save();
                SkipSave = true;
                Application.Exit();
            }

        }

        private void PreferencesForm_Load(object sender, EventArgs e)
        {
            this.logLevelComboBox.Items.AddRange(NLog.LogLevel.AllLevels.ToArray());
            this.logLevelComboBox.SelectedIndex = Settings.FileLogLevel.Ordinal;
            this.allowNetworkCheckbox.Checked = Settings.AllowWebDataFetch;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            Settings.FileLogLevel = this.logLevelComboBox.SelectedItem as NLog.LogLevel;
            Settings.AllowWebDataFetch = this.allowNetworkCheckbox.Checked;
            Settings.Save();

            NLog.LogManager.Configuration.Variables["fileLogLevel"] = Settings.FileLogLevel.ToString();

            this.Hide();
        }

        private void reloadLibraryButton_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show(this, GPSaveConverter.Resources.Dialogs.ReloadDefaults + "Do you wish to continue?", "Are you sure", MessageBoxButtons.YesNo);
            if(res == DialogResult.Yes)
            {
                Library.GameLibrary.LoadDefaultLibrary();
            }
        }

        private void PreferencesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void resetAllButton_Click(object sender, EventArgs e)
        {
            ResetSettings();
        }
    }
}
