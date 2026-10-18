using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using XboxFile = GPSaveConverter.Library.TranslationSimulator.XboxFile;

namespace GPSaveConverter.Library
{
    /// <summary>
    /// A file translation in plain words, as the translation editor shows it: each name written as it
    /// is, with a placeholder such as {number} where it changes from file to file. A placeholder used
    /// on both sides carries its value across. Only a translation whose names are plain text and
    /// placeholders can be shown this way; a pattern such as ".*" in a name cannot.
    /// </summary>
    internal class PlainTranslation
    {
        /// <summary>The placeholders any translation can use, with the pattern each stands for.</summary>
        internal static readonly IReadOnlyDictionary<string, string> BuiltIn = new Dictionary<string, string>
        {
            { "number", "[0-9]+" },
            { "name", @"[^\\/]+" },
        };

        /// <summary>Stands for the Xbox profile's ID, as ${XboxProfileID} does in a translation.</summary>
        internal const string Profile = "profile";

        public string ContainerName1 = string.Empty;
        public string ContainerName2 = string.Empty;
        public string BlobName = string.Empty;
        public string FileName = string.Empty;

        /// <summary>
        /// The translation's own named groups, kept as they are written, so that editing a name leaves
        /// what a placeholder matches as it was.
        /// </summary>
        private readonly Dictionary<string, string> ownGroups = new Dictionary<string, string>();

        /// <summary>The placeholders that come from the translation itself, such as {FileSlot}.</summary>
        internal IEnumerable<string> OwnPlaceholders { get { return ownGroups.Keys; } }

        /// <summary>
        /// The translation in plain words.
        /// </summary>
        /// <returns>Null if a name in it is a pattern that plain words cannot show.</returns>
        internal static PlainTranslation From(FileTranslation translation)
        {
            if (translation == null || translation.NamedRegexGroups == null || translation.ContainerName1 == null
                || translation.ContainerName2 == null || translation.XboxFileID == null || translation.NonXboxFilename == null)
            {
                return null;
            }

            PlainTranslation plain = new PlainTranslation();
            foreach (string group in translation.NamedRegexGroups)
            {
                string name;
                try
                {
                    // The same name the translation substitutes.
                    name = new Regex(group).GetGroupNames().Last();
                }
                catch (ArgumentException)
                {
                    return null;
                }

                // A group with no name of its own, or one that would read as the profile.
                if (Regex.IsMatch(name, "^[0-9]+$") || name.Equals(Profile, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                if (!plain.ownGroups.ContainsKey(name))
                {
                    plain.ownGroups.Add(name, group);
                }
            }

            plain.ContainerName1 = plain.wordsFor(translation.ContainerName1);
            plain.ContainerName2 = plain.wordsFor(translation.ContainerName2);
            plain.BlobName = plain.wordsFor(translation.XboxFileID);
            plain.FileName = plain.wordsFor(translation.NonXboxFilename);
            if (plain.ContainerName1 == null || plain.ContainerName2 == null || plain.BlobName == null || plain.FileName == null)
            {
                return null;
            }
            return plain;
        }

        /// <summary>
        /// The translation these words describe.
        /// </summary>
        /// <param name="problem">What is wrong with the words, worded for the user.</param>
        /// <returns>Null if something is wrong with the words.</returns>
        internal FileTranslation ToTranslation(out string problem)
        {
            if (string.IsNullOrWhiteSpace(ContainerName1))
            {
                problem = "Give the Xbox container's name.";
                return null;
            }
            if (string.IsNullOrWhiteSpace(BlobName))
            {
                problem = "Give the Xbox blob's name.";
                return null;
            }
            if (string.IsNullOrWhiteSpace(FileName))
            {
                problem = "Give the file's name on this PC.";
                return null;
            }

            List<string> groups = new List<string>();
            string containerName1 = patternFor(ContainerName1, groups, out problem);
            string containerName2 = problem == null ? patternFor(ContainerName2 ?? string.Empty, groups, out problem) : null;
            string xboxFileID = problem == null ? patternFor(BlobName, groups, out problem) : null;
            string nonXboxFilename = problem == null ? patternFor(FileName, groups, out problem) : null;
            if (problem != null)
            {
                return null;
            }

            return new FileTranslation
            {
                ContainerName1 = containerName1,
                ContainerName2 = containerName2,
                XboxFileID = xboxFileID,
                NonXboxFilename = nonXboxFilename,
                NamedRegexGroups = groups.ToArray()
            };
        }

        /// <summary>A name of a translation in plain words, or null if it is a pattern.</summary>
        private string wordsFor(string template)
        {
            StringBuilder words = new StringBuilder();
            int position = 0;
            foreach (Match substitution in Regex.Matches(template, @"\$\{(\w+)\}"))
            {
                string text = plainText(template.Substring(position, substitution.Index - position));
                if (text == null)
                {
                    return null;
                }
                words.Append(text);

                string name = substitution.Groups[1].Value;
                if (name == "XboxProfileID")
                {
                    words.Append("{" + Profile + "}");
                }
                else if (ownGroups.ContainsKey(name))
                {
                    words.Append("{" + name + "}");
                }
                else
                {
                    // ${XboxProfileID_Int}, or a group the translation does not have.
                    return null;
                }
                position = substitution.Index + substitution.Length;
            }

            string rest = plainText(template.Substring(position));
            return rest == null ? null : words.Append(rest).ToString();
        }

        /// <summary>
        /// Pattern text read as the characters it stands for, or null if it is more than that. A dot
        /// stands for itself, the way the game library writes one.
        /// </summary>
        private static string plainText(string pattern)
        {
            const string special = "\\*+?|{}[]()^$#";
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                if (c == '\\')
                {
                    if (i + 1 < pattern.Length && (special + ".").IndexOf(pattern[i + 1]) >= 0)
                    {
                        text.Append(pattern[++i]);
                        continue;
                    }
                    // \d, \w and the like are patterns.
                    return null;
                }
                if (special.IndexOf(c) >= 0)
                {
                    return null;
                }
                text.Append(c);
            }

            // Braces in plain words mark a placeholder, so a name with one in it cannot be written.
            return text.ToString().IndexOfAny(new[] { '{', '}' }) >= 0 ? null : text.ToString();
        }

        /// <summary>A name in plain words as a translation writes it.</summary>
        /// <param name="groups">The named groups used so far. A placeholder seen for the first time adds its own.</param>
        private string patternFor(string words, List<string> groups, out string problem)
        {
            problem = null;
            StringBuilder pattern = new StringBuilder();
            HashSet<string> usedHere = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int position = 0;
            foreach (Match placeholder in Regex.Matches(words, @"\{([^{}]*)\}"))
            {
                if (!appendText(pattern, words.Substring(position, placeholder.Index - position), out problem))
                {
                    return null;
                }

                string name = placeholder.Groups[1].Value.Trim();
                if (!usedHere.Add(name))
                {
                    problem = "{" + name + "} is in \"" + words + "\" twice. A placeholder can be used once in each name.";
                    return null;
                }

                if (name.Equals(Profile, StringComparison.OrdinalIgnoreCase))
                {
                    pattern.Append("${XboxProfileID}");
                }
                else if (ownGroups.ContainsKey(name))
                {
                    pattern.Append("${" + name + "}");
                    addGroup(groups, ownGroups[name]);
                }
                else if (BuiltIn.ContainsKey(name.ToLowerInvariant()))
                {
                    string builtIn = name.ToLowerInvariant();
                    pattern.Append("${" + builtIn + "}");
                    addGroup(groups, "(?<" + builtIn + ">" + BuiltIn[builtIn] + ")");
                }
                else
                {
                    problem = "{" + name + "} is not a placeholder. Use " + string.Join(", ", AllPlaceholders().Select(p => "{" + p + "}")) + ".";
                    return null;
                }
                position = placeholder.Index + placeholder.Length;
            }

            return appendText(pattern, words.Substring(position), out problem) ? pattern.ToString() : null;
        }

        private static bool appendText(StringBuilder pattern, string text, out string problem)
        {
            if (text.IndexOfAny(new[] { '{', '}' }) >= 0)
            {
                problem = "Use { and } only around a placeholder, such as {number}.";
                return false;
            }
            problem = null;
            pattern.Append(TranslationSuggester.Literal(text));
            return true;
        }

        private static void addGroup(List<string> groups, string group)
        {
            if (!groups.Contains(group))
            {
                groups.Add(group);
            }
        }

        /// <summary>Every placeholder these words can use: the translation's own, then the ones any can.</summary>
        internal IEnumerable<string> AllPlaceholders()
        {
            return ownGroups.Keys.Concat(BuiltIn.Keys.Where(b => !ownGroups.ContainsKey(b))).Concat(new[] { Profile });
        }

        /// <summary>What a translation does with the files listed now.</summary>
        internal class Preview
        {
            /// <summary>A line for each pair, and for each file that would be made on the other side or that fails.</summary>
            public List<string> Lines = new List<string>();
            public int XboxFilesCovered;
            public int NonXboxFilesCovered;
        }

        /// <summary>
        /// Replays a translation, on its own, over the files listed now: which pairs it finds, and
        /// which files it would make on the other side.
        /// </summary>
        internal static Preview PreviewOf(FileTranslation translation, IList<XboxFile> xboxFiles, IList<string> nonXboxFiles, string xboxProfileID)
        {
            TranslationSimulator simulator = new TranslationSimulator(new[] { translation }, xboxFiles, nonXboxFiles, xboxProfileID);
            Preview preview = new Preview();

            // Worked out first, so that a pair can be shown once, on its Xbox file's line.
            Dictionary<string, TranslationSimulator.Result> toXbox = new Dictionary<string, TranslationSimulator.Result>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in nonXboxFiles)
            {
                try
                {
                    toXbox[path] = simulator.ToXbox(path);
                }
                catch (Exception)
                {
                    // Matched, but a name it needs cannot be filled in. Said on its own line below.
                    toXbox[path] = null;
                }
            }

            HashSet<string> paired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (XboxFile file in xboxFiles)
            {
                TranslationSimulator.Result result;
                try
                {
                    result = simulator.ToNonXbox(file);
                }
                catch (Exception)
                {
                    preview.Lines.Add(XboxName(file) + ":   no name on this PC can be worked out from it");
                    continue;
                }

                if (result.Outcome == TranslationSimulator.Outcome.ExistingFile)
                {
                    preview.XboxFilesCovered++;
                    TranslationSimulator.Result back;
                    if (toXbox.TryGetValue(result.RelativePath, out back) && back != null && back.Outcome == TranslationSimulator.Outcome.ExistingFile && back.XboxFile == file)
                    {
                        paired.Add(result.RelativePath);
                        preview.Lines.Add(XboxName(file) + "   <->   " + result.RelativePath);
                    }
                    else
                    {
                        preview.Lines.Add(XboxName(file) + "   ->   " + result.RelativePath);
                    }
                }
                else if (result.Outcome == TranslationSimulator.Outcome.NewFile)
                {
                    preview.XboxFilesCovered++;
                    preview.Lines.Add(XboxName(file) + "   ->   " + result.RelativePath + "   (new on this PC)");
                }
            }

            foreach (string path in nonXboxFiles)
            {
                TranslationSimulator.Result result = toXbox[path];
                if (result == null)
                {
                    preview.Lines.Add(path + ":   no Xbox name can be worked out from it");
                    continue;
                }
                if (result.Outcome == TranslationSimulator.Outcome.NoTranslation)
                {
                    continue;
                }

                preview.NonXboxFilesCovered++;
                if (paired.Contains(path))
                {
                    continue;
                }
                switch (result.Outcome)
                {
                    case TranslationSimulator.Outcome.ExistingFile:
                        preview.Lines.Add(path + "   ->   " + XboxName(result.XboxFile));
                        break;
                    case TranslationSimulator.Outcome.NewFile:
                        preview.Lines.Add(path + "   ->   " + XboxName(new XboxFile(result.XboxFile.ContainerName1, result.XboxFile.ContainerName2, result.NewFileID)) + "   (new on Xbox)");
                        break;
                    case TranslationSimulator.Outcome.NoContainer:
                        preview.Lines.Add(path + "   ->   a new Xbox container");
                        break;
                    case TranslationSimulator.Outcome.AmbiguousContainer:
                        preview.Lines.Add(path + ":   more than one Xbox container fits it");
                        break;
                }
            }
            return preview;
        }

        /// <summary>An Xbox file as the window's other questions name one: its container, then its blob.</summary>
        internal static string XboxName(XboxFile file)
        {
            bool secondNameSays = !string.IsNullOrEmpty(file.ContainerName2) && file.ContainerName2 != file.ContainerName1;
            return file.ContainerName1 + (secondNameSays ? " / " + file.ContainerName2 : string.Empty) + " / " + file.FileID;
        }
    }
}
