namespace GPSaveConverter.Interfaces
{
    /// <summary>
    /// Default implementation that delegates to Microsoft.Win32.Registry.
    /// </summary>
    public class DefaultRegistry : IRegistry
    {
        public object GetValue(string keyName, string valueName, object defaultValue) =>
            Microsoft.Win32.Registry.GetValue(keyName, valueName, defaultValue);
    }
}
