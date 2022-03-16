using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
            , DisplayName("Xbox file ID")
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
        public List<string> NamedRegexGroups { get; set; }

        public FileTranslation() { }

        public static FileTranslation getDefaultInstance()
        {
            FileTranslation instance = new FileTranslation();
            instance.NamedRegexGroups = new List<string>(new string[] { "(?<FileName>[\\w\\-. \\\\]+)" });
            instance.NonXboxFilename = "${FileName}";
            instance.XboxFileID = "${FileName}";
            instance.ContainerName1 = "${FileName}";
            instance.ContainerName2 = "${FileName}";
            return instance;
        }

        private string replaceRegex(string value)
        {
            string returnVal = value;
            for (int j = 0; j < this.NamedRegexGroups.Count; j++)
            {
                System.Text.RegularExpressions.Regex ex = new System.Text.RegularExpressions.Regex(NamedRegexGroups[j]);

                string groupName = ex.GetGroupNames().Last();
                returnVal = returnVal.Replace("${" + groupName + "}", this.NamedRegexGroups[j]);
            }
            return returnVal;
        }

        public override string ToString()
        {
            return this.NonXboxFilename;
        }
    }
}
