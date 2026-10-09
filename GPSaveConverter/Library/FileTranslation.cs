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

        internal string replaceRegex(string value, bool escape = false)
        {
            string returnVal = value;

            if (returnVal.Contains("${XboxProfileID}"))
            {
                returnVal = returnVal.Replace("${XboxProfileID}", XboxFileInfo.Parent.Parent.XboxProfileID.TrimStart('0'));
            }

            if (returnVal.Contains("${XboxProfileID_Int}"))
            {
                long profileIDLong = Convert.ToInt64(XboxFileInfo.Parent.Parent.XboxProfileID, 16);
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
