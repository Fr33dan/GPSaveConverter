using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter.Library
{
    internal class FileTranslation
    {
        public string ContainerName1 { get; set; }
        public string ContainerName2 { get; set; }
        public string NonXboxRegex { get; set; }
        public string NonXboxSubstitution { get; set; }
        public string XboxFileIDRegex { get; set; }
        public string XboxFileIDSubstitution { get; set; }

        public FileTranslation() { }
    }
}
