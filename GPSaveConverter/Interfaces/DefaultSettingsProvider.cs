namespace GPSaveConverter.Interfaces
{
    using NLog;

    /// <summary>
    /// Default implementation that delegates to Properties.Settings.Default.
    /// </summary>
    public class DefaultSettingsProvider : ISettingsProvider
    {
        public bool ShowFileTranslations
        {
            get => Properties.Settings.Default.ShowFileTranslations;
            set => Properties.Settings.Default.ShowFileTranslations = value;
        }

        public bool FirstRun
        {
            get => Properties.Settings.Default.FirstRun;
            set => Properties.Settings.Default.FirstRun = value;
        }

        public bool AllowWebDataFetch
        {
            get => Properties.Settings.Default.AllowWebDataFetch;
            set => Properties.Settings.Default.AllowWebDataFetch = value;
        }

        public string DefaultGameLibrary
        {
            get => Properties.Settings.Default.DefaultGameLibrary;
            set => Properties.Settings.Default.DefaultGameLibrary = value;
        }

        public string UserGameLibrary
        {
            get => Properties.Settings.Default.UserGameLibrary;
            set => Properties.Settings.Default.UserGameLibrary = value;
        }

        public LogLevel FileLogLevel
        {
            get => Properties.Settings.Default.FileLogLevel;
            set => Properties.Settings.Default.FileLogLevel = value;
        }

        public void Save() => Properties.Settings.Default.Save();

        public void Reset() => Properties.Settings.Default.Reset();
    }
}
