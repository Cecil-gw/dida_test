import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()

# 1. drawer: hide/show instead of clear+rebuild (fixes "logical child" crash)
old = '''    private void ToggleDrawer()
    {
        if (_drawerGrid.Children.Count > 0) { CloseDrawer(); return; }
        BuildDrawer();
    }

    private void CloseDrawer()
    {
        if (_dirty && MessageBox.Show("有未保存的修改,确定丢弃?", "AI 服务商", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
        {
            return;
        }
        _dirty = false;
        _drawerGrid.Children.Clear();
    }'''
new = '''    private void ToggleDrawer()
    {
        if (_drawerGrid.Children.Count > 0)
        {
            CloseDrawer();
            _drawerGrid.Visibility = Visibility.Collapsed;
            return;
        }
        BuildDrawer();
        _drawerGrid.Visibility = Visibility.Visible;
    }

    private void CloseDrawer()
    {
        if (_dirty && MessageBox.Show("有未保存的修改,确定丢弃?", "AI 服务商", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
        {
            return;
        }
        _dirty = false;
        _drawerGrid.Visibility = Visibility.Collapsed;
    }'''
assert old in c
c = c.replace(old, new, 1)

# 2. BuildDrawer: skip rebuild if already built
c = c.replace('''    private void BuildDrawer()
    {
        var overlay''',
'''    private void BuildDrawer()
    {
        if (_drawerGrid.Children.Count > 0) return;
        var overlay''')

# 3. inner window 30% white explicit + margins 10% sides / 5% top; bottom bar same width
c = c.replace('Background = new SolidColorBrush(Color.FromArgb(0x4D, 0xFF, 0xFF, 0xFF)),',
              'Background = new SolidColorBrush(Color.FromArgb(0x4D, 0xFF, 0xFF, 0xFF)),')
c = c.replace('''            double mx = ActualWidth * 0.20;
            double mt = ActualHeight * 0.10;
            _innerWindow.Margin = new Thickness(mx, mt, mx, 4);
            bottom.Margin = new Thickness(ActualWidth * 0.15, 6, ActualWidth * 0.15, 10);''',
'''            double mx = ActualWidth * 0.10;
            double mt = ActualHeight * 0.05;
            _innerWindow.Margin = new Thickness(mx, mt, mx, 4);
            bottom.Margin = new Thickness(mx, 6, mx, 10);''')

# 4. provider button: full text padding
c = c.replace('''        var providersBtn = MakeToolButton("服务商");
        providersBtn.Click += (s, e) => ToggleDrawer();''',
'''        var providersBtn = MakeToolButton("服务商");
        providersBtn.Padding = new Thickness(18, 6, 18, 6);
        providersBtn.MinWidth = 76;
        providersBtn.Click += (s, e) => ToggleDrawer();''')

with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('FIX13 DONE')
