using Godot;
using Godot.Collections;

namespace YusGameFrame.YusEventSystem;

[GlobalClass]
[Tool]
public partial class YusEventDefinitionItem : Resource
{
    [Export]
    public string EventName { get; set; } = string.Empty;

    [Export(PropertyHint.Range, "0,3,1")]
    public int ParameterCount
    {
        get => _parameterCount;
        set => _parameterCount = Mathf.Clamp(value, 0, 3);
    }

    [Export]
    public Array<string> ParameterTypes { get; set; } = [];

    private int _parameterCount;
}
