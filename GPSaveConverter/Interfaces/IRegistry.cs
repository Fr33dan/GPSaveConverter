namespace GPSaveConverter.Interfaces
{
    /// <summary>
    /// Abstracts Windows registry access for testability.
    /// </summary>
    public interface IRegistry
    {
        object GetValue(string keyName, string valueName, object defaultValue);
    }
}
