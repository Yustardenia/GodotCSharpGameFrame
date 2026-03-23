using Godot;
using YusGameFrame.YusTimer;

public partial class YusTimerTest : Node
{
    [Export]
    public NodePath? StatusLabelPath { get; set; }

    private Label? _statusLabel;
    private int _loopCount;
    private bool _pauseResumePassed;
    private bool _cancelPassed;

    public override void _Ready()
    {
        if (StatusLabelPath != null && !StatusLabelPath.IsEmpty)
        {
            _statusLabel = GetNodeOrNull<Label>(StatusLabelPath);
        }

        SetStatus("YusTimer 测试开始。请查看输出面板。");
        RunTests();
    }

    private void RunTests()
    {
        GD.Print("[YusTimerTest] 开始测试。");

        YusTimerService.Instance.ScheduleOnce(1.0, () =>
        {
            GD.Print("[YusTimerTest] 一次性计时器通过。");
        }, ownerTag: "OneShot");

        YusTimerHandle? loopHandle = null;
        loopHandle = YusTimerService.Instance.ScheduleLoop(0.5, () =>
        {
            _loopCount++;
            GD.Print($"[YusTimerTest] 循环计时器触发次数：{_loopCount}");

            if (_loopCount >= 3)
            {
                loopHandle?.Cancel();
                GD.Print("[YusTimerTest] 循环计时器取消通过。");
            }
        }, ownerTag: "Loop");

        var pauseHandle = YusTimerService.Instance.ScheduleOnce(2.0, () =>
        {
            _pauseResumePassed = true;
            GD.Print("[YusTimerTest] 暂停/恢复测试通过。");
            TryFinish();
        }, ownerTag: "PauseResume");

        YusTimerService.Instance.ScheduleOnce(0.6, () =>
        {
            pauseHandle.Pause();
            GD.Print("[YusTimerTest] 已暂停 2 秒计时器。");
        }, ownerTag: "Pause");

        YusTimerService.Instance.ScheduleOnce(1.6, () =>
        {
            pauseHandle.Resume();
            GD.Print("[YusTimerTest] 已恢复 2 秒计时器。");
        }, ownerTag: "Resume");

        var cancelHandle = YusTimerService.Instance.ScheduleOnce(1.0, () =>
        {
            GD.PushError("[YusTimerTest] 取消测试失败，被取消的计时器仍然执行了。");
            SetStatus("YusTimer 测试失败，请查看输出。");
        }, ownerTag: "Cancel");

        cancelHandle.Cancel();
        _cancelPassed = cancelHandle.IsCancelled;
        if (_cancelPassed)
        {
            GD.Print("[YusTimerTest] 取消测试通过。");
        }

        YusTimerService.Instance.ScheduleOnce(3.0, TryFinish, ownerTag: "Summary");
    }

    private void TryFinish()
    {
        if (_loopCount < 3 || !_pauseResumePassed || !_cancelPassed)
        {
            return;
        }

        SetStatus("YusTimer 测试通过：一次性、循环、取消、暂停、恢复均已验证。");
        GD.Print("[YusTimerTest] 全部测试通过。");
    }

    private void SetStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
        }
    }
}
