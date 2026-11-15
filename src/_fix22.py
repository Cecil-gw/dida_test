import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.MarkDown\MarkDownEditor.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
old = '''\t\tint num = (from block in (_seedSemanticDocumentForRefresh?.Blocks)?.Where((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex)
\t\t\tselect Math.Max(0, block.Indent)).DefaultIfEmpty(-1).Max();'''
new = '''\t\tint num = -1;
\t\tif (_seedSemanticDocumentForRefresh?.Blocks != null)
\t\t{
\t\t\tnum = (from block in _seedSemanticDocumentForRefresh.Blocks.Where((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex)
\t\t\t\tselect Math.Max(0, block.Indent)).DefaultIfEmpty(-1).Max();
\t\t}'''
assert old in c
c = c.replace(old, new, 1)
old2 = '''\t\tint num2 = (from block in _semanticDocument?.Blocks?.Where((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex)
\t\t\tselect Math.Max(0, block.Indent)).DefaultIfEmpty(-1).Max();'''
new2 = '''\t\tint num2 = -1;
\t\tif (_semanticDocument?.Blocks != null)
\t\t{
\t\t\tnum2 = (from block in _semanticDocument.Blocks.Where((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex)
\t\t\t\tselect Math.Max(0, block.Indent)).DefaultIfEmpty(-1).Max();
\t\t}'''
assert old2 in c
c = c.replace(old2, new2, 1)
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('markdown null-safe done')
