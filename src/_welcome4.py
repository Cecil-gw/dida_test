import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
orphan = '''        {
            Content = _chat,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = Brushes.Transparent
        };
'''
assert orphan in c
c = c.replace(orphan, '', 1)
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('orphan removed')
