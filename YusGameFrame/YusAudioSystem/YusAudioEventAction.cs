namespace YusGameFrame.YusAudioSystem;

public enum YusAudioEventAction
{
    PlayBoundAudio = 0,
    PlayBgm = 1,
    PlaySfx = 2,
    PlayUi = 3,
    PlayVoice = 4,
    StopCurrentBgm = 5,
    RequestCurrentBgmOutro = 6,
    ReturnToPreviousBgm = 7
}
