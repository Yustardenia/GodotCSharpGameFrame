# YusTimer 使用说明

## 功能概述
`YusTimer` 提供统一的运行时计时器服务，适合处理一次性延迟、循环触发、暂停、恢复和取消。

公共入口：
- `res://YusGameFrame/YusTimer/YusTimerService.cs`
- `res://YusGameFrame/YusTimer/YusTimerHandle.cs`

## 接入方式
项目已通过 `Autoload` 挂载 `YusTimerService`，运行时可直接使用：

```csharp
using YusGameFrame.YusTimer;
```

## 基本用法
一次性计时器：

```csharp
var handle = YusTimerService.Instance.ScheduleOnce(1.5, () =>
{
    GD.Print("1.5 秒后执行");
}, ownerTag: "DemoOnce");
```

循环计时器：

```csharp
YusTimerHandle? loopHandle = null;
loopHandle = YusTimerService.Instance.ScheduleLoop(0.5, () =>
{
    GD.Print("每 0.5 秒执行一次");
    loopHandle?.Cancel();
}, ownerTag: "DemoLoop");
```

暂停、恢复、取消：

```csharp
var handle = YusTimerService.Instance.ScheduleOnce(2.0, OnTimeout, ownerTag: "SkillCd");
handle.Pause();
handle.Resume();
handle.Cancel();
```

## 句柄说明
`YusTimerHandle` 可用于查询当前状态：
- `IsValid`
- `IsRunning`
- `IsPaused`
- `IsCancelled`
- `IsCompleted`

## 示例
可将 [YusTimerTest.cs](/e:/Stardenia/Godot/test/YusGameFrame/YusTimer/Example/YusTimerTest.cs) 挂到任意空 `Control` 节点。脚本会动态生成按钮与日志标签，点击即可测试：
- `单次计时`
- `循环计时`
- `暂停恢复`
- `取消测试`
- `全部运行`
