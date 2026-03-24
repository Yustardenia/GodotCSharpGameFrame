using Godot;
using YusGameFrame.YusEventSystem;

public partial class YusEventSystemTest : Control
{
    private Label _statusLabel = null!;
    private Label _logLabel = null!;
    private AutoListenerNode? _autoListenerNode;
    private int _playerHp = -1;
    private int _damageTotal;
    private int _questStep = -1;
    private bool _questFinished;
    private bool _exceptionFlowVerified;

    public override void _Ready()
    {
        BuildUi();
        RegisterMainListeners();
        SpawnAutoListenerNode();
        SetStatus("事件监听已注册，点击下方按钮广播事件。");
        AppendLog("YusEventSystem 测试界面已就绪。");
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

        layout.AddChild(new Label { Text = "YusEventSystem 测试" });

        _statusLabel = new Label();
        _statusLabel.Text = "正在构建测试界面……";
        layout.AddChild(_statusLabel);

        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", 8);
        layout.AddChild(buttonRow);

        buttonRow.AddChild(CreateButton("广播全部", BroadcastAll));
        buttonRow.AddChild(CreateButton("血量变化", BroadcastHpChanged));
        buttonRow.AddChild(CreateButton("伤害事件", BroadcastDamage));
        buttonRow.AddChild(CreateButton("任务事件", BroadcastQuest));
        buttonRow.AddChild(CreateButton("释放监听节点", FreeAutoListener));

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

    private void RegisterMainListeners()
    {
        YusEventSignals.GameStartedEvent.AddListener(this, OnGameStarted);
        YusEventSignals.PlayerHpChangedEvent.AddListener(this, OnPlayerHpChanged);
        YusEventSignals.DamageReportedEvent.AddListener(this, OnDamageReported);
        YusEventSignals.DamageReportedEvent.AddListener(this, OnDamageReportedThrows);
        YusEventSignals.DamageReportedEvent.AddListener(this, OnDamageReportedSecondListener);
        YusEventSignals.QuestStateChangedEvent.AddListener(this, OnQuestStateChanged);
    }

    private void SpawnAutoListenerNode()
    {
        _autoListenerNode?.QueueFree();

        _autoListenerNode = new AutoListenerNode();
        _autoListenerNode.LogRaised += AppendLog;
        AddChild(_autoListenerNode);
        AppendLog("已创建自动监听节点，它在释放后会自动取消订阅。");
    }

    private void BroadcastAll()
    {
        _playerHp = -1;
        _damageTotal = 0;
        _questStep = -1;
        _questFinished = false;
        _exceptionFlowVerified = false;

        AppendLog("正在广播全部示例事件。");
        YusEventSignals.GameStartedEvent.Broadcast();
        YusEventSignals.PlayerHpChangedEvent.Broadcast(120);
        YusEventSignals.DamageReportedEvent.Broadcast("Slime", 23);
        YusEventSignals.DamageReportedEvent.RemoveListener(OnDamageReportedThrows);
        AppendLog("已手动移除会抛异常的伤害监听。");
        YusEventSignals.QuestStateChangedEvent.Broadcast("MainQuest", 2, true);
        ValidateResults();
    }

    private void BroadcastHpChanged()
    {
        AppendLog("正在广播 PlayerHpChanged(88)。");
        YusEventSignals.PlayerHpChangedEvent.Broadcast(88);
        SetStatus("已广播 PlayerHpChanged。");
    }

    private void BroadcastDamage()
    {
        AppendLog("正在广播 DamageReported(\"Bat\", 10)。");
        YusEventSignals.DamageReportedEvent.Broadcast("Bat", 10);
        SetStatus("已广播 DamageReported。");
    }

    private void BroadcastQuest()
    {
        AppendLog("正在广播 QuestStateChanged(\"SideQuest\", 1, false)。");
        YusEventSignals.QuestStateChangedEvent.Broadcast("SideQuest", 1, false);
        SetStatus("已广播 QuestStateChanged。");
    }

    private void FreeAutoListener()
    {
        if (_autoListenerNode == null || !GodotObject.IsInstanceValid(_autoListenerNode))
        {
            AppendLog("自动监听节点已经不存在了。");
            return;
        }

        _autoListenerNode.QueueFree();
        _autoListenerNode = null;
        AppendLog("已请求释放自动监听节点，后续广播不应再触发它。");
        SetStatus("自动监听节点已释放，其订阅会自动移除。");
    }

    private void OnGameStarted()
    {
        AppendLog("主监听已收到 GameStarted。");
    }

    private void OnPlayerHpChanged(int hp)
    {
        _playerHp = hp;
        AppendLog($"主监听已收到 PlayerHpChanged：{hp}。");
    }

    private void OnDamageReported(string source, int damage)
    {
        _damageTotal += damage;
        AppendLog($"主监听已收到 DamageReported，来源：{source}，数值：{damage}。");
    }

    private void OnDamageReportedThrows(string source, int damage)
    {
        _exceptionFlowVerified = true;
        throw new System.InvalidOperationException($"模拟监听异常：{source} / {damage}。");
    }

    private void OnDamageReportedSecondListener(string source, int damage)
    {
        AppendLog($"副监听仍然收到了 DamageReported，来源：{source}，数值：{damage}。");
    }

    private void OnQuestStateChanged(string questId, int step, bool isCompleted)
    {
        _questStep = step;
        _questFinished = isCompleted;
        AppendLog($"主监听已收到 QuestStateChanged：{questId}，阶段={step}，完成={isCompleted}。");
    }

    private void ValidateResults()
    {
        var passed = _playerHp == 120
            && _damageTotal == 23
            && _questStep == 2
            && _questFinished
            && _exceptionFlowVerified;

        if (!passed)
        {
            GD.PushError("[YusEventSystemTest] 校验失败。");
            SetStatus("校验失败，请查看日志区域。");
            return;
        }

        SetStatus("校验通过，自动清理和手动移除都已生效。");
        AppendLog("校验通过。");
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private void AppendLog(string message)
    {
        GD.Print($"[YusEventSystemTest] {message}");
        _logLabel.Text = _logLabel.Text == "日志会显示在这里。"
            ? message
            : $"{_logLabel.Text}\n{message}";
    }

    private partial class AutoListenerNode : Node
    {
        public event System.Action<string>? LogRaised;

        public override void _Ready()
        {
            YusEventSignals.GameStartedEvent.AddListener(this, OnGameStarted);
            YusEventSignals.PlayerHpChangedEvent.AddListener(this, OnPlayerHpChanged);
        }

        private void OnGameStarted()
        {
            LogRaised?.Invoke("自动监听节点已收到 GameStarted。");
        }

        private void OnPlayerHpChanged(int hp)
        {
            LogRaised?.Invoke($"自动监听节点已收到 PlayerHpChanged：{hp}。");
        }
    }
}
