import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
LOG = 'System.IO.File.AppendAllText(@"C:\\\\114514\\\\dida\\\\ticktick-wpf-custom\\\\docs\\\\drag_trace.log", "TOPDRAG src=" + (e.OriginalSource?.GetType().Name ?? "null") + "\\r\\n");'
old = '''        top.MouseLeftButtonDown += (s, e) =>
        {'''
new = '''        top.MouseLeftButtonDown += (s, e) =>
        {
            System.IO.File.AppendAllText(@"C:\\\\114514\\\\dida\\\\ticktick-wpf-custom\\\\docs\\\\drag_trace.log", "TOPDRAG src=" + (e.OriginalSource?.GetType().Name ?? "null") + "\\r\\n");'''
assert old in c
c = c.replace(old, new, 1)
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('drag trace added')
