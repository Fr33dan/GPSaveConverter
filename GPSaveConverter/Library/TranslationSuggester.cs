using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using XboxFile = GPSaveConverter.Library.TranslationSimulator.XboxFile;
using Outcome = GPSaveConverter.Library.TranslationSimulator.Outcome;

namespace GPSaveConverter.Library
{
    /// <summary>
    /// Proposes a translation for a game from the files it has on both sides. It tries the shapes
    /// most of the game library's entries have, and keeps the one that pairs the most files, each
    /// pair found from both sides.
    /// </summary>
    internal static class TranslationSuggester
    {
        internal class Suggestion
        {
            public FileTranslation Translation;

            /// <summary>Each Xbox file the translation pairs with a file on the other side, with that file's path.</summary>
            public List<KeyValuePair<XboxFile, string>> Pairs;

            /// <summary>Files on the other side the translation covers that the Xbox save has nothing for.</summary>
            public List<string> Unpaired;
        }

        // Steam keeps a file of its own in many save folders. It is never a save.
        private const string FileNameGroup = "(?<FileName>(?!steam_autocloud\\.vdf$)[^\\\\/]+)";
        private const string FolderGroup = "(?<Folder>[^\\\\/]+)";

        /// <returns>The best translation, or null if none of them pairs a single file.</returns>
        internal static Suggestion Suggest(IList<XboxFile> xboxFiles, IList<string> nonXboxFiles, string xboxProfileID)
        {
            Suggestion best = null;
            foreach (FileTranslation candidate in Candidates(xboxFiles, nonXboxFiles))
            {
                Suggestion tried;
                try
                {
                    tried = Try(candidate, xboxFiles, nonXboxFiles, xboxProfileID);
                }
                catch (Exception)
                {
                    // A name that makes no valid pattern, or a path with nothing to fill in. Not a candidate.
                    continue;
                }

                // Only more pairs win, so of two that pair as many, the simpler shape, tried first, stays.
                if (tried.Pairs.Count > 0 && (best == null || tried.Pairs.Count > best.Pairs.Count))
                {
                    best = tried;
                }
            }
            return best;
        }

        private static Suggestion Try(FileTranslation candidate, IList<XboxFile> xboxFiles, IList<string> nonXboxFiles, string xboxProfileID)
        {
            TranslationSimulator game = new TranslationSimulator(new[] { candidate }, xboxFiles, nonXboxFiles, xboxProfileID);

            List<KeyValuePair<XboxFile, string>> pairs = new List<KeyValuePair<XboxFile, string>>();
            foreach (XboxFile file in xboxFiles)
            {
                TranslationSimulator.Result toNonXbox = game.ToNonXbox(file);
                if (toNonXbox.Outcome != Outcome.ExistingFile)
                {
                    continue;
                }

                // The same pair has to come out from the other side as well.
                TranslationSimulator.Result back = game.ToXbox(toNonXbox.RelativePath);
                if (back.Outcome == Outcome.ExistingFile && back.XboxFile == file)
                {
                    pairs.Add(new KeyValuePair<XboxFile, string>(file, toNonXbox.RelativePath));
                }
            }

            List<string> unpaired = new List<string>();
            if (pairs.Count > 0)
            {
                HashSet<string> paired = new HashSet<string>(pairs.Select(p => p.Value), StringComparer.OrdinalIgnoreCase);
                foreach (string path in nonXboxFiles.Where(p => !paired.Contains(p)))
                {
                    Outcome outcome = game.ToXbox(path).Outcome;
                    if (outcome == Outcome.NewFile || outcome == Outcome.NoContainer)
                    {
                        unpaired.Add(path);
                    }
                }
            }

            return new Suggestion { Translation = candidate, Pairs = pairs, Unpaired = unpaired };
        }

        /// <summary>
        /// The translations worth trying, simplest first. The names they spell out come from the files.
        /// </summary>
        private static IEnumerable<FileTranslation> Candidates(IList<XboxFile> xboxFiles, IList<string> nonXboxFiles)
        {
            // An ending the other side adds to each name, if any: ".sav" where the Xbox save has "Save00".
            List<string> endings = new List<string> { String.Empty };
            endings.AddRange(nonXboxFiles.Select(p => Path.GetExtension(p))
                .Where(e => e.Length > 1)
                .GroupBy(e => e, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(3));

            // The same name for the file, its container and its blob.
            foreach (string containerName2 in new[] { "${FileName}", String.Empty })
            {
                yield return Translation("${FileName}", "${FileName}", containerName2, "${FileName}", FileNameGroup);
            }

            // One container, holding every file as a blob of the file's name.
            foreach (XboxFile container in xboxFiles.GroupBy(f => new { f.ContainerName1, f.ContainerName2 }).OrderByDescending(g => g.Count()).Take(3).Select(g => g.First()))
            {
                foreach (string ending in endings)
                {
                    yield return Translation("${FileName}" + Literal(ending), Literal(container.ContainerName1), Literal(container.ContainerName2), "${FileName}", FileNameGroup);
                }
            }

            // A container for each file, named after it, holding a blob of the same name in each.
            foreach (string blob in xboxFiles.GroupBy(f => f.FileID).Where(g => g.Count() > 1).OrderByDescending(g => g.Count()).Take(3).Select(g => g.Key))
            {
                foreach (string containerName2 in new[] { "${FileName}", String.Empty })
                {
                    foreach (string ending in endings)
                    {
                        yield return Translation("${FileName}" + Literal(ending), "${FileName}", containerName2, Literal(blob), FileNameGroup);
                    }
                }
            }

            // A folder for each container, holding a file for each of its blobs.
            foreach (string containerName2 in new[] { "${Folder}", String.Empty })
            {
                foreach (string ending in endings)
                {
                    yield return Translation("${Folder}\\\\${FileName}" + Literal(ending), "${Folder}", containerName2, "${FileName}", FolderGroup, FileNameGroup);
                }
            }
        }

        private static FileTranslation Translation(string nonXboxFilename, string containerName1, string containerName2, string xboxFileID, params string[] namedRegexGroups)
        {
            return new FileTranslation
            {
                NonXboxFilename = nonXboxFilename,
                ContainerName1 = containerName1,
                ContainerName2 = containerName2,
                XboxFileID = xboxFileID,
                NamedRegexGroups = namedRegexGroups
            };
        }

        /// <summary>
        /// A name written so that a translation's pattern matches it alone. A dot stays as it is, the
        /// way the game library writes one.
        /// </summary>
        private static string Literal(string name)
        {
            StringBuilder literal = new StringBuilder();
            foreach (char c in name)
            {
                if ("\\*+?|{}[]()^$#".IndexOf(c) != -1)
                {
                    literal.Append('\\');
                }
                literal.Append(c);
            }
            return literal.ToString();
        }
    }
}
