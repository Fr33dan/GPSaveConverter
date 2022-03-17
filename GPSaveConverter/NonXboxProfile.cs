using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter
{
    internal class NonXboxProfile
    {
        internal ProfileType profileType;
        internal enum ProfileType { Steam, DisplayOnly };

        [Browsable(false)]
        public string UserIDFolder { get; private set; }

        private string userName;

        [DisplayName("User Name"),Display(Order = 2)]
        public string UserName { get { return userName == null ? UserIDFolder : userName; } set { userName = value; } }

        private System.Drawing.Bitmap userIcon;
        [DisplayName("Icon"), Display(Order = 1)]
        public System.Drawing.Bitmap UserIcon
        {
            get
            {
                if(userIcon == null && UserIconLocation != null)
                {
                    userIcon = new System.Drawing.Bitmap(UserIconLocation);
                }
                return userIcon;
            }
        }

        [Browsable(false)]
        public string UserIconLocation { get; set; }

        internal NonXboxProfile(string folderName, ProfileType type)
        {
            this.profileType = type;
            this.UserIDFolder = folderName;
        }

        internal async Task FetchProfileInformation()
        {
            if (Properties.Settings.Default.AllowWebDataFetch)
            {
                if (profileType == ProfileType.Steam)
                {
                    await Library.Steam.GetUserInformation(this);
                    await Task.Run(() => userIcon = new System.Drawing.Bitmap(UserIconLocation));
                }
            }
        }
    }
}
