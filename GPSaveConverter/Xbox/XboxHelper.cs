using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter.Xbox
{
    internal static class XboxHelper
    {
        internal const int FileNameDataLength = 16;
        internal const int EncodedPathByteLength = 128;
        internal const int EntryByteLength = EncodedPathByteLength + FileNameDataLength + FileNameDataLength;

        internal static string DecodeFileName(byte[] fileCode, int startIndex = 0)
        {
            string filePath;
            byte[] firstSectionData = new byte[4];
            Array.Copy(fileCode, startIndex, firstSectionData, 0, firstSectionData.Length);
            string firstSection = BitConverter.ToString(firstSectionData.Reverse().ToArray()).Replace("-", "");

            byte[] secondSectionData = new byte[2];
            Array.Copy(fileCode, startIndex + 4, secondSectionData, 0, secondSectionData.Length);
            string secondSection = BitConverter.ToString(secondSectionData.Reverse().ToArray()).Replace("-", "");

            byte[] thirdSectionData = new byte[2];
            Array.Copy(fileCode, startIndex + 6, thirdSectionData, 0, thirdSectionData.Length);
            string thirdSection = BitConverter.ToString(thirdSectionData.Reverse().ToArray()).Replace("-", "");

            byte[] fourthSectionData = new byte[8];
            Array.Copy(fileCode, startIndex + 8, fourthSectionData, 0, fourthSectionData.Length);
            string fourthSection = BitConverter.ToString(fourthSectionData.ToArray()).Replace("-", "");

            filePath = string.Concat(firstSection, secondSection, thirdSection, fourthSection);
            return filePath;

        }
    }
}
