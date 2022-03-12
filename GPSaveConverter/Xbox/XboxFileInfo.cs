using System;
using System.Collections.Generic;
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
        string encodedPath;
        byte[] FileCode;
        DateTime timestamp;
        XboxFileContainer parent;

        public string EncodedPath { get { return encodedPath; } }

        public DateTime Timestamp { get { return timestamp; } }

        internal XboxFileInfo(XboxFileContainer parent, byte[] sourceFile,int index)
        {
            this.parent = parent;
            encodedPath = Encoding.Unicode.GetString(sourceFile, index, XboxHelper.EncodedPathByteLength).Trim('\0');

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
        internal XboxFileInfo(XboxFileContainer parent,string filePath, string relativeFilePath)
        {
            this.parent=parent;

            this.encodedPath = relativeFilePath;
            this.FileCode = GetChecksum(filePath);
            File.Copy(filePath, getFilePath());
        }

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
            return encodedPath;
        }
    }
}
