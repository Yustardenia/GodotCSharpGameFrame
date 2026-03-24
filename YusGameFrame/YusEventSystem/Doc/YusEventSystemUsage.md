# YusEventSystem 使用说明

## 功能概述
`YusEventSystem` 提供全局事件广播能力，支持在编辑器里维护事件定义，并生成强类型 C# 广播与监听入口。

公共入口：
- `res://YusGameFrame/YusEventSystem/YusEventSystemService.cs`
- `res://YusGameFrame/YusEventSystem/YusEventDefinitionConfig.cs`
- `res://YusGameFrame/YusEventSystem/Generated/YusEventSignals.g.cs`

## 编辑器工作流
1. 打开 `res://YusGameFrame/YusEventSystem/Example/YusEventSystemDefinition.tres`
2. 在 `Events` 中新增或修改事件定义
3. 填写：
   - `EventName`
   - `ParameterCount`
   - `ParameterTypes`
4. 点击 `Generate C# API`
5. 使用生成后的 `YusEventSignals.g.cs`

## 基本用法
监听事件时需要传入一个 `Node owner`，监听会跟随该节点生命周期自动清理。

```csharp
using YusGameFrame.YusEventSystem;

public partial class DemoNode : Node
{
	public override void _Ready()
	{
		YusEventSignals.GameStartedEvent.AddListener(this, OnGameStarted);
		YusEventSignals.PlayerHpChangedEvent.AddListener(this, OnPlayerHpChanged);

		YusEventSignals.GameStartedEvent.Broadcast();
		YusEventSignals.PlayerHpChangedEvent.Broadcast(100);
	}

	private void OnGameStarted()
	{
		GD.Print("游戏开始");
	}

	private void OnPlayerHpChanged(int hp)
	{
		GD.Print($"当前血量：{hp}");
	}
}
```

如果节点还活着，但你想提前停止监听，可以手动移除：

```csharp
YusEventSignals.PlayerHpChangedEvent.RemoveListener(OnPlayerHpChanged);
```

## 参数规则
- `0` 个参数：`Broadcast()` / `AddListener(Node, Action)`
- `1` 个参数：`Broadcast(arg1)` / `AddListener(Node, Action<T1>)`
- `2` 个参数：`Broadcast(arg1, arg2)`
- `3` 个参数：`Broadcast(arg1, arg2, arg3)`

超过 `3` 个参数时，建议封装成一个 payload 对象再广播。

## 示例
可将 [YusEventSystemTest.cs](/e:/Stardenia/Godot/test/YusGameFrame/YusEventSystem/Example/YusEventSystemTest.cs) 挂到任意空 `Control` 节点。脚本会动态生成按钮与日志标签，支持测试：
- 广播无参、一参、二参、三参事件
- 手动 `RemoveListener`
- 动态创建并释放监听节点，验证自动取消订阅
