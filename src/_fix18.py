import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
start = c.index('        var chatHost = new Grid();')
end = c.index('        var bottom = new Grid { Margin = new Thickness(0, 6, 0, 10) };')
clean = '''        var mainGrid = new Grid();
        _mainGrid = mainGrid;
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var historyPanel = BuildHistoryPanel();
        Grid.SetColumn(historyPanel, 0);
        mainGrid.Children.Add(historyPanel);
        var contentHost = new Grid();
        Grid.SetColumn(contentHost, 1);
        mainGrid.Children.Add(contentHost);

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
            Padding = new Thickness(10, 8, 6, 8)
        };
        _innerWindow = innerWindow;
        _welcomeRoot = (Grid)BuildWelcome();
        contentHost.Children.Add(_scroll);
        contentHost.Children.Add(_welcomeRoot);

        Grid.SetColumnSpan(_drawerGrid, 2);
        Grid.SetZIndex(_drawerGrid, 5);
        mainGrid.Children.Add(_drawerGrid);

        root.Children.Add(mainGrid);
        Grid.SetRow(mainGrid, 1);

'''
c = c[:start] + clean + c[end:]
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('ctor cleaned')
