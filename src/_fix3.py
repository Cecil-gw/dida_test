import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.Tag\TagSerializer.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
old = '''\tpublic static List<string> NormalizeNames(IEnumerable<string> names)
\t{
\t\treturn (from name in names?.Select(NormalizeName)'''
new = '''\tpublic static List<string> NormalizeNames(IEnumerable<string> names)
\t{
\t\tif (names == null)
\t\t{
\t\t\treturn new List<string>();
\t\t}
\t\treturn (from name in names.Select(NormalizeName)'''
assert old in c
c = c.replace(old, new, 1)
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('TagSerializer guarded')
