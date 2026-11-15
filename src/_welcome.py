import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()

# fields
c = c.replace('    private Border _innerWindow;',
'''    private Border _innerWindow;
    private Grid _welcomeRoot;
    private ScrollViewer _historyScroll;
    private StackPanel _historyList;
    private bool _inConversation;
    private Grid _mainGrid;''')

# build main layout: row1 becomes col-grid (history + content), wrap in _mainGrid
old = '''        var chatHost = new Grid();
        _scroll = new ScrollViewer'''
new = '''        var chatHost = new Grid();
        var mainGrid = new Grid();
        _mainGrid = mainGrid;
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var historyPanel = BuildHistoryPanel();
        Grid.SetColumn(historyPanel, 0);
        mainGrid.Children.Add(historyPanel);
        var contentHost = new Grid();
        Grid.SetColumn(contentHost, 1);
        mainGrid.Children.Add(contentHost);
        c = None  # placeholder
'''
c = c.replace(old, new, 1)
# undo the accidental python-only line (it stayed inside the C# string), fix:
c = c.replace('        c = None  # placeholder\n', '')

# now inject welcome root + scroll into contentHost instead of chatHost
c = c.replace('''        var innerWindow = new Border''', '''        var innerWindow = new Border''')
old_host = '''        chatHost.Children.Add(_innerWindow);
        chatHost.Children.Add(_drawerGrid);
        root.Children.Add(chatHost);
        Grid.SetRow(chatHost, 1);'''
new_host = '''        _scroll = new ScrollViewer
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
            Padding = new Thickness(10, 8, 6, 8)
        };
        _innerWindow = innerWindow;
        _welcomeRoot = BuildWelcome();
        contentHost.Children.Add(_scroll);
        contentHost.Children.Add(_welcomeRoot);
        chatHost.Children.Add(innerWindow);
        chatHost.Children.Add(_drawerGrid);
        root.Children.Add(mainGrid);
        Grid.SetRow(mainGrid, 1);'''
assert old_host in c
c = c.replace(old_host, new_host, 1)

# remove the older _scroll/_innerWindow construction block (duplicate inside contentHost region)
dup = '''        var chatHost = new Grid();
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
'''
if dup in c:
    c = c.replace(dup, '        var chatHost = new Grid();\n', 1)

# disclaimer row + rows adjust
c = c.replace('''        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });''',
'''        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });''')
c = c.replace('''        root.Children.Add(bottom);
        Grid.SetRow(bottom, 2);

        root.PreviewMouseLeftButtonDown''',
'''        root.Children.Add(bottom);
        Grid.SetRow(bottom, 2);
        var disclaimer = new TextBlock
        {
            Text = "内容由 AI 生成,请核查重要信息。",
            FontSize = 11,
            Opacity = 0.55,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        };
        root.Children.Add(disclaimer);
        Grid.SetRow(disclaimer, 3);

        root.PreviewMouseLeftButtonDown''')

with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('layout patched')
