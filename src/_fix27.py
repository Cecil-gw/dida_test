import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.TabBar\LeftMenuBar.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
old = '''\tprivate void EnsureNotchCover()
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
\t}
'''
assert old in c
c = c.replace(old, '', 1)
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('removed')
