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

namespace GPSaveConverter
{
    public partial class SaveFileConverterForm : Form
    {
        private static NLog.Logger logger = LogHelper.getClassLogger();
        Xbox.XboxContainerIndex currentContainer;
        internal Library.GameInfo ActiveGame { get; set; }
        List<NonXboxFileInfo> nonXboxFiles;
        BindingList<NonXboxProfile> nonXboxProfiles = new BindingList<NonXboxProfile>();
        private PreferencesForm prefsForm;
        private CreditsForm creditsForm;
        public SaveFileConverterForm()
        {
            InitializeComponent();

            if (!Properties.Settings.Default.ShowFileTranslations)
            {
                toggleFileTranslationPanel();
            }
            this.nonXboxProfileTable.DataSource = nonXboxProfiles;
        }

        private async Task fetchNonXboxSaveFiles()
        {
            nonXboxFiles = new List<NonXboxFileInfo>();
            string fetchLocation = ActiveGame.NonXboxSaveLocation;

            this.foldersToolTip.SetToolTip(this.nonXboxFilesLabel, fetchLocation);

            if(Directory.Exists(fetchLocation)) fetchNonXboxSaveFiles(fetchLocation, fetchLocation);

            this.nonXboxFilesTable.DataSource = nonXboxFiles;
            this.viewNonXboxFileButton.Enabled = true;
        }
        private void fetchNonXboxSaveFiles(string folder,string root)
        {
            foreach(string file in Directory.GetFiles(folder))
            {
                NonXboxFileInfo newInfo = new NonXboxFileInfo();
                newInfo.FilePath = file;
                newInfo.RelativePath = file.Replace(root, "");
                newInfo.Timestamp = System.IO.File.GetLastWriteTime(file);
                this.nonXboxFiles.Add(newInfo);
            }

            foreach(string dir in Directory.GetDirectories(folder))
            {
                fetchNonXboxSaveFiles(dir,root);
            }
        }

        private async Task<bool> promptForNonXboxSaveLocation(string reason)
        {
            DialogResult res;
            if (reason != null)
            {
                 res = MessageBox.Show(this, reason + " Please select save file location.", "Non-Xbox save location not found", MessageBoxButtons.OKCancel);
            }
            else
            {
                res = DialogResult.OK;
            }
            if (res == DialogResult.OK)
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                res = dialog.ShowDialog();
                if (res == DialogResult.OK)
                {
                    ActiveGame.BaseNonXboxSaveLocation = dialog.SelectedPath + "\\";
                    await fetchNonXboxSaveFiles();
                    return true;
                }
                else return false;
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

        private async Task fetchNonXboxProfiles()
        {
            string profilesDir = Library.GameLibrary.GetNonXboxProfileLocation(ActiveGame.BaseNonXboxSaveLocation);
            bool failed = false;

            this.nonXboxProfiles.Clear();
            if (Directory.Exists(profilesDir))
            {
                logger.Info("Fetching non-Xbox profile information...");
                foreach (string p in Directory.GetDirectories(profilesDir))
                {
                    Library.GameLibrary.ProfileID = p.Replace(profilesDir, "");

                    if (Directory.Exists(ActiveGame.NonXboxSaveLocation))
                    {
                        NonXboxProfile newProfile = new NonXboxProfile(Library.GameLibrary.ProfileID, NonXboxProfile.ProfileType.Steam);
                        await newProfile.FetchProfileInformation();
                        this.nonXboxProfiles.Add(newProfile);
                    }
                }

                Library.GameLibrary.ProfileID = null;
                if (this.nonXboxProfiles.Count == 0)
                {
                    failed = true;
                    logger.Info("No Non-Xbox profiles found.");
                }
                else
                {
                    logger.Info("Non-Xbox profiles obtained!");
                    if (this.nonXboxProfiles.Count == 1)
                    {
                        this.nonXboxProfileTable.Rows[0].Selected = true;
                        nonXboxProfileTable_CellClicked(this, null);
                    }
                }
            }
            else
            {
                failed = true;
            }

            if (failed)
            {
                this.nonXboxProfiles.Add(new NonXboxProfile("No non-Xbox profiles found", NonXboxProfile.ProfileType.DisplayOnly));
                this.nonXboxProfileTable.Enabled = false;
                await promptForNonXboxSaveLocation("Game library defines non-Xbox profiles, but none were found.");
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

            this.exportGameLibraryToolStripMenuItem.Enabled = true;

            Library.GameInfo[] gameInfo = await LoadGameInfo();

            this.packagesDataGridView.Height = gameInfo.Length * 75 + 10;

            this.packagesDataGridView.DataSource = gameInfo;
        }

        private void SaveFileConverterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // No need to save library if it was never initialized.
            if (Library.GameLibrary.Initialized)
            {
                GPSaveConverter.Properties.Settings.Default.GameLibrary = Library.GameLibrary.GetLibraryJson();
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

            this.promptNonXboxLocationButton.Enabled = false;
            this.fileTranslationListBox.Enabled = false;
            this.viewXboxFilesButton.Enabled = false;
            this.viewNonXboxFileButton.Enabled = false;
            this.nonXboxProfiles.Clear();
            this.nonXboxProfileTable.Enabled = true;
            this.xboxFilesTable.DataSource = null;
            this.nonXboxFilesTable.DataSource = null;
        }

        private async void nonXboxProfileTable_CellClicked(object sender, DataGridViewCellEventArgs e)
        {
            if (this.nonXboxProfiles.Count > 0)
            {
                NonXboxProfile profile = this.nonXboxProfileTable.SelectedRows[0].DataBoundItem as NonXboxProfile;
                Library.GameLibrary.ProfileID = profile.UserIDFolder;
                if (this.currentContainer != null)
                {
                    await fetchNonXboxSaveFiles();
                }
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

            if(ActiveGame.BaseNonXboxSaveLocation.Contains(Library.GameLibrary.NonSteamProfileMarker) && Library.GameLibrary.ProfileID == null)
            {
                MessageBox.Show(this, "Select non-Xbox Profile (or select save file location manually)", "Configure Profile");
                return false;
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
            System.Diagnostics.Process.Start(this.currentContainer.Children[0].getSaveFilePath());
        }

        private void viewNonXboxFileButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(ActiveGame.NonXboxSaveLocation);
        }

        private async void packagesDataGridView_Click(object sender, EventArgs e)
        {
            ClearForm();

            ActiveGame = (Library.GameInfo)this.packagesDataGridView.SelectedRows[0].DataBoundItem;
            
            // Do this before working with non-UWP data or the fetch won't be awaited.
            if (!ActiveGame.NonUWPDataPopulated)
            {
                await Library.GameLibrary.PopulateNonUWPInformation(ActiveGame);
            }

            this.saveGameProfileToolStripMenuItem.Enabled = true;
            this.loadGameProfileToolStripMenuItem.Enabled = true;

            this.fileTranslationListBox.Items.AddRange(ActiveGame.FileTranslations.ToArray());
            this.fileTranslationListBox.Enabled = true;

            

            await fetchXboxProfiles();

            this.promptNonXboxLocationButton.Enabled = true;

            if (ActiveGame.BaseNonXboxSaveLocation == null || ActiveGame.BaseNonXboxSaveLocation == string.Empty)
            {
                await promptForNonXboxSaveLocation("Non-Xbox save location not found in game library.");
            }
            else
            {
                if (ActiveGame.BaseNonXboxSaveLocation.Contains(Library.GameLibrary.NonSteamProfileMarker))
                {
                    await fetchNonXboxProfiles();
                }
                else
                {
                    this.nonXboxProfiles.Add(new NonXboxProfile("Profiles not defined in non-Xbox save location", NonXboxProfile.ProfileType.DisplayOnly));
                    this.nonXboxProfileTable.Enabled = false;


                    if (Directory.Exists(ActiveGame.NonXboxSaveLocation))
                    {
                        await fetchNonXboxSaveFiles();
                    }
                    else
                    {
                        await promptForNonXboxSaveLocation("Non-Xbox save location from library does not exist.");
                    }
                }
            }
        }

        private async void promptNonXboxLocationButton_Click(object sender, EventArgs e)
        {
            await this.promptForNonXboxSaveLocation(null);
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
    }
}
