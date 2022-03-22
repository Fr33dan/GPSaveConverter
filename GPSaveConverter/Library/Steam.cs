using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace GPSaveConverter.Library
{
    internal static class Steam
    {
        private const ulong SteamID64IndividualProfile = 0x0110000100000000;
        internal static async Task GetUserInformation(NonXboxProfile profile)
        {
            using (WebClient wc = new WebClient())
            {
                try
                {
                    string url = String.Format(@"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}"
                                                , GPSaveConverter.Properties.Resources.SteamAPIKey
                                                , GetSteamID(profile.UserID));
                    string queryJson = await wc.DownloadStringTaskAsync(url);
                    JsonNode queryRoot = JsonValue.Parse(queryJson);

                    profile.UserName = queryRoot["response"]["players"][0]["personaname"].GetValue<string>();
                    profile.UserIconLocation = queryRoot["response"]["players"][0]["avatar"].GetValue<string>();
                }
                catch (Exception e) { }
            }
        }

        internal static async Task<System.Drawing.Bitmap> LoadIcon(NonXboxProfile profile)
        {
            System.Drawing.Bitmap returnVal = null;
            using (WebClient wc = new WebClient())
            {
                try
                {
                    byte[] imageData = await wc.DownloadDataTaskAsync(profile.UserIconLocation);

                    returnVal = new System.Drawing.Bitmap(new System.IO.MemoryStream(imageData));
                }
                catch (Exception e) { }
            }
            return returnVal;
        }

        /// <summary>
        /// https://developer.valvesoftware.com/wiki/SteamID
        /// </summary>
        /// <param name="steam3ID"></param>
        /// <param name="accoundIDY"></param>
        /// <returns></returns>
        internal static ulong GetSteamID(string steam3ID)
        {
            ulong steam3IDValue = ulong.Parse(steam3ID);

            return SteamID64IndividualProfile | (steam3IDValue);
        }
    }
}
