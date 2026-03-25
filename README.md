# YusGameFrame for Godot C#

一个基于 **Godot 4 + C#** 的轻量游戏框架示例仓库。  
它的目标不是做一套“大而全”的引擎外壳，而是把项目里常见、可复用的运行时系统拆成独立模块，用统一风格沉淀下来，方便后续跨项目复用。

这套仓库目前主要围绕下面这条思路组织：

- 运行时系统尽量独立
- 统一采用 C#
- 优先使用 `Autoload + Resource`
- 每个系统都配示例和文档
- 能做编辑器辅助的系统，尽量补上编辑器工具

## 项目特点

- 面向 Godot C# 项目，而不是 GDScript 为主的工作流
- 偏“可复用运行时模块”设计，而不是单一游戏业务代码堆积
- 强调资源配置、场景接入、示例验证和文档配套
- 适合逐步积累成自己的常用框架库

## 当前包含的系统

### YusTimer
- 统一运行时计时器服务
- 支持一次性、循环、取消、暂停、恢复

### YusEventSystem
- 全局事件系统
- 支持维护事件定义并生成强类型 C# 广播/监听入口
- 监听绑定 `Node` 生命周期，节点销毁后自动清理

### SimpleBinarySaver
- 简洁的二进制持久化服务
- 支持基础类型、Godot Variant、普通 C# 对象
- 附带编辑器查看与修改工具

### YusFSMSystem
- 有限状态机系统
- 支持启动、切换、回退、停止

### YusUIFlowSystem
- 页面与弹窗流转系统
- 支持路由配置、动态加载界面、共享数据节点联动
- 附带编辑器路由扫描工具

### YusAudioSystem
- 统一音频库与播放服务
- 支持 `BGM / SFX / UI / Voice`
- 支持 `SingleLoop` 和 `Intro / Loop / Outro`
- 支持场景默认音乐接管、事件触发播放、音量持久化
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

通常每个系统都会包含：

- 运行时入口
- `Example/` 示例
- `Doc/` 使用文档
- 必要时配套 `addons/` 编辑器插件

## 环境要求

- Godot 4.6+
- .NET 8
- C#

## 当前项目已配置内容

### Autoload
- `YusTimer`
- `YusEventSystem`
- `SimpleBinarySaver`
- `YusUIFlow`
- `YusAudio`

### 编辑器插件
- `SimpleBinarySaverEditor`
- `YusUIFlowEditor`
- `YusAudioEditor`

## 快速开始

如果你想先快速体验系统效果，推荐从示例场景开始。

### 音频系统示例

打开：

`res://YusGameFrame/YusAudioSystem/Example/YusAudioExampleRoot.tscn`

这个示例使用真实 `.wav` 音频资源和 `.tres` 音频库配置，演示：

- 场景默认 BGM 接管
- BGM 切换与恢复上一首
- `UI / SFX / Voice` 播放
- 事件触发音频
- 分类音量调节

### UI 流转系统示例

打开：

`res://YusGameFrame/YusUIFlowSystem/Example/YusUIFlowExampleRoot.tscn`

这个示例主要演示：

- 路由配置驱动的页面切换
- 弹窗与页面栈管理
- `YusUIDataNode` 共享数据与持久化

## 文档入口

- `res://YusGameFrame/YusTimer/Doc/YusTimerUsage.md`
- `res://YusGameFrame/YusEventSystem/Doc/YusEventSystemUsage.md`
- `res://YusGameFrame/SimpleBinarySaver/Doc/SimpleBinarySaverUsage.md`
- `res://YusGameFrame/YusFSMSystem/Doc/YusFSMSystemUsage.md`
- `res://YusGameFrame/YusUIFlowSystem/Doc/YusUIFlowSystemUsage.md`
- `res://YusGameFrame/YusAudioSystem/Doc/YusAudioSystemUsage.md`

## 推荐阅读顺序

如果你第一次看这个仓库，推荐按下面顺序了解：

1. `README.md`
2. `AGENTS.md`
3. 目标系统的 `Doc/` 文档
4. 对应系统的 `Example/` 示例
5. 最后再读运行时代码

## 适合拿来做什么

- 作为 Godot C# 个人常用框架底座
- 从 Unity C# 工作流迁移到 Godot 时，逐步沉淀自己的系统层
- 为多个项目复用通用模块，而不是每次从零开始搭

## 说明

- 仓库内所有系统统一使用 C#
- 文档、注释、日志、报错文案和 UI 文本默认使用简体中文
- 如果系统入口、职责或结构发生变化，请同步更新 `AGENTS.md` 与对应系统文档

---

如果你正在看这个仓库，最值得优先体验的两个模块通常是：

- `YusUIFlowSystem`
- `YusAudioSystem`

一个更偏 UI 框架化接入，一个更偏运行时资源驱动与场景控制，整体风格也最能代表这个仓库现在的方向。
