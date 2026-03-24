namespace YusGameFrame.SimpleBinarySaver;

public static class SimpleBinarySaver
{
    public static void Save<T>(T value, string key)
    {
        SimpleBinarySaverService.RequireInstance().Save(value, key);
    }

    public static T Load<T>(string key, T defaultValue = default!)
    {
        return SimpleBinarySaverService.RequireInstance().Load(key, defaultValue);
    }

    public static string GetSaveDirectoryPath()
    {
        return SimpleBinarySaverService.GetSaveDirectoryPath();
    }

    public static string GetSaveDirectoryAbsolutePath()
    {
        return SimpleBinarySaverService.GetSaveDirectoryAbsolutePath();
    }

    public static System.Collections.Generic.IReadOnlyList<SimpleBinarySaverEntryInfo> GetAllEntries()
    {
        return SimpleBinarySaverService.GetAllEntries();
    }

    public static bool TryUpdateEntry(string storageKey, string editedText, out string message)
    {
        return SimpleBinarySaverService.TryUpdateEntry(storageKey, editedText, out message);
    }
}
