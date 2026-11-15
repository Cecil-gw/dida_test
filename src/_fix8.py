import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()

# 1. top bar: remove "AI 助手" title, providers button to left, status after
old_top = '''        var top = new DockPanel { Margin = new Thickness(10, 8, 10, 4), LastChildFill = false };
        var title = new TextBlock { Text = "AI 助手", FontWeight = FontWeights.SemiBold, FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
        var providersBtn = MakeToolButton("服务商");
        providersBtn.Click += (s, e) => ToggleDrawer();
        providersBtn.Margin = new Thickness(3, 0, 68, 0);
        DockPanel.SetDock(providersBtn, Dock.Right);
        top.Children.Add(providersBtn);
        top.Children.Add(_statusText);
        top.Children.Add(title);'''
new_top = '''        var top = new DockPanel { Margin = new Thickness(10, 8, 10, 4), LastChildFill = false };
        var providersBtn = MakeToolButton("服务商");
        providersBtn.Click += (s, e) => ToggleDrawer();
        DockPanel.SetDock(_statusText, Dock.Right);
        top.Children.Add(providersBtn);
        top.Children.Add(_statusText);'''
assert old_top in c
c = c.replace(old_top, new_top, 1)

# 2. status: no "未激活服务商"
c = c.replace('''        _statusText.Text = active == null
            ? "未激活服务商"
            : active.name + " · " + (string.IsNullOrEmpty(active.model) ? active.base_url : active.model);''',
'''        _statusText.Text = active == null
            ? ""
            : active.name + " · " + (string.IsNullOrEmpty(active.model) ? active.base_url : active.model);''')

# 3. wrap chat scroll in translucent white inner window
old_host = '''        var chatHost = new Grid();
        _scroll = new ScrollViewer
        {
            Content = _chat,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = Brushes.Transparent
        };
        _scroll.Resources = new ResourceDictionary();
        _scroll.Resources[typeof(ScrollBar)] = MakeSlimScrollBarStyle();
        chatHost.Children.Add(_scroll);
        chatHost.Children.Add(_drawerGrid);
        root.Children.Add(chatHost);
        Grid.SetRow(chatHost, 1);'''
new_host = '''        var chatHost = new Grid();
        _scroll = new ScrollViewer
        {
            Content = _chat,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = Brushes.Transparent
        };
        _scroll.Resources = new ResourceDictionary();
        _scroll.Resources[typeof(ScrollBar)] = MakeSlimScrollBarStyle();
        var innerWindow = new Border
        {
            Child = _scroll,
            Background = new SolidColorBrush(Color.FromArgb(0x4D, 0xFF, 0xFF, 0xFF)),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10, 8, 6, 8),
            Margin = new Thickness(10, 0, 10, 4)
        };
        chatHost.Children.Add(innerWindow);
        chatHost.Children.Add(_drawerGrid);
        root.Children.Add(chatHost);
        Grid.SetRow(chatHost, 1);'''
assert old_host in c
c = c.replace(old_host, new_host, 1)

# 4. icon buttons aligned with input height + rounded input
old_bottom = '''        var iconCol = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };
        var injectBtn = MakeToolButton("📋");
        injectBtn.ToolTip = new ToolTip { Content = "把当前待办任务注入对话上下文" };
        injectBtn.Click += (s, e) => { _ = InjectTodosAsync(); };
        var promptBtn = MakeToolButton("🧩");
        promptBtn.ToolTip = new ToolTip { Content = "切换系统提示词" };
        promptBtn.Click += (s, e) => ShowPromptMenu(promptBtn);
        iconCol.Children.Add(injectBtn);
        iconCol.Children.Add(promptBtn);
        Grid.SetColumn(iconCol, 1);
        bottom.Children.Add(iconCol);
        _sendBtn.Style = MakeCapsuleStyle();'''
new_bottom = '''        var iconCol = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };
        var injectBtn = MakeToolButton("📋");
        injectBtn.ToolTip = new ToolTip { Content = "把当前待办任务注入对话上下文" };
        injectBtn.Click += (s, e) => { _ = InjectTodosAsync(); };
        var promptBtn = MakeToolButton("🧩");
        promptBtn.ToolTip = new ToolTip { Content = "切换系统提示词" };
        promptBtn.Click += (s, e) => ShowPromptMenu(promptBtn);
        iconCol.Children.Add(injectBtn);
        iconCol.Children.Add(promptBtn);
        _input.SizeChanged += (s, e) =>
        {
            injectBtn.Height = _input.ActualHeight;
            promptBtn.Height = _input.ActualHeight;
        };
        Grid.SetColumn(iconCol, 1);
        bottom.Children.Add(iconCol);
        _sendBtn.Style = MakeCapsuleStyle();'''
assert old_bottom in c
c = c.replace(old_bottom, new_bottom, 1)

# 5. input rounded: wrap in Border corner 14
c = c.replace('''        _input.PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                SendAsync();
            }
        };
        Grid.SetColumn(_input, 0);
        bottom.Children.Add(_input);''',
'''        _input.PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                SendAsync();
            }
        };
        var inputBorder = new Border
        {
            Child = _input,
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF))
        };
        Grid.SetColumn(inputBorder, 0);
        bottom.Children.Add(inputBorder);''')

with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('FIX8 DONE')
