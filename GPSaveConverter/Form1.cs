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
    public partial class Form1 : Form
    {
        ContainerManager currentContainer;

        string sourceParentFolder = @"C:\Users\Joseph\AppData\LocalLow\Two Point Studios\Two Point Hospital\Cloud\15618986\";
        public Form1()
        {
            InitializeComponent();

        }

        private void sourceFiles(string folder)
        {
            foreach(string file in Directory.GetFiles(folder))
            {
                this.sourceFileListBox.Items.Add(file.Replace(sourceParentFolder, ""));
            }

            foreach(string dir in Directory.GetDirectories(folder))
            {
                sourceFiles(dir);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.packagesListBox.Items.AddRange(GameList.GetList());
            sourceFiles(sourceParentFolder);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentContainer = new ContainerManager((string)this.packagesListBox.SelectedItem);
            containerFileDisplayBox.Clear();
            foreach(FileInfo i in currentContainer.getFileList())
            {
                containerFileDisplayBox.AppendText(i.GetRelativeFilePath() + " => " + i.getFileName() + Environment.NewLine);
            }
            this.indexTextBox.Text = currentContainer.getIndexText();
        }

        private void insertButton_Click(object sender, EventArgs e)
        {
            if (this.currentContainer == null)
            {
                MessageBox.Show(this, "Select a game profile");
            }
            else
            {
                foreach (string item in this.sourceFileListBox.SelectedItems)
                {
                    this.currentContainer.AddFile(item, sourceParentFolder);
                }
            }
        }

        private void selectAllButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < sourceFileListBox.Items.Count; i++)
            {
                sourceFileListBox.SetSelected(i, true);
            }
        }
    }
}
