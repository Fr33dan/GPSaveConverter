namespace GPSaveConverter.Interfaces
{
    /// <summary>
    /// Abstracts PowerShell script execution for testability.
    /// </summary>
    public interface IScriptRunner
    {
        string RunScript(string scriptText);
    }
}
