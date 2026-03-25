namespace YusGameFrame.YusAudioSystem;

public sealed class YusAudioPlayOptions
{
    public YusAudioCategory? CategoryOverride { get; init; }

    public float FadeInSeconds { get; init; }

    public float FadeOutSeconds { get; init; }

    public float VolumeScale { get; init; } = 1f;

    public float PitchScale { get; init; } = 1f;

    public bool RememberCurrentBgm { get; init; }

    public bool SkipIfAlreadyCurrentBgm { get; init; }

    public bool IsTemporaryBgm { get; init; }
}
