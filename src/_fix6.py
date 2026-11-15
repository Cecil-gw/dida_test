import io, re
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
c = c.replace('p.api_key = _apiKey.Text.Trim();', 'p.api_key = _fApiKey.Text.Trim();')
c = c.replace('TryExtractTool(reply2, out _, out var v2);', '_ = TryExtractTool(reply2, out var v2);')
c = c.replace('content.SetValue(ContentPresenter.VerticalContentAlignmentProperty, VerticalAlignment.Center);',
              'content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);')
c = c.replace('System.Windows.Controls.Primitives.PlacementPlacement.Bottom', 'System.Windows.Controls.Primitives.Placement.Bottom')
# replace whole slim scrollbar template with simple opacity style
start = c.index('var tpl = new ControlTemplate(typeof(ScrollBar));')
end = c.index('st.Setters.Add(new Setter(ScrollBar.TemplateProperty, tpl));', start)
end = c.index('\n', end) + 1
simple = '        // minimal scrollbar: keep default template, just slim + fade\n'
c = c[:start] + simple + c[end:]
# also remove hover block referencing removed vars? keep hover (works without template)
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('fixed')
