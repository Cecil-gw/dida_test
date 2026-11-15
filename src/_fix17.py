import io, re
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
c = re.sub(r'\n\s*System\.IO\.File\.AppendAllText\(@?"?C:[^"]*ctor_trace\.log",\s*"STEP[^"]*"\);', '', c)
# also remove the CTOR catch wrapper? keep ctor try/catch but simplify to direct construct
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('trace cleaned; remaining ctor_trace refs:', c.count('ctor_trace'))
