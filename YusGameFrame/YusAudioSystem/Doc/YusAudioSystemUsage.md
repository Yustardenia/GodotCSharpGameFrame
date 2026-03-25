# YusAudioSystem 使用说明

## 简介
`YusAudioSystem` 提供一套偏 Godot 风格的统一音频方案：
- 运行时使用 `YusAudioService` 作为 Autoload 全局入口
- 音频资源通过 `YusAudioLibrary` 与 `YusAudioDefinition` 配置
- 场景通过 `YusSceneAudioController + YusSceneAudioProfile` 声明默认 BGM 行为
- 支持 BGM、SFX、UI、Voice 四类音频
- 支持单曲循环与 `Intro -> Loop -> Outro` 两种 BGM 播放模型
- 支持事件触发播放、音量持久化、场景切换接管与上一首 BGM 恢复

## 核心入口
- 运行时服务：`res://YusGameFrame/YusAudioSystem/YusAudioService.cs`
- 音频库资源：`res://YusGameFrame/YusAudioSystem/YusAudioLibrary.cs`
- 音频条目资源：`res://YusGameFrame/YusAudioSystem/YusAudioDefinition.cs`
- 场景配置资源：`res://YusGameFrame/YusAudioSystem/YusSceneAudioProfile.cs`
- 场景接入节点：`res://YusGameFrame/YusAudioSystem/YusSceneAudioController.cs`
- 示例入口：`res://YusGameFrame/YusAudioSystem/Example/YusAudioExampleRoot.tscn`

## 接入方式
1. 确认 `project.godot` 已将 `YusAudioService` 配置为 Autoload。
2. 创建一个 `YusAudioLibrary.tres`，在 `Definitions` 中维护所有音频条目。
3. 每个条目使用 `AudioId` 作为运行时访问键。
4. 需要场景默认音乐时，在场景里挂一个 `YusSceneAudioController`，并引用 `YusSceneAudioProfile`。
5. 如果需要事件联动，可在 `YusAudioDefinition` 或 `YusSceneAudioProfile` 上填写 `YusAudioEventBinding`。

## 基本用法
```csharp
var audioService = YusAudioService.RequireInstance();

audioService.PlayBgm("main_theme", 0.4f);
audioService.PlaySfx("hit_light");
audioService.PlayUi("button_click");
audioService.PlayVoice("npc_warning");
```

## 场景默认 BGM
`YusSceneAudioProfile` 主要字段：
- `DefaultBgmAudioId`
- `EnterPolicy`
- `ExitPolicy`
- `EnterFadeSeconds`
- `ExitFadeSeconds`
- `OverrideLibrary`
- `AdditionalLibraries`

示例：
```csharp
var profile = new YusSceneAudioProfile
{
    DefaultBgmAudioId = "battle_bgm",
    EnterPolicy = YusSceneBgmEnterPolicy.PlayAlways,
    ExitPolicy = YusSceneBgmExitPolicy.RequestOutro,
    EnterFadeSeconds = 0.35f
};

var controller = new YusSceneAudioController
{
    Profile = profile
};

AddChild(controller);
```

## BGM 编排
如果 `Category` 为 `Bgm`：
- `SingleLoop` 模式使用 `SingleStream`
- `IntroLoopOutro` 模式使用 `IntroStream`、`LoopStream`、`OutroStream`
- `WaitLoopBoundaryBeforeOutro` 为真时，会等当前 loop 播放结束后再进入 outro

## 事件联动
你可以在 `YusAudioDefinition` 上填写：
- `OnEnterEventIds`
- `OnExitEventIds`
- `EventBindings`

也可以在 `YusSceneAudioProfile` 上追加 `EventBindings`。

常见动作包括：
- `PlayBoundAudio`
- `PlayBgm`
- `PlaySfx`
- `PlayUi`
- `PlayVoice`
- `StopCurrentBgm`
- `RequestCurrentBgmOutro`
- `ReturnToPreviousBgm`

## 音量与持久化
音量数据默认通过 `SimpleBinarySaver` 保存到：

```text
YusAudioSystem/Settings
```

可直接调用：
```csharp
var audioService = YusAudioService.RequireInstance();
audioService.SetCategoryVolume(YusAudioCategory.Bgm, 0.7f);
audioService.SetCategoryMute(YusAudioCategory.Sfx, false);
```

## 示例说明
示例位于 `res://YusGameFrame/YusAudioSystem/Example/`，运行后会动态生成：
- 测试按钮
- 运行时音频库
- 两套场景音频配置
- 几段程序生成的测试音频

示例覆盖：
- 场景切换带来的默认 BGM 接管
- 临时切歌与恢复上一首
- 命中音效、UI 音效、语音播放
- 通过 `YusEventSystem` 广播事件触发音频
- 分类音量调整
