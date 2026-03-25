using Godot;

namespace YusGameFrame.YusAudioSystem;

[GlobalClass]
public partial class YusAudioEventBinding : Resource
{
    [Export]
    public string EventId { get; set; } = string.Empty;

    [Export]
    public YusAudioEventAction Action { get; set; } = YusAudioEventAction.PlayBoundAudio;

    [Export]
    public string TargetAudioId { get; set; } = string.Empty;

    [Export(PropertyHint.Range, "0,10,0.01,or_greater")]
    public float DelaySeconds { get; set; }

    [Export(PropertyHint.Range, "0,10,0.01,or_greater")]
    public float FadeSeconds { get; set; }

    [Export(PropertyHint.Range, "1,32,1,or_greater")]
    public int Times { get; set; } = 1;

    [Export(PropertyHint.Range, "0,10,0.01,or_greater")]
    public float IntervalSeconds { get; set; }
}
