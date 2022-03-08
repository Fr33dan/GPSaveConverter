using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter
{
    internal class FileInfo
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
        internal const int EncodedPathByteLength = 128;
        internal const int FileNameDataLength = 16;
        internal const int EntryByteLength = EncodedPathByteLength + FileNameDataLength + FileNameDataLength;
        string EncodedPath;
        byte[] FileCode;
        ContainerManager parent;

        internal FileInfo(ContainerManager parent, byte[] sourceFile,int index)
        {
            this.parent = parent;
            EncodedPath = Encoding.Unicode.GetString(sourceFile, index, EncodedPathByteLength).Trim('\0');

            FileCode = new byte[FileNameDataLength];
            Array.Copy(sourceFile, index + EncodedPathByteLength, FileCode, 0, FileNameDataLength);

            byte[] fileNameDuplicate = new byte[FileNameDataLength];
            Array.Copy(sourceFile, index + EncodedPathByteLength + FileNameDataLength, fileNameDuplicate, 0, FileNameDataLength);

            if (!FileCode.SequenceEqual(fileNameDuplicate))
            {
                throw new FileFormatException();
            } else if (!File.Exists(Path.Combine(parent.getSaveFilePath(), this.getFileName())))
            {
                throw new FileNotFoundException("Could not find file for " + this.GetRelativeFilePath() + " (" + getFileName() + ")");
            }
        }
        internal FileInfo(ContainerManager parent,string filePath, string relativeFilePath)
        {
            this.parent=parent;

            this.EncodedPath = relativeFilePath;
            this.FileCode = GetChecksum(filePath);
            File.Copy(filePath, getFilePath());
        }

        internal void Write(Stream s)
        {
            byte[] pathData = new byte[EncodedPathByteLength];
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
            string filePath;
            byte[] firstSectionData = new byte[4];
            Array.Copy(FileCode, 0, firstSectionData, 0, firstSectionData.Length);
            string firstSection = BitConverter.ToString(firstSectionData.Reverse().ToArray()).Replace("-", "");

            byte[] secondSectionData = new byte[2];
            Array.Copy(FileCode, 4, secondSectionData, 0, secondSectionData.Length);
            string secondSection = BitConverter.ToString(secondSectionData.Reverse().ToArray()).Replace("-", "");

            byte[] thirdSectionData = new byte[2];
            Array.Copy(FileCode, 6, thirdSectionData, 0, thirdSectionData.Length);
            string thirdSection = BitConverter.ToString(thirdSectionData.Reverse().ToArray()).Replace("-", "");

            byte[] fourthSectionData = new byte[8];
            Array.Copy(FileCode, 8, fourthSectionData, 0, fourthSectionData.Length);
            string fourthSection = BitConverter.ToString(fourthSectionData.ToArray()).Replace("-", "");

            filePath = string.Concat(firstSection, secondSection, thirdSection, fourthSection);
            return filePath;
        }

        /// <summary>
        /// Gets the encoded file path relative to the parent save file folder structure.
        /// </summary>
        /// <returns></returns>
        public string GetRelativeFilePath()
        {
            return EncodedPath;
        }
    }
}
