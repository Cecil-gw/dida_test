import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
dup = '''        _scroll.Resources = new ResourceDictionary();
        _scroll.Resources[typeof(ScrollBar)] = MakeSlimScrollBarStyle();
        _innerWindow = new Border
        {
            Child = _scroll,
            Background = new SolidColorBrush(Color.FromArgb(0x4D, 0xFF, 0xFF, 0xFF)),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10, 8, 6, 8)
        };
        _scroll = new ScrollViewer'''
assert dup in c
c = c.replace(dup, '        _scroll = new ScrollViewer', 1)
# also remove step trace logs (temporary)
c = re.sub(r'\n *System\.IO\.File\.AppendAllText\(@"C:\\\\114514\\\\dida\\\\ticktick-wpf-custom\\\\docs\\\\ctor_trace\.log", "STEP[^\r\n]*\);', '', c)
import re as _re
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('dup removed')
