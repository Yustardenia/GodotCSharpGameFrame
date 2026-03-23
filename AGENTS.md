# AGENTS

## 项目约定
- 本项目始终只使用 C#。
- 所有文本文件统一使用 UTF-8 编码，避免中文乱码。
- 若框架结构、公共接口、系统职责发生变化，且会影响后续复用或阅读，需要同步更新本文件。

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
