using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YusGameFrame.YusAudioSystem;

[Tool]
public partial class YusAudioEditorDock : Control
{
    private readonly EditorPlugin _plugin;

    private LineEdit _libraryPathEdit = null!;
    private LineEdit _scanDirectoryEdit = null!;
    private OptionButton _categoryOption = null!;
    private ItemList _definitionList = null!;
    private Label _statusLabel = null!;

    private YusAudioLibrary? _library;

    public YusAudioEditorDock(EditorPlugin plugin)
    {
        _plugin = plugin;
    }

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var root = new MarginContainer();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("margin_left", 12);
        root.AddThemeConstantOverride("margin_top", 12);
        root.AddThemeConstantOverride("margin_right", 12);
        root.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(root);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 10);
        root.AddChild(layout);

        layout.AddChild(new Label
        {
            Text = "YusAudio 资源维护"
        });

        _libraryPathEdit = CreateLineEdit("res://YusGameFrame/YusAudioSystem/Example/YusAudioLibrary.tres");
        layout.AddChild(CreateField("音频库路径", _libraryPathEdit));

        _scanDirectoryEdit = CreateLineEdit("res://");
        layout.AddChild(CreateField("扫描目录", _scanDirectoryEdit));

        _categoryOption = new OptionButton();
        _categoryOption.AddItem("Bgm", (int)YusAudioCategory.Bgm);
        _categoryOption.AddItem("Sfx", (int)YusAudioCategory.Sfx);
        _categoryOption.AddItem("Ui", (int)YusAudioCategory.Ui);
        _categoryOption.AddItem("Voice", (int)YusAudioCategory.Voice);
        layout.AddChild(CreateField("生成分类", _categoryOption));

        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", 8);
        layout.AddChild(buttonRow);

        buttonRow.AddChild(CreateButton("加载库", LoadLibrary));
        buttonRow.AddChild(CreateButton("保存库", SaveLibrary));
        buttonRow.AddChild(CreateButton("校验", ValidateLibrary));
        buttonRow.AddChild(CreateButton("批量生成草稿", GenerateDefinitionsFromDirectory));

        var secondRow = new HBoxContainer();
        secondRow.AddThemeConstantOverride("separation", 8);
        layout.AddChild(secondRow);

        secondRow.AddChild(CreateButton("打开库到检查器", OpenLibraryInInspector));
        secondRow.AddChild(CreateButton("打开选中条目", OpenSelectedDefinition));

        _definitionList = new ItemList();
        _definitionList.CustomMinimumSize = new Vector2(0, 220);
        _definitionList.SizeFlagsVertical = SizeFlags.ExpandFill;
        layout.AddChild(_definitionList);

        _statusLabel = new Label();
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _statusLabel.Text = "先加载一个音频库，再进行校验或扫描。";
        layout.AddChild(_statusLabel);
    }

    private static Control CreateField(string labelText, Control field)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        box.AddChild(new Label { Text = labelText });
        box.AddChild(field);
        return box;
    }

    private static LineEdit CreateLineEdit(string text)
    {
        return new LineEdit
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
    }

    private static Button CreateButton(string text, Action callback)
    {
        var button = new Button();
        button.Text = text;
        button.Pressed += callback;
        return button;
    }

    private void LoadLibrary()
    {
        var path = _libraryPathEdit.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            _statusLabel.Text = "音频库路径不能为空。";
            return;
        }

        _library = ResourceLoader.Exists(path)
            ? ResourceLoader.Load<YusAudioLibrary>(path)
            : new YusAudioLibrary();

        if (_library == null)
        {
            _statusLabel.Text = $"无法加载音频库：{path}";
            return;
        }

        RefreshDefinitionList();
        _statusLabel.Text = $"已加载音频库：{path}";
    }

    private void SaveLibrary()
    {
        if (!EnsureLibraryLoaded())
        {
            return;
        }

        var result = ResourceSaver.Save(_library!, _libraryPathEdit.Text.Trim());
        _statusLabel.Text = result == Error.Ok
            ? "音频库保存成功。"
            : $"音频库保存失败：{result}";
    }

    private void ValidateLibrary()
    {
        if (!EnsureLibraryLoaded())
        {
            return;
        }

        var messages = _library!.GetValidationMessages();
        if (messages.Length == 0)
        {
            _statusLabel.Text = "校验通过，没有发现问题。";
            return;
        }

        _statusLabel.Text = string.Join("\n", messages);
    }

    private void GenerateDefinitionsFromDirectory()
    {
        if (!EnsureLibraryLoaded())
        {
            return;
        }

        var scanPath = _scanDirectoryEdit.Text.Trim();
        if (string.IsNullOrWhiteSpace(scanPath))
        {
            _statusLabel.Text = "扫描目录不能为空。";
            return;
        }

        var absolutePath = ProjectSettings.GlobalizePath(scanPath);
        if (!Directory.Exists(absolutePath))
        {
            _statusLabel.Text = $"扫描目录不存在：{scanPath}";
            return;
        }

        var definitions = _library!.Definitions?.ToList() ?? [];
        var existingIds = new HashSet<string>(definitions
            .Where(definition => definition != null && !string.IsNullOrWhiteSpace(definition.AudioId))
            .Select(definition => definition!.AudioId.Trim()), StringComparer.Ordinal);

        var category = (YusAudioCategory)_categoryOption.GetSelectedId();
        var addedCount = 0;

        foreach (var filePath in Directory.GetFiles(absolutePath, "*.*", SearchOption.AllDirectories))
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension is not ".wav" and not ".ogg" and not ".mp3")
            {
                continue;
            }

            var resourcePath = ProjectSettings.LocalizePath(filePath);
            var audioId = BuildAudioId(Path.GetFileNameWithoutExtension(filePath));
            if (!existingIds.Add(audioId))
            {
                continue;
            }

            var stream = ResourceLoader.Load<AudioStream>(resourcePath);
            if (stream == null)
            {
                continue;
            }

            definitions.Add(new YusAudioDefinition
            {
                AudioId = audioId,
                Category = category,
                BgmPlaybackMode = YusBgmPlaybackMode.SingleLoop,
                SingleStream = stream
            });
            addedCount++;
        }

        _library.Definitions = definitions.ToArray();
        RefreshDefinitionList();
        _statusLabel.Text = $"批量生成完成，新增加了 {addedCount} 条草稿。";
    }

    private void OpenLibraryInInspector()
    {
        if (!EnsureLibraryLoaded())
        {
            return;
        }

        EditorInterface.Singleton.EditResource(_library);
        _statusLabel.Text = "已在检查器中打开音频库。";
    }

    private void OpenSelectedDefinition()
    {
        if (!EnsureLibraryLoaded())
        {
            return;
        }

        var selectedItems = _definitionList.GetSelectedItems();
        if (selectedItems.Length == 0)
        {
            _statusLabel.Text = "请先选择一个条目。";
            return;
        }

        var definition = _library!.Definitions[selectedItems[0]];
        if (definition == null)
        {
            _statusLabel.Text = "当前条目为空。";
            return;
        }

        EditorInterface.Singleton.EditResource(definition);
        _statusLabel.Text = $"已在检查器中打开条目：{definition.AudioId}";
    }

    private void RefreshDefinitionList()
    {
        _definitionList.Clear();
        if (_library?.Definitions == null)
        {
            return;
        }

        foreach (var definition in _library.Definitions)
        {
            if (definition == null)
            {
                _definitionList.AddItem("(空条目)");
                continue;
            }

            _definitionList.AddItem($"{definition.AudioId} [{definition.Category}]");
        }
    }

    private bool EnsureLibraryLoaded()
    {
        if (_library != null)
        {
            return true;
        }

        _statusLabel.Text = "请先加载音频库。";
        return false;
    }

    private static string BuildAudioId(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "unnamed_audio";
        }

        var parts = fileName
            .Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return fileName;
        }

        return string.Join("_", parts).ToLowerInvariant();
    }
}
