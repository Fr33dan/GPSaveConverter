namespace GPSaveConverter.Interfaces
{
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Default implementation that delegates to System.Net.WebClient.
    /// </summary>
    public class DefaultHttpClient : IHttpClient
    {
        public string DownloadString(string address)
        {
            using (WebClient wc = new WebClient())
            {
                return wc.DownloadString(address);
            }
        }

        public async Task<string> DownloadStringAsync(string address)
        {
            using (WebClient wc = new WebClient())
            {
                return await wc.DownloadStringTaskAsync(address);
            }
        }

        public async Task<byte[]> DownloadDataAsync(string address)
        {
            using (WebClient wc = new WebClient())
            {
                return await wc.DownloadDataTaskAsync(address);
            }
        }
    }
}
