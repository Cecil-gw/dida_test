import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
lines = io.open(p, encoding='utf-8').read().split('\n')
# remove 0-based indexes 124..128 (providers block) and 132 (top.Children.Add(providersBtn))
for idx in sorted([128,127,126,125,124,132], reverse=True):
    del lines[idx]
io.open(p, 'w', encoding='utf-8').write('\n'.join(lines))
print('removed')
