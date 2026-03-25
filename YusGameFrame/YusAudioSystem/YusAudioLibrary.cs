using Godot;
using System;
using System.Collections.Generic;

namespace YusGameFrame.YusAudioSystem;

[GlobalClass]
[Tool]
public partial class YusAudioLibrary : Resource
{
    [Export]
    public YusAudioDefinition[] Definitions { get; set; } = Array.Empty<YusAudioDefinition>();

    [ExportToolButton("校验音频库", Icon = "Search")]
    public Callable ValidateButton => Callable.From(LogValidationMessages);

    private Dictionary<string, YusAudioDefinition>? _index;

    public void Initialize()
    {
        _index = new Dictionary<string, YusAudioDefinition>(StringComparer.Ordinal);

        foreach (var definition in Definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.AudioId))
            {
                continue;
            }

            if (!_index.TryAdd(definition.AudioId.Trim(), definition))
            {
                GD.PushWarning($"[YusAudioLibrary] 检测到重复的 AudioId：{definition.AudioId}");
            }
        }
    }

    public bool TryGetDefinition(string audioId, out YusAudioDefinition definition)
    {
        if (_index == null)
        {
            Initialize();
        }

        if (_index != null && _index.TryGetValue(audioId, out definition!))
        {
            return true;
        }

        definition = null!;
        return false;
    }

    public string[] GetValidationMessages()
    {
        var messages = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var definition in Definitions)
        {
            if (definition == null)
            {
                messages.Add("音频库中存在空条目。");
                continue;
            }

            foreach (var message in definition.GetValidationMessages())
            {
                messages.Add(message);
            }

            if (!string.IsNullOrWhiteSpace(definition.AudioId) &&
                !ids.Add(definition.AudioId.Trim()))
            {
                messages.Add($"检测到重复的 AudioId：{definition.AudioId}");
            }
        }

        return messages.ToArray();
    }

    private void LogValidationMessages()
    {
        var messages = GetValidationMessages();
        if (messages.Length == 0)
        {
            GD.Print("[YusAudioLibrary] 校验通过，没有发现问题。");
            return;
        }

        foreach (var message in messages)
        {
            GD.PushWarning($"[YusAudioLibrary] {message}");
        }
    }
}
