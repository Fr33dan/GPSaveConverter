namespace GPSaveConverter.Interfaces
{
    /// <summary>
    /// Default implementation that delegates to ScriptManager.
    /// </summary>
    public class DefaultScriptRunner : IScriptRunner
    {
        public string RunScript(string scriptText) => ScriptManager.RunScript(scriptText);
    }
}
