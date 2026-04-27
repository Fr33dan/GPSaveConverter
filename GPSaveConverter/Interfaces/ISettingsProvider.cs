namespace GPSaveConverter.Interfaces
{
    using NLog;

    /// <summary>
    /// Abstracts application settings access for testability.
    /// </summary>
    public interface ISettingsProvider
    {
        bool ShowFileTranslations { get; set; }
        bool FirstRun { get; set; }
        bool AllowWebDataFetch { get; set; }
        string DefaultGameLibrary { get; set; }
        string UserGameLibrary { get; set; }
        LogLevel FileLogLevel { get; set; }

        void Save();
        void Reset();
    }
}
