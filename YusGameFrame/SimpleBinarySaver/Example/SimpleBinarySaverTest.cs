using Godot;
using Godot.Collections;
using YusGameFrame.SimpleBinarySaver;

public partial class SimpleBinarySaverTest : Control
{
    private Label _statusLabel = null!;
    private Label _logLabel = null!;

    public override void _Ready()
    {
        BuildUi();
        SetStatus("SimpleBinarySaver 测试界面已就绪。");
        AppendLog("点击按钮即可执行保存与读取测试。");
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var root = new MarginContainer();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("margin_left", 24);
        root.AddThemeConstantOverride("margin_top", 24);
        root.AddThemeConstantOverride("margin_right", 24);
        root.AddThemeConstantOverride("margin_bottom", 24);
        AddChild(root);

        var layout = new VBoxContainer();
        layout.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        layout.SizeFlagsVertical = SizeFlags.ExpandFill;
        layout.AddThemeConstantOverride("separation", 12);
        root.AddChild(layout);

        layout.AddChild(new Label { Text = "SimpleBinarySaver 测试" });

        _statusLabel = new Label();
        _statusLabel.Text = "正在构建测试界面...";
        layout.AddChild(_statusLabel);

        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", 8);
        layout.AddChild(buttonRow);

        buttonRow.AddChild(CreateButton("整数测试", RunIntTest));
        buttonRow.AddChild(CreateButton("字符串测试", RunStringTest));
        buttonRow.AddChild(CreateButton("字典测试", RunDictionaryTest));
        buttonRow.AddChild(CreateButton("对象测试", RunPocoTest));
        buttonRow.AddChild(CreateButton("默认值测试", RunDefaultValueTest));
        buttonRow.AddChild(CreateButton("覆盖测试", RunOverwriteTest));

        _logLabel = new Label();
        _logLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _logLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
        _logLabel.Text = "日志会显示在这里。";
        layout.AddChild(_logLabel);
    }

    private Godot.Button CreateButton(string text, System.Action pressedCallback)
    {
        var button = new Godot.Button();
        button.Text = text;
        button.CustomMinimumSize = new Vector2(140, 42);
        button.Pressed += pressedCallback;
        return button;
    }

    private void RunIntTest()
    {
        const string key = "IntValue";
        SimpleBinarySaver.Save(123, key);
        var value = SimpleBinarySaver.Load(key, 0);
        var passed = value == 123;
        SetStatus(passed ? "整数测试通过。" : "整数测试失败。");
        AppendLog($"整数测试结果：读取到 {value}。");
    }

    private void RunStringTest()
    {
        const string key = "StringValue";
        const string savedValue = "这是一个字符串存档示例";
        SimpleBinarySaver.Save(savedValue, key);
        var value = SimpleBinarySaver.Load(key, string.Empty);
        var passed = value == savedValue;
        SetStatus(passed ? "字符串测试通过。" : "字符串测试失败。");
        AppendLog($"字符串测试结果：读取到“{value}”。");
    }

    private void RunDictionaryTest()
    {
        const string key = "DictionaryValue";
        var dictionary = new Dictionary
        {
            { "Name", "史莱姆" },
            { "Hp", 88 },
            { "IsElite", false }
        };

        SimpleBinarySaver.Save(dictionary, key);
        var value = SimpleBinarySaver.Load(key, new Dictionary());
        var passed = value.Count == 3
            && value["Name"].AsString() == "史莱姆"
            && value["Hp"].AsInt32() == 88;

        SetStatus(passed ? "字典测试通过。" : "字典测试失败。");
        AppendLog($"字典测试结果：键数量 {value.Count}。");
    }

    private void RunPocoTest()
    {
        const string key = "PlayerArchive";
        var playerArchive = new PlayerArchive
        {
            Name = "测试勇者",
            Level = 7,
            Gold = 256
        };

        SimpleBinarySaver.Save(playerArchive, key);
        var loadedValue = SimpleBinarySaver.Load(key, new PlayerArchive());
        var passed = loadedValue.Name == playerArchive.Name
            && loadedValue.Level == playerArchive.Level
            && loadedValue.Gold == playerArchive.Gold;

        SetStatus(passed ? "对象测试通过。" : "对象测试失败。");
        AppendLog($"对象测试结果：Name={loadedValue.Name}，Level={loadedValue.Level}，Gold={loadedValue.Gold}。");
    }

    private void RunDefaultValueTest()
    {
        var value = SimpleBinarySaver.Load("MissingValue", 999);
        var passed = value == 999;
        SetStatus(passed ? "默认值测试通过。" : "默认值测试失败。");
        AppendLog($"默认值测试结果：读取到 {value}。");
    }

    private void RunOverwriteTest()
    {
        const string key = "OverwriteValue";
        SimpleBinarySaver.Save(1, key);
        SimpleBinarySaver.Save(2, key);
        var value = SimpleBinarySaver.Load(key, 0);
        var passed = value == 2;
        SetStatus(passed ? "覆盖测试通过。" : "覆盖测试失败。");
        AppendLog($"覆盖测试结果：最终读取到 {value}。");
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private void AppendLog(string message)
    {
        GD.Print($"[SimpleBinarySaverTest] {message}");
        _logLabel.Text = _logLabel.Text == "日志会显示在这里。"
            ? message
            : $"{_logLabel.Text}\n{message}";
    }

    private sealed class PlayerArchive
    {
        public string Name { get; set; } = string.Empty;

        public int Level { get; set; }

        public int Gold { get; set; }
    }
}
