using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter.Xbox
{
    internal class XboxFileInfo
    {
        public static byte[] GetChecksum(string filename)
        {
            using (System.Security.Cryptography.HashAlgorithm hasher = System.Security.Cryptography.MD5.Create())
            {
                using (Stream stream = System.IO.File.OpenRead(filename))
                {
                    return hasher.ComputeHash(stream);
                }
            }
        }
        string fileID;
        byte[] FileCode;
        DateTime timestamp;
        XboxFileContainer parent;

        [Browsable(false)]
        public XboxFileContainer Parent { get { return parent; } }

        [Display(Order = 1), DisplayName("Container Name 1")]
        public string ContainerName1 { get { return parent.ContainerID[0]; } }
        [Display(Order = 2), DisplayName("Container Name 1")]
        public string ContainerName2 { get { return parent.ContainerID[1]; } }

        /// <summary>
        /// File identifier seen in container table.
        /// </summary>
        [Display(Name = "File ID", Order = 3), DisplayName("File ID")]
        public string FileID { get { return fileID; } }


        [Display(Order = 4), DisplayName("Last Modified")]
        public DateTime Timestamp { get { return timestamp; } }

        internal XboxFileInfo(XboxFileContainer parent, byte[] sourceFile,int index)
        {
            this.parent = parent;
            fileID = Encoding.Unicode.GetString(sourceFile, index, XboxHelper.EncodedPathByteLength).Trim('\0');

            FileCode = new byte[XboxHelper.FileNameDataLength];
            Array.Copy(sourceFile, index + XboxHelper.EncodedPathByteLength, FileCode, 0, XboxHelper.FileNameDataLength);

            byte[] fileNameDuplicate = new byte[XboxHelper.FileNameDataLength];
            Array.Copy(sourceFile, index + XboxHelper.EncodedPathByteLength + XboxHelper.FileNameDataLength, fileNameDuplicate, 0, XboxHelper.FileNameDataLength);

            if (!FileCode.SequenceEqual(fileNameDuplicate))
            {
                //throw new FileFormatException();
            } else if (!File.Exists(getFilePath()))
            {
                throw new FileNotFoundException("Could not find file for " + this.GetRelativeFilePath() + " (" + getFileName() + ")");
            }
            this.timestamp = File.GetLastWriteTime(getFilePath());
        }
        internal XboxFileInfo(XboxFileContainer parent,string filePath, string xboxFileID)
        {
            this.parent=parent;

            this.fileID = xboxFileID;
            this.FileCode = GetChecksum(filePath);
            File.Copy(filePath, getFilePath());
        }

        /// <summary>
        /// Replace this Xbox file with the specified file.
        /// </summary>
        /// <param name="replacement"></param>
        internal void Replace(NonXboxFileInfo replacement)
        {
            File.Copy(replacement.FilePath, this.getFilePath(), true);
        }

        /// <summary>
        /// Write the file's entry in the container.
        /// </summary>
        /// <param name="s">Stream to container file.</param>
        internal void Write(Stream s)
        {
            byte[] pathData = new byte[XboxHelper.EncodedPathByteLength];
            Encoding.Unicode.GetBytes(GetRelativeFilePath(), 0, GetRelativeFilePath().Length, pathData, 0);

            s.Write(pathData, 0, pathData.Length);

            s.Write(FileCode, 0, FileCode.Length);
            s.Write(FileCode, 0, FileCode.Length);
        }

        /// <summary>
        /// Get the path of the actual save file on the drive.
        /// </summary>
        /// <returns></returns>
        public string getFilePath()
        {
            return Path.Combine(parent.getSaveFilePath(), this.getFileName());
        }

        public string getFileName()
        {
            return XboxHelper.DecodeFileName(FileCode);
        }

        /// <summary>
        /// Gets the encoded file path relative to the parent save file folder structure.
        /// </summary>
        /// <returns></returns>
        public string GetRelativeFilePath()
        {
            return fileID;
        }
    }
}
