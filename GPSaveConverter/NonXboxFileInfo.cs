using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter
{
    internal class NonXboxFileInfo
    {

        [Browsable(false)]
        public string FilePath { get; set; }

        [Display(Order = 1), DisplayName("File Path")]
        public string RelativePath { get; set; }

        [Display(Order = 2), DisplayName("Last Modified")]
        public DateTime Timestamp { get; set; }

        public override bool Equals(object obj)
        {
            bool returnVal = false;
            if(obj.GetType() == typeof(NonXboxFileInfo))
            {
                returnVal = FilePath.Equals(((NonXboxFileInfo)obj).FilePath)
                            && RelativePath.Equals(((NonXboxFileInfo)obj).RelativePath);
            }
            return returnVal;
        }

        public override int GetHashCode()
        {
            int hashCode = 1637432162;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FilePath);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RelativePath);
            return hashCode;
        }
    }
}
