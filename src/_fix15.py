import io, re
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
LOG = 'System.IO.File.AppendAllText(@"C:\\\\114514\\\\dida\\\\ticktick-wpf-custom\\\\docs\\\\ctor_trace.log", "STEP ' + "'" + '" + "X' + '"' + ");"
anchors = [
    ('        var root = new Grid();', 'A_root'),
    ('        var chatHost = new Grid();', 'B_chatHost'),
    ('        var mainGrid = new Grid();', 'C_mainGrid'),
    ('        var historyPanel = BuildHistoryPanel();', 'D_history'),
    ('        _welcomeRoot = (Grid)BuildWelcome();', 'E_welcome'),
    ('        var bottom = new Grid ', 'F_bottom'),
    ('        Content = root;', 'G_content'),
    ('        RefreshSendState();', 'H_send'),
    ('        RefreshStatus(null);', 'I_status'),
    ('        AppendBubble(', 'J_bubble'),
]
lines = c.split('\n')
out = []
step = 0
for ln in lines:
    matched = False
    for a, name in anchors:
        if ln.startswith(a) and step < 99:
            step += 1
            tag = 'STEP' + name
            out.append('        System.IO.File.AppendAllText(@"C:\\\\114514\\\\dida\\\\ticktick-wpf-custom\\\\docs\\\\ctor_trace.log", "' + tag + '\\r\\n");')
            break
    out.append(ln)
c = '\n'.join(out)
# fix: remove duplicated anchor line artifacts (we added log BEFORE anchor, fine)
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('trace steps inserted:', step)
