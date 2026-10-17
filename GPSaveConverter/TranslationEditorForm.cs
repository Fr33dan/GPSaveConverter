using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GPSaveConverter.Library;
using XboxFile = GPSaveConverter.Library.TranslationSimulator.XboxFile;

namespace GPSaveConverter
{
    /// <summary>
    /// Shows a file translation in plain words and lets the user change it. The pairs it makes of
    /// the files listed in the main window are shown as the names are typed.
    /// </summary>
    public partial class TranslationEditorForm : Form
    {
        private readonly PlainTranslation words;
        private readonly IList<XboxFile> xboxFiles;
        private readonly IList<string> nonXboxFiles;
        private readonly string xboxProfileID;

        /// <summary>The translation the words describe, once OK is pressed.</summary>
        internal FileTranslation Translation { get; private set; }

        internal TranslationEditorForm(PlainTranslation words, IList<XboxFile> xboxFiles, IList<string> nonXboxFiles, string xboxProfileID)
        {
            InitializeComponent();
            this.words = words;
            this.xboxFiles = xboxFiles;
            this.nonXboxFiles = nonXboxFiles;
            this.xboxProfileID = xboxProfileID;

            List<string> own = words.OwnPlaceholders.ToList();
            this.introLabel.Text = "Write each name as it is. Where it changes from file to file, put a placeholder in its place: "
                + "{number} for a number, {name} for a name without folders, or {profile} for the Xbox profile's ID. "
                + "A placeholder on both sides carries its value across."
                + (own.Count == 0 ? string.Empty : " This translation also has its own: " + string.Join(", ", own.Select(p => "{" + p + "}")) + ".");

            // Each box would otherwise read all four back into the words as soon as it is filled,
            // while the ones after it are still empty.
            filling = true;
            this.container1Box.Text = words.ContainerName1;
            this.container2Box.Text = words.ContainerName2;
            this.blobBox.Text = words.BlobName;
            this.fileBox.Text = words.FileName;
            filling = false;
            showPreview();
        }

        private bool filling;

        private void nameBox_TextChanged(object sender, EventArgs e)
        {
            if (!filling)
            {
                showPreview();
            }
        }

        private FileTranslation translationTyped(out string problem)
        {
            words.ContainerName1 = this.container1Box.Text;
            words.ContainerName2 = this.container2Box.Text;
            words.BlobName = this.blobBox.Text;
            words.FileName = this.fileBox.Text;
            return words.ToTranslation(out problem);
        }

        private void showPreview()
        {
            string problem;
            FileTranslation translation = translationTyped(out problem);
            PlainTranslation.Preview preview = null;
            if (translation != null)
            {
                try
                {
                    preview = PlainTranslation.PreviewOf(translation, xboxFiles, nonXboxFiles, xboxProfileID);
                }
                catch (Exception e)
                {
                    problem = "The names cannot be tried on the files: " + e.Message;
                }
            }

            this.previewList.BeginUpdate();
            this.previewList.Items.Clear();
            if (preview != null)
            {
                this.previewList.Items.AddRange(preview.Lines.ToArray());
            }
            this.previewList.EndUpdate();

            this.summaryLabel.Text = preview == null ? string.Empty
                : "It covers " + preview.XboxFilesCovered + " of the " + countFiles(xboxFiles.Count, "Xbox") + " and "
                    + preview.NonXboxFilesCovered + " of the " + countFiles(nonXboxFiles.Count, "") + " on this PC.";
            this.problemLabel.Text = problem ?? string.Empty;
            this.okButton.Enabled = preview != null;
        }

        private static string countFiles(int count, string kind)
        {
            return count + " " + (kind.Length > 0 ? kind + " " : string.Empty) + (count == 1 ? "file" : "files");
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            string problem;
            FileTranslation translation = translationTyped(out problem);
            if (translation == null)
            {
                this.problemLabel.Text = problem;
                return;
            }

            this.Translation = translation;
            this.DialogResult = DialogResult.OK;
            Close();
        }
    }
}
