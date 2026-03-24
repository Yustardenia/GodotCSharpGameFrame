namespace YusGameFrame.SimpleBinarySaver;

public sealed class SimpleBinarySaverEntryInfo
{
    public string StorageKey { get; init; } = string.Empty;

    public string Key { get; init; } = string.Empty;

    public string TypeName { get; init; } = string.Empty;

    public string DataKind { get; init; } = string.Empty;

    public string RelativePath { get; init; } = string.Empty;

    public string AbsolutePath { get; init; } = string.Empty;

    public string EditableText { get; init; } = string.Empty;

    public string DisplayText { get; init; } = string.Empty;

    public bool IsEditable { get; init; }
}
