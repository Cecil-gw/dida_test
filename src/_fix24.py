import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
old = '''    private void ToggleDrawer()
    {
        if (_drawerGrid.Children.Count > 0)
        {
            CloseDrawer();
            _drawerGrid.Visibility = Visibility.Collapsed;
            return;
        }
        BuildDrawer();
        _drawerGrid.Visibility = Visibility.Visible;
    }'''
new = '''    private void ToggleDrawer()
    {
        if (_drawerGrid.Children.Count == 0)
        {
            BuildDrawer();
        }
        if (_drawerGrid.Visibility == Visibility.Visible)
        {
            CloseDrawer();
            _drawerGrid.Visibility = Visibility.Collapsed;
        }
        else
        {
            _drawerGrid.Visibility = Visibility.Visible;
        }
    }'''
assert old in c
c = c.replace(old, new, 1)
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('drawer toggle fixed')

# avatar notch cover
p2 = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.TabBar\LeftMenuBar.cs'
c2 = io.open(p2, encoding='utf-8').read()
if '_notchCover' not in c2:
    c2 = c2.replace('\tinternal Image HeadProImage;',
'''\tinternal Image HeadProImage;
\tprivate System.Windows.Shapes.Ellipse _notchCover;''')
    old_crown = '''\tprivate void SetProCrown()
\t{
\t\tHeadProImage.Visibility = Visibility.Hidden;
\t\tHeadFreeImage.Visibility = Visibility.Visible;
\t}'''
    new_crown = '''\tprivate void SetProCrown()
\t{
\t\tHeadProImage.Visibility = Visibility.Hidden;
\t\tHeadFreeImage.Visibility = Visibility.Collapsed;
\t\tEnsureNotchCover();
\t}

\tprivate void EnsureNotchCover()
\t{
\t\tif (_notchCover != null) return;
\t\tif (!(HeadProImage.Parent is Panel parent)) return;
\t\t_notchCover = new System.Windows.Shapes.Ellipse
\t\t{
\t\t\tFill = (System.Windows.Media.Brush)TryFindResource("LeftBarBackground")
\t\t\t   ?? (System.Windows.Media.Brush)TryFindResource("PopupBackground")
\t\t\t   ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF7, 0xF8, 0xFA))
\t\t};
\t\ttry
\t\t{
\t\t\tSystem.Windows.Controls.Grid.SetRow(_notchCover, System.Windows.Controls.Grid.GetRow(HeadProImage));
\t\t\tSystem.Windows.Controls.Grid.SetColumn(_notchCover, System.Windows.Controls.Grid.GetColumn(HeadProImage));
\t\t}
\t\tcatch { }
\t\t_notchCover.HorizontalAlignment = HeadProImage.HorizontalAlignment;
\t\t_notchCover.VerticalAlignment = HeadProImage.VerticalAlignment;
\t\t_notchCover.Margin = HeadProImage.Margin;
\t\tparent.Children.Insert(0, _notchCover);
\t}'''
    assert old_crown in c2
    c2 = c2.replace(old_crown, new_crown, 1)
with io.open(p2, 'w', encoding='utf-8') as f: f.write(c2)
print('notch cover added')
