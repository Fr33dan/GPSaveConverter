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
    public partial class SaveFileConverterForm : Form
    {
        private static NLog.Logger logger = LogHelper.getClassLogger();
        Xbox.XboxContainerIndex currentContainer;
        Library.GameInfo gameInfo;
        List<NonXboxFileInfo> nonXboxFiles;
        private PreferencesForm prefsForm;

        public SaveFileConverterForm()
        {
            InitializeComponent();
        }

        private void fetchNonXboxSaveFiles()
        {
            nonXboxFiles = new List<NonXboxFileInfo>();
            string fetchLocation = gameInfo.NonXboxSaveLocation;

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

        private bool promptForNonXboxSaveLocation(string reason)
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
                    gameInfo.BaseNonXboxSaveLocation = dialog.SelectedPath + "\\";
                    fetchNonXboxSaveFiles();
                    return true;
                }
                else return false;
            }
            else return false;
        }

        private void fetchXboxProfiles()
        {
            bool failed = false;
            string wgsFolder = Xbox.XboxPackageList.getWGSFolder(gameInfo.PackageName);
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

        private void fetchNonXboxProfiles()
        {
            string profilesDir = Library.GameLibrary.GetNonXboxProfileLocation(gameInfo.BaseNonXboxSaveLocation);
            bool failed = false;

            this.nonXboxProfileListBox.Items.Clear();
            if (Directory.Exists(profilesDir))
            {
                foreach (string p in Directory.GetDirectories(profilesDir))
                {
                    Library.GameLibrary.ProfileID = p.Replace(profilesDir, "");

                    if (Directory.Exists(gameInfo.NonXboxSaveLocation))
                    {
                        this.nonXboxProfileListBox.Items.Add(Library.GameLibrary.ProfileID);
                    }
                }
                Library.GameLibrary.ProfileID = null;
                if (nonXboxProfileListBox.Items.Count == 0)
                {
                    failed = true;
                }
                else
                {
                    this.nonXboxProfileListBox.Enabled = true;
                    if (this.nonXboxProfileListBox.Items.Count == 1)
                    {
                        this.nonXboxProfileListBox.SelectedItem = this.nonXboxProfileListBox.Items[0];
                    }
                }
            }
            else
            {
                failed = true;
            }

            if (failed) 
            {
                this.nonXboxProfileListBox.Items.Add("No profiles found");
                promptForNonXboxSaveLocation("Game library defines non-Xbox profiles, but none were found.");
            }

        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            logger.Info("Loading game info...");
            
            Library.GameInfo[] gameInfo = await LoadGameInfo();

            this.packagesDataGridView.Height = gameInfo.Length * 75 + 10;

            this.packagesDataGridView.DataSource = gameInfo;

            logger.Info("Load Successful");
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

            this.promptNonXboxLocationButton.Enabled = false;
            this.viewXboxFilesButton.Enabled = false;
            this.viewNonXboxFileButton.Enabled = false;
            this.nonXboxProfileListBox.Items.Clear();
            this.nonXboxProfileListBox.Enabled = false;
            this.xboxFilesTable.DataSource = null;
            this.nonXboxFilesTable.DataSource = null;
        }

        private void profileListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Library.GameLibrary.ProfileID = (string)this.nonXboxProfileListBox.SelectedItem;
            if (this.currentContainer != null)
            {
                fetchNonXboxSaveFiles();
            }
        }

        private bool CheckReadyToMove()
        {
            if(gameInfo == null)
            {
                MessageBox.Show(this, "Select a game", "Select a game");
                return false;
            }

            if(this.currentContainer == null)
            {
                MessageBox.Show(this, "Xbox Save location not configured", "Configure Xbox Location");
                return false;
            }

            if(gameInfo.BaseNonXboxSaveLocation == null || gameInfo.BaseNonXboxSaveLocation == String.Empty)
            {
                MessageBox.Show(this, "Non-Xbox Save location not configured", "Configure non-Xbox Location");
                return false;
            }

            if(gameInfo.BaseNonXboxSaveLocation.Contains(Library.GameLibrary.NonSteamProfileMarker) && Library.GameLibrary.ProfileID == null)
            {
                MessageBox.Show(this, "Select non-Xbox Profile (or select save file location manually)", "Configure Profile");
                return false;
            }

            if (!Directory.Exists(gameInfo.NonXboxSaveLocation))
            {
                MessageBox.Show(this, "Non-Xbox save location not found. Please check your configuration", "Configure Profile");
                return false;
            }


            return true;
        }

        private void moveFilesToXbox(System.Collections.IEnumerable rows)
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
                            gameInfo.getXboxFileVersion(this.currentContainer, file, true);
                        }
                        catch (Exception e)
                        {
                            res = MessageBox.Show(this, "An error occured updating file " + file.RelativePath + Environment.NewLine + e.Message, "Error", MessageBoxButtons.AbortRetryIgnore);

                            if (res == DialogResult.Abort)
                            {
                                return;
                            }
                        }
                    } while (res == DialogResult.Retry);
                }

                this.xboxFilesTable.DataSource = currentContainer.getFileList();
            }
        }

        private void moveFilesFromXbox(System.Collections.IEnumerable rows)
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
                            gameInfo.getNonXboxFileVersion(file, true);
                        }
                        catch (Exception e)
                        {
                            res = MessageBox.Show(this, "An error occured updating file " + file.FileID + Environment.NewLine + e.Message, "Error", MessageBoxButtons.AbortRetryIgnore);

                            if (res == DialogResult.Abort)
                            {
                                return;
                            }
                        }
                    } while (res == DialogResult.Retry);
                }
                this.fetchNonXboxSaveFiles();
            }
        }

        private void moveSelectionToXboxButton_Click(object sender, EventArgs e)
        {
            moveFilesToXbox(this.nonXboxFilesTable.SelectedRows);
        }

        private void moveAllToXboxButton_Click(object sender, EventArgs e)
        {
            moveFilesToXbox(this.nonXboxFilesTable.Rows);
        }

        private void moveSelectionFromXboxButton_Click(object sender, EventArgs e)
        {
            moveFilesFromXbox(this.xboxFilesTable.SelectedRows);
        }

        private void moveAllFromXboxButton_Click(object sender, EventArgs e)
        {
            moveFilesFromXbox(this.xboxFilesTable.Rows);
        }

        private void viewXboxFilesButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(this.currentContainer.Children[0].getSaveFilePath());
        }

        private void viewNonXboxFileButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(gameInfo.NonXboxSaveLocation);
        }

        private void packagesDataGridView_Click(object sender, EventArgs e)
        {
            ClearForm();

            gameInfo = (Library.GameInfo)this.packagesDataGridView.SelectedRows[0].DataBoundItem;

            this.fetchXboxProfiles();

            this.promptNonXboxLocationButton.Enabled = true;

            if (gameInfo.BaseNonXboxSaveLocation == null || gameInfo.BaseNonXboxSaveLocation == string.Empty)
            {
                promptForNonXboxSaveLocation("Non-Xbox save location not found in game library.");
            }
            else
            {
                if (gameInfo.BaseNonXboxSaveLocation.Contains(Library.GameLibrary.NonSteamProfileMarker))
                {
                    fetchNonXboxProfiles();
                }
                else
                {
                    this.nonXboxProfileListBox.Items.Add("Profiles not defined in game library");
                    this.nonXboxProfileListBox.Enabled = false;


                    if (Directory.Exists(gameInfo.NonXboxSaveLocation))
                    {
                        fetchNonXboxSaveFiles();
                    }
                    else
                    {
                        promptForNonXboxSaveLocation("Non-Xbox save location from library does not exist.");
                    }
                }
            }
        }

        private void promptNonXboxLocationButton_Click(object sender, EventArgs e)
        {
            this.promptForNonXboxSaveLocation(null);
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

                    matchedFiles.Add(this.gameInfo.getXboxFileVersion(this.currentContainer, i));
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

                    matchedFiles.Add(this.gameInfo.getNonXboxFileVersion(i));
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
            currentContainer = new Xbox.XboxContainerIndex(gameInfo, (string)this.xboxProfileListBox.SelectedItem);
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
    }
}
