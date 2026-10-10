using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;

namespace GPSaveConverter.Library
{
    internal class FileTranslation
    {
        [Category("File Info")
            , DisplayName("Non-Xbox file name")
            , Description("Name and path of a non-Xbox file relative to the non-Xbox save location. Substitutions are resolved from Xbox ID and container names..")
            , Display(Order = 1)]
        public string NonXboxFilename { get; set; }

        [Browsable(false), JsonIgnore]
        public string NonXboxFilenameRegex { get { return replaceRegex(NonXboxFilename); } }

        [Category("File Info")
            , DisplayName("Xbox Blob ID")
            , Description("Descriptive name within Xbox container file. Substitutions are resolved from non-Xbox file name.")
            , Display(Order = 2)]
        public string XboxFileID { get; set; }

        [Browsable(false), JsonIgnore]
        public string XboxFileIDRegex { get { return replaceRegex(XboxFileID); } }

        [Category("File Info")
            , DisplayName("Container Name 1")
            , Description("First container name. Substitutions are resolved from non-Xbox file name.")
            , Display(Order = 3)]
        public string ContainerName1 { get; set; }

        [Browsable(false), JsonIgnore]
        public string ContainerName1Regex { get { return replaceRegex(ContainerName1); } }


        [Category("File Info")
            , DisplayName("Container Name 2")
            , Description("Second container name. Substitutions are resolved from non-Xbox file name.")
            , Display(Order = 4)]
        public string ContainerName2 { get; set; }

        [Browsable(false), JsonIgnore]
        public string ContainerName2Regex { get { return replaceRegex(ContainerName2); } }

        [Category("File Info")
            , DisplayName("Named Regex Groups")
            , Description("Regex groups with names for use in substitutions.")
            , Display(Order = 5)]
        public string[] NamedRegexGroups { get; set; }

        [Browsable(false), JsonIgnore]
        public NonXboxFileInfo NonXboxFileInfo { get; set; }

        [Browsable(false),JsonIgnore]
        public Xbox.XboxFileInfo XboxFileInfo { get; set; }

        /// <summary>
        /// The Xbox profile that ${XboxProfileID} stands for, as its folder names it. Set whenever a
        /// translation is picked for a file, so it does not depend on which Xbox file was looked at last.
        /// </summary>
        internal string XboxProfileID { get; set; }

        public FileTranslation() { }



        public static FileTranslation getDefaultInstance()
        {
            FileTranslation instance = new FileTranslation();
            instance.NamedRegexGroups = (new string[] { "(?<FileName>[\\w\\-. \\\\]+)" });
            instance.NonXboxFilename = "${FileName}";
            instance.XboxFileID = "${FileName}";
            instance.ContainerName1 = "${FileName}";
            instance.ContainerName2 = "${FileName}";
            return instance;
        }

        /// <summary>
        /// Checks this translation for mistakes that would make it fail when it is used.
        /// </summary>
        /// <returns>A description of the first problem found, or null if there is none.</returns>
        internal string FindProblem()
        {
            if (NonXboxFilename == null) return "NonXboxFilename is missing.";
            if (XboxFileID == null) return "XboxFileID is missing.";
            if (ContainerName1 == null) return "ContainerName1 is missing.";
            if (ContainerName2 == null) return "ContainerName2 is missing.";
            if (NamedRegexGroups == null) return "NamedRegexGroups is missing.";

            Dictionary<string, string> groups = new Dictionary<string, string>();
            foreach (string groupPattern in NamedRegexGroups)
            {
                string groupName;
                try
                {
                    // The same name replaceRegex substitutes. A pattern with no named group provides none.
                    groupName = new Regex(groupPattern).GetGroupNames().Last();
                }
                catch (ArgumentException e)
                {
                    return "Named regex group '" + groupPattern + "' is not a valid pattern: " + e.Message;
                }

                if (!groups.ContainsKey(groupName))
                {
                    groups.Add(groupName, groupPattern);
                }
            }

            foreach (string value in new string[] { NonXboxFilename, XboxFileID, ContainerName1, ContainerName2 })
            {
                string pattern = value;
                foreach (Match substitution in Regex.Matches(value, @"\$\{(\w+)\}"))
                {
                    string name = substitution.Groups[1].Value;
                    if (name == "XboxProfileID" || name == "XboxProfileID_Int")
                    {
                        // Filled in from the Xbox profile when the translation is used.
                        pattern = pattern.Replace(substitution.Value, "0");
                    }
                    else if (groups.ContainsKey(name))
                    {
                        pattern = pattern.Replace(substitution.Value, groups[name]);
                    }
                    else
                    {
                        return "'" + value + "' uses " + substitution.Value + ", which is not one of the named regex groups.";
                    }
                }

                try
                {
                    new Regex(ExactRegex(pattern));
                }
                catch (ArgumentException e)
                {
                    return "'" + value + "' is not a valid pattern: " + e.Message;
                }
            }

            return null;
        }

        /// <summary>
        /// Works out the Xbox blob ID for a non-Xbox file from the values its path gave the named groups.
        /// The text around each ${Name} is written as a pattern, so it is un-escaped to get the real ID.
        /// </summary>
        /// <param name="nonXboxMatch">The match of the file's relative path against <see cref="NonXboxFilenameRegex"/>.</param>
        /// <param name="xboxProfileID">The Xbox profile being written to, as its folder names it.</param>
        /// <param name="asPattern">True for a pattern that finds the blob, false for the ID itself.</param>
        /// <param name="complete">False if a ${Name} got no value from the path and was left in place.</param>
        internal string FillXboxFileID(Match nonXboxMatch, string xboxProfileID, bool asPattern, out bool complete)
        {
            return Fill(XboxFileID,
                        name => ProfileValue(name, xboxProfileID) ?? GroupValue(name, nonXboxMatch),
                        text => TemplateText(text, asPattern),
                        value => asPattern ? Regex.Escape(value) : value,
                        out complete);
        }

        /// <summary>
        /// Finds the blob a non-Xbox file maps to among the blobs of its container.
        /// </summary>
        /// <param name="newFileID">The ID to give the blob if it has to be created.</param>
        /// <param name="newFileIDComplete">False if that ID still holds a ${Name} with no value, so it cannot be used.</param>
        /// <returns>The matching blob, or null if the container has none.</returns>
        internal T FindXboxFile<T>(IEnumerable<T> files, Func<T, string> fileID, string nonXboxRelativePath, string xboxProfileID, out string newFileID, out bool newFileIDComplete) where T : class
        {
            // First the reading every earlier version used: the blob ID taken exactly as it is written.
            string asWritten = ExactRegex(replaceRegex(Regex.Escape(Regex.Replace(nonXboxRelativePath, NonXboxFilenameRegex, XboxFileID))));
            T matchedFile = files.Where(f => Regex.Match(fileID(f), asWritten).Success).FirstOrDefault();

            Match nonXboxMatch = Regex.Match(nonXboxRelativePath, NonXboxFilenameRegex);
            newFileID = FillXboxFileID(nonXboxMatch, xboxProfileID, false, out newFileIDComplete);

            if (matchedFile == null)
            {
                // Then as a pattern, where a backslash or dot in the real ID has a backslash in front of it.
                string pattern = replaceRegex(FillXboxFileID(nonXboxMatch, xboxProfileID, true, out newFileIDComplete));
                matchedFile = files.Where(f => Regex.Match(fileID(f), pattern).Success).FirstOrDefault();
            }
            return matchedFile;
        }

        /// <summary>
        /// Picks out the Xbox containers a non-Xbox file could belong in.
        /// </summary>
        internal List<T> FindContainers<T>(IEnumerable<T> containers, Func<T, string> name1, Func<T, string> name2, string nonXboxRelativePath, string xboxProfileID)
        {
            Match nonXboxMatch = Regex.Match(nonXboxRelativePath, NonXboxFilenameRegex);
            string pattern1 = FillContainerPattern(ContainerName1, nonXboxMatch, xboxProfileID);
            string pattern2 = FillContainerPattern(ContainerName2, nonXboxMatch, xboxProfileID);

            List<T> found = containers.Where(c => Regex.Match(name1(c), pattern1).Success && Regex.Match(name2(c), pattern2).Success).ToList();

            // Earlier versions put the values from the file's path into the pattern unescaped. A saved
            // translation may have come to depend on that, so it is still tried when nothing else matches.
            if (found.Count == 0)
            {
                try
                {
                    string asWritten1 = replaceRegex(ExactRegex(Regex.Replace(nonXboxRelativePath, NonXboxFilenameRegex, ContainerName1)));
                    string asWritten2 = replaceRegex(ExactRegex(Regex.Replace(nonXboxRelativePath, NonXboxFilenameRegex, ContainerName2)));

                    found = containers.Where(c => Regex.Match(name1(c), asWritten1).Success && Regex.Match(name2(c), asWritten2).Success).ToList();
                }
                catch (ArgumentException)
                {
                    // The file's path does not make a valid pattern. A folder separator before some letters does that.
                }
            }
            return found;
        }

        /// <summary>
        /// Works out what to call the container a non-Xbox file belongs in, for when the Xbox save
        /// has no such container and one has to be made.
        /// </summary>
        /// <param name="problem">The reason, in words for the user, if the names cannot be worked out.</param>
        /// <returns>The container's two names, or null.</returns>
        internal string[] NewContainerNames(string nonXboxRelativePath, string xboxProfileID, out string problem)
        {
            problem = null;
            Match nonXboxMatch = Regex.Match(nonXboxRelativePath, NonXboxFilenameRegex);

            string[] templates = new string[] { ContainerName1, ContainerName2 };
            string[] names = new string[templates.Length];
            for (int i = 0; i < templates.Length; i++)
            {
                // A name can only come from a template that spells one out. "Save.*" finds a container
                // that exists. It does not say what a new one is called.
                if (IsPatternForSeveralNames(templates[i]))
                {
                    problem = "The file translation has a pattern, \"" + templates[i] + "\", where the name of the new container is needed.";
                    return null;
                }

                bool complete;
                names[i] = Fill(templates[i],
                                name => ProfileValue(name, xboxProfileID) ?? GroupValue(name, nonXboxMatch),
                                text => TemplateText(text, false),
                                value => value,
                                out complete);
                if (!complete)
                {
                    problem = "The file's path gives no value for part of the container name \"" + templates[i] + "\".";
                    return null;
                }
            }
            return names;
        }

        /// <summary>
        /// True if the text of a template, outside its ${Name} parts, uses the pattern syntax for more
        /// than spelling out one name: a "*", a "[", a "\d". A "." counts as a plain dot, which is how
        /// the game library writes one, and so does a character with a backslash in front of it.
        /// </summary>
        private static bool IsPatternForSeveralNames(string template)
        {
            string text = Regex.Replace(template, @"\$\{\w+\}", String.Empty);
            for (int position = 0; position < text.Length; position++)
            {
                if (text[position] == '\\')
                {
                    // "\+" is a plus sign. "\d" is any digit.
                    position++;
                    if (position < text.Length && char.IsLetterOrDigit(text[position]))
                    {
                        return true;
                    }
                }
                else if ("*+?()[]{}|^$".IndexOf(text[position]) != -1)
                {
                    return true;
                }
            }
            return false;
        }

        private string FillContainerPattern(string containerName, Match nonXboxMatch, string xboxProfileID)
        {
            // The container name in a translation is a pattern already. Only the values going into it are escaped.
            bool complete;
            return replaceRegex(Fill(containerName,
                                     name => ProfileValue(name, xboxProfileID) ?? GroupValue(name, nonXboxMatch),
                                     text => text,
                                     Regex.Escape,
                                     out complete));
        }

        /// <summary>
        /// Works out where an Xbox file this translation matches goes in the non-Xbox save folder.
        /// </summary>
        /// <param name="complete">False if a ${Name} got no value from the Xbox names and was left in place.</param>
        internal string FillNonXboxFilename(string containerName1, string containerName2, string fileID, string xboxProfileID, out bool complete)
        {
            // In the order earlier versions applied them: the blob ID first, then each container name.
            Match[] xboxMatches = new Match[] { Regex.Match(fileID, XboxFileIDRegex), Regex.Match(containerName1, ContainerName1Regex), Regex.Match(containerName2, ContainerName2Regex) };

            return Fill(NonXboxFilename,
                        name => ProfileValue(name, xboxProfileID) ?? GroupValue(name, xboxMatches),
                        text => TemplateText(text, false),
                        value => value,
                        out complete);
        }

        /// <summary>
        /// The non-Xbox path as earlier versions built it, with the template text left exactly as written.
        /// </summary>
        internal string NonXboxFilenameAsWritten(string containerName1, string containerName2, string fileID)
        {
            string path = Regex.Replace(fileID, XboxFileIDRegex, NonXboxFilename);
            path = Regex.Replace(containerName1, ContainerName1Regex, path);
            return Regex.Replace(containerName2, ContainerName2Regex, path);
        }

        /// <summary>
        /// Finds the existing non-Xbox file an Xbox file maps to.
        /// </summary>
        /// <param name="relativePaths">The files in the non-Xbox save folder, relative to it.</param>
        /// <param name="path">The path from <see cref="FillNonXboxFilename"/>.</param>
        /// <param name="pathAsWritten">The path from <see cref="NonXboxFilenameAsWritten"/>.</param>
        /// <returns>The matching entry of <paramref name="relativePaths"/>, or null if there is none.</returns>
        internal static string FindNonXboxFile(IEnumerable<string> relativePaths, string path, string pathAsWritten)
        {
            // An exact match first, so a file is never passed over for one whose name only contains it.
            foreach (string wanted in new string[] { path, pathAsWritten })
            {
                string exactMatch = relativePaths.FirstOrDefault(p => string.Equals(SingleSeparators(p), SingleSeparators(wanted), StringComparison.OrdinalIgnoreCase));
                if (exactMatch != null) return exactMatch;
            }

            // Then the way earlier versions looked: the path read as a pattern and matched anywhere in a name.
            try
            {
                return relativePaths.FirstOrDefault(p => Regex.Match(p, pathAsWritten).Success);
            }
            catch (ArgumentException)
            {
                // The path does not make a valid pattern. A folder separator before some letters does that.
                return null;
            }
        }

        private static string SingleSeparators(string path)
        {
            return Regex.Replace(path, @"\\+", @"\");
        }

        /// <summary>
        /// Puts values in place of the ${Name} parts of a template.
        /// </summary>
        /// <param name="valueOf">Gives the value for a name, or null if there is none.</param>
        /// <param name="text">Applied to the template text between the ${Name} parts.</param>
        /// <param name="value">Applied to each value before it goes in.</param>
        /// <param name="complete">False if a ${Name} had no value and was left in place.</param>
        private static string Fill(string template, Func<string, string> valueOf, Func<string, string> text, Func<string, string> value, out bool complete)
        {
            complete = true;
            StringBuilder result = new StringBuilder();
            int position = 0;
            foreach (Match substitution in Regex.Matches(template, @"\$\{(\w+)\}"))
            {
                result.Append(text(template.Substring(position, substitution.Index - position)));

                string found = valueOf(substitution.Groups[1].Value);
                if (found != null)
                {
                    result.Append(value(found));
                }
                else
                {
                    result.Append(substitution.Value);
                    complete = false;
                }
                position = substitution.Index + substitution.Length;
            }
            result.Append(text(template.Substring(position)));

            return result.ToString();
        }

        private static string ProfileValue(string name, string xboxProfileID)
        {
            if (name == "XboxProfileID") return xboxProfileID.TrimStart('0');
            if (name == "XboxProfileID_Int") return Convert.ToInt64(xboxProfileID, 16).ToString();
            return null;
        }

        private static string GroupValue(string name, params Match[] matches)
        {
            foreach (Match match in matches)
            {
                if (match.Groups[name].Success) return match.Groups[name].Value;
            }
            return null;
        }

        /// <summary>
        /// Reads a stretch of template text as the real characters it stands for. Templates are patterns,
        /// so a backslash or a dot in a real name is written with a backslash in front of it.
        /// </summary>
        private static string TemplateText(string text, bool asPattern)
        {
            string plainText = text;
            try
            {
                string unescaped = Regex.Unescape(text);

                // A control character means the backslash was not an escape: "save\name" is a name, not a line break.
                if (!unescaped.Any(char.IsControl))
                {
                    plainText = unescaped;
                }
            }
            catch (ArgumentException)
            {
                // Not an escape the pattern syntax has, so the text is meant as it is written.
            }
            return asPattern ? Regex.Escape(plainText) : plainText;
        }

        internal string replaceRegex(string value, bool escape = false)
        {
            string returnVal = value;

            if (returnVal.Contains("${XboxProfileID}"))
            {
                returnVal = returnVal.Replace("${XboxProfileID}", (XboxProfileID ?? XboxFileInfo.Parent.Parent.XboxProfileID).TrimStart('0'));
            }

            if (returnVal.Contains("${XboxProfileID_Int}"))
            {
                long profileIDLong = Convert.ToInt64(XboxProfileID ?? XboxFileInfo.Parent.Parent.XboxProfileID, 16);
                returnVal = returnVal.Replace("${XboxProfileID_Int}", profileIDLong.ToString());
            }

            foreach (string groupPattern in NamedRegexGroups)
            {
                System.Text.RegularExpressions.Regex ex = new System.Text.RegularExpressions.Regex(groupPattern);

                string groupName = ex.GetGroupNames().Last();
                returnVal = returnVal.Replace("${" + groupName + "}", groupPattern);
            }

            if (escape)
            {
                returnVal = Regex.Escape(returnVal);
            }

            return ExactRegex(returnVal);
        }

        public static string ExactRegex(string regexInput)
        {
            // Add string start and end markers for exact match.
            return "^" + regexInput + "$";
        }

        internal bool CheckMatch(Xbox.XboxFileInfo file)
        {
            if (Regex.Match(file.ContainerName1, ContainerName1Regex).Success
                    && Regex.Match(file.ContainerName2, ContainerName2Regex).Success
                    && Regex.Match(file.FileID, XboxFileIDRegex).Success)
            {
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return this.NonXboxFilename;
        }

        public override bool Equals(object obj)
        {
            FileTranslation t = obj as FileTranslation;

            if(t == null) return false;

            bool regexMatches = true;
            for(int j = 0;regexMatches && j < this.NamedRegexGroups.Length; j++)
            {
                if(j >= t.NamedRegexGroups.Length)
                {
                    regexMatches = false;
                }
                else
                {
                    regexMatches = this.NamedRegexGroups[j].Equals(t.NamedRegexGroups[j]);
                }
            }

            return regexMatches
                && this.NonXboxFilename == t.NonXboxFilename
                && this.XboxFileID == t.XboxFileID
                && this.ContainerName1 == t.ContainerName1
                && this.ContainerName2 == t.ContainerName2;
        }

        public override int GetHashCode()
        {
            int returnVal = 0;
            returnVal = (returnVal * 17) + this.NonXboxFilename.GetHashCode();
            returnVal = (returnVal * 17) + this.XboxFileID.GetHashCode();
            returnVal = (returnVal * 17) + this.ContainerName1.GetHashCode();
            returnVal = (returnVal * 17) + this.ContainerName2.GetHashCode();

            foreach(string regex in NamedRegexGroups.OrderBy(x=> x))
            {
                returnVal = (returnVal * 17) + regex.GetHashCode();
            }
            return returnVal;
        }
    }
}
