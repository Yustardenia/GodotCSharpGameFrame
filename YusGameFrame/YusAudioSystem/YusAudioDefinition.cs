using Godot;
using System;
using System.Collections.Generic;

namespace YusGameFrame.YusAudioSystem;

[GlobalClass]
public partial class YusAudioDefinition : Resource
{
    [Export]
    public string AudioId { get; set; } = string.Empty;

    [Export]
    public YusAudioCategory Category { get; set; } = YusAudioCategory.Sfx;

    [Export]
    public string BusName { get; set; } = string.Empty;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float VolumeScale { get; set; } = 1f;

    [Export(PropertyHint.Range, "0.1,4,0.01,or_greater")]
    public float PitchScale { get; set; } = 1f;

    [Export]
    public bool AllowReplayWhilePlaying { get; set; } = true;

    [Export(PropertyHint.Range, "1,64,1,or_greater")]
    public int MaxConcurrentCount { get; set; } = 8;

    [Export(PropertyHint.Range, "0,10,0.01,or_greater")]
    public float MinReplayIntervalSeconds { get; set; }

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float RandomVolumeMin { get; set; } = 1f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float RandomVolumeMax { get; set; } = 1f;

    [Export(PropertyHint.Range, "0.1,4,0.01,or_greater")]
    public float RandomPitchMin { get; set; } = 1f;

    [Export(PropertyHint.Range, "0.1,4,0.01,or_greater")]
    public float RandomPitchMax { get; set; } = 1f;

    [Export]
    public YusBgmPlaybackMode BgmPlaybackMode { get; set; } = YusBgmPlaybackMode.SingleLoop;

    [Export]
    public AudioStream? SingleStream { get; set; }

    [Export]
    public AudioStream? IntroStream { get; set; }

    [Export]
    public AudioStream? LoopStream { get; set; }

    [Export]
    public AudioStream? OutroStream { get; set; }

    [Export]
    public bool WaitLoopBoundaryBeforeOutro { get; set; } = true;

    [Export]
    public string[] OnEnterEventIds { get; set; } = Array.Empty<string>();

    [Export]
    public string[] OnExitEventIds { get; set; } = Array.Empty<string>();

    [Export]
    public YusAudioEventBinding[] EventBindings { get; set; } = Array.Empty<YusAudioEventBinding>();

    public bool HasAnyPlayableStream()
    {
        return Category == YusAudioCategory.Bgm
            ? BgmPlaybackMode switch
            {
                YusBgmPlaybackMode.SingleLoop => SingleStream != null,
                YusBgmPlaybackMode.IntroLoopOutro => IntroStream != null || LoopStream != null || OutroStream != null,
                _ => false
            }
            : SingleStream != null;
    }

    public IEnumerable<string> GetValidationMessages()
    {
        if (string.IsNullOrWhiteSpace(AudioId))
        {
            yield return "存在未填写 AudioId 的音频条目。";
        }

        if (VolumeScale < 0f || VolumeScale > 1f)
        {
            yield return $"音频 '{AudioId}' 的 VolumeScale 必须位于 0 到 1 之间。";
        }

        if (RandomVolumeMin > RandomVolumeMax)
        {
            yield return $"音频 '{AudioId}' 的随机音量范围无效。";
        }

        if (RandomPitchMin > RandomPitchMax)
        {
            yield return $"音频 '{AudioId}' 的随机音高范围无效。";
        }

        if (Category == YusAudioCategory.Bgm)
        {
            if (BgmPlaybackMode == YusBgmPlaybackMode.SingleLoop && SingleStream == null)
            {
                yield return $"BGM '{AudioId}' 处于 SingleLoop 模式，但没有设置 SingleStream。";
            }

            if (BgmPlaybackMode == YusBgmPlaybackMode.IntroLoopOutro &&
                IntroStream == null &&
                LoopStream == null &&
                OutroStream == null)
            {
                yield return $"BGM '{AudioId}' 处于 IntroLoopOutro 模式，但 Intro/Loop/Outro 都为空。";
            }
        }
        else if (SingleStream == null)
        {
            yield return $"音频 '{AudioId}' 不是 BGM，但没有设置 SingleStream。";
        }
    }

    public IEnumerable<YusAudioEventBinding> GetImplicitEventBindings()
    {
        foreach (var eventId in OnEnterEventIds)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                continue;
            }

            yield return new YusAudioEventBinding
            {
                EventId = eventId.Trim(),
                Action = YusAudioEventAction.PlayBoundAudio,
                TargetAudioId = AudioId
            };
        }

        foreach (var eventId in OnExitEventIds)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                continue;
            }

            yield return new YusAudioEventBinding
            {
                EventId = eventId.Trim(),
                Action = Category == YusAudioCategory.Bgm
                    ? YusAudioEventAction.RequestCurrentBgmOutro
                    : YusAudioEventAction.StopCurrentBgm,
                TargetAudioId = AudioId
            };
        }

        foreach (var binding in EventBindings)
        {
            if (binding != null)
            {
                yield return binding;
            }
        }
    }
}
