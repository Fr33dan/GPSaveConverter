namespace GPSaveConverter.Interfaces
{
    /// <summary>
    /// Default implementation that delegates to System.Environment.
    /// </summary>
    public class DefaultEnvironment : IEnvironment
    {
        public string ExpandEnvironmentVariables(string name) =>
            System.Environment.ExpandEnvironmentVariables(name);

        public string GetFolderPath(System.Environment.SpecialFolder folder) =>
            System.Environment.GetFolderPath(folder);
    }
}
