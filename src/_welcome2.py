import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()

# ShowIn: load conversations & show welcome when empty
c = c.replace('''    public static void ShowIn(Border host)
    {
        AIProviderStore.Load();
        if (_instance == null) _instance = new LocalAIChatPanel();
        host.Child = _instance;
    }''',
'''    public static void ShowIn(Border host)
    {
        AIProviderStore.Load();
        AIConversationStore.Load();
        if (_instance == null) _instance = new LocalAIChatPanel();
        host.Child = _instance;
        _instance.RefreshHistory();
        if (_instance._history.Count == 0) _instance.ShowWelcome();
        _instance.RefreshSendState();
    }''')

# end of ChatRoundAsync: sync to store (insert after finally block closing of ChatRoundAsync)
c = c.replace('''        catch (Exception ex)
        {
            RemoveBubble(loading);
            AppendBubble("调用失败: " + ex.Message, false);
        }
    }

    private async Task<string> CallApiAsync''',
'''        catch (Exception ex)
        {
            RemoveBubble(loading);
            AppendBubble("调用失败: " + ex.Message, false);
        }
        AIConversationStore.SyncFromHistory(_history);
        RefreshHistory();
    }

    private async Task<string> CallApiAsync''')

# methods appended before final class close
tail = '''
    // ================= welcome & history =================
    private UIElement BuildHistoryPanel()
    {
        var panel = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 0, 8, 0) };
        var top = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        var newBtn = new Button { Content = "＋ 新会话", Padding = new Thickness(10, 3, 10, 3) };
        newBtn.Click += (s, e) => NewSession();
        top.Children.Add(newBtn);
        DockPanel.SetDock(top, Dock.Top);
        panel.Children.Add(top);
        _historyScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _historyList = new StackPanel();
        _historyScroll.Content = _historyList;
        panel.Children.Add(_historyScroll);
        return panel;
    }

    private void RefreshHistory()
    {
        if (_historyList == null) return;
        _historyList.Children.Clear();
        foreach (var conv in AIConversationStore.Conversations.OrderByDescending(x => x.created_at))
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 1, 0, 1),
                Background = conv.id == AIConversationStore.ActiveId
                    ? new SolidColorBrush(Color.FromArgb(0x33, 0x4F, 0x7C, 0xFF))
                    : Brushes.Transparent,
                Cursor = Cursors.Hand
            };
            var tb = new TextBlock { Text = conv.title, TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 12 };
            border.Child = tb;
            var convId = conv.id;
            border.MouseLeftButtonDown += (s, e) =>
            {
                LoadConversation(convId);
            };
            _historyList.Children.Add(border);
        }
    }

    private void LoadConversation(string id)
    {
        var conv = AIConversationStore.Conversations.FirstOrDefault(x => x.id == id);
        if (conv == null) return;
        AIConversationStore.ActiveId = id;
        _history.Clear();
        foreach (var m in conv.messages) _history.Add((m.role, m.content));
        _chat.Children.Clear();
        foreach (var m in conv.messages.Where(x => x.role != "system"))
        {
            AppendBubble(m.content, m.role == "user");
        }
        SwitchToChat();
        RefreshHistory();
    }

    private void NewSession()
    {
        _history.Clear();
        _chat.Children.Clear();
        AIConversationStore.ActiveId = null;
        ShowWelcome();
        RefreshHistory();
        RefreshSendState();
    }

    private void ShowWelcome()
    {
        _inConversation = false;
        if (_welcomeRoot != null) _welcomeRoot.Visibility = Visibility.Visible;
        if (_scroll != null) _scroll.Visibility = Visibility.Collapsed;
        if (_innerWindow != null) _innerWindow.Background = Brushes.Transparent;
        RefreshSendState();
    }

    private void SwitchToChat()
    {
        _inConversation = true;
        if (_welcomeRoot != null) _welcomeRoot.Visibility = Visibility.Collapsed;
        if (_scroll != null) _scroll.Visibility = Visibility.Visible;
        if (_innerWindow != null) _innerWindow.Background = new SolidColorBrush(Color.FromArgb(0x4D, 0xFF, 0xFF, 0xFF));
        RefreshSendState();
    }

    private UIElement BuildWelcome()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var top = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 30)
        };
        var hour = DateTime.Now.Hour;
        string greet = hour < 12 ? "上午好" : (hour < 18 ? "下午好" : "晚上好");
        var userName = LocalSettings.Settings.LoginUserName;
        if (string.IsNullOrWhiteSpace(userName)) userName = "Sikoll";

        var star = new Path
        {
            Data = Geometry.Parse("M 50,0 C 55,35 65,45 100,50 C 65,55 55,65 50,100 C 45,65 35,55 0,50 C 35,45 45,35 50,0 Z"),
            Width = 56,
            Height = 56,
            Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 14)
        };
        var grad = new LinearGradientBrush();
        grad.StartPoint = new Point(0, 0);
        grad.EndPoint = new Point(1, 1);
        grad.GradientStops.Add(new GradientStop(Color.FromRgb(0x63, 0x8B, 0xFF), 0));
        grad.GradientStops.Add(new GradientStop(Color.FromRgb(0x3B, 0x6E, 0xF5), 1));
        star.Fill = grad;
        top.Children.Add(star);

        top.Children.Add(new TextBlock
        {
            Text = greet + "," + userName,
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        Grid.SetRow(top, 0);
        root.Children.Add(top);

        var quicks = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var options = new (string icon, string text)[]
        {
            ("🔍", "今天有哪些任务"),
            ("📄", "总结一下今天"),
            ("⏰", "规划今日安排")
        };
        foreach (var opt in options)
        {
            var card = new Border
            {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(0, 5, 0, 5),
                MinWidth = 280,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                Cursor = Cursors.Hand
            };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new TextBlock { Text = opt.icon, Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(new TextBlock { Text = opt.text, VerticalAlignment = VerticalAlignment.Center });
            card.Child = row;
            var txt = opt.text;
            card.MouseLeftButtonDown += (s, e) => SendText(txt);
            quicks.Children.Add(card);
        }
        Grid.SetRow(quicks, 1);
        root.Children.Add(quicks);
        return root;
    }

    private async void SendText(string text)
    {
        if (_loading) return;
        SwitchToChat();
        AppendBubble(text, true);
        _history.Add(("user", text));
        await ChatRoundAsync();
        AIConversationStore.SyncFromHistory(_history);
        RefreshHistory();
    }
}'''
# replace the final two braces (end of class + nothing) by tail
idx = c.rfind('\n}')
c = c[:idx] + '\n' + tail + '\n'
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('welcome methods added')
