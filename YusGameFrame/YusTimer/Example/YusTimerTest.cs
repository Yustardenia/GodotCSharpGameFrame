using Godot;
using YusGameFrame.YusTimer;

public partial class YusTimerTest : Control
{
    private Label _statusLabel = null!;
    private Label _logLabel = null!;
    private int _loopCount;
    private YusTimerHandle? _loopHandle;

    public override void _Ready()
    {
        BuildUi();
        AppendLog("YusTimer 测试界面已就绪。");
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

        var title = new Label();
        title.Text = "YusTimer 测试";
        layout.AddChild(title);

        _statusLabel = new Label();
        _statusLabel.Text = "点击下方按钮运行对应的计时器测试。";
        layout.AddChild(_statusLabel);

        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", 8);
        layout.AddChild(buttonRow);

        buttonRow.AddChild(CreateButton("单次计时", RunOneShotTest));
        buttonRow.AddChild(CreateButton("循环计时", RunLoopTest));
        buttonRow.AddChild(CreateButton("暂停恢复", RunPauseResumeTest));
        buttonRow.AddChild(CreateButton("取消测试", RunCancelTest));
        buttonRow.AddChild(CreateButton("全部运行", RunAllTests));

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
        button.CustomMinimumSize = new Vector2(120, 42);
        button.Pressed += pressedCallback;
        return button;
    }

    private void RunOneShotTest()
    {
        SetStatus("正在运行单次计时测试……");
        AppendLog("已创建一个 1 秒后触发的单次计时器。");

        YusTimerService.Instance.ScheduleOnce(1.0, () =>
        {
            SetStatus("单次计时测试完成。");
            AppendLog("单次计时器回调已触发。");
        }, ownerTag: "UiOneShot");
    }

    private void RunLoopTest()
    {
        _loopCount = 0;
        _loopHandle?.Cancel();

        SetStatus("正在运行循环计时测试……");
        AppendLog("已创建一个每 0.5 秒触发一次的循环计时器。");

        _loopHandle = YusTimerService.Instance.ScheduleLoop(0.5, () =>
        {
            _loopCount++;
            AppendLog($"循环计时器已触发第 {_loopCount} 次。");

            if (_loopCount < 3)
            {
                return;
            }

            _loopHandle?.Cancel();
            SetStatus("循环计时测试完成，已触发 3 次。");
            AppendLog("循环计时器在第 3 次触发后已取消。");
        }, ownerTag: "UiLoop");
    }

    private void RunPauseResumeTest()
    {
        SetStatus("正在运行暂停恢复测试……");
        AppendLog("已创建一个 2 秒计时器，并准备执行暂停与恢复。");

        var handle = YusTimerService.Instance.ScheduleOnce(2.0, () =>
        {
            SetStatus("暂停恢复测试完成。");
            AppendLog("计时器已恢复并最终执行完成。");
        }, ownerTag: "UiPauseResume");

        YusTimerService.Instance.ScheduleOnce(0.6, () =>
        {
            handle.Pause();
            AppendLog("计时器已在 0.6 秒时暂停。");
        }, ownerTag: "UiPause");

        YusTimerService.Instance.ScheduleOnce(1.6, () =>
        {
            handle.Resume();
            AppendLog("计时器已在 1.6 秒时恢复。");
        }, ownerTag: "UiResume");
    }

    private void RunCancelTest()
    {
        SetStatus("正在运行取消测试……");
        AppendLog("已创建一个计时器，并立刻尝试取消。");

        var handle = YusTimerService.Instance.ScheduleOnce(1.0, () =>
        {
            SetStatus("取消测试失败。");
            AppendLog("本应被取消的计时器仍然执行了。");
            GD.PushError("[YusTimerTest] 本应被取消的计时器仍然执行了。");
        }, ownerTag: "UiCancel");

        handle.Cancel();
        SetStatus(handle.IsCancelled ? "取消测试通过。" : "取消测试失败。");
        AppendLog(handle.IsCancelled
            ? "计时器已成功取消。"
            : "计时器没有进入取消状态。");
    }

    private void RunAllTests()
    {
        AppendLog("开始顺序运行全部计时器测试。");
        RunOneShotTest();
        YusTimerService.Instance.ScheduleOnce(0.1, RunLoopTest, ownerTag: "UiAllLoop");
        YusTimerService.Instance.ScheduleOnce(0.2, RunPauseResumeTest, ownerTag: "UiAllPauseResume");
        YusTimerService.Instance.ScheduleOnce(0.3, RunCancelTest, ownerTag: "UiAllCancel");
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private void AppendLog(string message)
    {
        GD.Print($"[YusTimerTest] {message}");
        _logLabel.Text = _logLabel.Text == "日志会显示在这里。"
            ? message
            : $"{_logLabel.Text}\n{message}";
    }
}
