using Godot;
using YusGameFrame.SimpleBinarySaver;

[Tool]
public partial class SimpleBinarySaverEditorDock : Control
{
    private ItemList _entryList = null!;
    private Label _relativeDirectoryLabel = null!;
    private Label _absoluteDirectoryLabel = null!;
    private Label _entryCountLabel = null!;
    private Label _keyLabel = null!;
    private Label _typeLabel = null!;
    private Label _kindLabel = null!;
    private Label _pathLabel = null!;
    private TextEdit _valueEditor = null!;
    private Label _statusLabel = null!;
    private Godot.Button _saveButton = null!;
    private VBoxContainer _directoryContent = null!;
    private VBoxContainer _listContent = null!;
    private VBoxContainer _detailContent = null!;
    private VBoxContainer _statusContent = null!;

    private System.Collections.Generic.IReadOnlyList<SimpleBinarySaverEntryInfo> _entries = System.Array.Empty<SimpleBinarySaverEntryInfo>();
    private SimpleBinarySaverEntryInfo? _selectedEntry;

    public override void _Ready()
    {
        BuildUi();
        RefreshEntries();
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var scroll = new ScrollContainer();
        scroll.SetAnchorsPreset(LayoutPreset.FullRect);
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
        AddChild(scroll);

        var root = new MarginContainer();
        root.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddThemeConstantOverride("margin_left", 12);
        root.AddThemeConstantOverride("margin_top", 12);
        root.AddThemeConstantOverride("margin_right", 12);
        root.AddThemeConstantOverride("margin_bottom", 12);
        scroll.AddChild(root);

        var layout = new VBoxContainer();
        layout.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        layout.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        layout.AddThemeConstantOverride("separation", 12);
        root.AddChild(layout);

        var title = new Label();
        title.Text = "SimpleBinarySaver 编辑器";
        title.AddThemeFontSizeOverride("font_size", 20);
        layout.AddChild(title);

        var directoryPanel = new PanelContainer();
        directoryPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        layout.AddChild(directoryPanel);

        var directoryLayout = CreatePanelLayout();
        directoryPanel.AddChild(directoryLayout);

        _directoryContent = CreateFoldSection(directoryLayout, "存档目录", true);

        _relativeDirectoryLabel = new Label();
        _relativeDirectoryLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _directoryContent.AddChild(_relativeDirectoryLabel);

        _absoluteDirectoryLabel = new Label();
        _absoluteDirectoryLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _directoryContent.AddChild(_absoluteDirectoryLabel);

        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 8);
        _directoryContent.AddChild(actionRow);

        var refreshButton = new Godot.Button();
        refreshButton.Text = "刷新列表";
        refreshButton.Pressed += RefreshEntries;
        actionRow.AddChild(refreshButton);

        _entryCountLabel = new Label();
        _entryCountLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _entryCountLabel.VerticalAlignment = VerticalAlignment.Center;
        actionRow.AddChild(_entryCountLabel);

        var listPanel = new PanelContainer();
        listPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        layout.AddChild(listPanel);

        var listLayout = CreatePanelLayout();
        listPanel.AddChild(listLayout);

        _listContent = CreateFoldSection(listLayout, "已保存的键", true);

        _entryList = new ItemList();
        _entryList.CustomMinimumSize = new Vector2(0, 160);
        _entryList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _entryList.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _entryList.ItemSelected += OnEntrySelected;
        _listContent.AddChild(_entryList);

        var detailPanel = new PanelContainer();
        detailPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        detailPanel.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        layout.AddChild(detailPanel);

        var detailLayout = CreatePanelLayout();
        detailLayout.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        detailLayout.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        detailPanel.AddChild(detailLayout);

        _detailContent = CreateFoldSection(detailLayout, "当前条目", true);

        _keyLabel = new Label();
        _keyLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailContent.AddChild(_keyLabel);

        _typeLabel = new Label();
        _typeLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailContent.AddChild(_typeLabel);

        _kindLabel = new Label();
        _kindLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailContent.AddChild(_kindLabel);

        _pathLabel = new Label();
        _pathLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailContent.AddChild(_pathLabel);

        _valueEditor = new TextEdit();
        _valueEditor.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _valueEditor.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        _valueEditor.WrapMode = TextEdit.LineWrappingMode.Boundary;
        _valueEditor.CustomMinimumSize = new Vector2(0, 260);
        _detailContent.AddChild(_valueEditor);

        _saveButton = new Godot.Button();
        _saveButton.Text = "保存当前值";
        _saveButton.Pressed += SaveSelectedEntry;
        _detailContent.AddChild(_saveButton);

        var statusPanel = new PanelContainer();
        statusPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        layout.AddChild(statusPanel);

        var statusLayout = CreatePanelLayout();
        statusPanel.AddChild(statusLayout);

        _statusContent = CreateFoldSection(statusLayout, "状态与说明", false);

        _statusLabel = new Label();
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _statusContent.AddChild(_statusLabel);
    }

    private static VBoxContainer CreatePanelLayout()
    {
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        return layout;
    }

    private static Label CreateSectionLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", 15);
        return label;
    }

    private static VBoxContainer CreateFoldSection(VBoxContainer parent, string title, bool expandedByDefault)
    {
        var headerButton = new Godot.Button();
        headerButton.Alignment = HorizontalAlignment.Left;
        headerButton.Flat = true;
        headerButton.FocusMode = FocusModeEnum.None;
        headerButton.Text = expandedByDefault ? $"▼ {title}" : $"▶ {title}";
        parent.AddChild(headerButton);

        var content = new VBoxContainer();
        content.Visible = expandedByDefault;
        content.AddThemeConstantOverride("separation", 6);
        parent.AddChild(content);

        headerButton.Pressed += () =>
        {
            content.Visible = !content.Visible;
            headerButton.Text = content.Visible ? $"▼ {title}" : $"▶ {title}";
        };

        return content;
    }

    private void RefreshEntries()
    {
        _relativeDirectoryLabel.Text = $"保存目录：{SimpleBinarySaver.GetSaveDirectoryPath()}";
        _absoluteDirectoryLabel.Text = $"绝对目录：{SimpleBinarySaver.GetSaveDirectoryAbsolutePath()}";

        _entries = SimpleBinarySaver.GetAllEntries();
        _entryList.Clear();

        foreach (var entry in _entries)
        {
            _entryList.AddItem(entry.Key);
        }

        _selectedEntry = null;
        ClearDetail();
        _entryCountLabel.Text = $"共 {_entries.Count} 个存档条目";
        _statusLabel.Text = _entries.Count > 0
            ? "选择左侧条目后可查看详情并直接编辑。"
            : "当前还没有检测到任何 .yus 存档。";

        if (_entries.Count > 0)
        {
            _entryList.Select(0);
            ShowEntry(_entries[0]);
        }
    }

    private void OnEntrySelected(long index)
    {
        if (index < 0 || index >= _entries.Count)
        {
            return;
        }

        ShowEntry(_entries[(int)index]);
    }

    private void ShowEntry(SimpleBinarySaverEntryInfo entry)
    {
        _selectedEntry = entry;
        _keyLabel.Text = $"键：{entry.Key}";
        _typeLabel.Text = $"类型：{entry.TypeName}";
        _kindLabel.Text = $"数据类别：{entry.DataKind}";
        _pathLabel.Text = $"文件：{entry.RelativePath}";
        _valueEditor.Text = entry.EditableText;
        _valueEditor.Editable = entry.IsEditable;
        _saveButton.Disabled = !entry.IsEditable;
        _statusLabel.Text = entry.IsEditable
            ? "当前条目可直接编辑。Godot Variant 使用 VarToStr / StrToVar 文本格式，C# 对象使用 JSON。"
            : "当前条目不可编辑。";
    }

    private void SaveSelectedEntry()
    {
        if (_selectedEntry == null)
        {
            _statusLabel.Text = "请先选择一个条目。";
            return;
        }

        if (!_selectedEntry.IsEditable)
        {
            _statusLabel.Text = "当前条目不支持编辑。";
            return;
        }

        var saved = SimpleBinarySaver.TryUpdateEntry(_selectedEntry.StorageKey, _valueEditor.Text, out var message);
        _statusLabel.Text = saved ? $"保存成功：{message}" : $"保存失败：{message}";
        if (saved)
        {
            RefreshEntries();
        }
    }

    private void ClearDetail()
    {
        _keyLabel.Text = "键：";
        _typeLabel.Text = "类型：";
        _kindLabel.Text = "数据类别：";
        _pathLabel.Text = "文件：";
        _valueEditor.Text = string.Empty;
        _valueEditor.Editable = false;
        _saveButton.Disabled = true;
    }
}
