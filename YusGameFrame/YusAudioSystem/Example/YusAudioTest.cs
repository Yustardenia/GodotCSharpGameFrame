using Godot;
using System;

namespace YusGameFrame.YusAudioSystem.Example;

public partial class YusAudioTest : Control
{
    private Label _statusLabel = null!;
    private Label _volumeLabel = null!;
    private YusSceneAudioController? _sceneController;
    private YusSceneAudioProfile _forestProfile = null!;
    private YusSceneAudioProfile _battleProfile = null!;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        LoadExampleResources();
        BuildUi();
        SwitchSceneProfile(_forestProfile);
        RefreshVolumeLabel();
    }

    private void LoadExampleResources()
    {
        var library = ResourceLoader.Load<YusAudioLibrary>("res://YusGameFrame/YusAudioSystem/Example/YusAudioExampleLibrary.tres");
        _forestProfile = ResourceLoader.Load<YusSceneAudioProfile>("res://YusGameFrame/YusAudioSystem/Example/YusAudioForestProfile.tres")
            ?? throw new InvalidOperationException("无法加载森林场景音频配置。");
        _battleProfile = ResourceLoader.Load<YusSceneAudioProfile>("res://YusGameFrame/YusAudioSystem/Example/YusAudioBattleProfile.tres")
            ?? throw new InvalidOperationException("无法加载战斗场景音频配置。");

        if (library == null)
        {
            throw new InvalidOperationException("无法加载示例音频库资源。");
        }

        YusAudioService.RequireInstance().SetLibraries(library);
    }

    private void BuildUi()
    {
        var root = new MarginContainer();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("margin_left", 24);
        root.AddThemeConstantOverride("margin_top", 24);
        root.AddThemeConstantOverride("margin_right", 24);
        root.AddThemeConstantOverride("margin_bottom", 24);
        AddChild(root);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 12);
        root.AddChild(layout);

        layout.AddChild(new Label
        {
            Text = "YusAudioSystem 示例",
            ThemeTypeVariation = "HeaderSmall"
        });

        layout.AddChild(new Label
        {
            Text = "按钮、音频库和场景配置都在运行时动态生成，用来快速验证播放、切歌、事件联动与音量控制。 ",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        _statusLabel = new Label
        {
            Text = "等待操作。",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        layout.AddChild(_statusLabel);

        _volumeLabel = new Label();
        layout.AddChild(_volumeLabel);

        AddButtonRow(layout, ("进入森林场景", () => SwitchSceneProfile(_forestProfile)),
            ("进入战斗场景", () => SwitchSceneProfile(_battleProfile)),
            ("移除场景配置", RemoveSceneProfile));

        AddButtonRow(layout, ("播放 UI 点击", () => PlayAndSetStatus(() => YusAudioService.RequireInstance().PlayUi("ui_click"), "播放了 UI 点击音效。")),
            ("播放语音", () => PlayAndSetStatus(() => YusAudioService.RequireInstance().PlayVoice("voice_alert"), "播放了语音提示。")),
            ("播放三连击", () => PlayAndSetStatus(() => YusAudioService.RequireInstance().PlaySfx("sfx_hit", 3, 0.12f), "播放了三连击音效。")));

        AddButtonRow(layout, ("临时切到森林 BGM", () => PlayAndSetStatus(() => YusAudioService.RequireInstance().SwitchBgmTemporary("bgm_forest", 0.25f), "临时切换到森林 BGM。")),
            ("恢复上一首 BGM", () =>
            {
                YusAudioService.RequireInstance().ReturnToPreviousBgm(0.25f);
                SetStatus("尝试恢复上一首 BGM。");
            }),
            ("停止 BGM", () =>
            {
                YusAudioService.RequireInstance().StopBgm(0.2f);
                SetStatus("请求停止当前 BGM。");
            }));

        AddButtonRow(layout, ("广播 GameStarted", () =>
            {
                YusGameFrame.YusEventSystem.YusEventSignals.GameStartedEvent.Broadcast();
                SetStatus("广播了 GameStarted，触发绑定的 UI 音效。");
            }),
            ("广播 DamageReported", () =>
            {
                YusGameFrame.YusEventSystem.YusEventSignals.DamageReportedEvent.Broadcast("演示敌人", 12);
                SetStatus("广播了 DamageReported，触发绑定的命中音效。");
            }),
            ("广播 PlayerHpChanged", () =>
            {
                YusGameFrame.YusEventSystem.YusEventSignals.PlayerHpChangedEvent.Broadcast(48);
                SetStatus("广播了 PlayerHpChanged，触发绑定的语音。");
            }));

        AddButtonRow(layout, ("BGM 音量 -", () => AdjustVolume(YusAudioCategory.Bgm, -0.1f)),
            ("BGM 音量 +", () => AdjustVolume(YusAudioCategory.Bgm, 0.1f)),
            ("SFX 音量 +", () => AdjustVolume(YusAudioCategory.Sfx, 0.1f)));
    }

    private void AddButtonRow(VBoxContainer parent, params (string Text, Action Callback)[] entries)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        parent.AddChild(row);

        foreach (var entry in entries)
        {
            var button = new Button();
            button.Text = entry.Text;
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            button.Pressed += entry.Callback;
            row.AddChild(button);
        }
    }

    private void SwitchSceneProfile(YusSceneAudioProfile profile)
    {
        RemoveSceneProfile();

        _sceneController = new YusSceneAudioController
        {
            Name = $"SceneAudio_{profile.DefaultBgmAudioId}",
            Profile = profile
        };

        AddChild(_sceneController);
        SetStatus($"切换到了场景配置：{profile.DefaultBgmAudioId}");
    }

    private void RemoveSceneProfile()
    {
        if (_sceneController == null || !GodotObject.IsInstanceValid(_sceneController))
        {
            return;
        }

        _sceneController.QueueFree();
        _sceneController = null;
        SetStatus("移除了当前场景配置节点。");
    }

    private void AdjustVolume(YusAudioCategory category, float delta)
    {
        var service = YusAudioService.RequireInstance();
        service.SetCategoryVolume(category, Mathf.Clamp(service.GetCategoryVolume(category) + delta, 0f, 1f));
        RefreshVolumeLabel();
        SetStatus($"调整了 {category} 音量。");
    }

    private void PlayAndSetStatus(Action playAction, string status)
    {
        playAction.Invoke();
        SetStatus(status);
    }

    private void SetStatus(string text)
    {
        _statusLabel.Text = $"{text}\n当前 BGM：{YusAudioService.RequireInstance().CurrentBgmAudioId}";
        RefreshVolumeLabel();
    }

    private void RefreshVolumeLabel()
    {
        var service = YusAudioService.RequireInstance();
        _volumeLabel.Text =
            $"BGM={service.GetCategoryVolume(YusAudioCategory.Bgm):0.00}  " +
            $"SFX={service.GetCategoryVolume(YusAudioCategory.Sfx):0.00}  " +
            $"UI={service.GetCategoryVolume(YusAudioCategory.Ui):0.00}  " +
            $"Voice={service.GetCategoryVolume(YusAudioCategory.Voice):0.00}";
    }
}
