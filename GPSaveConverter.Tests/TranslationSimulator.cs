using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// Follows the same steps as GameInfo.getNonXboxFileVersion and GameInfo.getXboxFileVersion,
    /// but on plain file tables, so a translation can be checked without a game's save containers.
    /// The matching itself is the application's own code in FileTranslation. Only the few lines that
    /// string those calls together are repeated here; if GameInfo changes them, change this to match.
    /// </summary>
    internal class TranslationSimulator
    {
        internal class XboxFile
        {
            public readonly string ContainerName1;
            public readonly string ContainerName2;
            public readonly string FileID;

            public XboxFile(string containerName1, string containerName2, string fileID)
            {
                ContainerName1 = containerName1;
                ContainerName2 = containerName2;
                FileID = fileID;
            }

            public override string ToString()
            {
                return ContainerName1 + " | " + ContainerName2 + " | " + FileID;
            }
        }

        internal enum Outcome
        {
            /// <summary>No translation matches the file. A transfer stops with "translation not found".</summary>
            NoTranslation,
            /// <summary>The file maps to one that already exists on the other side. A transfer replaces it.</summary>
            ExistingFile,
            /// <summary>The file maps to a name that does not exist on the other side. A transfer creates it.</summary>
            NewFile,
            /// <summary>The Xbox container the file belongs in does not exist. A transfer asks whether to make it, and stops for this file on a no.</summary>
            NoContainer,
            /// <summary>More than one Xbox container matches. A transfer stops with "Ambiguous Xbox container results".</summary>
            AmbiguousContainer
        }

        internal class Result
        {
            public Outcome Outcome;
            /// <summary>Non-Xbox side: the relative path of the file read or written.</summary>
            public string RelativePath;
            /// <summary>Xbox side: the existing file that matched, or the container a new file goes in.</summary>
            public XboxFile XboxFile;
            /// <summary>Xbox side: the ID a new file would be given.</summary>
            public string NewFileID;

            /// <summary>The path on disk. Windows treats repeated separators as one.</summary>
            public string PathOnDisk
            {
                get { return RelativePath == null ? null : Regex.Replace(RelativePath, @"\\+", @"\"); }
            }
        }

        /// <summary>The Xbox profile the simulated save belongs to.</summary>
        internal const string XboxProfileID = "0009000000000001";

        private readonly IList<FileTranslation> translations;
        private readonly IList<XboxFile> xboxFiles;
        private readonly IList<string> nonXboxFiles;

        /// <param name="nonXboxFiles">Paths relative to the save folder, in the order the folder lists them.</param>
        internal TranslationSimulator(IList<FileTranslation> translations, IList<XboxFile> xboxFiles, IList<string> nonXboxFiles)
        {
            this.translations = translations;
            this.xboxFiles = xboxFiles;
            this.nonXboxFiles = nonXboxFiles;
        }

        /// <summary>
        /// Where an Xbox file goes on the non-Xbox side. Mirrors GameInfo.getNonXboxFileVersion.
        /// </summary>
        internal Result ToNonXbox(XboxFile file)
        {
            FileTranslation t = null;
            foreach (FileTranslation candidate in translations)
            {
                candidate.XboxProfileID = XboxProfileID;
                if (Regex.Match(file.ContainerName1, candidate.ContainerName1Regex).Success
                    && Regex.Match(file.ContainerName2, candidate.ContainerName2Regex).Success
                    && Regex.Match(file.FileID, candidate.XboxFileIDRegex).Success)
                {
                    t = candidate;
                    break;
                }
            }
            if (t == null) return new Result { Outcome = Outcome.NoTranslation };

            bool pathComplete;
            string relativePath = t.FillNonXboxFilename(file.ContainerName1, file.ContainerName2, file.FileID, XboxProfileID, out pathComplete);
            string pathAsWritten = t.NonXboxFilenameAsWritten(file.ContainerName1, file.ContainerName2, file.FileID);

            string existingPath = FileTranslation.FindNonXboxFile(nonXboxFiles, relativePath, pathAsWritten);
            if (existingPath != null) return new Result { Outcome = Outcome.ExistingFile, RelativePath = existingPath };

            if (!pathComplete)
            {
                throw new Exception("No substitution data found.");
            }
            return new Result { Outcome = Outcome.NewFile, RelativePath = relativePath };
        }

        /// <summary>
        /// Where a non-Xbox file goes on the Xbox side. Mirrors GameInfo.getXboxFileVersion.
        /// </summary>
        internal Result ToXbox(string relativePath)
        {
            FileTranslation t = null;
            foreach (FileTranslation candidate in translations)
            {
                candidate.XboxProfileID = XboxProfileID;
                if (candidate.NonXboxFilenameRegex != null && Regex.Match(relativePath, candidate.NonXboxFilenameRegex).Success)
                {
                    t = candidate;
                    break;
                }
            }
            if (t == null) return new Result { Outcome = Outcome.NoTranslation };

            List<XboxFile> allContainers = xboxFiles.GroupBy(f => new { f.ContainerName1, f.ContainerName2 }).Select(g => g.First()).ToList();
            List<XboxFile> containers = t.FindContainers(allContainers, c => c.ContainerName1, c => c.ContainerName2, relativePath, XboxProfileID);

            if (containers.Count > 1) return new Result { Outcome = Outcome.AmbiguousContainer };
            if (containers.Count == 0) return new Result { Outcome = Outcome.NoContainer };

            XboxFile container = containers[0];
            List<XboxFile> containerFiles = xboxFiles.Where(f => f.ContainerName1 == container.ContainerName1 && f.ContainerName2 == container.ContainerName2).ToList();

            string xboxFileID;
            bool xboxFileIDComplete;
            XboxFile matchedFile = t.FindXboxFile(containerFiles, f => f.FileID, relativePath, XboxProfileID, out xboxFileID, out xboxFileIDComplete);

            if (matchedFile != null) return new Result { Outcome = Outcome.ExistingFile, XboxFile = matchedFile };
            if (!xboxFileIDComplete)
            {
                throw new Exception("No substitution data found.");
            }
            return new Result { Outcome = Outcome.NewFile, XboxFile = container, NewFileID = xboxFileID };
        }
    }
}
