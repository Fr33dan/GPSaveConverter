using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GPSaveConverter
{
    internal class NonXboxProfile
    {
        internal ProfileType profileType;


        [JsonConverter(typeof(JsonStringEnumConverter))]
        internal enum ProfileType { Steam, Xbox, DisplayOnly };

        [Browsable(false)]
        public string UserID { get; private set; }

        private System.Drawing.Bitmap userIcon;
        [DisplayName("Icon"), Display(Order = 1)]
        public System.Drawing.Bitmap UserIcon
        {
            get
            {
                if (userIcon == null && UserIconLocation != null)
                {
                    userIcon = new System.Drawing.Bitmap(UserIconLocation);
                }
                return userIcon;
            }
        }

        private string userName;

        [DisplayName("User Name"),Display(Order = 2)]
        public string UserName { get { return userName == null ? UserID : userName; } set { userName = value; } }


        internal int ProfileIndex;

        [Browsable(false)]
        public string UserIconLocation { get; set; }

        internal NonXboxProfile(string folderName, int index, ProfileType type)
        {
            this.profileType = type;
            this.UserID = folderName;
            this.ProfileIndex = index;
        }

        internal NonXboxProfile(int index, ProfileType type)
        {
            this.ProfileIndex = index;
            this.profileType = type;
        }


        private string ProfileMarkerPrefix(int? index = null)
        {
            if (index == null) index = ProfileIndex;
            return "<user-id" + (index >= 1 ? (index + 1).ToString() : ""); 
        }

        internal string ExpandSaveLocation(string baseLocation)
        {
            return ExpandSaveLocation(baseLocation, this.UserID);
        }
        private string ExpandSaveLocation(string baseLocation, string profileID)
        {
            string returnVal = baseLocation.Replace(ProfileMarkerPrefix() + ">", profileID);

            if (this.profileType == ProfileType.Xbox)
            {
                if (returnVal.Contains(ProfileMarkerPrefix() + "_XboxInt>"))
                {
                    long profileIDLong = Convert.ToInt64(profileID, 16);
                    returnVal = returnVal.Replace(ProfileMarkerPrefix() + "_XboxInt>", profileIDLong.ToString());
                }
            }

            return returnVal;
        }

        internal async Task<NonXboxProfile[]> getProfileOptions(string baseLocation)
        {
            int markerStart = baseLocation.IndexOf(ProfileMarkerPrefix());
            string profilesDir = baseLocation.Substring(0,markerStart);
            string profileDirMarker = baseLocation.Substring(markerStart, baseLocation.IndexOf('>',markerStart)-markerStart + 1);

            List<NonXboxProfile> returnVal = new List<NonXboxProfile>();

            if (Directory.Exists(profilesDir))
            {
                foreach (string p in Directory.GetDirectories(profilesDir))
                {
                    string newUserID = p.Replace(profilesDir, "");

                    if(this.profileType == ProfileType.Xbox)
                    {
                        if (profileDirMarker.EndsWith("_XboxInt>"))
                        {
                            newUserID = Convert.ToString(long.Parse(newUserID),16).ToUpper();
                        }
                    }

                    string expandedPath = this.ExpandSaveLocation(baseLocation, newUserID);

                    if(expandedPath.Contains(ProfileMarkerPrefix(ProfileIndex + 1)))
                    {
                        expandedPath = expandedPath.Substring(0,expandedPath.IndexOf(ProfileMarkerPrefix(ProfileIndex + 1)));
                    }

                    if (Directory.Exists(expandedPath))
                    {
                        NonXboxProfile newProfile = new NonXboxProfile(newUserID, this.ProfileIndex,this.profileType);
                        await newProfile.FetchProfileInformation();
                        returnVal.Add(newProfile);
                    }
                }
            }

            return returnVal.ToArray();
        }

        internal async Task FetchProfileInformation()
        {
            if (Properties.Settings.Default.AllowWebDataFetch)
            {
                if (profileType == ProfileType.Steam)
                {
                    await Library.Steam.GetUserInformation(this);

                    this.userIcon = await Library.Steam.LoadIcon(this);
                }
            }
        }
    }
}
