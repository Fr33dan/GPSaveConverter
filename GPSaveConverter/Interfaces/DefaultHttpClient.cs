namespace GPSaveConverter.Interfaces
{
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Default implementation that delegates to System.Net.WebClient.
    /// </summary>
    public class DefaultHttpClient : IHttpClient
    {
        /// <summary>
        /// Names the tool to the sites it asks. GitHub's API turns away a request without one.
        /// </summary>
        internal static string UserAgent()
        {
            return "GPSaveConverter/" + ErrorReport.Version() + " (+https://github.com/Fr33dan/GPSaveConverter)";
        }

        private static WebClient NewClient()
        {
            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.UserAgent] = UserAgent();
            return wc;
        }

        public string DownloadString(string address)
        {
            using (WebClient wc = NewClient())
            {
                return wc.DownloadString(address);
            }
        }

        public async Task<string> DownloadStringAsync(string address)
        {
            using (WebClient wc = NewClient())
            {
                return await wc.DownloadStringTaskAsync(address);
            }
        }

        public async Task<byte[]> DownloadDataAsync(string address)
        {
            using (WebClient wc = NewClient())
            {
                return await wc.DownloadDataTaskAsync(address);
            }
        }
    }
}
