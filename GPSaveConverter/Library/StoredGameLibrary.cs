using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter.Library
{
    internal class StoredGameLibrary
    {
        public IList<GameInfo> GameInfo { get; set; }

        public string Version { get; set; }
    }
}
