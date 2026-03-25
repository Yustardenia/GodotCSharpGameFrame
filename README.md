# YusGameFrame for Godot C#

一个以 **Godot 4 + C#** 为基础、偏运行时系统化组织的轻量游戏框架示例仓库。  
项目目标是把常见的可复用系统拆成独立模块，尽量通过 `Autoload + Resource + 示例 + 文档` 的方式沉淀下来，方便后续跨项目复用。

## 当前已包含的系统

### YusTimer
- 统一计时器服务
- 支持一次性、循环、取消、暂停、恢复

### YusEventSystem
- 全局事件系统
- 支持编辑器维护事件定义并生成强类型 C# 广播与监听入口
- 监听绑定 `Node` 生命周期，节点销毁后自动清理

### SimpleBinarySaver
- 简洁的二进制持久化服务
- 支持基础类型、Godot Variant、普通 C# 对象
- 附带编辑器查看与修改工具

### YusFSMSystem
- 有限状态机能力
- 支持启动、切换、回退、停止

### YusUIFlowSystem
- 页面与弹窗流转
- 支持路由配置、动态加载界面、共享数据节点联动
- 附带编辑器路由扫描工具

### YusAudioSystem
- 统一音频库与播放服务
- 支持 `BGM / SFX / UI / Voice`
- 支持 `SingleLoop` 与 `Intro / Loop / Outro`
- 支持场景音频配置节点、事件触发播放、音量持久化
- 附带音频库编辑器草稿生成与校验工具

## 目录结构

```text
res://YusGameFrame/
  YusTimer/
  YusEventSystem/
  SimpleBinarySaver/
  YusFSMSystem/
  YusUIFlowSystem/
  YusAudioSystem/
```

每个系统通常包含：
- 运行时入口
- `Example/` 示例
- `Doc/` 使用文档
- 必要时提供 `addons/` 编辑器插件

## 运行要求

- Godot 4.6+
- .NET 8
- C#

## 当前项目设置

项目已在 `project.godot` 中配置以下 Autoload：
- `YusTimer`
- `YusEventSystem`
- `SimpleBinarySaver`
- `YusUIFlow`
- `YusAudio`

已启用的编辑器插件：
- `SimpleBinarySaverEditor`
- `YusUIFlowEditor`
- `YusAudioEditor`

## 快速体验

如果你想先看音频系统，可打开：

`res://YusGameFrame/YusAudioSystem/Example/YusAudioExampleRoot.tscn`

这个示例使用真实 `.wav` 资源与 `.tres` 音频库配置，演示：
- 场景默认 BGM 接管
- BGM 切换与恢复上一首
- UI / SFX / Voice 播放
- 事件触发音频
- 分类音量调节

如果你想看 UI 流转系统，可打开：

`res://YusGameFrame/YusUIFlowSystem/Example/YusUIFlowExampleRoot.tscn`

## 文档入口

- `res://YusGameFrame/YusTimer/Doc/YusTimerUsage.md`
- `res://YusGameFrame/YusEventSystem/Doc/YusEventSystemUsage.md`
- `res://YusGameFrame/SimpleBinarySaver/Doc/SimpleBinarySaverUsage.md`
- `res://YusGameFrame/YusFSMSystem/Doc/YusFSMSystemUsage.md`
- `res://YusGameFrame/YusUIFlowSystem/Doc/YusUIFlowSystemUsage.md`
- `res://YusGameFrame/YusAudioSystem/Doc/YusAudioSystemUsage.md`

## 说明

仓库内所有系统统一使用 C#，文档、注释、日志和 UI 文案默认使用简体中文。  
如果系统结构、入口或职责发生变化，请同步更新 `AGENTS.md` 与对应系统文档。
