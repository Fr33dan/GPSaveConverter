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
        bool BackupBeforeTransfer { get; set; }
        int BackupsToKeep { get; set; }

        /// <summary>
        /// Whether translations that are still being tested are used as well. They are tried
        /// before any other.
        /// </summary>
        bool UsePreviewTranslations { get; set; }

        /// <summary>
        /// The translations being tested, as they were last downloaded. Kept so that they are there
        /// when the tool starts with no connection.
        /// </summary>
        string PreviewGameLibrary { get; set; }

        void Save();
        void Reset();
    }
}
