import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
c = c.replace('_welcomeRoot = BuildWelcome();', '_welcomeRoot = (Grid)BuildWelcome();')
c = c.replace('        var star = new Path', '        var star = new System.Windows.Shapes.Path')
c = c.replace('Stretch = Stretch.Fill,', 'Stretch = System.Windows.Media.Stretch.Fill,')
c = c.replace('Geometry.Parse(', 'System.Windows.Media.Geometry.Parse(')
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('fixed')
