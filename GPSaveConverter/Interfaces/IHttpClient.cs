namespace GPSaveConverter.Interfaces
{
    using System.Threading.Tasks;

    /// <summary>
    /// Abstracts HTTP operations for testability.
    /// </summary>
    public interface IHttpClient
    {
        string DownloadString(string address);
        Task<string> DownloadStringAsync(string address);
        Task<byte[]> DownloadDataAsync(string address);
    }
}
