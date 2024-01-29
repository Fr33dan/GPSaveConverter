namespace GPSaveConverter.Interfaces
{
    using System;

    /// <summary>
    /// Abstracts environment variable and special folder access for testability.
    /// </summary>
    public interface IEnvironment
    {
        string ExpandEnvironmentVariables(string name);
        string GetFolderPath(Environment.SpecialFolder folder);
    }
}
