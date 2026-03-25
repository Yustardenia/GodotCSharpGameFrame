using Godot;

[Tool]
public partial class YusAudioEditorPlugin : EditorPlugin
{
    private YusAudioEditorDock? _dock;

    public override void _EnterTree()
    {
        _dock = new YusAudioEditorDock(this);
        _dock.Name = "YusAudio";
#pragma warning disable CS0618
        AddControlToDock(DockSlot.RightUl, _dock);
#pragma warning restore CS0618
    }

    public override void _ExitTree()
    {
        if (_dock == null)
        {
            return;
        }

#pragma warning disable CS0618
        RemoveControlFromDocks(_dock);
#pragma warning restore CS0618
        _dock.QueueFree();
        _dock = null;
    }
}
