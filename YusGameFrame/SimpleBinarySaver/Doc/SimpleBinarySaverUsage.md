# SimpleBinarySaver 使用说明

## 功能概述
`SimpleBinarySaver` 提供统一的二进制持久化能力，适合保存基础类型、Godot Variant 数据以及常规 C# 对象。

公共入口：
- `res://YusGameFrame/SimpleBinarySaver/SimpleBinarySaver.cs`
- `res://YusGameFrame/SimpleBinarySaver/SimpleBinarySaverService.cs`

默认保存目录为 `user://SimpleBinarySaver/`，文件扩展名统一为 `.yus`。

## 接入方式
项目通过 `Autoload` 挂载 `SimpleBinarySaverService` 后，运行时可直接使用：

```csharp
using YusGameFrame.SimpleBinarySaver;
```

当前保存目录也可以直接获取：

```csharp
var relativePath = SimpleBinarySaver.GetSaveDirectoryPath();
var absolutePath = SimpleBinarySaver.GetSaveDirectoryAbsolutePath();
```

## 基本用法
保存与读取整数：

```csharp
SimpleBinarySaver.Save(123, "IntValue");
var intValue = SimpleBinarySaver.Load("IntValue", 0);
```

保存与读取字符串：

```csharp
SimpleBinarySaver.Save("测试文本", "StringValue");
var text = SimpleBinarySaver.Load("StringValue", string.Empty);
```

保存与读取 C# 对象：

```csharp
var archive = new PlayerArchive
{
    Name = "测试勇者",
    Level = 5
};

SimpleBinarySaver.Save(archive, "PlayerArchive");
var loadedArchive = SimpleBinarySaver.Load("PlayerArchive", new PlayerArchive());
```

## 行为说明
- 当文件不存在时，`Load` 会返回传入的默认值。
- 当 `key` 非法、文件损坏或类型不匹配时，系统会输出简体中文日志并回退到默认值。
- `Save(null, key)` 是允许的；若读取目标类型不可为空，则会回退到默认值。
- 普通 C# 对象默认使用 `System.Text.Json` 自动序列化，适合公开属性或字段组成的数据类。
- 每个 `.yus` 文件都会记录原始 key、数据类型与数据类别，便于在编辑器工具窗口中浏览和修改。

## 编辑器窗口
项目新增了编辑器插件目录 `res://addons/SimpleBinarySaverEditor/`。

启用方式：
1. 打开 Godot 编辑器的 `项目 -> 项目设置 -> 插件`
2. 启用 `SimpleBinarySaverEditor`
3. 在编辑器右侧 Dock 中查看 `SimpleBinarySaver` 窗口

窗口支持：
- 显示当前保存目录和绝对路径
- 列出当前所有 `.yus` 文件
- 展示 key、类型、数据类别、文件路径
- 直接编辑并保存已有值

编辑规则：
- `Godot Variant` 数据使用 `GD.VarToStr` / `GD.StrToVar` 文本格式
- `C# 对象` 使用 JSON 文本格式

## 示例
可将 [SimpleBinarySaverTest.cs](/e:/Stardenia/Godot/test/YusGameFrame/SimpleBinarySaver/Example/SimpleBinarySaverTest.cs) 挂到任意空 `Control` 节点。脚本会动态生成按钮与日志标签，支持测试：
- 保存/读取整数
- 保存/读取字符串
- 保存/读取字典
- 保存/读取普通 C# 对象
- 缺失文件的默认值回退
- 同 key 覆盖保存
