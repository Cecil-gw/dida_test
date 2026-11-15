import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
c = io.open(p, encoding='utf-8').read()

# 1. restore header hit-test (remove AI-mode disable / restore)
c = c.replace('''\t\tSetViewVisible(AIBorder);
\t\tif (_headerDragBorder != null)
\t\t{
\t\t\t_headerDragBorder.IsHitTestVisible = false;
\t\t}
\t\tSystem.Windows.Controls.Panel.SetZIndex(AIBorder, 10);''',
'''\t\tSetViewVisible(AIBorder);''')
c = c.replace('''\t\tPanel.SetZIndex(AIBorder, (border == AIBorder) ? 10 : 0);
\t\tif (_headerDragBorder != null && border != AIBorder)
\t\t{
\t\t\t_headerDragBorder.IsHitTestVisible = true;
\t\t}''',
'''\t\tPanel.SetZIndex(AIBorder, (border == AIBorder) ? 10 : 0);''')

# 2. providers button: move from top bar into bottom icon toolbar (first position)
c = c.replace('''        var providersBtn = MakeToolButton("服务商");
        providersBtn.Padding = new Thickness(18, 6, 18, 6);
        providersBtn.MinWidth = 76;
        providersBtn.Opacity = 0.15;
        providersBtn.Click += (s, e) => ToggleDrawer();
        DockPanel.SetDock(_statusText, Dock.Right);
        top.Children.Add(providersBtn);
        top.Children.Add(_statusText);''',
'''        DockPanel.SetDock(_statusText, Dock.Right);
        top.Children.Add(_statusText);''')
c = c.replace('''        var iconCol = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };
        var injectBtn = MakeToolButton("📋", 999);''',
'''        var iconCol = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };
        var providersBtn = MakeToolButton("服务商", 16);
        providersBtn.Padding = new Thickness(16, 7, 16, 7);
        providersBtn.ToolTip = new ToolTip { Content = "服务商设置" };
        providersBtn.Click += (s, e) => ToggleDrawer();
        iconCol.Children.Add(providersBtn);
        var injectBtn = MakeToolButton("📋", 999);''')

# 3. input transparent so border corners show
c = c.replace('        Background = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),\n        BorderThickness = new Thickness(0)',
              '        Background = Brushes.Transparent,\n        BorderThickness = new Thickness(0)')
c = c.replace('''        var inputBorder = new Border
        {
            Child = _input,
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF))
        };''',
'''        var inputBorder = new Border
        {
            Child = _input,
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF))
        };''')

# remove now-unused top preview dblclick? harmless keep.
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('panel patched')

# 4. LeftMenuBar: avatar corner - always show free badge, hide crown (both methods)
p2 = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.TabBar\LeftMenuBar.cs'
c2 = io.open(p2, encoding='utf-8').read()
c2 = c2.replace('''\t\tHeadProImage.Visibility = Visibility.Hidden;
\t\tHeadFreeImage.Visibility = Visibility.Collapsed;''',
'''\t\tHeadProImage.Visibility = Visibility.Hidden;
\t\tHeadFreeImage.Visibility = Visibility.Visible;''')
with io.open(p2, 'w', encoding='utf-8') as f: f.write(c2)
print('LeftMenuBar avatar patched')
