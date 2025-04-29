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

namespace GPSaveConverter
{
    public partial class SaveFileConverterForm : Form
    {
        private static NLog.Logger logger = LogHelper.getClassLogger();
        Xbox.XboxContainerIndex currentContainer;
        internal Library.GameInfo ActiveGame { get; set; }
        private PreferencesForm prefsForm;
        private CreditsForm creditsForm;

        private List<TabPage> profileTabs;


        public SaveFileConverterForm()
        {
            InitializeComponent();

            if (!Properties.Settings.Default.ShowFileTranslations)
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
            await ActiveGame.fetchNonXboxSaveFiles();
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
                ActiveGame.BaseNonXboxSaveLocation = dialog.SelectedPath + "\\";

                // Clear profiles when manual save file location is used.
                ActiveGame.TargetProfiles = null;

                setNonXboxSaveLocationError(string.Empty);
                await fetchNonXboxSaveFiles();
                return true;
            }
            else return false;
        }

        private async Task fetchXboxProfiles()
        {
            bool failed = false;
            string wgsFolder = Xbox.XboxPackageList.getWGSFolder(ActiveGame.PackageName);
            this.xboxProfileListBox.Items.Clear();
            foreach (string dir in Directory.GetDirectories(wgsFolder))
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
                this.xboxProfileListBox.Items.Add("No profiles found");
            }
        }

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
            if (GPSaveConverter.Properties.Settings.Default.FirstRun)
            {
                DialogResult res;
                do {
                    res = MessageBox.Show(this, "Xbox Save File Converter can lookup save file locations online from pcgamingwiki.com." + Environment.NewLine + Environment.NewLine + "Do you allow this? (Can be changed any time in preferences)", "Allow internet access?", MessageBoxButtons.YesNo);
                }while (res == DialogResult.Cancel);
                GPSaveConverter.Properties.Settings.Default.AllowWebDataFetch = res == DialogResult.Yes;
                GPSaveConverter.Properties.Settings.Default.FirstRun = false;
                GPSaveConverter.Properties.Settings.Default.Save();
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
                GPSaveConverter.Properties.Settings.Default.UserGameLibrary = Library.GameLibrary.GetLibraryJson();
                GPSaveConverter.Properties.Settings.Default.Save();
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

            if(ActiveGame.BaseNonXboxSaveLocation == null || ActiveGame.BaseNonXboxSaveLocation == String.Empty)
            {
                MessageBox.Show(this, "Non-Xbox Save location not configured", "Configure non-Xbox Location");
                return false;
            }

            if (ActiveGame.TargetProfiles != null) 
            {
                foreach (NonXboxProfile p in ActiveGame.TargetProfiles)
                {
                    if (p.UserID == null)
                    {
                        MessageBox.Show(this, "Select non-Xbox Profile(s) (or select save file location manually)", "Configure Profile");
                        return false;
                    }
                }
            }

            if (!Directory.Exists(ActiveGame.NonXboxSaveLocation))
            {
                MessageBox.Show(this, "Non-Xbox save location not found. Please check your configuration", "Configure Profile");
                return false;
            }


            return true;
        }

        private async Task moveFilesToXbox(System.Collections.IEnumerable rows)
        {
            if (!CheckReadyToMove()) return;

            DialogResult res = MessageBox.Show(this, "This could overwrite files in your Xbox save data which cannot be undone. Are you sure?", "Are you sure?", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                foreach (DataGridViewRow row in rows)
                {
                    NonXboxFileInfo file = row.DataBoundItem as NonXboxFileInfo; do
                    {
                        try
                        {
                            ActiveGame.getXboxFileVersion(this.currentContainer, file, true);
                        }
                        catch (Exception e)
                        {
                            res = MessageBox.Show(this, "An error occured updating file " + file.RelativePath + Environment.NewLine + e.Message, "Error", MessageBoxButtons.AbortRetryIgnore);

                            if (res == DialogResult.Abort)
                            {
                                logger.Info("Transfer aborted");
                                return;
                            }
                        }
                    } while (res == DialogResult.Retry);
                }

                currentContainer.UpdateIndex();

                logger.Info("Transfer complete");

                // Reload to refresh UI.
                currentContainer = new Xbox.XboxContainerIndex(ActiveGame, (string)this.xboxProfileListBox.SelectedItem);
                this.xboxFilesTable.DataSource = currentContainer.getFileList();
            }
            else { logger.Info("Transfer canceled"); }
            
        }

        private async Task moveFilesFromXbox(System.Collections.IEnumerable rows)
        {
            if (!CheckReadyToMove()) return;
            DialogResult res = MessageBox.Show(this, "This could overwrite save files in your non-Xbox save data which cannot be undone. Are you sure?", "Are you sure?", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                foreach (DataGridViewRow row in rows)
                {
                    Xbox.XboxFileInfo file = row.DataBoundItem as Xbox.XboxFileInfo;
                    do
                    {
                        try
                        {
                            ActiveGame.getNonXboxFileVersion(file, true);
                        }
                        catch (Exception e)
                        {
                            res = MessageBox.Show(this, "An error occured updating file " + file.FileID + Environment.NewLine + e.Message, "Error", MessageBoxButtons.AbortRetryIgnore);

                            if (res == DialogResult.Abort)
                            {
                                logger.Info("Transfer aborted");
                                return;
                            }
                        }
                    } while (res == DialogResult.Retry);
                }
                
                logger.Info("Transfer complete");

                // Reload to refresh UI.
                await this.fetchNonXboxSaveFiles();
            }
            else { logger.Info("Transfer canceled"); }
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
                    if (profileTabs != null)
                    {
                        foreach (TabPage p in this.profileTabs)
                        {
                            this.tabControl1.Controls.Remove(p);
                        }
                    }


                    if (Directory.Exists(ActiveGame.NonXboxSaveLocation))
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
                foreach (DataGridViewRow r in this.nonXboxFilesTable.SelectedRows)
                {
                    NonXboxFileInfo i = (NonXboxFileInfo)r.DataBoundItem;

                    matchedFiles.Add(this.ActiveGame.getXboxFileVersion(this.currentContainer, i));
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
                foreach (DataGridViewRow r in this.xboxFilesTable.SelectedRows)
                {
                    Xbox.XboxFileInfo i = (Xbox.XboxFileInfo)r.DataBoundItem;

                    matchedFiles.Add(this.ActiveGame.getNonXboxFileVersion(i));
                }

                suspendCrossMatch = true;
                foreach (DataGridViewRow r in this.nonXboxFilesTable.Rows)
                {
                    r.Selected = matchedFiles.Contains(r.DataBoundItem);
                }
                suspendCrossMatch = false;
            }
        }

        private void xboxProfileListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentContainer = new Xbox.XboxContainerIndex(ActiveGame, (string)this.xboxProfileListBox.SelectedItem);
            this.viewXboxFilesButton.Enabled = true;
            //this.foldersToolTip.SetToolTip(this.xboxFileLabel, currentContainer.Children[0].getSaveFilePath());
            this.xboxFilesTable.DataSource = currentContainer.getFileList();
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
            Properties.Settings.Default.ShowFileTranslations = !Properties.Settings.Default.ShowFileTranslations;
            Properties.Settings.Default.Save();
            toggleFileTranslationPanel();
        }

        private void toggleFileTranslationPanel()
        {
            int sizeDelta = Properties.Settings.Default.ShowFileTranslations ? -this.fileTranslationPanel.Height : this.fileTranslationPanel.Height;
            this.packagesScrollPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.packagesScrollPanel.Height = this.packagesScrollPanel.Height + sizeDelta;
            this.packagesBasePanel.Height = this.packagesBasePanel.Height + sizeDelta;
            this.packagesScrollPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;

            this.fileTranslationPanel.Visible = Properties.Settings.Default.ShowFileTranslations;
            this.showFileTranslationsToolStripMenuItem.Checked = Properties.Settings.Default.ShowFileTranslations;
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
                File.WriteAllText(saveFileDialog.FileName,JsonSerializer.Serialize(ActiveGame, typeof(Library.GameInfo),options));
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
                Library.GameInfo newInfo = JsonSerializer.Deserialize(File.ReadAllText(openFileDialog.FileName), typeof(Library.GameInfo)) as Library.GameInfo;
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
                    File.WriteAllText(saveFileDialog.FileName, Library.GameLibrary.GetLibraryJson(options));
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
