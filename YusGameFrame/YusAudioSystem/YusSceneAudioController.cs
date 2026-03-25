using Godot;

namespace YusGameFrame.YusAudioSystem;

[GlobalClass]
public partial class YusSceneAudioController : Node
{
    [Export]
    public YusSceneAudioProfile? Profile { get; set; }

    public override void _Ready()
    {
        if (Profile != null && !Engine.IsEditorHint())
        {
            YusAudioService.RequireInstance().AttachSceneProfile(this, Profile);
        }
    }

    public override void _ExitTree()
    {
        if (!Engine.IsEditorHint())
        {
            YusAudioService.InstanceOrNull?.DetachSceneProfile(this);
        }
    }

    public void ApplyProfileNow()
    {
        if (Profile != null)
        {
            YusAudioService.RequireInstance().AttachSceneProfile(this, Profile);
        }
    }
}
