using Godot;
using System;

namespace YusGameFrame.YusAudioSystem;

[GlobalClass]
public partial class YusSceneAudioProfile : Resource
{
    [Export]
    public string DefaultBgmAudioId { get; set; } = string.Empty;

    [Export]
    public YusSceneBgmEnterPolicy EnterPolicy { get; set; } = YusSceneBgmEnterPolicy.PlayIfDifferent;

    [Export]
    public YusSceneBgmExitPolicy ExitPolicy { get; set; } = YusSceneBgmExitPolicy.DoNothing;

    [Export(PropertyHint.Range, "0,10,0.01,or_greater")]
    public float EnterFadeSeconds { get; set; } = 0.4f;

    [Export(PropertyHint.Range, "0,10,0.01,or_greater")]
    public float ExitFadeSeconds { get; set; } = 0.4f;

    [Export]
    public YusAudioLibrary? OverrideLibrary { get; set; }

    [Export]
    public YusAudioLibrary[] AdditionalLibraries { get; set; } = Array.Empty<YusAudioLibrary>();

    [Export]
    public YusAudioEventBinding[] EventBindings { get; set; } = Array.Empty<YusAudioEventBinding>();
}
