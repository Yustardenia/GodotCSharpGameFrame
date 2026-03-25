namespace YusGameFrame.YusAudioSystem;

public readonly struct YusAudioHandle
{
    private readonly YusAudioService? _service;

    internal YusAudioHandle(YusAudioService service, long playbackId)
    {
        _service = service;
        PlaybackId = playbackId;
    }

    public long PlaybackId { get; }

    public bool IsValid => _service != null && PlaybackId > 0 && _service.HasPlayback(PlaybackId);

    public bool IsPlaying => _service != null && _service.IsPlaybackPlaying(PlaybackId);

    public void Stop(float fadeOutSeconds = 0f)
    {
        _service?.StopPlayback(PlaybackId, fadeOutSeconds);
    }

    public void SetVolume(float volumeScale)
    {
        _service?.SetPlaybackVolume(PlaybackId, volumeScale);
    }
}
