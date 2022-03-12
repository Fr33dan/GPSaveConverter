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
        Xbox.XboxContainerIndex currentContainer;
        Library.GameInfo gameInfo;
        string profileID = string.Empty;
        bool nonXboxFetchReady = false;
        List<NonXboxFileInfo> nonXboxFiles;

        public SaveFileConverterForm()
        {
            InitializeComponent();
        }

        private string expandedNonXboxFilesLocation()
        {
            return gameInfo.NonXboxSaveLocation.Replace(Library.GameLibrary.NonSteamProfileMarker, this.profileID);

        }

        private void fetchNonXboxSaveFiles()
        {
            nonXboxFiles = new List<NonXboxFileInfo>();
            string fetchLocation = expandedNonXboxFilesLocation();

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
            DialogResult res = MessageBox.Show(this, reason + " Please select save file location.", "Non-Xbox save location not found", MessageBoxButtons.OKCancel);
            if (res == DialogResult.OK)
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                res = dialog.ShowDialog();
                if (res == DialogResult.OK)
                {
                    gameInfo.NonXboxSaveLocation = dialog.SelectedPath;
                    fetchNonXboxSaveFiles();
                    return true;
                }
                else return false;
            }
            else return false;
        }

        private void fetchProfiles()
        {
            string profilesDir = gameInfo.NonXboxSaveLocation.Substring(0, gameInfo.NonXboxSaveLocation.IndexOf(Library.GameLibrary.NonSteamProfileMarker));
            bool failed = false;

            if (Directory.Exists(profilesDir))
            {
                foreach (string p in Directory.GetDirectories(profilesDir))
                {
                    string testProfileID = p.Replace(profilesDir, "");

                    string testProfileSaveFolder = gameInfo.NonXboxSaveLocation.Replace(Library.GameLibrary.NonSteamProfileMarker, testProfileID);

                    if (Directory.Exists(testProfileSaveFolder))
                    {
                        this.profileListBox.Items.Add(testProfileID);
                    }
                }
                if (profileListBox.Items.Count == 0)
                {
                    failed = true;
                }
                else
                {
                    this.profileListBox.Enabled = true;
                    if (this.profileListBox.Items.Count == 1)
                    {
                        this.profileListBox.SelectedItem = this.profileListBox.Items[0];
                    }
                }
            }
            else
            {
                failed = true;
            }

            if (failed) 
            {
                this.profileListBox.Items.Add("No profiles found");
                promptForNonXboxSaveLocation("Game library defines non-Xbox profiles, but none were found.");
            }

        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            this.infoStatusLabel.Text = "Loading game info...";

            Library.GameInfo[] gameInfo = await LoadGameInfo();

            this.packagesDataGridView.Height = gameInfo.Length * 75 + 10;

            this.packagesDataGridView.DataSource = gameInfo;

            this.infoStatusLabel.Text = "Load Successful";
        }

        private async Task<Library.GameInfo[]> LoadGameInfo()
        {
            Library.GameInfo[] result = null;
            await Task.Run(() => result = Xbox.XboxPackageList.GetList());
            return result;
        }


        private void packagesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void ClearForm()
        {
            this.foldersToolTip.RemoveAll();

            this.viewXboxFilesButton.Enabled = false;
            this.viewNonXboxFileButton.Enabled = false;
            this.profileListBox.Items.Clear();
            this.profileListBox.Enabled = false;
            this.xboxFilesTable.DataSource = null;
            this.xboxFilesTable.Columns.Clear();
            this.xboxFilesTable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { this.File,
                                                                                                 this.LastEdit});
            this.nonXboxFilesTable.DataSource = null;
            this.nonXboxFilesTable.Columns.Clear();
            this.nonXboxFilesTable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { this.dataGridViewTextBoxColumn1,
                                                                                                  this.FullPath,
                                                                                                  this.dataGridViewTextBoxColumn2 });
        }

        private void profileListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.currentContainer != null)
            {
                this.profileID = (string)this.profileListBox.SelectedItem;

                fetchNonXboxSaveFiles();
            }
        }

        private void moveFilesToXbox(System.Collections.IEnumerable rows)
        {
            DialogResult res = MessageBox.Show(this, "This could overwrite files in your Xbox save data which cannot be undone. Are you sure?", "Are you sure?", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                string nonXboxParentLocation = expandedNonXboxFilesLocation();
                foreach (DataGridViewRow row in rows)
                {
                    NonXboxFileInfo file = row.DataBoundItem as NonXboxFileInfo;
                    currentContainer.Children[0].AddFile(file.RelativePath, nonXboxParentLocation);
                }
            }
        }

        private void moveFilesFromXbox(System.Collections.IEnumerable rows)
        {
            if(!this.nonXboxFetchReady)
            {
                MessageBox.Show(this, "Please configure non-Xbox save file location");
                return;
            }

            DialogResult res = MessageBox.Show(this, "This could overwrite save files in your non-Xbox save data which cannot be undone. Are you sure?", "Are you sure?", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                string nonXboxParentLocation = expandedNonXboxFilesLocation();
                foreach (DataGridViewRow row in rows)
                {
                    Xbox.XboxFileInfo file = row.DataBoundItem as Xbox.XboxFileInfo;
                    System.IO.File.Copy(file.getFilePath(), Path.Combine(this.expandedNonXboxFilesLocation(), file.GetRelativeFilePath()), true);
                }
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
            System.Diagnostics.Process.Start(expandedNonXboxFilesLocation());
        }

        private void packagesDataGridView_Click(object sender, EventArgs e)
        {
            ClearForm();
            this.nonXboxFetchReady = false;

            gameInfo = (Library.GameInfo)this.packagesDataGridView.SelectedRows[0].DataBoundItem;
            currentContainer = new Xbox.XboxContainerIndex(gameInfo.PackageName, "0009000000293F71");

            this.viewXboxFilesButton.Enabled = true;
            this.foldersToolTip.SetToolTip(this.xboxFileLabel, currentContainer.Children[0].getSaveFilePath());
            this.xboxFilesTable.DataSource = currentContainer.Children[0].getFileList();

            if (gameInfo.NonXboxSaveLocation == null || gameInfo.NonXboxSaveLocation == string.Empty)
            {
                promptForNonXboxSaveLocation("Non-Xbox save location not found in game library.");
            }
            else
            {
                if (gameInfo.NonXboxSaveLocation.Contains(Library.GameLibrary.NonSteamProfileMarker))
                {
                    fetchProfiles();
                }
                else
                {
                    this.profileListBox.Items.Add("Profiles not defined in game library");
                    this.profileListBox.Enabled = false;


                    if (Directory.Exists(expandedNonXboxFilesLocation()))
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
    }
}
