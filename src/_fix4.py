import io
root = r'C:\114514\dida\ticktick-wpf-custom'

def edit(rel, pairs):
    p = root + '\\' + rel
    with io.open(p, encoding='utf-8') as f: c = f.read()
    for a, b in pairs:
        if a not in c:
            print('MISS', rel, repr(a[:60])); raise SystemExit(1)
        c = c.replace(a, b, 1)
    with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
    print('patched', rel)

# 1. MainWindow: crown button (UpgradeButton) permanently collapsed + click no-op
edit('TickTick\\ticktick_WPF.Views\\MainWindow.cs', [
    ('\tprivate void UpgradeProClick(object sender, RoutedEventArgs e)\n\t{\n\t\tUtils.StartUpgrade("upgrade_icon");\n\t}',
     '\tprivate void UpgradeProClick(object sender, RoutedEventArgs e)\n\t{\n\t}'),
    ('\t\t\t\tUpgradeButton.Visibility = Visibility.Visible;\n\t\t\t}\n\t\t\telse\n\t\t\t{\n\t\t\t\tUpgradeButton.Visibility = Visibility.Collapsed;\n\t\t\t}\n\t\t\tif (isPro != currentIsPro)',
     '\t\t\t\tUpgradeButton.Visibility = Visibility.Collapsed;\n\t\t\t}\n\t\t\telse\n\t\t\t{\n\t\t\t\tUpgradeButton.Visibility = Visibility.Collapsed;\n\t\t\t}\n\t\t\tif (isPro != currentIsPro)')
])
# also the earlier branch (needUpgrade true path) around 4495-4503
c_path = root + r'\TickTick\ticktick_WPF.Views\MainWindow.cs'
c = io.open(c_path, encoding='utf-8').read()
c = c.replace('UpgradeButton.Visibility = Visibility.Visible;', 'UpgradeButton.Visibility = Visibility.Collapsed;')
io.open(c_path, 'w', encoding='utf-8').write(c)
print('all UpgradeButton occurrences collapsed')

# 2. LeftMenuBar: avatar fix (restore free badge) + remove CheckUpdate/OpenOnWeb
edit('TickTick\\ticktick_WPF.Views.TabBar\\LeftMenuBar.cs', [
    ('\tprivate void SetProCrown()\n\t{\n\t\tHeadProImage.Visibility = Visibility.Hidden;\n\t\tHeadFreeImage.Visibility = Visibility.Hidden;\n\t}',
     '\tprivate void SetProCrown()\n\t{\n\t\tHeadProImage.Visibility = Visibility.Hidden;\n\t\tHeadFreeImage.Visibility = Visibility.Collapsed;\n\t}'),
    ('''\t\tCustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
\t\t{
\t\t\tnew CustomMenuItemViewModel("OpenOnWeb", Utils.GetString("ViewOnWeb"), Utils.GetGeometry("Ic.Tabbar.Home")),
\t\t\tnew CustomMenuItemViewModel("CheckUpdate", Utils.GetString("PublicCheckUpdate"), Utils.GetGeometry("Ic.Tabbar.CheckUpdate"))
\t\t\t{
\t\t\t\tExtraIcon = (CanUpdate ? Utils.GetIcon("NewUpgradeIcon") : null),
\t\t\t\tExtraIconColor = ThemeUtil.GetColorInString("#FF281A"),
\t\t\t\tExtraIconAngle = 90,
\t\t\t\tExtraIconSize = 12
\t\t\t},
\t\t\tnew CustomMenuItemViewModel("Shortcut", Utils.GetString("Shortcut"), Utils.GetGeometry("Ic.Tabbar.HotKey")),''',
     '''\t\tCustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
\t\t{
\t\t\tnew CustomMenuItemViewModel("Shortcut", Utils.GetString("Shortcut"), Utils.GetGeometry("Ic.Tabbar.HotKey")),''')
])
print('LEFTBAR DONE')
