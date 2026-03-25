# AGENTS

## 项目约定
- 本项目始终只使用 C#。
- 所有文本文件统一使用 UTF-8 编码，避免中文乱码。
- 若框架结构、公共接口、系统职责发生变化，且会影响后续复用或阅读，需要同步更新本文件。
- 每落实一个新系统，都需要在该系统目录下的 `Doc/` 文件夹内补充一份说明其用途、接入方式与基本用法的 `.md` 使用文档。
- 示例脚本应优先通过动态生成物体、按钮、标签等测试对象来完成便捷测试，尽量减少对手工搭场景的依赖。
- 生成的所有文本内容都应使用简体中文，包括文档、代码注释、日志提示、报错文案和 UI 文本。

## YusGameFrame
- 根目录：`res://YusGameFrame/`
- 用途：存放可跨项目复用的运行时系统。
- 组织方式：每个系统单独放在自己的子目录中，并提供明确的公共入口。

## 系统路由

### YusTimer
- 路径：`res://YusGameFrame/YusTimer/`
- 功能：提供统一的运行时计时器服务，支持一次性、循环、取消、暂停、恢复。
- 应用场景：技能冷却、UI 延迟提示、循环轮询、战斗节奏控制、演出定时触发。
- 入口：
  - `res://YusGameFrame/YusTimer/YusTimerService.cs`
  - `res://YusGameFrame/YusTimer/YusTimerHandle.cs`
- 示例：
  - `res://YusGameFrame/YusTimer/Example/YusTimerTest.cs`
  - `res://YusGameFrame/YusTimer/Doc/YusTimerUsage.md`

### YusEventSystem
- 路径：`res://YusGameFrame/YusEventSystem/`
- 功能：提供统一的全局事件服务，支持在编辑器中维护事件定义，并生成强类型 C# 广播与监听入口。
- 应用场景：跨场景广播、UI 状态同步、战斗事件通知、任务状态变更、全局系统解耦。
- 特性：监听绑定 `Node` 生命周期，节点退出场景树或释放后会自动取消订阅，也支持手动移除监听。
- 入口：
  - `res://YusGameFrame/YusEventSystem/YusEventSystemService.cs`
  - `res://YusGameFrame/YusEventSystem/YusEventDefinitionConfig.cs`
  - `res://YusGameFrame/YusEventSystem/Generated/YusEventSignals.g.cs`
- 示例：
  - `res://YusGameFrame/YusEventSystem/Example/YusEventSystemTest.cs`
  - `res://YusGameFrame/YusEventSystem/Example/YusEventSystemDefinition.tres`
  - `res://YusGameFrame/YusEventSystem/Doc/YusEventSystemUsage.md`

### SimpleBinarySaver
- 路径：`res://YusGameFrame/SimpleBinarySaver/`
- 功能：提供统一的二进制数据持久化服务，对外暴露简洁的 `Save` 与 `Load` 入口。
- 应用场景：玩家设置存档、数值缓存、离线进度记录、简单配置持久化。
- 特性：默认保存到 `user://SimpleBinarySaver/`，文件扩展名统一为 `.yus`，同时支持基础类型、Godot Variant 数据与普通 C# 对象，并提供编辑器窗口查看与修改已有存档。
- 入口：
  - `res://YusGameFrame/SimpleBinarySaver/SimpleBinarySaver.cs`
  - `res://YusGameFrame/SimpleBinarySaver/SimpleBinarySaverService.cs`
- 示例：
  - `res://YusGameFrame/SimpleBinarySaver/Example/SimpleBinarySaverTest.cs`
  - `res://YusGameFrame/SimpleBinarySaver/Doc/SimpleBinarySaverUsage.md`

### YusFSMSystem
- 路径：`res://YusGameFrame/YusFSMSystem/`
- 功能：提供统一的有限状态机能力，支持启动、切换、回退、停止，以及状态级生命周期回调。
- 应用场景：角色行为切换、游戏流程控制、UI 状态管理、战斗阶段切换、可回退的运行时逻辑组织。
- 特性：状态实例按类型缓存复用；宿主可在 `_Process` 与 `_PhysicsProcess` 中手动驱动；状态内可直接注册 `YusEventSystem` 事件，并在切换或停止时自动清理监听。
- 入口：
  - `res://YusGameFrame/YusFSMSystem/YusFSM.cs`
  - `res://YusGameFrame/YusFSMSystem/YusState.cs`
- 示例：
  - `res://YusGameFrame/YusFSMSystem/Example/YusFSMTest.cs`
  - `res://YusGameFrame/YusFSMSystem/Doc/YusFSMSystemUsage.md`

### YusUIFlowSystem
- 路径：`res://YusGameFrame/YusUIFlowSystem/`
- 功能：提供统一的页面与弹窗流转能力，支持动态加载界面、路由配置、返回栈与共享数据节点联动。
- 应用场景：主菜单切换、背包与商店页面跳转、确认弹窗、状态面板、跨场景复用 UI。
- 特性：运行时使用 `ScreenId + ActionId` 执行路由；界面场景根节点统一继承 `YusUIScreen`；`YusUIDataNode` 提供键值数据、变更通知与可选的 `SimpleBinarySaver` 持久化；编辑器支持扫描按钮生成路由草稿。
- 入口：
  - `res://YusGameFrame/YusUIFlowSystem/YusUIFlowService.cs`
  - `res://YusGameFrame/YusUIFlowSystem/YusUIScreen.cs`
  - `res://YusGameFrame/YusUIFlowSystem/YusUIRouteConfig.cs`
  - `res://YusGameFrame/YusUIFlowSystem/Data/YusUIDataNode.cs`
- 示例：
  - `res://YusGameFrame/YusUIFlowSystem/Example/YusUIFlowExampleRoot.tscn`
  - `res://YusGameFrame/YusUIFlowSystem/Example/YusUIFlowExampleRouteConfig.tres`
  - `res://YusGameFrame/YusUIFlowSystem/Doc/YusUIFlowSystemUsage.md`

### YusAudioSystem
- 路径：`res://YusGameFrame/YusAudioSystem/`
- 功能：提供统一的音频库、BGM、SFX、UI、Voice 管理能力，支持场景配置接管、事件触发、淡入淡出与音量持久化。
- 应用场景：场景默认背景音乐、战斗切歌、按钮点击音效、角色语音提示、事件驱动的演出播放。
- 特性：运行时通过 `YusAudioService` 统一实际播放；支持 `SingleLoop` 与 `Intro/Loop/Outro` 两种 BGM 模式；场景通过 `YusSceneAudioController + YusSceneAudioProfile` 声明默认音乐策略；编辑器支持浏览、校验和批量生成音频库草稿。
- 入口：
  - `res://YusGameFrame/YusAudioSystem/YusAudioService.cs`
  - `res://YusGameFrame/YusAudioSystem/YusAudioLibrary.cs`
  - `res://YusGameFrame/YusAudioSystem/YusAudioDefinition.cs`
  - `res://YusGameFrame/YusAudioSystem/YusSceneAudioProfile.cs`
  - `res://YusGameFrame/YusAudioSystem/YusSceneAudioController.cs`
- 示例：
  - `res://YusGameFrame/YusAudioSystem/Example/YusAudioExampleRoot.tscn`
  - `res://YusGameFrame/YusAudioSystem/Doc/YusAudioSystemUsage.md`
