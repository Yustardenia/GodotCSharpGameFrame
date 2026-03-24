using Godot;
using Godot.Collections;

namespace YusGameFrame.YusEventSystem;

[GlobalClass]
[Tool]
public partial class YusEventDefinitionConfig : Resource
{
    [Export]
    public Array<YusEventDefinitionItem> Events { get; set; } = [];

    [ExportToolButton("Generate C# API", Icon = "Reload")]
    public Callable GenerateButton => Callable.From(GenerateCode);

    public void GenerateCode()
    {
        YusEventCodeGenerator.Generate(this);
    }
}
