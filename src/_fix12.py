import io

# --- MainWindow: capture header drag border + hit-test off in AI mode ---
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views\MainWindow.cs'
c = io.open(p, encoding='utf-8').read()

if '_headerDragBorder' not in c:
    c = c.replace('\tinternal Border AIBorder;',
                  '\tinternal Border AIBorder;\n\tprivate System.Windows.Controls.Border _headerDragBorder;')
    c = c.replace('''\t\tcase 32:
\t\t\t((Border)target).MouseLeftButtonDown += HeaderGrid_MouseLeftButtonDown;''',
'''\t\tcase 32:
\t\t\t_headerDragBorder = (Border)target;
\t\t\t((Border)target).MouseLeftButtonDown += HeaderGrid_MouseLeftButtonDown;''')
    c = c.replace('''\tprivate async void SwitchAI()
\t{
\t\tSetViewVisible(AIBorder);
\t\tSystem.Windows.Controls.Panel.SetZIndex(AIBorder, 10);''',
'''\tprivate async void SwitchAI()
\t{
\t\tSetViewVisible(AIBorder);
\t\tif (_headerDragBorder != null)
\t\t{
\t\t\t_headerDragBorder.IsHitTestVisible = false;
\t\t}
\t\tSystem.Windows.Controls.Panel.SetZIndex(AIBorder, 10);''')
    c = c.replace('''\t\tPanel.SetZIndex(AIBorder, (border == AIBorder) ? 10 : 0);''',
'''\t\tPanel.SetZIndex(AIBorder, (border == AIBorder) ? 10 : 0);
\t\tif (_headerDragBorder != null && border != AIBorder)
\t\t{
\t\t\t_headerDragBorder.IsHitTestVisible = true;
\t\t}''')
io.open(p, 'w', encoding='utf-8').write(c)
print('MainWindow header fix done')

# --- LocalAIChatPanel: margins 20% sides / 10% top ---
p2 = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
c2 = io.open(p2, encoding='utf-8').read()
c2 = c2.replace('private readonly Grid _drawerGrid = new Grid();',
                'private readonly Grid _drawerGrid = new Grid();\n    private Border _innerWindow;')
c2 = c2.replace('''        var innerWindow = new Border
        {
            Child = _scroll,
            Background = new SolidColorBrush(Color.FromArgb(0x4D, 0xFF, 0xFF, 0xFF)),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10, 8, 6, 8),
            Margin = new Thickness(10, 0, 10, 4)
        };''',
'''        _innerWindow = new Border
        {
            Child = _scroll,
            Background = new SolidColorBrush(Color.FromArgb(0x4D, 0xFF, 0xFF, 0xFF)),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10, 8, 6, 8)
        };''')
c2 = c2.replace('''        root.PreviewMouseLeftButtonDown += (s, e) => { if (e.ClickCount >= 2) e.Handled = true; };
        Content = root;''',
'''        root.PreviewMouseLeftButtonDown += (s, e) => { if (e.ClickCount >= 2) e.Handled = true; };
        SizeChanged += (s, e) =>
        {
            double mx = ActualWidth * 0.20;
            double mt = ActualHeight * 0.10;
            _innerWindow.Margin = new Thickness(mx, mt, mx, 4);
            bottom.Margin = new Thickness(ActualWidth * 0.15, 6, ActualWidth * 0.15, 10);
        };
        Content = root;''')
io.open(p2, 'w', encoding='utf-8').write(c2)
print('panel margins done')
