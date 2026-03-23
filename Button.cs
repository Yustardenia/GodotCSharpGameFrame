using Godot;
using YusGameFrame.YusTimer;

public partial class Button : Godot.Button
{
    private Label _label = null!;
    private YusTimerHandle? _pendingHandle;

    public override void _Ready()
    {
        _label = GetNode<Label>("../Label");
        _label.Text = "点击按钮后，2 秒后会执行 YusTimer 回调。";
    }

    public void _on_pressed()
    {
        _pendingHandle?.Cancel();
        _label.Text = "YusTimer 已创建，等待回调中...";

        _pendingHandle = YusTimerService.Instance.ScheduleOnce(2.0, () =>
        {
            _label.Text = "YusTimer 回调已执行。";
        }, ownerTag: "DemoButton");
    }
}
