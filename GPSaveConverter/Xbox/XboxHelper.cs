using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter.Xbox
{
    internal static class XboxHelper
    {
        internal const int GuidLength = 16;
        internal const int FileIDByteLength = 128;
        internal const int EntryByteLength = FileIDByteLength + GuidLength + GuidLength;
    }
}
