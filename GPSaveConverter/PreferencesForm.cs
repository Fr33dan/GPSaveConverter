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

        /// <summary>
        /// Raised after a save that turned the translations being tested on or off, once they have
        /// been downloaded or dropped. The game on screen may have gained or lost translations.
        /// </summary>
        internal event EventHandler PreviewTranslationsChanged;
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
            this.previewTranslationsCheckbox.Checked = Settings.UsePreviewTranslations;
            this.previewTranslationsCheckbox.Enabled = this.allowNetworkCheckbox.Checked;
            this.backupCheckbox.Checked = Settings.BackupBeforeTransfer;
            this.backupsToKeepUpDown.Value = Math.Min(Math.Max(Settings.BackupsToKeep, this.backupsToKeepUpDown.Minimum), this.backupsToKeepUpDown.Maximum);
        }

        private void allowNetworkCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            // The translations being tested are downloaded, so they need the same permission.
            this.previewTranslationsCheckbox.Enabled = this.allowNetworkCheckbox.Checked;
        }

        private async void saveButton_Click(object sender, EventArgs e)
        {
            bool usePreview = this.previewTranslationsCheckbox.Checked && this.allowNetworkCheckbox.Checked;
            bool previewChanged = usePreview != Settings.UsePreviewTranslations;

            Settings.FileLogLevel = this.logLevelComboBox.SelectedItem as NLog.LogLevel;
            Settings.AllowWebDataFetch = this.allowNetworkCheckbox.Checked;
            Settings.UsePreviewTranslations = usePreview;
            Settings.BackupBeforeTransfer = this.backupCheckbox.Checked;
            Settings.BackupsToKeep = (int)this.backupsToKeepUpDown.Value;
            Settings.Save();

            NLog.LogManager.Configuration.Variables["fileLogLevel"] = Settings.FileLogLevel.ToString();

            this.Hide();

            if (previewChanged && Library.GameLibrary.Initialized)
            {
                // Downloads them, or drops them, without waiting for the next start. Only the download
                // runs off this thread: handing them to the games happens here, where the games are used.
                await Task.Run((Action)Library.GameLibrary.DownloadPreviewTranslations);
                Library.GameLibrary.LoadPreviewTranslations();

                EventHandler changed = this.PreviewTranslationsChanged;
                if (changed != null)
                {
                    changed(this, EventArgs.Empty);
                }
            }
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
