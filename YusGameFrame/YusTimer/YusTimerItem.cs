using System;

namespace YusGameFrame.YusTimer;

internal sealed class YusTimerItem
{
    public required long Id { get; init; }
    public required double Duration { get; init; }
    public required double RemainingTime { get; set; }
    public required bool IsLooping { get; init; }
    public required Action Callback { get; init; }
    public required YusTimerState State { get; set; }
    public string? OwnerTag { get; init; }
}
