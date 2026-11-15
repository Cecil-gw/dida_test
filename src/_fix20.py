import io, re
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
c = re.sub(r'\n\s*var providersBtn = MakeToolButton\("服务商"\);\n\s*providersBtn\.Padding = new Thickness\(18, 6, 18, 6\);\n\s*providersBtn\.MinWidth = 76;\n\s*providersBtn\.Opacity = 0\.15;\n\s*providersBtn\.Click \+= \(s, e\) => ToggleDrawer\(\);\n\s*top\.Children\.Add\(providersBtn\);', '', c)
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('removed count check:', '服务商' in c.split('iconCol')[0])
