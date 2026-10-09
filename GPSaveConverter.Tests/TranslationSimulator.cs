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
    /// It copies what the application does today, quirks included. If GameInfo changes, change this to match.
    /// Translations that use ${XboxProfileID} are not supported: resolving it needs a real container.
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
            /// <summary>The Xbox container the file belongs in does not exist. A transfer stops; containers cannot be created.</summary>
            NoContainer,
            /// <summary>More than one Xbox container matches. A transfer stops with "Ambiguous Xbox container results".</summary>
            AmbiguousContainer
        }

        internal class Result
        {
            public Outcome Outcome;
            /// <summary>Non-Xbox side: the relative path exactly as the application builds it.</summary>
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
            FileTranslation t = translations.FirstOrDefault(c => Regex.Match(file.ContainerName1, c.ContainerName1Regex).Success
                                                              && Regex.Match(file.ContainerName2, c.ContainerName2Regex).Success
                                                              && Regex.Match(file.FileID, c.XboxFileIDRegex).Success);
            if (t == null) return new Result { Outcome = Outcome.NoTranslation };

            string relativePath = Regex.Replace(file.FileID, t.XboxFileIDRegex, t.NonXboxFilename);
            relativePath = Regex.Replace(file.ContainerName1, t.ContainerName1Regex, relativePath);
            relativePath = Regex.Replace(file.ContainerName2, t.ContainerName2Regex, relativePath);

            foreach (string existing in nonXboxFiles)
            {
                if (Regex.Match(existing, relativePath).Success)
                {
                    return new Result { Outcome = Outcome.ExistingFile, RelativePath = existing };
                }
            }

            Regex r = new Regex(t.replaceRegex(relativePath));
            if (r.GetGroupNames().Length > 1)
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
            FileTranslation t = translations.FirstOrDefault(c => c.NonXboxFilenameRegex != null && Regex.Match(relativePath, c.NonXboxFilenameRegex).Success);
            if (t == null) return new Result { Outcome = Outcome.NoTranslation };

            string container1Name = t.replaceRegex(FileTranslation.ExactRegex(Regex.Replace(relativePath, t.NonXboxFilenameRegex, t.ContainerName1)));
            string container2Name = t.replaceRegex(FileTranslation.ExactRegex(Regex.Replace(relativePath, t.NonXboxFilenameRegex, t.ContainerName2)));

            List<XboxFile> containers = xboxFiles.GroupBy(f => new { f.ContainerName1, f.ContainerName2 })
                                                 .Select(g => g.First())
                                                 .Where(c => Regex.Match(c.ContainerName1, container1Name).Success && Regex.Match(c.ContainerName2, container2Name).Success)
                                                 .ToList();

            if (containers.Count > 1) return new Result { Outcome = Outcome.AmbiguousContainer };
            if (containers.Count == 0) return new Result { Outcome = Outcome.NoContainer };

            XboxFile container = containers[0];
            string xboxFileID = Regex.Escape(Regex.Replace(relativePath, t.NonXboxFilenameRegex, t.XboxFileID));

            XboxFile matchedFile = xboxFiles.Where(f => f.ContainerName1 == container.ContainerName1 && f.ContainerName2 == container.ContainerName2)
                                            .Where(f => Regex.Match(f.FileID, FileTranslation.ExactRegex(t.replaceRegex(xboxFileID))).Success)
                                            .FirstOrDefault();

            if (matchedFile != null) return new Result { Outcome = Outcome.ExistingFile, XboxFile = matchedFile };
            return new Result { Outcome = Outcome.NewFile, XboxFile = container, NewFileID = xboxFileID };
        }
    }
}
