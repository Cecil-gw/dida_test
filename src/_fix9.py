import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()

# MakeToolButton: parameterized circle radius
old = '''    private static Button MakeToolButton(string text)
    {'''
new = '''    private static Button MakeToolButton(string text)
    {
        return MakeToolButton(text, 14);
    }

    private static Button MakeToolButton(string text, double radius)
    {'''
assert old in c
c = c.replace(old, new, 1)
c = c.replace('border.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));',
              'border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));')

# icons: circular
old_icons = '''        var injectBtn = MakeToolButton("📋");
        injectBtn.ToolTip = new ToolTip { Content = "把当前待办任务注入对话上下文" };
        injectBtn.Click += (s, e) => { _ = InjectTodosAsync(); };
        var promptBtn = MakeToolButton("🧩");
        promptBtn.ToolTip = new ToolTip { Content = "切换系统提示词" };
        promptBtn.Click += (s, e) => ShowPromptMenu(promptBtn);
        iconCol.Children.Add(injectBtn);
        iconCol.Children.Add(promptBtn);
        _input.SizeChanged += (s, e) =>
        {
            injectBtn.Height = _input.ActualHeight;
            promptBtn.Height = _input.ActualHeight;
        };'''
new_icons = '''        var injectBtn = MakeToolButton("📋", 999);
        injectBtn.Padding = new Thickness(6);
        injectBtn.ToolTip = new ToolTip { Content = "把当前待办任务注入对话上下文" };
        injectBtn.Click += (s, e) => { _ = InjectTodosAsync(); };
        var promptBtn = MakeToolButton("🧩", 999);
        promptBtn.Padding = new Thickness(6);
        promptBtn.ToolTip = new ToolTip { Content = "切换系统提示词" };
        promptBtn.Click += (s, e) => ShowPromptMenu(promptBtn);
        iconCol.Children.Add(injectBtn);
        iconCol.Children.Add(promptBtn);
        _input.SizeChanged += (s, e) =>
        {
            injectBtn.Height = _input.ActualHeight;
            injectBtn.Width = _input.ActualHeight;
            promptBtn.Height = _input.ActualHeight;
            promptBtn.Width = _input.ActualHeight;
        };'''
assert old_icons in c
c = c.replace(old_icons, new_icons, 1)

# bottom margin 15% both sides, keep top/bottom fixed
old_bottom = '''        var bottom = new Grid { Margin = new Thickness(10, 6, 10, 10) };'''
new_bottom = '''        var bottom = new Grid { Margin = new Thickness(0, 6, 0, 10) };
        SizeChanged += (s, e) =>
        {
            var m = ActualWidth * 0.15;
            bottom.Margin = new Thickness(m, 6, m, 10);
        };'''
assert old_bottom in c
c = c.replace(old_bottom, new_bottom, 1)

with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('FIX9 DONE')
