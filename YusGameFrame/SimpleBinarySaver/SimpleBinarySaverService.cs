using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using BinaryReader = System.IO.BinaryReader;
using BinaryWriter = System.IO.BinaryWriter;
using Directory = System.IO.Directory;
using File = System.IO.File;
using MemoryStream = System.IO.MemoryStream;
using Path = System.IO.Path;

namespace YusGameFrame.SimpleBinarySaver;

public partial class SimpleBinarySaverService : Node
{
    private const string SaveDirectoryPath = "user://SimpleBinarySaver";
    private const string FileExtension = ".yus";
    private const string Magic = "YUS1";
    private const byte CurrentVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions PrettyJsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true
    };

    public static SimpleBinarySaverService Instance { get; private set; } = null!;

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            GD.PushError("SimpleBinarySaverService 已存在一个有效实例。");
            QueueFree();
            return;
        }

        Instance = this;
        EnsureSaveDirectory();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null!;
        }
    }

    public static SimpleBinarySaverService RequireInstance()
    {
        if (Instance == null)
        {
            throw new InvalidOperationException("SimpleBinarySaverService 当前不可用，请确认已正确配置 Autoload。");
        }

        return Instance;
    }

    public void Save<T>(T value, string key)
    {
        SaveStatic(value, key);
    }

    public T Load<T>(string key, T defaultValue = default!)
    {
        return LoadStatic(key, defaultValue);
    }

    public static void SaveStatic<T>(T value, string key)
    {
        if (!TryResolveFilePath(key, out var filePath))
        {
            return;
        }

        try
        {
            var package = CreatePackage(value, key);
            using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PushError($"SimpleBinarySaver 无法打开保存文件：{filePath}");
                return;
            }

            file.StoreBuffer(package);
        }
        catch (Exception exception)
        {
            GD.PushError($"SimpleBinarySaver 保存键 '{key}' 失败：{exception}");
        }
    }

    public static T LoadStatic<T>(string key, T defaultValue = default!)
    {
        if (!TryResolveFilePath(key, out var filePath))
        {
            return defaultValue;
        }

        if (!Godot.FileAccess.FileExists(filePath))
        {
            return defaultValue;
        }

        try
        {
            using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PushWarning($"SimpleBinarySaver 无法读取键 '{key}' 对应的文件，将返回默认值。");
                return defaultValue;
            }

            var length = file.GetLength();
            if (length > int.MaxValue)
            {
                GD.PushWarning($"SimpleBinarySaver 键 '{key}' 的文件过大，将返回默认值。");
                return defaultValue;
            }

            var bytes = file.GetBuffer(checked((long)length));
            if (!TryReadPackage(bytes, filePath, out var package, out var errorMessage))
            {
                GD.PushWarning($"SimpleBinarySaver 读取键 '{key}' 失败，将返回默认值：{errorMessage}");
                return defaultValue;
            }

            return ReadValueFromPackage(package, key, defaultValue);
        }
        catch (Exception exception)
        {
            GD.PushWarning($"SimpleBinarySaver 读取键 '{key}' 失败，将返回默认值：{exception.Message}");
            return defaultValue;
        }
    }

    public static string GetSaveDirectoryPath()
    {
        return SaveDirectoryPath;
    }

    public static string GetSaveDirectoryAbsolutePath()
    {
        return ProjectSettings.GlobalizePath(SaveDirectoryPath);
    }

    public static IReadOnlyList<SimpleBinarySaverEntryInfo> GetAllEntries()
    {
        EnsureSaveDirectory();

        var entries = new List<SimpleBinarySaverEntryInfo>();
        var directoryPath = GetSaveDirectoryAbsolutePath();
        if (!Directory.Exists(directoryPath))
        {
            return entries;
        }

        foreach (var filePath in Directory.GetFiles(directoryPath, $"*{FileExtension}", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var bytes = File.ReadAllBytes(filePath);
                if (!TryReadPackage(bytes, filePath, out var package, out var errorMessage))
                {
                    var storageKey = Path.GetFileNameWithoutExtension(filePath);
                    entries.Add(new SimpleBinarySaverEntryInfo
                    {
                        StorageKey = storageKey,
                        Key = BeautifyLegacyKey(storageKey),
                        TypeName = "未知类型",
                        DataKind = "损坏文件",
                        RelativePath = ProjectSettings.LocalizePath(filePath),
                        AbsolutePath = filePath,
                        EditableText = string.Empty,
                        DisplayText = $"读取失败：{errorMessage}",
                        IsEditable = false
                    });
                    continue;
                }

                entries.Add(CreateEntryInfo(package, filePath));
            }
            catch (Exception exception)
            {
                var storageKey = Path.GetFileNameWithoutExtension(filePath);
                entries.Add(new SimpleBinarySaverEntryInfo
                {
                    StorageKey = storageKey,
                    Key = BeautifyLegacyKey(storageKey),
                    TypeName = "未知类型",
                    DataKind = "读取异常",
                    RelativePath = ProjectSettings.LocalizePath(filePath),
                    AbsolutePath = filePath,
                    EditableText = string.Empty,
                    DisplayText = $"读取失败：{exception.Message}",
                    IsEditable = false
                });
            }
        }

        entries.Sort((left, right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
        return entries;
    }

    public static bool TryUpdateEntry(string storageKey, string editedText, out string message)
    {
        message = string.Empty;
        if (!TryResolveFilePath(storageKey, out var filePath))
        {
            message = "键无效，无法保存。";
            return false;
        }

        if (!Godot.FileAccess.FileExists(filePath))
        {
            message = "目标文件不存在，请先执行运行时保存。";
            return false;
        }

        try
        {
            var bytes = File.ReadAllBytes(GetAbsoluteFilePath(storageKey));
            if (!TryReadPackage(bytes, filePath, out var package, out var errorMessage))
            {
                message = $"文件解析失败：{errorMessage}";
                return false;
            }

            if (!TryBuildPackageFromEditedText(package, editedText, out var updatedPackage, out errorMessage))
            {
                message = errorMessage;
                return false;
            }

            File.WriteAllBytes(GetAbsoluteFilePath(storageKey), updatedPackage);
            message = "保存成功。";
            return true;
        }
        catch (Exception exception)
        {
            message = $"保存失败：{exception.Message}";
            return false;
        }
    }

    private static byte[] CreatePackage<T>(T value, string key)
    {
        var dataType = typeof(T);
        var typeName = dataType.AssemblyQualifiedName ?? dataType.FullName ?? dataType.Name;

        SerializedDataKind dataKind;
        byte[] payload;

        if (value is null)
        {
            dataKind = SerializedDataKind.Null;
            payload = [];
        }
        else if (TrySerializeVariant(value, out var variantBytes))
        {
            dataKind = SerializedDataKind.GodotVariant;
            payload = variantBytes;
        }
        else
        {
            dataKind = SerializedDataKind.JsonObject;
            payload = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        }

        return CreatePackageBytes(key, typeName, dataKind, payload);
    }

    private static byte[] CreatePackageBytes(string key, string typeName, SerializedDataKind dataKind, byte[] payload)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write(CurrentVersion);
        writer.Write(key);
        writer.Write((byte)dataKind);
        writer.Write(typeName);
        writer.Write(payload.Length);
        writer.Write(payload);
        writer.Flush();
        return memoryStream.ToArray();
    }

    private static T ReadValueFromPackage<T>(SimpleBinarySaverPackage package, string key, T defaultValue)
    {
        var targetType = typeof(T);
        var targetTypeName = targetType.AssemblyQualifiedName ?? targetType.FullName ?? targetType.Name;
        if (!string.Equals(package.TypeName, targetTypeName, StringComparison.Ordinal))
        {
            GD.PushWarning($"SimpleBinarySaver 键 '{key}' 的类型不匹配，期望 '{targetTypeName}'，实际为 '{package.TypeName}'。");
            return defaultValue;
        }

        try
        {
            return package.DataKind switch
            {
                SerializedDataKind.Null => ReadNullValue(defaultValue),
                SerializedDataKind.GodotVariant => ReadVariantValue(package.Payload, key, defaultValue),
                SerializedDataKind.JsonObject => ReadJsonValue(package.Payload, key, defaultValue),
                _ => defaultValue
            };
        }
        catch (Exception exception)
        {
            GD.PushWarning($"SimpleBinarySaver 键 '{key}' 的反序列化失败，将返回默认值：{exception.Message}");
            return defaultValue;
        }
    }

    private static bool TryReadPackage(byte[] bytes, string sourcePath, out SimpleBinarySaverPackage package, out string errorMessage)
    {
        package = null!;
        errorMessage = string.Empty;

        try
        {
            using var memoryStream = new MemoryStream(bytes);
            using var reader = new BinaryReader(memoryStream, Encoding.UTF8, leaveOpen: true);

            var magic = reader.ReadString();
            if (!string.Equals(magic, Magic, StringComparison.Ordinal))
            {
                errorMessage = "文件头无效。";
                return false;
            }

            var version = reader.ReadByte();
            if (version is not 1 and not CurrentVersion)
            {
                errorMessage = $"文件版本 {version} 不受支持。";
                return false;
            }

            var key = version >= 2
                ? reader.ReadString()
                : BeautifyLegacyKey(Path.GetFileNameWithoutExtension(sourcePath));

            var dataKind = (SerializedDataKind)reader.ReadByte();
            var typeName = reader.ReadString();
            var payloadLength = reader.ReadInt32();
            if (payloadLength < 0 || payloadLength > memoryStream.Length - memoryStream.Position)
            {
                errorMessage = "文件内容损坏。";
                return false;
            }

            package = new SimpleBinarySaverPackage
            {
                Key = key,
                TypeName = typeName,
                DataKind = dataKind,
                Payload = reader.ReadBytes(payloadLength)
            };
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private static SimpleBinarySaverEntryInfo CreateEntryInfo(SimpleBinarySaverPackage package, string absolutePath)
    {
        var previewText = BuildEditableText(package);
        var canEdit = package.DataKind switch
        {
            SerializedDataKind.Null => true,
            SerializedDataKind.GodotVariant => true,
            SerializedDataKind.JsonObject => true,
            _ => false
        };

        return new SimpleBinarySaverEntryInfo
        {
            StorageKey = package.Key,
            Key = package.Key,
            TypeName = package.TypeName,
            DataKind = GetKindDisplayName(package.DataKind),
            RelativePath = ProjectSettings.LocalizePath(absolutePath),
            AbsolutePath = absolutePath,
            EditableText = previewText,
            DisplayText = previewText,
            IsEditable = canEdit
        };
    }

    private static string BuildEditableText(SimpleBinarySaverPackage package)
    {
        return package.DataKind switch
        {
            SerializedDataKind.Null => "null",
            SerializedDataKind.GodotVariant => GD.VarToStr(GD.BytesToVar(package.Payload)),
            SerializedDataKind.JsonObject => PrettyPrintJson(package.Payload),
            _ => string.Empty
        };
    }

    private static string PrettyPrintJson(byte[] payload)
    {
        var document = JsonDocument.Parse(payload);
        return JsonSerializer.Serialize(document.RootElement, PrettyJsonOptions);
    }

    private static bool TryBuildPackageFromEditedText(SimpleBinarySaverPackage package, string editedText, out byte[] updatedPackage, out string errorMessage)
    {
        updatedPackage = Array.Empty<byte>();
        errorMessage = string.Empty;

        try
        {
            switch (package.DataKind)
            {
                case SerializedDataKind.Null:
                    updatedPackage = CreatePackageBytes(package.Key, package.TypeName, SerializedDataKind.Null, []);
                    return true;

                case SerializedDataKind.GodotVariant:
                    var variant = GD.StrToVar(editedText);
                    var variantBytes = GD.VarToBytes(variant);
                    updatedPackage = CreatePackageBytes(package.Key, package.TypeName, SerializedDataKind.GodotVariant, variantBytes);
                    return true;

                case SerializedDataKind.JsonObject:
                    var payload = RebuildJsonPayload(package.TypeName, editedText);
                    updatedPackage = CreatePackageBytes(package.Key, package.TypeName, SerializedDataKind.JsonObject, payload);
                    return true;

                default:
                    errorMessage = "当前类型暂不支持编辑。";
                    return false;
            }
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private static byte[] RebuildJsonPayload(string typeName, string editedText)
    {
        var targetType = ResolveType(typeName)
            ?? throw new InvalidOperationException($"无法解析类型：{typeName}");

        var parsedObject = JsonSerializer.Deserialize(editedText, targetType, JsonOptions);
        return JsonSerializer.SerializeToUtf8Bytes(parsedObject, targetType, JsonOptions);
    }

    private static Type? ResolveType(string typeName)
    {
        var type = Type.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private static string GetKindDisplayName(SerializedDataKind dataKind)
    {
        return dataKind switch
        {
            SerializedDataKind.Null => "空值",
            SerializedDataKind.GodotVariant => "Godot Variant",
            SerializedDataKind.JsonObject => "C# 对象",
            _ => "未知类型"
        };
    }

    private static T ReadNullValue<T>(T defaultValue)
    {
        var targetType = typeof(T);
        var canBeNull = !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
        return canBeNull ? default! : defaultValue;
    }

    private static T ReadVariantValue<T>(byte[] payload, string key, T defaultValue)
    {
        var variant = GD.BytesToVar(payload);

        try
        {
            return ConvertVariantToTarget<T>(variant);
        }
        catch (Exception exception)
        {
            GD.PushWarning($"SimpleBinarySaver 键 '{key}' 的 Godot Variant 转换失败，将返回默认值：{exception.Message}");
            return defaultValue;
        }
    }

    private static T ReadJsonValue<T>(byte[] payload, string key, T defaultValue)
    {
        var result = JsonSerializer.Deserialize<T>(payload, JsonOptions);
        if (result is null)
        {
            var targetType = typeof(T);
            var canBeNull = !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
            if (!canBeNull)
            {
                GD.PushWarning($"SimpleBinarySaver 键 '{key}' 读取到了空对象，但目标类型不可为空，将返回默认值。");
                return defaultValue;
            }
        }

        return result!;
    }

    private static bool TrySerializeVariant(object value, out byte[] bytes)
    {
        try
        {
            var variant = CreateVariant(value);
            bytes = GD.VarToBytes(variant);
            return true;
        }
        catch
        {
            bytes = [];
            return false;
        }
    }

    private static Variant CreateVariant(object value)
    {
        return value switch
        {
            Variant variant => variant,
            bool boolValue => boolValue,
            char charValue => charValue.ToString(),
            byte byteValue => (long)byteValue,
            sbyte sbyteValue => (long)sbyteValue,
            short shortValue => (long)shortValue,
            ushort ushortValue => (long)ushortValue,
            int intValue => (long)intValue,
            uint uintValue => checked((long)uintValue),
            long longValue => longValue,
            float floatValue => (double)floatValue,
            double doubleValue => doubleValue,
            string stringValue => stringValue,
            StringName stringNameValue => stringNameValue,
            NodePath nodePathValue => nodePathValue,
            Vector2 vector2Value => vector2Value,
            Vector2I vector2IValue => vector2IValue,
            Rect2 rect2Value => rect2Value,
            Rect2I rect2IValue => rect2IValue,
            Vector3 vector3Value => vector3Value,
            Vector3I vector3IValue => vector3IValue,
            Transform2D transform2DValue => transform2DValue,
            Vector4 vector4Value => vector4Value,
            Vector4I vector4IValue => vector4IValue,
            Plane planeValue => planeValue,
            Quaternion quaternionValue => quaternionValue,
            Aabb aabbValue => aabbValue,
            Basis basisValue => basisValue,
            Transform3D transform3DValue => transform3DValue,
            Projection projectionValue => projectionValue,
            Color colorValue => colorValue,
            Rid ridValue => ridValue,
            Callable callableValue => callableValue,
            Signal signalValue => signalValue,
            Godot.Collections.Array arrayValue => arrayValue,
            Godot.Collections.Dictionary dictionaryValue => dictionaryValue,
            byte[] byteArrayValue => byteArrayValue,
            int[] intArrayValue => intArrayValue,
            long[] longArrayValue => longArrayValue,
            float[] floatArrayValue => floatArrayValue,
            double[] doubleArrayValue => doubleArrayValue,
            string[] stringArrayValue => stringArrayValue,
            _ => throw new InvalidOperationException($"类型 '{value.GetType().FullName}' 不能直接转换为 Godot Variant。")
        };
    }

    private static T ConvertVariantToTarget<T>(Variant variant)
    {
        var targetType = typeof(T);

        if (targetType == typeof(bool))
        {
            return (T)(object)(bool)variant;
        }

        if (targetType == typeof(char))
        {
            var stringValue = (string)variant;
            return (T)(object)(string.IsNullOrEmpty(stringValue) ? '\0' : stringValue[0]);
        }

        if (targetType == typeof(byte))
        {
            return (T)(object)checked((byte)(long)variant);
        }

        if (targetType == typeof(sbyte))
        {
            return (T)(object)checked((sbyte)(long)variant);
        }

        if (targetType == typeof(short))
        {
            return (T)(object)checked((short)(long)variant);
        }

        if (targetType == typeof(ushort))
        {
            return (T)(object)checked((ushort)(long)variant);
        }

        if (targetType == typeof(int))
        {
            return (T)(object)checked((int)(long)variant);
        }

        if (targetType == typeof(uint))
        {
            return (T)(object)checked((uint)(long)variant);
        }

        if (targetType == typeof(long))
        {
            return (T)(object)(long)variant;
        }

        if (targetType == typeof(float))
        {
            return (T)(object)(float)(double)variant;
        }

        if (targetType == typeof(double))
        {
            return (T)(object)(double)variant;
        }

        if (targetType == typeof(string))
        {
            return (T)(object)(string)variant;
        }

        if (targetType == typeof(StringName))
        {
            return (T)(object)(StringName)variant;
        }

        if (targetType == typeof(NodePath))
        {
            return (T)(object)(NodePath)variant;
        }

        if (targetType == typeof(Vector2))
        {
            return (T)(object)(Vector2)variant;
        }

        if (targetType == typeof(Vector2I))
        {
            return (T)(object)(Vector2I)variant;
        }

        if (targetType == typeof(Rect2))
        {
            return (T)(object)(Rect2)variant;
        }

        if (targetType == typeof(Rect2I))
        {
            return (T)(object)(Rect2I)variant;
        }

        if (targetType == typeof(Vector3))
        {
            return (T)(object)(Vector3)variant;
        }

        if (targetType == typeof(Vector3I))
        {
            return (T)(object)(Vector3I)variant;
        }

        if (targetType == typeof(Transform2D))
        {
            return (T)(object)(Transform2D)variant;
        }

        if (targetType == typeof(Vector4))
        {
            return (T)(object)(Vector4)variant;
        }

        if (targetType == typeof(Vector4I))
        {
            return (T)(object)(Vector4I)variant;
        }

        if (targetType == typeof(Plane))
        {
            return (T)(object)(Plane)variant;
        }

        if (targetType == typeof(Quaternion))
        {
            return (T)(object)(Quaternion)variant;
        }

        if (targetType == typeof(Aabb))
        {
            return (T)(object)(Aabb)variant;
        }

        if (targetType == typeof(Basis))
        {
            return (T)(object)(Basis)variant;
        }

        if (targetType == typeof(Transform3D))
        {
            return (T)(object)(Transform3D)variant;
        }

        if (targetType == typeof(Projection))
        {
            return (T)(object)(Projection)variant;
        }

        if (targetType == typeof(Color))
        {
            return (T)(object)(Color)variant;
        }

        if (targetType == typeof(Rid))
        {
            return (T)(object)(Rid)variant;
        }

        if (targetType == typeof(Callable))
        {
            return (T)(object)(Callable)variant;
        }

        if (targetType == typeof(Signal))
        {
            return (T)(object)(Signal)variant;
        }

        if (targetType == typeof(Godot.Collections.Array))
        {
            return (T)(object)(Godot.Collections.Array)variant;
        }

        if (targetType == typeof(Godot.Collections.Dictionary))
        {
            return (T)(object)(Godot.Collections.Dictionary)variant;
        }

        if (targetType == typeof(byte[]))
        {
            return (T)(object)(byte[])variant;
        }

        if (targetType == typeof(int[]))
        {
            return (T)(object)(int[])variant;
        }

        if (targetType == typeof(long[]))
        {
            return (T)(object)(long[])variant;
        }

        if (targetType == typeof(float[]))
        {
            return (T)(object)(float[])variant;
        }

        if (targetType == typeof(double[]))
        {
            return (T)(object)(double[])variant;
        }

        if (targetType == typeof(string[]))
        {
            return (T)(object)(string[])variant;
        }

        throw new InvalidOperationException($"类型 '{targetType.FullName}' 暂不支持从 Godot Variant 直接读取。");
    }

    private static string BuildFilePath(string key)
    {
        return $"{SaveDirectoryPath}/{EncodeKeyAsFileName(key)}{FileExtension}";
    }

    private static string GetAbsoluteFilePath(string key)
    {
        return ProjectSettings.GlobalizePath(BuildFilePath(key));
    }

    private static string EncodeKeyAsFileName(string key)
    {
        return Uri.EscapeDataString(key.Trim());
    }

    private static string BeautifyLegacyKey(string storageKey)
    {
        var decodedKey = Uri.UnescapeDataString(storageKey);
        var separatorIndex = decodedKey.LastIndexOf('_');
        if (separatorIndex <= 0)
        {
            return decodedKey;
        }

        var suffix = decodedKey[(separatorIndex + 1)..];
        if (suffix.Length != 12)
        {
            return decodedKey;
        }

        foreach (var character in suffix)
        {
            if (!Uri.IsHexDigit(character))
            {
                return decodedKey;
            }
        }

        return decodedKey[..separatorIndex];
    }

    private static bool TryResolveFilePath(string key, out string filePath)
    {
        filePath = string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            GD.PushError("SimpleBinarySaver 的 key 不能为空或纯空白。");
            return false;
        }

        EnsureSaveDirectory();
        filePath = BuildFilePath(key);
        return true;
    }

    private static void EnsureSaveDirectory()
    {
        Directory.CreateDirectory(GetSaveDirectoryAbsolutePath());
    }

    private enum SerializedDataKind : byte
    {
        Null = 0,
        GodotVariant = 1,
        JsonObject = 2
    }

    private sealed class SimpleBinarySaverPackage
    {
        public string Key { get; init; } = string.Empty;

        public string TypeName { get; init; } = string.Empty;

        public SerializedDataKind DataKind { get; init; }

        public byte[] Payload { get; init; } = Array.Empty<byte>();
    }
}
