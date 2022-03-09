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
        ContainerManager currentContainer;
        GameInfo gameInfo;
        string profileID = string.Empty;
        bool nonXboxFetchReady = false;
        List<NonXboxFileInfo> nonXboxFiles;

        public SaveFileConverterForm()
        {
            InitializeComponent();
        }

        private string expandedNonXboxFilesLocation()
        {
            return gameInfo.NonXboxSaveLocation.Replace(GameLibrary.NonSteamProfileMarker, this.profileID);

        }

        private void fetchNonXboxSaveFiles()
        {
            nonXboxFiles = new List<NonXboxFileInfo>();
            string fetchLocation = expandedNonXboxFilesLocation();
            fetchNonXboxSaveFiles(fetchLocation, fetchLocation);
            this.nonXboxFilesTable.DataSource = nonXboxFiles;
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

        private void fetchProfiles()
        {
            string profilesDir = gameInfo.NonXboxSaveLocation.Substring(0, gameInfo.NonXboxSaveLocation.IndexOf(GameLibrary.NonSteamProfileMarker));

            foreach(string p in Directory.GetDirectories(profilesDir))
            {
                this.profileListBox.Items.Add(p.Replace(profilesDir, ""));
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.packagesListBox.Items.AddRange(XboxPackageList.GetList());
        }

        private void packagesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearForm();
            this.nonXboxFetchReady = false;

            gameInfo = (GameInfo)this.packagesListBox.SelectedItem;
            currentContainer = new ContainerManager(gameInfo.PackageName);

            this.xboxFilesTable.DataSource = currentContainer.getFileList();

            if (gameInfo.NonXboxSaveLocation == null || gameInfo.NonXboxSaveLocation == string.Empty)
            {
                DialogResult res = MessageBox.Show(this, "Non-Xbox save location not found in library. Please select save file location.", "Non-Xbox save location not found", MessageBoxButtons.OKCancel);
                if (res == DialogResult.OK)
                {
                    FolderBrowserDialog dialog = new FolderBrowserDialog();
                    res = dialog.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        gameInfo.NonXboxSaveLocation = dialog.SelectedPath;
                    }
                    else return;
                }
                else return;
            }

            if (gameInfo.NonXboxSaveLocation.Contains(GameLibrary.NonSteamProfileMarker))
            {
                this.profileListBox.Enabled = true;
                fetchProfiles();
            }
            else
            {
                this.profileListBox.Items.Add("Profiles not defined in game library");
                this.profileListBox.Enabled = false;
                this.nonXboxFetchReady = true;
                fetchNonXboxSaveFiles();
            }
        }

        private void ClearForm()
        {
            this.profileListBox.Items.Clear();
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
                this.nonXboxFetchReady = true;
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
                    this.currentContainer.AddFile(file.RelativePath, nonXboxParentLocation);
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
                    XboxFileInfo file = row.DataBoundItem as XboxFileInfo;
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
    }
}
