using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ookii.Dialogs.WinForms;
using GPSaveConverter.Interfaces;

namespace GPSaveConverter
{
    public partial class SaveFileConverterForm : Form
    {
        internal static ISettingsProvider Settings { get; set; } = new DefaultSettingsProvider();
        internal static IFileSystem FileSystem { get; set; } = new DefaultFileSystem();
        internal static SaveBackups.SaveBackupStore Backups { get; set; } = new SaveBackups.SaveBackupStore(SaveBackups.SaveBackupStore.DefaultRoot);
        private static NLog.Logger logger = LogHelper.getClassLogger();
        Xbox.XboxContainerIndex currentContainer;
        internal Library.GameInfo ActiveGame { get; set; }
        private PreferencesForm prefsForm;
        private CreditsForm creditsForm;

        private List<TabPage> profileTabs;


        public SaveFileConverterForm()
        {
            InitializeComponent();

            if (!Settings.ShowFileTranslations)
            {
                toggleFileTranslationPanel();
            }
            this.nonXboxFilesTable.DataSource = GPSaveConverter.Library.GameLibrary.nonXboxFiles;
            this.xboxFilesTable.DataSource = GPSaveConverter.Library.GameLibrary.xboxFiles;
        }

        private async Task fetchNonXboxSaveFiles()
        {
            this.foldersToolTip.SetToolTip(this.nonXboxFilesLabel, ActiveGame.NonXboxSaveLocation);

            this.viewNonXboxFileButton.Enabled = true;
            await ActiveGame.refreshNonXboxSaveFiles();
        }

        private async void setNonXboxSaveLocationError(string reason)
        {
            if (reason != null && reason != string.Empty)
            {
                nonXboxLocationError.SetError(promptNonXboxLocationButton, reason + " Please select save file location.");
            }
            else
            {
                nonXboxLocationError.SetError(promptNonXboxLocationButton, string.Empty);
            }
        }

        private async Task<bool> promptForNonXboxSaveLocation()
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            if (!string.IsNullOrEmpty(ActiveGame.BaseNonXboxSaveLocation) && ActiveGame.BaseNonXboxSaveLocation.IndexOfAny(Path.GetInvalidPathChars()) < 0)
            {
                dialog.SelectedPath = ActiveGame.BaseNonXboxSaveLocation;
            }
            DialogResult res = dialog.ShowDialog();
            if (res == DialogResult.OK)
            {
                await useNonXboxSaveLocation(dialog.SelectedPath);
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Takes a folder the user picked as the non-Xbox save location and lists the files in it.
        /// </summary>
        internal async Task useNonXboxSaveLocation(string folder)
        {
            // Clear profiles when manual save file location is used.
            ActiveGame.UsePickedSaveLocation(folder);

            // The profile lists belong to the location this one replaces. A click in one would have
            // no profile left to set.
            removeProfileTabs();

            setNonXboxSaveLocationError(string.Empty);
            await fetchNonXboxSaveFiles();
        }

        private void removeProfileTabs()
        {
            if (profileTabs != null)
            {
                foreach (TabPage p in this.profileTabs)
                {
                    this.tabControl1.Controls.Remove(p);
                }
            }
        }

        private async Task fetchXboxProfiles()
        {
            bool failed = false;
            string wgsFolder = Xbox.XboxPackageList.getWGSFolder(ActiveGame.PackageName);
            this.xboxProfileListBox.Items.Clear();
            foreach (string dir in FileSystem.GetDirectories(wgsFolder))
            {
                string folderName = dir.Replace(wgsFolder, "");
                int underscoreLocation = folderName.IndexOf('_');
                if(underscoreLocation != -1)
                {
                    string profileID = folderName.Substring(0, underscoreLocation);
                    if (!this.xboxProfileListBox.Items.Contains(profileID))
                    {
                        this.xboxProfileListBox.Items.Add(profileID);
                    }
                }

                if (xboxProfileListBox.Items.Count == 0)
                {
                    failed = true;
                }
                else
                {
                    this.xboxProfileListBox.Enabled = true;
                    if (this.xboxProfileListBox.Items.Count == 1)
                    {
                        this.xboxProfileListBox.SelectedItem = this.xboxProfileListBox.Items[0];
                    }
                }
            }

            if (failed)
            {
                this.xboxProfileListBox.Items.Add(NoXboxProfiles);
            }
        }

        /// <summary>
        /// Shown in the Xbox profile list in place of a profile. Selecting it does nothing.
        /// </summary>
        private const string NoXboxProfiles = "No profiles found";

        private async Task fetchNonXboxProfiles(int index)
        {
            if(profileTabs == null)
            {
                this.profileTabs = new List<TabPage>();
            }
            DataGridView profileDataGrid;
            TabPage targetTab;
            if (profileTabs.Count <= index)
            {
                targetTab = new TabPage();
                targetTab.Text = "Profile " + (index + 1).ToString();


                DataGridViewColumn userIconColumn = new System.Windows.Forms.DataGridViewImageColumn();
                DataGridViewColumn userNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
                // 
                // UserIcon
                // 
                userIconColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
                userIconColumn.DataPropertyName = "UserIcon";
                userIconColumn.HeaderText = "User Icon";
                userIconColumn.Name = "UserIcon";
                userIconColumn.ReadOnly = true;
                userIconColumn.Resizable = System.Windows.Forms.DataGridViewTriState.False;
                userIconColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
                userIconColumn.Width = 32;
                // 
                // UserName
                // 
                userNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
                userNameColumn.DataPropertyName = "UserName";
                userNameColumn.HeaderText = "User Name";
                userNameColumn.Name = "UserName";
                userNameColumn.ReadOnly = true;

                profileDataGrid = new DataGridView();

                profileDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                profileDataGrid.ColumnHeadersVisible = false;
                profileDataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { userIconColumn, userNameColumn });
                profileDataGrid.Location = new System.Drawing.Point(183, 78);
                profileDataGrid.Name = "nonXboxProfileTable" + index;
                profileDataGrid.ReadOnly = true;
                profileDataGrid.RowHeadersVisible = false;
                profileDataGrid.RowTemplate.Height = 32;
                profileDataGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
                profileDataGrid.Size = new System.Drawing.Size(204, 95);
                profileDataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
                profileDataGrid.TabIndex = 10;
                profileDataGrid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.nonXboxProfileTable_CellClicked);
                profileDataGrid.DataSource = new BindingList<NonXboxProfile>();

                targetTab.Controls.Add(profileDataGrid);
                this.tabControl1.Controls.Add(targetTab);
                profileTabs.Add(targetTab);
            }
            else
            {
                targetTab = profileTabs[index];

                if (!tabControl1.Controls.Contains(targetTab))
                {
                    this.tabControl1.Controls.Add(targetTab);
                }
                for (int j = index + 1;j < profileTabs.Count; j++)
                {
                    this.tabControl1.Controls.Remove(profileTabs[j]);
                }
            }


            profileDataGrid = targetTab.Controls[0] as DataGridView;
            // The list is turned off when it finds no profiles, and it is used again for the next
            // game, or for this one once a profile has a folder.
            profileDataGrid.Enabled = true;
            BindingList<NonXboxProfile> profileList = profileDataGrid.DataSource as BindingList<NonXboxProfile>;
            profileList.Clear();
            foreach(NonXboxProfile p in await ActiveGame.getProfileOptions(index))
            {
                profileList.Add(p);
            }

            if (profileList.Count == 1)
            {
                profileDataGrid.Rows[0].Selected = true;
                nonXboxProfileTable_CellClicked(profileDataGrid, null);
                setNonXboxSaveLocationError(string.Empty);
            }
            else if(profileList.Count == 0)
            {
                profileList.Add(new NonXboxProfile("No non-Xbox profiles found", index, NonXboxProfile.ProfileType.DisplayOnly));
                profileDataGrid.Enabled = false;
                setNonXboxSaveLocationError("Game library defines non-Xbox profiles, but none were found.");
            }
        }

        

        private async void SaveFileConverterForm_Load(object sender, EventArgs e)
        {
            if (Settings.FirstRun)
            {
                DialogResult res;
                do {
                    res = MessageBox.Show(this, "Xbox Save File Converter can lookup save file locations online from pcgamingwiki.com." + Environment.NewLine + Environment.NewLine + "Do you allow this? (Can be changed any time in preferences)", "Allow internet access?", MessageBoxButtons.YesNo);
                }while (res == DialogResult.Cancel);
                Settings.AllowWebDataFetch = res == DialogResult.Yes;
                Settings.FirstRun = false;
                Settings.Save();
            }
        }

        private async void SaveFileConverterForm_Shown(object sender, EventArgs e)
        {
            await Library.GameLibrary.Initialize();

            if (Library.GameLibrary.Default.Version.CompareTo(Library.GameLibrary.UserLibraryVersion) > 0)
            {
                DialogResult res = MessageBox.Show("The default game library has been updated. Do you want to merge these updates?" + Environment.NewLine + Environment.NewLine + GPSaveConverter.Resources.Dialogs.ReloadDefaults, "Update library?", MessageBoxButtons.YesNo);

                if(res == DialogResult.Yes)
                {
                    Library.GameLibrary.LoadDefaultLibrary();
                }
            }


            this.exportGameLibraryToolStripMenuItem.Enabled = true;

            Library.GameInfo[] gameInfo = await LoadGameInfo();

            this.packagesDataGridView.Height = Math.Max(252, gameInfo.Length * 75 + 10);

            this.packagesDataGridView.DataSource = gameInfo;
        }

        private void SaveFileConverterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // No need to save library if it was never initialized.
            if (Library.GameLibrary.Initialized && (this.prefsForm == null || !this.prefsForm.SkipSave))
            {
                Settings.UserGameLibrary = Library.GameLibrary.GetLibraryJson();
                Settings.Save();
            }
        }

        private async Task<Library.GameInfo[]> LoadGameInfo()
        {
            Library.GameInfo[] result = null;
            await Task.Run(() => result = Xbox.XboxPackageList.GetList());
            return result;
        }

        private void ClearForm()
        {
            this.foldersToolTip.RemoveAll();
            this.fileTranslationListBox.Items.Clear();
            this.fileTranslationPropertyGrid.SelectedObject = null;

            this.promptNonXboxLocationButton.Enabled = false;
            this.fileTranslationListBox.Enabled = false;
            this.viewXboxFilesButton.Enabled = false;
            this.viewNonXboxFileButton.Enabled = false;
            this.copySaveFileTablesToolStripMenuItem.Enabled = false;
            GPSaveConverter.Library.GameLibrary.nonXboxProfiles.Clear();
            GPSaveConverter.Library.GameLibrary.xboxFiles.Clear();
            GPSaveConverter.Library.GameLibrary.nonXboxFiles.Clear();
        }

        private async void nonXboxProfileTable_CellClicked(object sender, DataGridViewCellEventArgs e)
        {
            TabPage sourceTab = this.profileTabs.Where(t => t.Controls[0] == sender).First();
            int sourceIndex = this.profileTabs.IndexOf(sourceTab);
            NonXboxProfile targetProfile = (sourceTab.Controls[0] as DataGridView).SelectedRows[0].DataBoundItem as NonXboxProfile;

            this.ActiveGame.TargetProfiles[sourceIndex] = targetProfile;

            if(sourceIndex == ActiveGame.TargetProfiles.Length - 1 )
            {
                if (this.currentContainer != null)
                {
                    await fetchNonXboxSaveFiles();
                }
            }
            else
            {
                await fetchNonXboxProfiles(sourceIndex + 1);
            }
        }

        private bool CheckReadyToMove()
        {
            if(ActiveGame == null)
            {
                MessageBox.Show(this, "Select a game", "Select a game");
                return false;
            }

            if(this.currentContainer == null)
            {
                MessageBox.Show(this, "Xbox Save location not configured", "Configure Xbox Location");
                return false;
            }

            // Selecting a game with several Xbox profiles opens none of them, so the profile left open
            // is the one of the game selected before it. Its files must not be read or written for this game.
            if (this.currentContainer.PackageName != ActiveGame.PackageName)
            {
                MessageBox.Show(this, "Select an Xbox profile", "Select an Xbox profile");
                return false;
            }

            if(ActiveGame.BaseNonXboxSaveLocation == null || ActiveGame.BaseNonXboxSaveLocation == String.Empty)
            {
                MessageBox.Show(this, "Non-Xbox Save location not configured", "Configure non-Xbox Location");
                return false;
            }

            if (ActiveGame.WaitingForProfile)
            {
                MessageBox.Show(this, "Select non-Xbox Profile(s) (or select save file location manually)", "Configure Profile");
                return false;
            }

            if (!FileSystem.DirectoryExists(ActiveGame.NonXboxSaveLocation))
            {
                MessageBox.Show(this, "Non-Xbox save location not found. Please check your configuration", "Configure Profile");
                return false;
            }


            return true;
        }

        private async Task moveFilesToXbox(System.Collections.IEnumerable rows)
        {
            if (!CheckReadyToMove()) return;

            List<NonXboxFileInfo> files = rows.Cast<DataGridViewRow>().Select(row => row.DataBoundItem as NonXboxFileInfo).ToList();
            if (files.Count == 0)
            {
                MessageBox.Show(this, "There are no files to copy", "No files");
                return;
            }

            bool backUp = Settings.BackupBeforeTransfer;
            string question = backUp
                ? "This will overwrite files in your Xbox save data." + Environment.NewLine + Environment.NewLine
                    + "The Xbox save is backed up first. To undo the transfer, choose File > Backups." + Environment.NewLine + Environment.NewLine
                    + "Continue?"
                : "This could overwrite files in your Xbox save data which cannot be undone. Are you sure?";
            DialogResult res = MessageBox.Show(this, question, "Are you sure?", MessageBoxButtons.YesNo);
            if (res != DialogResult.Yes)
            {
                logger.Info("Transfer canceled");
                return;
            }

            bool createContainers = false;
            List<KeyValuePair<string, List<NonXboxFileInfo>>> missing = containersToCreate(files);
            if (missing.Count > 0)
            {
                res = MessageBox.Show(this, createContainersQuestion(missing), "Create Xbox containers?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.Cancel)
                {
                    logger.Info("Transfer canceled");
                    return;
                }

                createContainers = res == DialogResult.Yes;
                if (!createContainers)
                {
                    // Copied without them. Asking about each of these files again would only repeat the question.
                    foreach (NonXboxFileInfo skipped in missing.SelectMany(container => container.Value))
                    {
                        files.Remove(skipped);
                    }
                    logger.Info("{0} left out: no Xbox container to put {1} in", countFiles(missing.Sum(container => container.Value.Count)), missing.Sum(container => container.Value.Count) == 1 ? "it" : "them");

                    if (files.Count == 0)
                    {
                        return;
                    }
                }
            }

            SaveBackups.SaveBackup backup = null;
            if (backUp)
            {
                try
                {
                    logger.Info("Backing up the Xbox save...");
                    string packageName = ActiveGame.PackageName;
                    string gameName = ActiveGame.Name;
                    string profileFolder = this.currentContainer.xboxProfileFolder;
                    string madeBefore = "copying " + countFiles(files.Count) + " to Xbox";
                    backup = await runLocked(() => Backups.BackUpXboxSave(packageName, gameName, profileFolder, madeBefore));
                }
                catch (Exception e)
                {
                    logger.Warn(e, "The Xbox save could not be backed up");
                    res = MessageBox.Show(this, "The Xbox save could not be backed up:" + Environment.NewLine + e.Message + Environment.NewLine + Environment.NewLine
                        + "Copy the files anyway, without a backup?", "Backup failed", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    if (res != DialogResult.Yes)
                    {
                        logger.Info("Transfer canceled");
                        return;
                    }
                }
            }

            int copied = 0;
            bool finished = TransferLoop.Run(files,
                file =>
                {
                    ActiveGame.getXboxFileVersion(this.currentContainer, file, true, createContainers);
                    copied++;
                },
                (file, e) => askAfterError(file.RelativePath, e));

            if (backup != null)
            {
                pruneBackups();
            }

            if (copied > 0)
            {
                // Also when the transfer was aborted part way. What was copied is in the save, and the
                // index has to say so: a container made for it is not part of the save until it does.
                currentContainer.UpdateIndex();
            }

            if (!finished)
            {
                logger.Info(backup != null ? "Transfer aborted. To undo the files already copied, choose File > Backups." : "Transfer aborted");
            }
            else
            {
                logger.Info(backup != null ? "Transfer complete. To undo it, choose File > Backups." : "Transfer complete");
            }

            // Reload to refresh UI.
            currentContainer = new Xbox.XboxContainerIndex(ActiveGame, (string)this.xboxProfileListBox.SelectedItem);
            this.xboxFilesTable.DataSource = currentContainer.getFileList();
        }

        /// <summary>
        /// The containers a transfer of these files to Xbox would have to make, with the files for each.
        /// </summary>
        private List<KeyValuePair<string, List<NonXboxFileInfo>>> containersToCreate(List<NonXboxFileInfo> files)
        {
            try
            {
                return ActiveGame.ContainersToCreate(this.currentContainer, files);
            }
            catch (Exception e)
            {
                // A mistake in a translation. The transfer itself reports it, file by file.
                logger.Debug(e, "The containers to create could not be worked out");
                return new List<KeyValuePair<string, List<NonXboxFileInfo>>>();
            }
        }

        private static string createContainersQuestion(List<KeyValuePair<string, List<NonXboxFileInfo>>> missing)
        {
            const int namesShown = 10;
            int fileCount = missing.Sum(container => container.Value.Count);

            StringBuilder question = new StringBuilder();
            question.AppendLine("The Xbox save has no container for " + countFiles(fileCount) + ". To copy " + (fileCount == 1 ? "it" : "them") + ", " + (missing.Count == 1 ? "this container has" : "these " + missing.Count + " containers have") + " to be created:");
            question.AppendLine();
            foreach (KeyValuePair<string, List<NonXboxFileInfo>> container in missing.Take(namesShown))
            {
                question.AppendLine("    " + container.Key);
            }
            if (missing.Count > namesShown)
            {
                question.AppendLine("    and " + (missing.Count - namesShown) + " more");
            }
            question.AppendLine();
            question.AppendLine("The Xbox app uploads a new container the next time the game runs. This tool cannot remove it from the cloud afterwards, so create one only if the game is meant to have it.");
            question.AppendLine();
            question.AppendLine("Yes: create " + (missing.Count == 1 ? "it" : "them") + " and copy every file.");
            question.AppendLine("No: copy only the files that have a container.");
            question.Append("Cancel: copy nothing.");
            return question.ToString();
        }

        private async Task moveFilesFromXbox(System.Collections.IEnumerable rows)
        {
            if (!CheckReadyToMove()) return;

            List<Xbox.XboxFileInfo> files = rows.Cast<DataGridViewRow>().Select(row => row.DataBoundItem as Xbox.XboxFileInfo).ToList();
            if (files.Count == 0)
            {
                MessageBox.Show(this, "There are no files to copy", "No files");
                return;
            }

            bool backUp = Settings.BackupBeforeTransfer;
            string question = backUp
                ? "This will overwrite save files in your non-Xbox save data." + Environment.NewLine + Environment.NewLine
                    + "Each file is backed up before it is replaced. To undo the transfer, choose File > Backups." + Environment.NewLine + Environment.NewLine
                    + "Continue?"
                : "This could overwrite save files in your non-Xbox save data which cannot be undone. Are you sure?";
            DialogResult res = MessageBox.Show(this, question, "Are you sure?", MessageBoxButtons.YesNo);
            if (res != DialogResult.Yes)
            {
                logger.Info("Transfer canceled");
                return;
            }

            // Filled as the transfer goes: each file is kept just before it is written.
            SaveBackups.SaveBackup backup = backUp
                ? Backups.StartNonXboxBackup(ActiveGame.PackageName, ActiveGame.Name, ActiveGame.NonXboxSaveLocation, "copying " + countFiles(files.Count) + " from Xbox")
                : null;

            bool finished = TransferLoop.Run(files,
                file => ActiveGame.getNonXboxFileVersion(file, true, backup),
                (file, e) => askAfterError(file.FileID, e));

            bool backedUp = backup != null && backup.Files.Count > 0;
            if (backedUp)
            {
                try
                {
                    backup.Complete();
                }
                catch (Exception e)
                {
                    // Nothing is lost. The journal the backup wrote as it went is read when it is loaded.
                    logger.Debug(e, "The backup's journal could not be folded into it");
                }
                pruneBackups();
            }

            if (!finished)
            {
                logger.Info(backedUp ? "Transfer aborted. To undo the files already copied, choose File > Backups." : "Transfer aborted");
                return;
            }

            logger.Info(backedUp ? "Transfer complete. To undo it, choose File > Backups." : "Transfer complete");

            // Reload to refresh UI.
            await this.fetchNonXboxSaveFiles();
        }

        private static string countFiles(int count)
        {
            return count == 1 ? "1 file" : count + " files";
        }

        /// <summary>
        /// Runs slow work off the UI thread. The window is locked meanwhile, so that nothing else can be
        /// started before the work is done.
        /// </summary>
        private async Task<T> runLocked<T>(Func<T> work)
        {
            this.Enabled = false;
            try
            {
                return await Task.Run(work);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        /// <summary>
        /// Deletes the oldest backups of the active game once there are more than the preferences allow.
        /// </summary>
        private void pruneBackups()
        {
            try
            {
                Backups.Prune(ActiveGame.PackageName, Settings.BackupsToKeep);
            }
            catch (Exception e)
            {
                // Never a reason to stop: the transfer the newest backup belongs to is going ahead or done.
                logger.Debug(e, "Old backups could not be removed");
            }
        }

        private AfterError askAfterError(string fileName, Exception e)
        {
            DialogResult res = MessageBox.Show(this, "An error occurred updating file " + fileName + Environment.NewLine + e.Message, "Error", MessageBoxButtons.AbortRetryIgnore);

            switch (res)
            {
                case DialogResult.Abort:
                    return AfterError.Abort;
                case DialogResult.Retry:
                    return AfterError.Retry;
                default:
                    return AfterError.Skip;
            }
        }

        private async void moveSelectionToXboxButton_Click(object sender, EventArgs e)
        {
            await moveFilesToXbox(this.nonXboxFilesTable.SelectedRows);
        }

        private async void moveAllToXboxButton_Click(object sender, EventArgs e)
        {
            await moveFilesToXbox(this.nonXboxFilesTable.Rows);
        }

        private async void moveSelectionFromXboxButton_Click(object sender, EventArgs e)
        {
            await moveFilesFromXbox(this.xboxFilesTable.SelectedRows);
        }

        private async void moveAllFromXboxButton_Click(object sender, EventArgs e)
        {
            await moveFilesFromXbox(this.xboxFilesTable.Rows);
        }

        private void viewXboxFilesButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(this.currentContainer.Children.Length > 0
                ? this.currentContainer.Children[0].getSaveFilePath()
                : this.currentContainer.xboxProfileFolder);
        }

        private void viewNonXboxFileButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(ActiveGame.NonXboxSaveLocation);
        }

        private async void packagesDataGridView_Click(object sender, EventArgs e)
        {
            if (this.packagesDataGridView.SelectedRows.Count < 1)
            {
                return;
            }
            ClearForm();

            ActiveGame = (Library.GameInfo)this.packagesDataGridView.SelectedRows[0].DataBoundItem;
            
            // Do this before working with non-UWP data or the fetch won't be awaited.
            if (!ActiveGame.NonUWPDataPopulated)
            {
                await Library.GameLibrary.PopulateNonUWPInformation(ActiveGame);
            }

            this.saveGameProfileToolStripMenuItem.Enabled = true;
            this.loadGameProfileToolStripMenuItem.Enabled = true;
            this.editNonXboxLocationToolStripMenuItem1.Enabled = true;
            this.copySaveFileTablesToolStripMenuItem.Enabled = true;
            this.backupsToolStripMenuItem.Enabled = true;
            this.editNonXboxLocationToolStripMenuItem2.Enabled = true;
            this.copyPackageIDToolStripMenuItem.Enabled = true;

            this.fileTranslationListBox.Items.AddRange(ActiveGame.FileTranslations.ToArray());
            this.fileTranslationListBox.Enabled = true;

            

            await fetchXboxProfiles();

            this.promptNonXboxLocationButton.Enabled = true;

            if (ActiveGame.BaseNonXboxSaveLocation == null || ActiveGame.BaseNonXboxSaveLocation == string.Empty)
            {
                setNonXboxSaveLocationError("Non-Xbox save location not found in game library.");
            }
            else
            {
                if (ActiveGame.BaseNonXboxSaveLocation.Contains(Library.GameLibrary.NonSteamProfileMarker))
                {
                    setNonXboxSaveLocationError(string.Empty);
                    await this.fetchNonXboxProfiles(0);
                }
                else
                {
                    removeProfileTabs();

                    if (FileSystem.DirectoryExists(ActiveGame.NonXboxSaveLocation))
                    {
                        setNonXboxSaveLocationError(string.Empty);
                        await fetchNonXboxSaveFiles();
                    }
                    else
                    {
                        setNonXboxSaveLocationError("Non-Xbox save location from library does not exist.");
                    }
                }
            }
        }

        private async void promptNonXboxLocationButton_Click(object sender, EventArgs e)
        {
            await this.promptForNonXboxSaveLocation();
        }

        private bool suspendCrossMatch = false;

        private void nonXboxFilesTable_SelectionChanged(object sender, EventArgs e)
        {
            if (!suspendCrossMatch)
            {
                List<Xbox.XboxFileInfo> matchedFiles = new List<Xbox.XboxFileInfo>();
                try
                {
                    foreach (DataGridViewRow r in this.nonXboxFilesTable.SelectedRows)
                    {
                        NonXboxFileInfo i = (NonXboxFileInfo)r.DataBoundItem;

                        matchedFiles.Add(this.ActiveGame.getXboxFileVersion(this.currentContainer, i));
                    }
                }
                catch (Exception ex)
                {
                    reportMatchFailure(ex);
                }

                suspendCrossMatch = true;
                foreach (DataGridViewRow r in this.xboxFilesTable.Rows)
                {
                    r.Selected = matchedFiles.Contains(r.DataBoundItem);
                }
                suspendCrossMatch = false;
            }
        }

        private void xboxFilesTable_SelectionChanged(object sender, EventArgs e)
        {
            if (!suspendCrossMatch)
            {
                List<NonXboxFileInfo> matchedFiles = new List<NonXboxFileInfo>();
                try
                {
                    foreach (DataGridViewRow r in this.xboxFilesTable.SelectedRows)
                    {
                        Xbox.XboxFileInfo i = (Xbox.XboxFileInfo)r.DataBoundItem;

                        matchedFiles.Add(this.ActiveGame.getNonXboxFileVersion(i));
                    }
                }
                catch (Exception ex)
                {
                    reportMatchFailure(ex);
                }

                suspendCrossMatch = true;
                foreach (DataGridViewRow r in this.nonXboxFilesTable.Rows)
                {
                    r.Selected = matchedFiles.Contains(r.DataBoundItem);
                }
                suspendCrossMatch = false;
            }
        }

        /// <summary>
        /// Says in the status line why the file on the other side could not be found. This runs on every
        /// click in a file list, often on a translation that is still being typed, so it must not put
        /// a window in the way.
        /// </summary>
        private static void reportMatchFailure(Exception e)
        {
            logger.Warn("The matching file could not be worked out: {0}", e.Message);
        }

        private void xboxProfileListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string profile = this.xboxProfileListBox.SelectedItem as string;
            if (profile == null || profile == NoXboxProfiles)
            {
                return;
            }

            try
            {
                currentContainer = new Xbox.XboxContainerIndex(ActiveGame, profile);
                this.viewXboxFilesButton.Enabled = true;
                //this.foldersToolTip.SetToolTip(this.xboxFileLabel, currentContainer.Children[0].getSaveFilePath());
                this.xboxFilesTable.DataSource = currentContainer.getFileList();

                int notOnThisPC = currentContainer.ContainersNotOnDisk;
                if (notOnThisPC > 0)
                {
                    logger.Info("{0} of the {1} Xbox containers are listed but not on this PC, so their files are not shown.", notOnThisPC, currentContainer.Children.Length);
                }
            }
            catch (Exception ex)
            {
                // No profile is open now. Leaving the last one open would show another save's files as this one's.
                currentContainer = null;
                this.viewXboxFilesButton.Enabled = false;
                this.xboxFilesTable.DataSource = GPSaveConverter.Library.GameLibrary.xboxFiles;
                ErrorReport.Show(this, "The Xbox save of profile " + profile + " could not be read.", ex);
            }
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(prefsForm == null)
            {
                prefsForm = new PreferencesForm();
            }

            prefsForm.Show(this);
        }

        private void fileTranslationListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.fileTranslationPropertyGrid.SelectedObject = this.fileTranslationListBox.SelectedItem;
        }

        private void showFileTranslationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.ShowFileTranslations = !Settings.ShowFileTranslations;
            Settings.Save();
            toggleFileTranslationPanel();
        }

        private void toggleFileTranslationPanel()
        {
            int sizeDelta = Settings.ShowFileTranslations ? -this.fileTranslationPanel.Height : this.fileTranslationPanel.Height;
            this.packagesScrollPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.packagesScrollPanel.Height = this.packagesScrollPanel.Height + sizeDelta;
            this.packagesBasePanel.Height = this.packagesBasePanel.Height + sizeDelta;
            this.packagesScrollPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;

            this.fileTranslationPanel.Visible = Settings.ShowFileTranslations;
            this.showFileTranslationsToolStripMenuItem.Checked = Settings.ShowFileTranslations;
        }

        private void addTranslationButton_Click(object sender, EventArgs e)
        {
            Library.FileTranslation newItem = Library.FileTranslation.getDefaultInstance();
            
            ActiveGame.FileTranslations.Add(newItem);
            fileTranslationListBox.Items.Add(newItem);
        }

        private void removeTranslationButton_Click(object sender, EventArgs e)
        {
            Library.FileTranslation newItem = fileTranslationListBox.SelectedItem as Library.FileTranslation;

            ActiveGame.FileTranslations.Remove(newItem);
            fileTranslationListBox.Items.Remove(newItem);
        }

        private void saveGameProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = this.ActiveGame.Name + ".json";
            saveFileDialog.Filter = "Game Property JSON (*.json)|*.json";
            DialogResult result = saveFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.WriteIndented = true;
                options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                FileSystem.WriteAllText(saveFileDialog.FileName,JsonSerializer.Serialize(ActiveGame, typeof(Library.GameInfo),options));
            }
        }

        private void loadGameProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.FileName = this.ActiveGame.Name + ".json";
            openFileDialog.Filter = "Game Property JSON (*.json)|*.json";
            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                Library.GameInfo newInfo = JsonSerializer.Deserialize(FileSystem.ReadAllText(openFileDialog.FileName), typeof(Library.GameInfo)) as Library.GameInfo;
                Library.GameLibrary.RegisterSerializedInfo(newInfo);

                if(newInfo.PackageName == ActiveGame.PackageName)
                {
                    packagesDataGridView_Click(sender, e);
                }
            }
        }

        private void exportGameLibraryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Library.GameLibrary.Initialized)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Game Library JSON (*.json)|*.json";
                DialogResult result = saveFileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    JsonSerializerOptions options = new JsonSerializerOptions();
                    options.WriteIndented = true;
                    options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                    options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                    FileSystem.WriteAllText(saveFileDialog.FileName, Library.GameLibrary.GetLibraryJson(options));
                }
            }
        }

        private void creditsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (creditsForm == null)
            {
                creditsForm = new CreditsForm();
            }
            creditsForm.Show(this);
        }

        private void editNonXboxLocationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditBaseLocationForm editBaseLocationForm = new EditBaseLocationForm();
            editBaseLocationForm.BaseLocation = ActiveGame.BaseNonXboxSaveLocation;

            DialogResult res = editBaseLocationForm.ShowDialog(this);
            if(res == DialogResult.OK)
            {
                ActiveGame.BaseNonXboxSaveLocation = editBaseLocationForm.BaseLocation;
            }
        }

        private void copyPackageIDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(this.ActiveGame.PackageName);
        }

        private async void backupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool restored;
            using (BackupsForm backupsForm = new BackupsForm(Backups, ActiveGame.PackageName, ActiveGame.Name))
            {
                backupsForm.ShowDialog(this);
                restored = backupsForm.Restored;
            }

            if (restored)
            {
                try
                {
                    // Reload to refresh UI.
                    if (this.currentContainer != null && this.currentContainer.PackageName == ActiveGame.PackageName)
                    {
                        currentContainer = new Xbox.XboxContainerIndex(ActiveGame, this.currentContainer.XboxProfileID);
                        this.xboxFilesTable.DataSource = currentContainer.getFileList();
                    }

                    // The button is on exactly when the non-Xbox list has been filled for this game.
                    if (this.viewNonXboxFileButton.Enabled)
                    {
                        await this.fetchNonXboxSaveFiles();
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "The save files could not be listed again after the restore");
                    MessageBox.Show(this, "The save files could not be listed again:" + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine + "Select the game again to refresh the lists.", "Error");
                }
            }
        }
        private void copySaveFileTablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Game Name:");
            sb.AppendLine(ActiveGame.Name);

            sb.Append("Game Package ID:");
            sb.AppendLine(this.ActiveGame.PackageName);
            sb.AppendLine();

            if (this.xboxFilesTable.DataSource != null)
            {
                sb.AppendLine("## Xbox Files:");
                sb.AppendLine("| Container Name 1 | Container Name 2 | Blob ID |");
                sb.AppendLine("| ---------------- | ---------------- | ------- |");
                IEnumerable<Xbox.XboxFileInfo> xboxFileList = (IEnumerable<Xbox.XboxFileInfo>)this.xboxFilesTable.DataSource;
                foreach(Xbox.XboxFileInfo xboxFileInfo in xboxFileList)
                {
                    sb.Append("| ");
                    sb.Append(xboxFileInfo.ContainerName1);
                    sb.Append(" | ");
                    sb.Append(xboxFileInfo.ContainerName2);
                    sb.Append(" | ");
                    sb.Append(xboxFileInfo.FileID);
                    sb.AppendLine(" |");
                }
            }

            if(this.nonXboxFilesTable.DataSource != null)
            {
                sb.AppendLine("## Non-Xbox Files:");
                sb.Append("Non-Xbox save location: ");
                sb.AppendLine(ActiveGame.BaseNonXboxSaveLocation);
                sb.AppendLine("| File Path |");
                sb.AppendLine("| --------  |");
                IEnumerable<NonXboxFileInfo> nonXboxFileList = (IEnumerable<NonXboxFileInfo>)this.nonXboxFilesTable.DataSource;
                foreach (NonXboxFileInfo xboxFileInfo in nonXboxFileList)
                {
                    sb.Append("| ");
                    sb.Append(xboxFileInfo.RelativePath);
                    sb.AppendLine(" |");
                }
            }

            Clipboard.SetText(sb.ToString());
        }

        private void filterText_TextChanged(object sender, EventArgs e)
        {
            if (this.packagesDataGridView.Rows.Count < 1)
            {
                return;
            }
            CurrencyManager cm = (CurrencyManager)BindingContext[this.packagesDataGridView.DataSource];
            cm.SuspendBinding();
            foreach (DataGridViewRow r in this.packagesDataGridView.Rows)
            {
                r.Visible = this.filterText.TextLength < 2 || (r.DataBoundItem as Library.GameInfo).Name.IndexOf(this.filterText.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            cm.ResumeBinding();

            // GetRowsHeight doesn't seem to function as intended for the Height calc
            this.packagesDataGridView.Height = Math.Max(252, this.packagesDataGridView.Rows.GetRowCount(DataGridViewElementStates.Visible) * 75 + 10);
        }
    }
}
