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
