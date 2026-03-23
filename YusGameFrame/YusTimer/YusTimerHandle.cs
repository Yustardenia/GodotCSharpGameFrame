namespace YusGameFrame.YusTimer;

public sealed class YusTimerHandle
{
    private readonly YusTimerService _service;

    internal YusTimerHandle(YusTimerService service, long id)
    {
        _service = service;
        Id = id;
    }

    public long Id { get; }

    public bool IsValid => _service.HasKnownTimer(Id);

    public bool IsRunning => _service.GetState(Id) == YusTimerState.Running;

    public bool IsPaused => _service.GetState(Id) == YusTimerState.Paused;

    public bool IsCancelled => _service.GetState(Id) == YusTimerState.Cancelled;

    public bool IsCompleted => _service.GetState(Id) == YusTimerState.Completed;

    public void Cancel()
    {
        _service.Cancel(Id);
    }

    public void Pause()
    {
        _service.Pause(Id);
    }

    public void Resume()
    {
        _service.Resume(Id);
    }
}
