import io
p2 = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.TabBar\LeftMenuBar.cs'
with io.open(p2, encoding='utf-8') as f: c2 = f.read()

# remove notch cover entirely (method + call + field)
c2 = c2.replace('\tprivate System.Windows.Shapes.Ellipse _notchCover;\n', '')
c2 = c2.replace('\t\tEnsureNotchCover();\n', '')
import re
c2 = re.sub(r'\n\tpublic void EnsureNotchCover\(\)\s*\{.*?\n\t\}\n', '\n', c2, count=1, flags=re.S)

# SetProCrown: crown hidden, full rounded-square avatar visible, circle path hidden
c2 = c2.replace('''\tprivate void SetProCrown()
\t{
\t\tHeadProImage.Visibility = Visibility.Hidden;
\t\tHeadFreeImage.Visibility = Visibility.Collapsed;
\t}''',
'''\tprivate void SetProCrown()
\t{
\t\tHeadProImage.Visibility = Visibility.Hidden;
\t\tHeadFreeImage.Visibility = Visibility.Visible;
\t\tAvatar.Visibility = Visibility.Collapsed;
\t}''')
c2 = c2.replace('''\t\tHeadProImage.Visibility = Visibility.Hidden;
\t\tHeadFreeImage.Visibility = Visibility.Visible;
\t}''',
'''\t\tHeadProImage.Visibility = Visibility.Hidden;
\t\tHeadFreeImage.Visibility = Visibility.Visible;
\t\tAvatar.Visibility = Visibility.Collapsed;
\t}''')
with io.open(p2, 'w', encoding='utf-8') as f: f.write(c2)
print('avatar: full rounded-square, notch cover removed')

# AI top bar: drag support
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
c = c.replace('''        top.PreviewMouseLeftButtonDown += (s, e) => { if (e.ClickCount >= 2) e.Handled = true; };''',
'''        top.PreviewMouseLeftButtonDown += (s, e) => { if (e.ClickCount >= 2) e.Handled = true; };
        top.MouseLeftButtonDown += (s, e) =>
        {
            if (e.OriginalSource is System.Windows.Controls.TextBlock || e.OriginalSource is System.Windows.Controls.Button) return;
            if (Window.GetWindow(this) is Window w)
            {
                try { w.DragMove(); } catch { }
                e.Handled = true;
            }
        };''')
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('AI topbar drag added')
