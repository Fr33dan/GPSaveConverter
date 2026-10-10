using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GPSaveConverter.SaveBackups;
using GPSaveConverter.Interfaces;

namespace GPSaveConverter
{
    /// <summary>
    /// Lists the backups of one game and restores or deletes them.
    /// </summary>
    public partial class BackupsForm : Form
    {
        internal static ISettingsProvider Settings { get; set; } = new DefaultSettingsProvider();
        private static NLog.Logger logger = LogHelper.getClassLogger();

        private readonly SaveBackupStore store;
        private readonly string packageName;
        private readonly string gameName;

        /// <summary>
        /// True once a restore has been attempted. The file lists in the main window may then be out of date.
        /// </summary>
        internal bool Restored { get; private set; }

        internal BackupsForm(SaveBackupStore store, string packageName, string gameName)
        {
            InitializeComponent();

            this.store = store;
            this.packageName = packageName;
            this.gameName = gameName;

            this.Text = "Backups - " + gameName;
            this.keptLabel.Text = Settings.BackupBeforeTransfer
                ? "A backup is made before every transfer. The " + Settings.BackupsToKeep + " newest of each game are kept. Change this in File > Preferences."
                : "Backups before a transfer are turned off. Turn them on in File > Preferences.";

            showBackups();
        }

        private void showBackups()
        {
            this.backupsListView.Items.Clear();
            foreach (SaveBackup backup in store.List(packageName))
            {
                ListViewItem item = new ListViewItem(backup.Created.ToString("G"));
                item.SubItems.Add(backup.Side == BackupSide.Xbox ? "Xbox save" : "Non-Xbox files");
                item.SubItems.Add(capitalize(backup.MadeBefore));
                item.SubItems.Add(SaveBackup.FormatSize(backup.Size));
                item.Tag = backup;
                this.backupsListView.Items.Add(item);
            }

            if (this.backupsListView.Items.Count > 0)
            {
                this.backupsListView.Items[0].Selected = true;
            }
            showSelection();
        }

        private SaveBackup selectedBackup()
        {
            return this.backupsListView.SelectedItems.Count == 0 ? null : (SaveBackup)this.backupsListView.SelectedItems[0].Tag;
        }

        private void showSelection()
        {
            SaveBackup backup = selectedBackup();

            this.restoreButton.Enabled = backup != null;
            this.deleteButton.Enabled = backup != null;
            this.openFolderButton.Enabled = backup != null;

            if (backup == null)
            {
                this.detailsLabel.Text = this.backupsListView.Items.Count == 0 ? "No backups have been made for this game yet." : string.Empty;
            }
            else
            {
                this.detailsLabel.Text = "Restoring " + describeRestore(backup) + ", in:" + Environment.NewLine + backup.OriginalFolder;
            }
        }

        /// <summary>
        /// Says what restoring a backup does, worded to follow "Restoring".
        /// </summary>
        private static string describeRestore(SaveBackup backup)
        {
            if (backup.Side == BackupSide.Xbox)
            {
                return "puts the whole Xbox save back to how it was then";
            }

            int putBack = backup.Files.Count(f => f.Existed);
            int removed = backup.Files.Count - putBack;

            string description = putBack > 0 ? "puts back " + countFiles(putBack) : string.Empty;
            if (removed > 0)
            {
                description += (putBack > 0 ? " and " : string.Empty) + "removes " + countFiles(removed) + " that " + (removed == 1 ? "was" : "were") + " not there then";
            }
            return description;
        }

        private static string countFiles(int count)
        {
            return count == 1 ? "1 file" : count + " files";
        }

        private static string capitalize(string text)
        {
            return string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0]) + text.Substring(1);
        }

        private void backupsListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            showSelection();
        }

        private async void restoreButton_Click(object sender, EventArgs e)
        {
            SaveBackup backup = selectedBackup();
            if (backup == null)
            {
                return;
            }

            string question;
            if (backup.Side == BackupSide.Xbox)
            {
                question = "Put the Xbox save of " + gameName + " back to how it was on " + backup.Created.ToString("G") + "?" + Environment.NewLine + Environment.NewLine
                    + "Everything in the Xbox save folder is replaced, including anything saved since then. What is there now is backed up first, so you can undo this." + Environment.NewLine + Environment.NewLine
                    + "Close the game before you continue. If the Xbox app has synced a newer save to the cloud since then, it may bring that save back.";
            }
            else
            {
                question = "Put the non-Xbox files of " + gameName + " back to how they were on " + backup.Created.ToString("G") + "?" + Environment.NewLine + Environment.NewLine
                    + "This " + describeRestore(backup) + ". Other files are left alone. What is there now is backed up first, so you can undo this." + Environment.NewLine + Environment.NewLine
                    + "Close the game before you continue.";
            }

            DialogResult res = MessageBox.Show(this, question, "Restore backup?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (res != DialogResult.Yes)
            {
                return;
            }

            // Locked while it runs, so the window cannot be closed or another restore started.
            Exception failure = null;
            this.Enabled = false;
            try
            {
                await Task.Run(() => store.Restore(backup));
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                this.Enabled = true;
            }

            // A restore that failed part way has changed files as well.
            this.Restored = true;
            showBackups();

            if (failure == null)
            {
                logger.Info("Backup restored");
                MessageBox.Show(this, "The backup was restored." + Environment.NewLine + Environment.NewLine
                    + "What it replaced is now the newest backup in the list, in case you want that back.", "Backup restored");
            }
            else
            {
                logger.Warn(failure, "The backup could not be restored");
                MessageBox.Show(this, "The backup could not be restored:" + Environment.NewLine + failure.Message + Environment.NewLine + Environment.NewLine
                    + "Close the game if it is running, then try again. If any files were changed, what was there before is the newest backup in the list.", "Restore failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            SaveBackup backup = selectedBackup();
            if (backup == null)
            {
                return;
            }

            DialogResult res = MessageBox.Show(this, "Delete the backup from " + backup.Created.ToString("G") + "? This cannot be undone.", "Delete backup?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (res != DialogResult.Yes)
            {
                return;
            }

            try
            {
                store.Delete(backup);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "The backup could not be deleted:" + Environment.NewLine + ex.Message, "Error");
            }
            showBackups();
        }

        private void openFolderButton_Click(object sender, EventArgs e)
        {
            SaveBackup backup = selectedBackup();
            if (backup != null)
            {
                System.Diagnostics.Process.Start(backup.Folder);
            }
        }
    }
}
