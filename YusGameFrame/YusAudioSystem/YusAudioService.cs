using Godot;
using System;
using System.Collections.Generic;
using YusGameFrame.YusEventSystem;
using BinarySaver = YusGameFrame.SimpleBinarySaver.SimpleBinarySaver;

namespace YusGameFrame.YusAudioSystem;

public partial class YusAudioService : Node
{
    private sealed class ActivePlayback
    {
        public long PlaybackId { get; init; }
        public string AudioId { get; init; } = string.Empty;
        public YusAudioCategory Category { get; init; }
        public AudioStreamPlayer Player { get; init; } = null!;
        public string BusName { get; init; } = "Master";
        public float BaseVolumeScale { get; set; } = 1f;
        public float UserVolumeScale { get; set; } = 1f;
        public float RandomVolumeScale { get; set; } = 1f;
        public float FadeScale { get; set; } = 1f;
        public float PitchScale { get; set; } = 1f;
        public Action? FinishedHandler { get; set; }
    }

    private sealed class FadeRequest
    {
        public long PlaybackId { get; init; }
        public float From { get; init; }
        public float To { get; init; }
        public double Duration { get; init; }
        public double Elapsed { get; set; }
        public Action? OnCompleted { get; init; }
    }

    private sealed class BgmSession
    {
        public YusAudioDefinition Definition { get; init; } = null!;
        public ActivePlayback Playback { get; init; } = null!;
        public bool ExitRequested { get; set; }
        public BgmSegment Segment { get; set; }
    }

    private sealed class PendingBgmTransition
    {
        public YusAudioDefinition Definition { get; init; } = null!;
        public YusAudioPlayOptions Options { get; init; } = new();
    }

    private sealed class SceneRegistration
    {
        public Node Controller { get; init; } = null!;
        public YusSceneAudioProfile Profile { get; init; } = null!;
    }

    private sealed class EventSubscription
    {
        public string EventId { get; init; } = string.Empty;
        public Action<Variant[]> Callback { get; init; } = null!;
    }

    [Serializable]
    private sealed class AudioSettingsData
    {
        public float MasterVolume = 1f;
        public bool MasterMute;
        public float BgmVolume = 1f;
        public bool BgmMute;
        public float SfxVolume = 1f;
        public bool SfxMute;
        public float UiVolume = 1f;
        public bool UiMute;
        public float VoiceVolume = 1f;
        public bool VoiceMute;
    }

    private enum BgmSegment
    {
        Single = 0,
        Intro = 1,
        Loop = 2,
        Outro = 3
    }

    private const string SettingsSaveKey = "YusAudioSystem/Settings";
    private static readonly RandomNumberGenerator Rng = new();

    public static YusAudioService Instance { get; private set; } = null!;
    public static YusAudioService? InstanceOrNull => Instance;

    [Export]
    public YusAudioLibrary? AudioLibrary { get; private set; }

    [Export]
    public YusAudioLibrary[] AdditionalLibraries { get; private set; } = [];

    private readonly Dictionary<string, YusAudioDefinition> _definitionIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<long, ActivePlayback> _playbacks = new();
    private readonly Dictionary<YusAudioCategory, List<AudioStreamPlayer>> _playerPool = new();
    private readonly Dictionary<string, double> _lastPlayTimes = new(StringComparer.Ordinal);
    private readonly Dictionary<YusAudioCategory, float> _categoryVolumes = new();
    private readonly Dictionary<YusAudioCategory, bool> _categoryMute = new();
    private readonly List<FadeRequest> _fadeRequests = [];
    private readonly List<SceneRegistration> _sceneRegistrations = [];
    private readonly List<EventSubscription> _eventSubscriptions = [];
    private readonly Stack<string> _bgmHistory = new();

    private Node _runtimeRoot = null!;
    private Node _bgmRoot = null!;
    private Node _oneShotRoot = null!;
    private BgmSession? _bgmSession;
    private PendingBgmTransition? _pendingBgmTransition;
    private long _nextPlaybackId = 1;
    private float _masterVolume = 1f;
    private bool _masterMute;

    public string CurrentBgmAudioId => _bgmSession?.Definition.AudioId ?? string.Empty;

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            GD.PushError("YusAudioService 已存在一个有效实例。");
            QueueFree();
            return;
        }

        Instance = this;
    }

    public override void _Ready()
    {
        InitializeCategoryState();
        EnsureRuntimeNodes();
        LoadSettings();
        RebuildDefinitionIndex();
        RebuildEventBindings();
    }

    public override void _ExitTree()
    {
        UnsubscribeAllEvents();
        StopAllPlaybackImmediately();

        if (Instance == this)
        {
            Instance = null!;
        }
    }

    public override void _Process(double delta)
    {
        UpdateFades(delta);
    }

    public static YusAudioService RequireInstance()
    {
        if (Instance == null)
        {
            throw new InvalidOperationException("YusAudioService 当前不可用，请确认已正确配置 Autoload。");
        }

        return Instance;
    }

    public void SetLibraries(YusAudioLibrary? primaryLibrary, params YusAudioLibrary[] additionalLibraries)
    {
        AudioLibrary = primaryLibrary;
        AdditionalLibraries = additionalLibraries ?? [];
        RebuildDefinitionIndex();
        RebuildEventBindings();
    }

    public YusAudioHandle PlayBgm(string audioId, float fadeInSeconds = 0f)
    {
        return Play(audioId, new YusAudioPlayOptions
        {
            CategoryOverride = YusAudioCategory.Bgm,
            FadeInSeconds = fadeInSeconds,
            FadeOutSeconds = fadeInSeconds
        });
    }

    public YusAudioHandle SwitchBgmTemporary(string audioId, float fadeSeconds = 0f)
    {
        return Play(audioId, new YusAudioPlayOptions
        {
            CategoryOverride = YusAudioCategory.Bgm,
            FadeInSeconds = fadeSeconds,
            FadeOutSeconds = fadeSeconds,
            RememberCurrentBgm = true,
            IsTemporaryBgm = true
        });
    }

    public bool ReturnToPreviousBgm(float fadeSeconds = 0f)
    {
        while (_bgmHistory.Count > 0)
        {
            var previousAudioId = _bgmHistory.Pop();
            if (!TryGetDefinition(previousAudioId, out _))
            {
                continue;
            }

            Play(previousAudioId, new YusAudioPlayOptions
            {
                CategoryOverride = YusAudioCategory.Bgm,
                FadeInSeconds = fadeSeconds,
                FadeOutSeconds = fadeSeconds
            });
            return true;
        }

        GD.PushWarning("[YusAudioService] 当前没有可恢复的上一首 BGM。");
        return false;
    }

    public bool StopBgm(float fadeOutSeconds = 0f)
    {
        _pendingBgmTransition = null;
        if (_bgmSession == null)
        {
            return false;
        }

        if (fadeOutSeconds > 0f)
        {
            FadePlayback(_bgmSession.Playback.PlaybackId, 0f, fadeOutSeconds, StopCurrentBgmImmediate);
        }
        else
        {
            StopCurrentBgmImmediate();
        }

        return true;
    }

    public bool PauseBgm()
    {
        if (_bgmSession == null)
        {
            return false;
        }

        _bgmSession.Playback.Player.StreamPaused = true;
        return true;
    }

    public bool ResumeBgm()
    {
        if (_bgmSession == null)
        {
            return false;
        }

        _bgmSession.Playback.Player.StreamPaused = false;
        return true;
    }

    public YusAudioHandle PlaySfx(string audioId, int times = 1, float intervalSeconds = 0f)
    {
        var handle = default(YusAudioHandle);
        var safeTimes = Math.Max(1, times);
        var safeInterval = Math.Max(0f, intervalSeconds);

        for (var index = 0; index < safeTimes; index++)
        {
            if (index == 0)
            {
                handle = Play(audioId, new YusAudioPlayOptions { CategoryOverride = YusAudioCategory.Sfx });
                continue;
            }

            ScheduleDelayedAction(safeInterval * index, () =>
            {
                Play(audioId, new YusAudioPlayOptions { CategoryOverride = YusAudioCategory.Sfx });
            });
        }

        return handle;
    }

    public YusAudioHandle PlayUi(string audioId)
    {
        return Play(audioId, new YusAudioPlayOptions { CategoryOverride = YusAudioCategory.Ui });
    }

    public YusAudioHandle PlayVoice(string audioId)
    {
        return Play(audioId, new YusAudioPlayOptions { CategoryOverride = YusAudioCategory.Voice });
    }

    public YusAudioHandle Play(string audioId, YusAudioPlayOptions? options = null)
    {
        var resolvedOptions = options ?? new YusAudioPlayOptions();

        if (string.IsNullOrWhiteSpace(audioId))
        {
            GD.PushWarning("[YusAudioService] Play 收到了空的 AudioId。");
            return default;
        }

        if (!TryGetDefinition(audioId.Trim(), out var definition))
        {
            GD.PushWarning($"[YusAudioService] 未找到 AudioId 为 '{audioId}' 的音频定义。");
            return default;
        }

        var category = resolvedOptions.CategoryOverride ?? definition.Category;
        return category == YusAudioCategory.Bgm
            ? PlayBgmInternal(definition, resolvedOptions)
            : PlayOneShotInternal(definition, category, resolvedOptions);
    }

    public void StopCategory(YusAudioCategory category)
    {
        if (category == YusAudioCategory.Bgm)
        {
            StopBgm();
            return;
        }

        var playbackIds = new List<long>();
        foreach (var playback in _playbacks.Values)
        {
            if (playback.Category == category)
            {
                playbackIds.Add(playback.PlaybackId);
            }
        }

        foreach (var playbackId in playbackIds)
        {
            StopPlayback(playbackId, 0f);
        }
    }

    public void SetMasterVolume(float volume01)
    {
        _masterVolume = Mathf.Clamp(volume01, 0f, 1f);
        ApplyAllPlaybackVolume();
        SaveSettings();
    }

    public float GetMasterVolume()
    {
        return _masterVolume;
    }

    public void SetMasterMute(bool isMuted)
    {
        _masterMute = isMuted;
        ApplyAllPlaybackVolume();
        SaveSettings();
    }

    public bool GetMasterMute()
    {
        return _masterMute;
    }

    public void SetCategoryVolume(YusAudioCategory category, float volume01)
    {
        _categoryVolumes[category] = Mathf.Clamp(volume01, 0f, 1f);
        ApplyAllPlaybackVolume();
        SaveSettings();
    }

    public float GetCategoryVolume(YusAudioCategory category)
    {
        return _categoryVolumes.TryGetValue(category, out var volume) ? volume : 1f;
    }

    public void SetCategoryMute(YusAudioCategory category, bool isMuted)
    {
        _categoryMute[category] = isMuted;
        ApplyAllPlaybackVolume();
        SaveSettings();
    }

    public bool GetCategoryMute(YusAudioCategory category)
    {
        return _categoryMute.TryGetValue(category, out var isMuted) && isMuted;
    }

    public void AttachSceneProfile(Node controller, YusSceneAudioProfile profile)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(profile);

        _sceneRegistrations.RemoveAll(item => item.Controller == controller || !IsNodeAlive(item.Controller));
        _sceneRegistrations.Add(new SceneRegistration
        {
            Controller = controller,
            Profile = profile
        });

        RebuildDefinitionIndex();
        RebuildEventBindings();
        ApplySceneEnter(profile);
    }

    public void DetachSceneProfile(Node controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        _sceneRegistrations.RemoveAll(item => !IsNodeAlive(item.Controller));
        var index = _sceneRegistrations.FindLastIndex(item => item.Controller == controller);
        if (index < 0)
        {
            return;
        }

        var removed = _sceneRegistrations[index];
        var wasCurrent = index == _sceneRegistrations.Count - 1;
        _sceneRegistrations.RemoveAt(index);

        if (wasCurrent)
        {
            ApplySceneExit(removed.Profile);
        }

        RebuildDefinitionIndex();
        RebuildEventBindings();

        if (wasCurrent && TryGetCurrentSceneProfile(out var fallbackProfile))
        {
            ApplySceneEnter(fallbackProfile, true);
        }
    }

    internal bool HasPlayback(long playbackId)
    {
        return _playbacks.ContainsKey(playbackId);
    }

    internal bool IsPlaybackPlaying(long playbackId)
    {
        return _playbacks.TryGetValue(playbackId, out var playback) &&
               GodotObject.IsInstanceValid(playback.Player) &&
               playback.Player.Playing;
    }

    internal void SetPlaybackVolume(long playbackId, float volumeScale)
    {
        if (!_playbacks.TryGetValue(playbackId, out var playback))
        {
            return;
        }

        playback.UserVolumeScale = Mathf.Clamp(volumeScale, 0f, 1f);
        ApplyPlaybackVolume(playback);
    }

    internal void StopPlayback(long playbackId, float fadeOutSeconds)
    {
        if (!_playbacks.TryGetValue(playbackId, out var playback))
        {
            return;
        }

        if (_bgmSession != null && _bgmSession.Playback.PlaybackId == playbackId)
        {
            StopBgm(fadeOutSeconds);
            return;
        }

        if (fadeOutSeconds > 0f)
        {
            FadePlayback(playbackId, 0f, fadeOutSeconds, () => CleanupPlayback(playbackId, true));
        }
        else
        {
            CleanupPlayback(playbackId, true);
        }
    }

    private void InitializeCategoryState()
    {
        foreach (YusAudioCategory category in Enum.GetValues(typeof(YusAudioCategory)))
        {
            _categoryVolumes.TryAdd(category, 1f);
            _categoryMute.TryAdd(category, false);
            _playerPool.TryAdd(category, []);
        }
    }

    private void EnsureRuntimeNodes()
    {
        if (_runtimeRoot != null && GodotObject.IsInstanceValid(_runtimeRoot))
        {
            return;
        }

        _runtimeRoot = new Node { Name = "YusAudioRuntime" };
        AddChild(_runtimeRoot);

        _bgmRoot = new Node { Name = "BgmPlayers" };
        _runtimeRoot.AddChild(_bgmRoot);

        _oneShotRoot = new Node { Name = "OneShotPlayers" };
        _runtimeRoot.AddChild(_oneShotRoot);
    }

    private void LoadSettings()
    {
        var settings = BinarySaver.Load<AudioSettingsData>(SettingsSaveKey, new AudioSettingsData());
        _masterVolume = Mathf.Clamp(settings.MasterVolume, 0f, 1f);
        _masterMute = settings.MasterMute;
        _categoryVolumes[YusAudioCategory.Bgm] = Mathf.Clamp(settings.BgmVolume, 0f, 1f);
        _categoryMute[YusAudioCategory.Bgm] = settings.BgmMute;
        _categoryVolumes[YusAudioCategory.Sfx] = Mathf.Clamp(settings.SfxVolume, 0f, 1f);
        _categoryMute[YusAudioCategory.Sfx] = settings.SfxMute;
        _categoryVolumes[YusAudioCategory.Ui] = Mathf.Clamp(settings.UiVolume, 0f, 1f);
        _categoryMute[YusAudioCategory.Ui] = settings.UiMute;
        _categoryVolumes[YusAudioCategory.Voice] = Mathf.Clamp(settings.VoiceVolume, 0f, 1f);
        _categoryMute[YusAudioCategory.Voice] = settings.VoiceMute;
    }

    private void SaveSettings()
    {
        var settings = new AudioSettingsData
        {
            MasterVolume = _masterVolume,
            MasterMute = _masterMute,
            BgmVolume = GetCategoryVolume(YusAudioCategory.Bgm),
            BgmMute = GetCategoryMute(YusAudioCategory.Bgm),
            SfxVolume = GetCategoryVolume(YusAudioCategory.Sfx),
            SfxMute = GetCategoryMute(YusAudioCategory.Sfx),
            UiVolume = GetCategoryVolume(YusAudioCategory.Ui),
            UiMute = GetCategoryMute(YusAudioCategory.Ui),
            VoiceVolume = GetCategoryVolume(YusAudioCategory.Voice),
            VoiceMute = GetCategoryMute(YusAudioCategory.Voice)
        };

        BinarySaver.Save(settings, SettingsSaveKey);
    }

    private void RebuildDefinitionIndex()
    {
        _definitionIndex.Clear();

        foreach (var library in GetEffectiveLibraries())
        {
            library.Initialize();

            foreach (var definition in library.Definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.AudioId))
                {
                    continue;
                }

                var audioId = definition.AudioId.Trim();
                if (!_definitionIndex.TryAdd(audioId, definition))
                {
                    GD.PushWarning($"[YusAudioService] 检测到重复的 AudioId：{audioId}，后续定义已忽略。");
                }
            }
        }
    }

    private IEnumerable<YusAudioLibrary> GetEffectiveLibraries()
    {
        if (TryGetCurrentSceneProfile(out var profile))
        {
            if (profile.OverrideLibrary != null)
            {
                yield return profile.OverrideLibrary;
            }

            foreach (var library in profile.AdditionalLibraries)
            {
                if (library != null)
                {
                    yield return library;
                }
            }
        }

        if (AudioLibrary != null)
        {
            yield return AudioLibrary;
        }

        foreach (var library in AdditionalLibraries)
        {
            if (library != null)
            {
                yield return library;
            }
        }
    }

    private bool TryGetDefinition(string audioId, out YusAudioDefinition definition)
    {
        if (_definitionIndex.TryGetValue(audioId, out definition!))
        {
            return true;
        }

        RebuildDefinitionIndex();
        return _definitionIndex.TryGetValue(audioId, out definition!);
    }

    private YusAudioHandle PlayBgmInternal(YusAudioDefinition definition, YusAudioPlayOptions options)
    {
        if (options.SkipIfAlreadyCurrentBgm &&
            string.Equals(CurrentBgmAudioId, definition.AudioId, StringComparison.Ordinal))
        {
            return _bgmSession != null ? new YusAudioHandle(this, _bgmSession.Playback.PlaybackId) : default;
        }

        if (options.RememberCurrentBgm &&
            !string.IsNullOrWhiteSpace(CurrentBgmAudioId) &&
            !string.Equals(CurrentBgmAudioId, definition.AudioId, StringComparison.Ordinal))
        {
            _bgmHistory.Push(CurrentBgmAudioId);
        }
        else if (!options.IsTemporaryBgm)
        {
            _bgmHistory.Clear();
        }

        if (_bgmSession != null)
        {
            _pendingBgmTransition = new PendingBgmTransition
            {
                Definition = definition,
                Options = options
            };

            if (options.FadeOutSeconds > 0f)
            {
                FadePlayback(_bgmSession.Playback.PlaybackId, 0f, options.FadeOutSeconds, StartPendingBgmTransition);
            }
            else
            {
                StopCurrentBgmImmediate();
                StartPendingBgmTransition();
            }

            return default;
        }

        return StartBgmSession(definition, options);
    }

    private YusAudioHandle StartBgmSession(YusAudioDefinition definition, YusAudioPlayOptions options)
    {
        if (!definition.HasAnyPlayableStream())
        {
            GD.PushWarning($"[YusAudioService] BGM '{definition.AudioId}' 没有可播放的流资源。");
            return default;
        }

        var playback = CreatePlayback(definition, YusAudioCategory.Bgm, options, false);
        var session = new BgmSession
        {
            Definition = definition,
            Playback = playback,
            Segment = definition.BgmPlaybackMode == YusBgmPlaybackMode.SingleLoop ? BgmSegment.Single : BgmSegment.Intro
        };

        _bgmSession = session;
        AttachBgmFinishedHandler(session);
        PlayBgmSegment(session, session.Segment);

        if (options.FadeInSeconds > 0f)
        {
            session.Playback.FadeScale = 0f;
            ApplyPlaybackVolume(session.Playback);
            FadePlayback(session.Playback.PlaybackId, 1f, options.FadeInSeconds, null);
        }

        return new YusAudioHandle(this, playback.PlaybackId);
    }

    private void StartPendingBgmTransition()
    {
        if (_pendingBgmTransition == null)
        {
            return;
        }

        var transition = _pendingBgmTransition;
        _pendingBgmTransition = null;
        StopCurrentBgmImmediate();
        StartBgmSession(transition.Definition, transition.Options);
    }

    private YusAudioHandle PlayOneShotInternal(YusAudioDefinition definition, YusAudioCategory category, YusAudioPlayOptions options)
    {
        if (definition.SingleStream == null)
        {
            GD.PushWarning($"[YusAudioService] 音频 '{definition.AudioId}' 没有设置 SingleStream。");
            return default;
        }

        if (!CanPlayDefinition(definition, category))
        {
            return default;
        }

        var playback = CreatePlayback(definition, category, options, true);
        playback.Player.Stream = definition.SingleStream;
        playback.Player.Play();
        return new YusAudioHandle(this, playback.PlaybackId);
    }

    private bool CanPlayDefinition(YusAudioDefinition definition, YusAudioCategory category)
    {
        var nowSeconds = Time.GetTicksMsec() / 1000.0;
        if (_lastPlayTimes.TryGetValue(definition.AudioId, out var lastTime) &&
            nowSeconds - lastTime < definition.MinReplayIntervalSeconds)
        {
            return false;
        }

        _lastPlayTimes[definition.AudioId] = nowSeconds;

        var concurrentCount = 0;
        foreach (var playback in _playbacks.Values)
        {
            if (playback.Category == category &&
                string.Equals(playback.AudioId, definition.AudioId, StringComparison.Ordinal) &&
                playback.Player.Playing)
            {
                concurrentCount++;
            }
        }

        if (!definition.AllowReplayWhilePlaying && concurrentCount > 0)
        {
            return false;
        }

        return concurrentCount < Math.Max(1, definition.MaxConcurrentCount);
    }

    private ActivePlayback CreatePlayback(YusAudioDefinition definition, YusAudioCategory category, YusAudioPlayOptions options, bool usePool)
    {
        var player = usePool ? AcquirePlayer(category) : CreatePlayer(category);
        var playbackId = _nextPlaybackId++;
        var playback = new ActivePlayback
        {
            PlaybackId = playbackId,
            AudioId = definition.AudioId,
            Category = category,
            Player = player,
            BusName = ResolveBusName(definition, category),
            BaseVolumeScale = Mathf.Clamp(definition.VolumeScale, 0f, 1f),
            UserVolumeScale = Mathf.Clamp(options.VolumeScale, 0f, 1f),
            RandomVolumeScale = GetRandomValue(definition.RandomVolumeMin, definition.RandomVolumeMax),
            FadeScale = 1f,
            PitchScale = Mathf.Clamp(definition.PitchScale * options.PitchScale * GetRandomValue(definition.RandomPitchMin, definition.RandomPitchMax), 0.01f, 4f)
        };

        player.Name = $"Audio_{playbackId}_{definition.AudioId}";
        player.StreamPaused = false;
        player.Bus = playback.BusName;
        player.PitchScale = playback.PitchScale;
        _playbacks[playbackId] = playback;

        if (usePool)
        {
            Action handler = () => CleanupPlayback(playbackId, true);
            player.Finished += handler;
            playback.FinishedHandler = handler;
        }

        ApplyPlaybackVolume(playback);
        return playback;
    }

    private AudioStreamPlayer AcquirePlayer(YusAudioCategory category)
    {
        var pool = _playerPool[category];
        while (pool.Count > 0)
        {
            var player = pool[^1];
            pool.RemoveAt(pool.Count - 1);
            if (GodotObject.IsInstanceValid(player))
            {
                return player;
            }
        }

        return CreatePlayer(category);
    }

    private AudioStreamPlayer CreatePlayer(YusAudioCategory category)
    {
        var player = new AudioStreamPlayer();
        player.ProcessMode = ProcessModeEnum.Pausable;

        if (category == YusAudioCategory.Bgm)
        {
            _bgmRoot.AddChild(player);
        }
        else
        {
            _oneShotRoot.AddChild(player);
        }

        return player;
    }

    private void ReleasePlayer(ActivePlayback playback)
    {
        if (!GodotObject.IsInstanceValid(playback.Player))
        {
            return;
        }

        playback.Player.Stop();
        playback.Player.Stream = null;
        playback.Player.StreamPaused = false;
        playback.Player.PitchScale = 1f;
        playback.Player.VolumeDb = -80f;

        if (playback.FinishedHandler != null)
        {
            playback.Player.Finished -= playback.FinishedHandler;
        }

        if (playback.Category == YusAudioCategory.Bgm)
        {
            playback.Player.QueueFree();
            return;
        }

        _playerPool[playback.Category].Add(playback.Player);
    }

    private void ApplyPlaybackVolume(ActivePlayback playback)
    {
        if (!GodotObject.IsInstanceValid(playback.Player))
        {
            return;
        }

        var categoryScale = GetCategoryMute(playback.Category) ? 0f : GetCategoryVolume(playback.Category);
        var masterScale = _masterMute ? 0f : _masterVolume;
        var finalScale = playback.BaseVolumeScale *
                         playback.UserVolumeScale *
                         playback.RandomVolumeScale *
                         playback.FadeScale *
                         categoryScale *
                         masterScale;

        playback.Player.VolumeDb = finalScale <= 0f ? -80f : Mathf.LinearToDb(finalScale);
    }

    private void ApplyAllPlaybackVolume()
    {
        foreach (var playback in _playbacks.Values)
        {
            ApplyPlaybackVolume(playback);
        }
    }

    private string ResolveBusName(YusAudioDefinition definition, YusAudioCategory category)
    {
        var candidate = string.IsNullOrWhiteSpace(definition.BusName)
            ? category.ToString()
            : definition.BusName.Trim();

        var busIndex = AudioServer.GetBusIndex(candidate);
        return busIndex >= 0 ? candidate : "Master";
    }

    private static float GetRandomValue(float min, float max)
    {
        min = Mathf.Min(min, max);
        max = Mathf.Max(min, max);
        return Mathf.IsEqualApprox(min, max) ? min : Rng.RandfRange(min, max);
    }

    private void AttachBgmFinishedHandler(BgmSession session)
    {
        Action handler = OnBgmPlaybackFinished;
        session.Playback.Player.Finished += handler;
        session.Playback.FinishedHandler = handler;
    }

    private void OnBgmPlaybackFinished()
    {
        if (_bgmSession == null)
        {
            return;
        }

        switch (_bgmSession.Segment)
        {
            case BgmSegment.Single:
                if (_bgmSession.ExitRequested)
                {
                    StopCurrentBgmImmediate();
                }
                else
                {
                    PlayBgmSegment(_bgmSession, BgmSegment.Single);
                }
                break;

            case BgmSegment.Intro:
                if (_bgmSession.ExitRequested && _bgmSession.Definition.OutroStream != null && !_bgmSession.Definition.WaitLoopBoundaryBeforeOutro)
                {
                    PlayBgmSegment(_bgmSession, BgmSegment.Outro);
                }
                else if (_bgmSession.Definition.LoopStream != null)
                {
                    PlayBgmSegment(_bgmSession, BgmSegment.Loop);
                }
                else if (_bgmSession.Definition.OutroStream != null && _bgmSession.ExitRequested)
                {
                    PlayBgmSegment(_bgmSession, BgmSegment.Outro);
                }
                else
                {
                    StopCurrentBgmImmediate();
                }
                break;

            case BgmSegment.Loop:
                if (_bgmSession.ExitRequested)
                {
                    if (_bgmSession.Definition.OutroStream != null)
                    {
                        PlayBgmSegment(_bgmSession, BgmSegment.Outro);
                    }
                    else
                    {
                        StopCurrentBgmImmediate();
                    }
                }
                else
                {
                    PlayBgmSegment(_bgmSession, BgmSegment.Loop);
                }
                break;

            case BgmSegment.Outro:
                StopCurrentBgmImmediate();
                break;
        }
    }

    private void PlayBgmSegment(BgmSession session, BgmSegment segment)
    {
        var stream = GetSegmentStream(session.Definition, segment);
        if (stream == null)
        {
            StopCurrentBgmImmediate();
            return;
        }

        session.Segment = segment;
        session.Playback.Player.Stop();
        session.Playback.Player.Stream = stream;
        session.Playback.Player.Play();
    }

    private static AudioStream? GetSegmentStream(YusAudioDefinition definition, BgmSegment segment)
    {
        return segment switch
        {
            BgmSegment.Single => definition.SingleStream,
            BgmSegment.Intro => definition.IntroStream ?? definition.LoopStream,
            BgmSegment.Loop => definition.LoopStream,
            BgmSegment.Outro => definition.OutroStream,
            _ => null
        };
    }

    private bool RequestCurrentBgmOutro(string? targetAudioId = null)
    {
        if (_bgmSession == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(targetAudioId) &&
            !string.Equals(_bgmSession.Definition.AudioId, targetAudioId, StringComparison.Ordinal))
        {
            return false;
        }

        if (_bgmSession.Definition.BgmPlaybackMode == YusBgmPlaybackMode.SingleLoop ||
            _bgmSession.Definition.OutroStream == null)
        {
            StopCurrentBgmImmediate();
            return true;
        }

        if (_bgmSession.Segment == BgmSegment.Loop && !_bgmSession.Definition.WaitLoopBoundaryBeforeOutro)
        {
            PlayBgmSegment(_bgmSession, BgmSegment.Outro);
            return true;
        }

        _bgmSession.ExitRequested = true;
        return true;
    }

    private void StopCurrentBgmImmediate()
    {
        if (_bgmSession == null)
        {
            return;
        }

        CleanupPlayback(_bgmSession.Playback.PlaybackId, true);
        _bgmSession = null;
    }

    private void CleanupPlayback(long playbackId, bool releasePlayer)
    {
        _fadeRequests.RemoveAll(item => item.PlaybackId == playbackId);

        if (!_playbacks.TryGetValue(playbackId, out var playback))
        {
            return;
        }

        _playbacks.Remove(playbackId);

        if (releasePlayer)
        {
            ReleasePlayer(playback);
        }
    }

    private void FadePlayback(long playbackId, float targetFadeScale, float durationSeconds, Action? onCompleted)
    {
        if (!_playbacks.TryGetValue(playbackId, out var playback))
        {
            onCompleted?.Invoke();
            return;
        }

        _fadeRequests.RemoveAll(item => item.PlaybackId == playbackId);
        if (durationSeconds <= 0f)
        {
            playback.FadeScale = targetFadeScale;
            ApplyPlaybackVolume(playback);
            onCompleted?.Invoke();
            return;
        }

        _fadeRequests.Add(new FadeRequest
        {
            PlaybackId = playbackId,
            From = playback.FadeScale,
            To = targetFadeScale,
            Duration = durationSeconds,
            OnCompleted = onCompleted
        });
    }

    private void UpdateFades(double delta)
    {
        if (_fadeRequests.Count == 0)
        {
            return;
        }

        for (var index = _fadeRequests.Count - 1; index >= 0; index--)
        {
            var fade = _fadeRequests[index];
            if (!_playbacks.TryGetValue(fade.PlaybackId, out var playback))
            {
                _fadeRequests.RemoveAt(index);
                fade.OnCompleted?.Invoke();
                continue;
            }

            fade.Elapsed += delta;
            var weight = fade.Duration <= 0d ? 1f : Mathf.Clamp((float)(fade.Elapsed / fade.Duration), 0f, 1f);
            playback.FadeScale = Mathf.Lerp(fade.From, fade.To, weight);
            ApplyPlaybackVolume(playback);

            if (weight < 1f)
            {
                continue;
            }

            _fadeRequests.RemoveAt(index);
            fade.OnCompleted?.Invoke();
        }
    }

    private void ScheduleDelayedAction(float delaySeconds, Action action)
    {
        if (delaySeconds <= 0f)
        {
            action.Invoke();
            return;
        }

        var timer = GetTree().CreateTimer(delaySeconds);
        timer.Timeout += action;
    }

    private void RebuildEventBindings()
    {
        UnsubscribeAllEvents();

        if (YusEventSystemService.Instance == null)
        {
            return;
        }

        foreach (var binding in CollectEffectiveEventBindings())
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.EventId))
            {
                continue;
            }

            var subscription = new EventSubscription
            {
                EventId = binding.EventId.Trim(),
                Callback = _ => HandleEventBinding(binding)
            };

            YusEventSystemService.RequireInstance().AddListener(subscription.EventId, this, subscription.Callback);
            _eventSubscriptions.Add(subscription);
        }
    }

    private IEnumerable<YusAudioEventBinding> CollectEffectiveEventBindings()
    {
        foreach (var definition in _definitionIndex.Values)
        {
            foreach (var binding in definition.GetImplicitEventBindings())
            {
                yield return binding;
            }
        }

        if (TryGetCurrentSceneProfile(out var profile))
        {
            foreach (var binding in profile.EventBindings)
            {
                if (binding != null)
                {
                    yield return binding;
                }
            }
        }
    }

    private void HandleEventBinding(YusAudioEventBinding binding)
    {
        if (binding.DelaySeconds > 0f)
        {
            ScheduleDelayedAction(binding.DelaySeconds, () => ExecuteEventBinding(binding));
            return;
        }

        ExecuteEventBinding(binding);
    }

    private void ExecuteEventBinding(YusAudioEventBinding binding)
    {
        switch (binding.Action)
        {
            case YusAudioEventAction.PlayBoundAudio:
                if (TryGetDefinition(binding.TargetAudioId, out var definition))
                {
                    Play(binding.TargetAudioId, new YusAudioPlayOptions
                    {
                        CategoryOverride = definition.Category,
                        FadeInSeconds = binding.FadeSeconds
                    });
                }
                break;

            case YusAudioEventAction.PlayBgm:
                Play(binding.TargetAudioId, new YusAudioPlayOptions
                {
                    CategoryOverride = YusAudioCategory.Bgm,
                    FadeInSeconds = binding.FadeSeconds,
                    FadeOutSeconds = binding.FadeSeconds
                });
                break;

            case YusAudioEventAction.PlaySfx:
                PlaySfx(binding.TargetAudioId, binding.Times, binding.IntervalSeconds);
                break;

            case YusAudioEventAction.PlayUi:
                PlayUi(binding.TargetAudioId);
                break;

            case YusAudioEventAction.PlayVoice:
                PlayVoice(binding.TargetAudioId);
                break;

            case YusAudioEventAction.StopCurrentBgm:
                if (string.IsNullOrWhiteSpace(binding.TargetAudioId) ||
                    string.Equals(CurrentBgmAudioId, binding.TargetAudioId, StringComparison.Ordinal))
                {
                    StopBgm(binding.FadeSeconds);
                }
                break;

            case YusAudioEventAction.RequestCurrentBgmOutro:
                RequestCurrentBgmOutro(binding.TargetAudioId);
                break;

            case YusAudioEventAction.ReturnToPreviousBgm:
                ReturnToPreviousBgm(binding.FadeSeconds);
                break;
        }
    }

    private void UnsubscribeAllEvents()
    {
        if (YusEventSystemService.Instance == null)
        {
            _eventSubscriptions.Clear();
            return;
        }

        foreach (var subscription in _eventSubscriptions)
        {
            YusEventSystemService.RequireInstance().RemoveListener(subscription.EventId, subscription.Callback);
        }

        _eventSubscriptions.Clear();
    }

    private bool TryGetCurrentSceneProfile(out YusSceneAudioProfile profile)
    {
        _sceneRegistrations.RemoveAll(item => !IsNodeAlive(item.Controller));
        if (_sceneRegistrations.Count == 0)
        {
            profile = null!;
            return false;
        }

        profile = _sceneRegistrations[^1].Profile;
        return true;
    }

    private void ApplySceneEnter(YusSceneAudioProfile profile, bool isReactivation = false)
    {
        if (string.IsNullOrWhiteSpace(profile.DefaultBgmAudioId))
        {
            return;
        }

        switch (profile.EnterPolicy)
        {
            case YusSceneBgmEnterPolicy.KeepCurrent:
                return;

            case YusSceneBgmEnterPolicy.PlayIfDifferent:
                Play(profile.DefaultBgmAudioId, new YusAudioPlayOptions
                {
                    CategoryOverride = YusAudioCategory.Bgm,
                    FadeInSeconds = profile.EnterFadeSeconds,
                    FadeOutSeconds = profile.EnterFadeSeconds,
                    SkipIfAlreadyCurrentBgm = true
                });
                return;

            case YusSceneBgmEnterPolicy.PlayAlways:
                Play(profile.DefaultBgmAudioId, new YusAudioPlayOptions
                {
                    CategoryOverride = YusAudioCategory.Bgm,
                    FadeInSeconds = profile.EnterFadeSeconds,
                    FadeOutSeconds = profile.EnterFadeSeconds
                });
                return;
        }

        if (isReactivation)
        {
            return;
        }
    }

    private void ApplySceneExit(YusSceneAudioProfile profile)
    {
        switch (profile.ExitPolicy)
        {
            case YusSceneBgmExitPolicy.DoNothing:
                return;

            case YusSceneBgmExitPolicy.Stop:
                StopBgm(0f);
                return;

            case YusSceneBgmExitPolicy.RequestOutro:
                RequestCurrentBgmOutro(string.IsNullOrWhiteSpace(profile.DefaultBgmAudioId) ? null : profile.DefaultBgmAudioId);
                return;

            case YusSceneBgmExitPolicy.FadeOutStop:
                StopBgm(profile.ExitFadeSeconds);
                return;
        }
    }

    private void StopAllPlaybackImmediately()
    {
        _fadeRequests.Clear();

        var playbackIds = new List<long>(_playbacks.Keys);
        foreach (var playbackId in playbackIds)
        {
            CleanupPlayback(playbackId, true);
        }

        _bgmSession = null;
        _pendingBgmTransition = null;
    }

    private static bool IsNodeAlive(Node node)
    {
        return GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();
    }
}
