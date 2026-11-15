import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views\MainWindow.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()

old = '''\tprivate async void HeaderGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
\t{
\t\t_mouseDown = true;
\t\tGetVirtualData();
\t\tSystem.Windows.Point position = Mouse.GetPosition((UIElement)sender);
\t\tif ((DateTime.Now - _headerClickOldTime).TotalMilliseconds <= 300.0)
\t\t{
\t\t\tif (base.ResizeMode != ResizeMode.NoResize)
\t\t\t{
\t\t\t\tToggleWindowState();
\t\t\t}
\t\t}
\t\telse
\t\t{
\t\t\tif (base.WindowState == WindowState.Maximized)
\t\t\t{
\t\t\t\t_dragStartPoint = position;
\t\t\t}
\t\t\tif (e.LeftButton == MouseButtonState.Pressed)
\t\t\t{
\t\t\t\tDragMove();
\t\t\t\te.Handled = false;
\t\t\t}
\t\t}
\t\t_headerClickOldTime = DateTime.Now;
\t}'''
new = '''\tprivate async void HeaderGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
\t{
\t\tvar source = e.OriginalSource as System.Windows.DependencyObject;
\t\twhile (source != null)
\t\t{
\t\t\tif (source is System.Windows.Controls.Button)
\t\t\t{
\t\t\t\treturn;
\t\t\t}
\t\t\tsource = System.Windows.Media.VisualTreeHelper.GetParent(source);
\t\t}
\t\t_mouseDown = true;
\t\tGetVirtualData();
\t\tSystem.Windows.Point position = Mouse.GetPosition((UIElement)sender);
\t\tif (base.WindowState == WindowState.Maximized)
\t\t{
\t\t\t_dragStartPoint = position;
\t\t}
\t\tif (e.LeftButton == MouseButtonState.Pressed)
\t\t{
\t\t\tDragMove();
\t\t\te.Handled = false;
\t\t}
\t\t_headerClickOldTime = DateTime.Now;
\t}'''
assert old in c
c = c.replace(old, new, 1)
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('header handler patched: dblclick disabled + buttons exempt')
