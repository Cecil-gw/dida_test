import io, re
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

# 1. Remove Account & Premium menu entries in settings
edit('TickTick\\ticktick_WPF.ViewModels\\SettingViewModel.cs', [
    ('\t\t\tnew SettingMenuItem("Account1", SettingsType.Account),\n'
     '\t\t\tnew SettingMenuItem("Premium", SettingsType.Premium)\n'
     '\t\t\t{\n'
     '\t\t\t\tShowExpired = (UserManager.InGraceDate() || UserManager.GetSubscribeErrorCode() > 0)\n'
     '\t\t\t},\n'
     '\t\t\tnew SettingMenuItem(),\n',
     '\t\t\tnew SettingMenuItem(),\n')
])

# 2. SettingDialog: redirect Account/Premium to More settings
edit('TickTick\\ticktick_WPF.Views\\SettingDialog.cs', [
    ('\tpublic static void ShowSettingDialog(SettingsType type = SettingsType.Account, Window owner = null)\n\t{\n',
     '\tpublic static void ShowSettingDialog(SettingsType type = SettingsType.Account, Window owner = null)\n\t{\n'
     '\t\tif (type == SettingsType.Premium || type == SettingsType.Account)\n'
     '\t\t{\n'
     '\t\t\ttype = SettingsType.More;\n'
     '\t\t}\n')
])

# 3. ProChecker: remove task/timer count limits
edit('TickTick\\ticktick_WPF.Util\\ProChecker.cs', [
    ('\tpublic static async Task<bool> CheckTaskLimit(string projectId, int addCount = 1)\n\t{\n',
     '\tpublic static async Task<bool> CheckTaskLimit(string projectId, int addCount = 1)\n\t{\n\t\tawait Task.CompletedTask;\n\t\treturn false;\n'),
    ('\tpublic static async Task<bool> CheckTimerLimit(Window owner)\n\t{\n',
     '\tpublic static async Task<bool> CheckTimerLimit(Window owner)\n\t{\n\t\tawait Task.CompletedTask;\n\t\treturn false;\n')
])

# 4. Utils.GetUserLimit: unlimited everywhere
edit('TickTick\\ticktick_WPF.Util\\Utils.cs', [
    ('\tpublic static long GetUserLimit(Constants.LimitKind kind)\n\t{\n\t\treturn LimitCache.GetLimitByKey(kind);\n\t}',
     '\tpublic static long GetUserLimit(Constants.LimitKind kind)\n\t{\n\t\treturn long.MaxValue;\n\t}')
])
print('UNLOCK PATCH DONE')
