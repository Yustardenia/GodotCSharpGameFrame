using Godot;
using System;
using System.Collections.Generic;

namespace YusGameFrame.YusTimer;

public partial class YusTimerService : Node
{
    public static YusTimerService Instance { get; private set; } = null!;

    private readonly Dictionary<long, YusTimerItem> _timers = new();
    private readonly Dictionary<long, YusTimerState> _finalStates = new();
    private readonly List<long> _pendingRemovalIds = new();
    private long _nextTimerId = 1;

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            GD.PushError("YusTimerService already has an active instance.");
            QueueFree();
            return;
        }

        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null!;
        }
    }

    public override void _Process(double delta)
    {
        if (_timers.Count == 0)
        {
            return;
        }

        var timerIds = new List<long>(_timers.Keys);
        foreach (var timerId in timerIds)
        {
            if (!_timers.TryGetValue(timerId, out var timer))
            {
                continue;
            }

            if (timer.State != YusTimerState.Running)
            {
                if (timer.State is YusTimerState.Cancelled or YusTimerState.Completed)
                {
                    _pendingRemovalIds.Add(timer.Id);
                }

                continue;
            }

            timer.RemainingTime -= delta;
            if (timer.RemainingTime > 0)
            {
                continue;
            }

            InvokeCallback(timer);
        }

        FlushPendingRemovals();
    }

    public YusTimerHandle ScheduleOnce(double durationSeconds, Action callback, string? ownerTag = null)
    {
        return CreateTimer(durationSeconds, false, callback, ownerTag);
    }

    public YusTimerHandle ScheduleLoop(double intervalSeconds, Action callback, string? ownerTag = null)
    {
        return CreateTimer(intervalSeconds, true, callback, ownerTag);
    }

    internal bool HasKnownTimer(long id)
    {
        return _timers.ContainsKey(id) || _finalStates.ContainsKey(id);
    }

    internal YusTimerState GetState(long id)
    {
        if (_timers.TryGetValue(id, out var timer))
        {
            return timer.State;
        }

        return _finalStates.TryGetValue(id, out var finalState)
            ? finalState
            : YusTimerState.Completed;
    }

    internal void Cancel(long id)
    {
        if (_timers.TryGetValue(id, out var timer))
        {
            timer.State = YusTimerState.Cancelled;
        }
    }

    internal void Pause(long id)
    {
        if (_timers.TryGetValue(id, out var timer) && timer.State == YusTimerState.Running)
        {
            timer.State = YusTimerState.Paused;
        }
    }

    internal void Resume(long id)
    {
        if (_timers.TryGetValue(id, out var timer) && timer.State == YusTimerState.Paused)
        {
            timer.State = YusTimerState.Running;
        }
    }

    private YusTimerHandle CreateTimer(double durationSeconds, bool isLooping, Action callback, string? ownerTag)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (durationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Timer duration must be greater than zero.");
        }

        var id = _nextTimerId++;
        var item = new YusTimerItem
        {
            Id = id,
            Duration = durationSeconds,
            RemainingTime = durationSeconds,
            IsLooping = isLooping,
            Callback = callback,
            State = YusTimerState.Running,
            OwnerTag = ownerTag
        };

        _timers.Add(id, item);
        return new YusTimerHandle(this, id);
    }

    private void InvokeCallback(YusTimerItem timer)
    {
        try
        {
            timer.Callback.Invoke();
        }
        catch (Exception exception)
        {
            GD.PushError($"YusTimer callback failed for timer {timer.Id}: {exception}");
        }

        if (!_timers.TryGetValue(timer.Id, out var currentTimer))
        {
            return;
        }

        if (currentTimer.State == YusTimerState.Cancelled)
        {
            _pendingRemovalIds.Add(currentTimer.Id);
            return;
        }

        if (currentTimer.IsLooping)
        {
            currentTimer.RemainingTime += currentTimer.Duration;
            return;
        }

        currentTimer.State = YusTimerState.Completed;
        _pendingRemovalIds.Add(currentTimer.Id);
    }

    private void FlushPendingRemovals()
    {
        if (_pendingRemovalIds.Count == 0)
        {
            return;
        }

        foreach (var id in _pendingRemovalIds)
        {
            if (_timers.TryGetValue(id, out var timer))
            {
                _finalStates[id] = timer.State;
            }

            _timers.Remove(id);
        }

        _pendingRemovalIds.Clear();
    }
}
