import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()

old_ctor = '''        var config = new Expander { Header = "AI API 设置(OpenAI 兼容)", Margin = new Thickness(8, 8, 8, 0), IsExpanded = false };
        var cg = new Grid { Margin = new Thickness(4) };
        cg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        cg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var rows = new (string label, TextBox box)[]
        {
            ("Base URL", _baseUrl), ("API Key", _apiKey), ("Model", _model)
        };
        for (int i = 0; i < rows.Length; i++)
        {
            cg.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var lb = new TextBlock { Text = rows[i].label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2) };
            Grid.SetRow(lb, i); Grid.SetColumn(lb, 0);
            Grid.SetRow(rows[i].box, i); Grid.SetColumn(rows[i].box, 1);
            rows[i].box.Margin = new Thickness(2);
            cg.Children.Add(lb); cg.Children.Add(rows[i].box);
        }
        var save = new Button { Content = "保存配置", Margin = new Thickness(4), Padding = new Thickness(10, 3, 10, 3), HorizontalAlignment = HorizontalAlignment.Left };
        save.Click += (s, e) =>
        {
            extra.AI_ApiBaseUrl = _baseUrl.Text.Trim();
            extra.AI_ApiKey = _apiKey.Text.Trim();
            extra.AI_Model = _model.Text.Trim();
            LocalSettings.Settings.Save();
            AppendBubble("已保存 AI API 配置。", isUser: false);
        };
        cg.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(save, 3); Grid.SetColumn(save, 1);
        cg.Children.Add(save);
        config.Content = cg;
        DockPanel.SetDock(config, Dock.Top);
        root.Children.Add(config);
'''
new_ctor = '''        var configBar = new DockPanel { Margin = new Thickness(8, 8, 8, 0), LastChildFill = false };
        var configBtn = new Button { Content = "AI API 设置", Padding = new Thickness(12, 4, 12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        configBtn.Click += (s, e) => ShowConfigDialog();
        DockPanel.SetDock(configBtn, Dock.Left);
        var status = new TextBlock { Text = "", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
        _statusText = status;
        configBar.Children.Add(configBtn);
        configBar.Children.Add(status);
        DockPanel.SetDock(configBar, Dock.Top);
        root.Children.Add(configBar);
'''
assert old_ctor in c
c = c.replace(old_ctor, new_ctor, 1)

# add status field + ShowConfigDialog method after _history declaration
c = c.replace('''    private readonly List<(string role, string content)> _history = new List<(string, string)>();''',
'''    private readonly List<(string role, string content)> _history = new List<(string, string)>();
    private TextBlock _statusText;''')

c = c.replace('''    public LocalAIChatPanel()
    {''',
'''    private void ShowConfigDialog()
    {
        var extra = LocalSettings.Settings.ExtraSettings;
        _baseUrl.Text = extra.AI_ApiBaseUrl ?? "";
        _apiKey.Text = extra.AI_ApiKey ?? "";
        _model.Text = string.IsNullOrEmpty(extra.AI_Model) ? "gpt-4o-mini" : extra.AI_Model;
        var win = new Window
        {
            Title = "AI API 设置(OpenAI 兼容)",
            Width = 520,
            Height = 260,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this)
        };
        var g = new Grid { Margin = new Thickness(16) };
        for (int i = 0; i < 4; i++) g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var rows = new (string label, TextBox box)[]
        {
            ("Base URL", _baseUrl), ("API Key", _apiKey), ("Model", _model)
        };
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].box.Height = 28; rows[i].box.Margin = new Thickness(4);
            var lb = new TextBlock { Text = rows[i].label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(lb, i); Grid.SetColumn(lb, 0);
            Grid.SetRow(rows[i].box, i); Grid.SetColumn(rows[i].box, 1);
            g.Children.Add(lb); g.Children.Add(rows[i].box);
        }
        var save = new Button { Content = "保存", Width = 90, Height = 28, Margin = new Thickness(4), HorizontalAlignment = HorizontalAlignment.Left };
        save.Click += (s, e) =>
        {
            extra.AI_ApiBaseUrl = _baseUrl.Text.Trim();
            extra.AI_ApiKey = _apiKey.Text.Trim();
            extra.AI_Model = _model.Text.Trim();
            LocalSettings.Settings.Save();
            if (_statusText != null) _statusText.Text = "已保存: " + extra.AI_ApiBaseUrl + " / " + extra.AI_Model;
            AppendBubble("已保存 AI API 配置。", isUser: false);
            win.Close();
        };
        Grid.SetRow(save, 3); Grid.SetColumn(save, 1);
        g.Children.Add(save);
        win.Content = g;
        win.ShowDialog();
    }

    public LocalAIChatPanel()
    {''')

# show current config in status at startup
c = c.replace('''        AppendBubble("你好!我是 Local Tasks 内置助手。请在上方配置 OpenAI 兼容 API(Base URL 如 https://api.openai.com/v1),然后即可对话并让我帮你添加/查询任务。", false);''',
'''        if (_statusText != null)
        {
            _statusText.Text = string.IsNullOrEmpty(_baseUrl.Text) ? "未配置 API" : _baseUrl.Text + " / " + _model.Text;
        }
        AppendBubble("你好!我是 Local Tasks 内置助手。点击左上角「AI API 设置」配置 OpenAI 兼容 API,然后即可对话并让我帮你添加/查询任务。", false);''')

with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('config dialog done')
