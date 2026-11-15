import io, re
root = r'C:\114514\dida\ticktick-wpf-custom'

# 1. App.cs login gate (regex)
p = root + r'\TickTick\ticktick_WPF\App.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()
gate = re.compile(
    r'\t\t\tif \(LocalSettings\.Settings\.LoginUserId == ""\)\r?\n'
    r'\t\t\t\{\r?\n'
    r'\t\t\t\tFirstUse = true;\r?\n'
    r'\t\t\t\tnew LoginDialog\(\)\.Show\(\);\r?\n'
    r'\t\t\t\}\r?\n')
new_gate = (
    '\t\t\tif (LocalSettings.Settings.LoginUserId == "")\n'
    '\t\t\t{\n'
    '\t\t\t\tFirstUse = false;\n'
    '\t\t\t\tLocalSettings.Settings.LoginUserId = "localuser";\n'
    '\t\t\t\tLocalSettings.Settings.LoginUserName = "Local";\n'
    '\t\t\t\tLocalSettings.Settings.Save();\n'
    '\t\t\t}\n'
)
c2, n = gate.subn(new_gate, c)
if n != 1: raise SystemExit('gate subn=%d' % n)
with io.open(p, 'w', encoding='utf-8') as f: f.write(c2)
print('App.cs gate patched')

def edit(rel, pairs):
    p = root + '\\' + rel
    with io.open(p, encoding='utf-8') as f: c = f.read()
    for a, b in pairs:
        if a not in c:
            print('MISS', rel, repr(a[:70])); raise SystemExit(1)
        c = c.replace(a, b, 1)
    with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
    print('patched', rel)

edit('TickTick\\ticktick_WPF\\App.cs', [
    ('\t\tABTestManager.AssignGroupOnLaunch();\n', ''),
    ('\t\tUpgradeAppService.SendUpdateFrom();\n', ''),
    ('\t\tCommunicator.PullAppConfig(fromLogin: false, ignoreEtag: true);\n'
     '\t\tAppConfigManager.PerformPullAppConfig();\n'
     '\t\tAppConfigManager.StartPolling();\n'
     '\t\tUserActCollectUtils.StartPost();\n', ''),
])
edit('TickTick\\ticktick_WPF.Util\\SyncManager.cs', [
    ('\t\tSyncService = new SyncService();\n'
     '\t\tSyncService.Init();\n'
     '\t\tSyncService.SyncFinished -= OnSyncFinished;\n'
     '\t\tSyncService.SyncFinished += OnSyncFinished;\n'
     '\t\tNetworkChange.NetworkAvailabilityChanged -= OnNetWorkChanged;\n'
     '\t\tNetworkChange.NetworkAvailabilityChanged += OnNetWorkChanged;',
     '\t\tSyncService = new SyncService();'),
    ('\tpublic static async Task Sync(int type = 0)\n\t{\n',
     '\tpublic static async Task Sync(int type = 0)\n\t{\n\t\tawait Task.CompletedTask;\n\t\treturn;\n'),
    ('\tpublic static async void Init()\n\t{\n\t\tSyncService.Init();\n\t\tawait Task.Delay(1000);\n\t\tSync(1);',
     '\tpublic static async void Init()\n\t{\n\t\tawait Task.CompletedTask;'),
    ('\tpublic static async Task PullProjectTasks(string projectId)\n\t{\n\t\tawait SyncService.PullProjectTasks(projectId);',
     '\tpublic static async Task PullProjectTasks(string projectId)\n\t{\n\t\tawait Task.CompletedTask;'),
    ('\tprivate static void OnNetWorkChanged(object sender, NetworkAvailabilityEventArgs e)\n\t{\n\t\tFocusOptionUploader.SaveOption(null, force: true, resetLocalPieces: true);\n\t}',
     '\tprivate static void OnNetWorkChanged(object sender, NetworkAvailabilityEventArgs e)\n\t{\n\t}'),
])
edit('TickTick\\ticktick_WPF.Dal\\UserDao.cs', [
    ('\tpublic static bool IsPro()\n\t{\n\t\treturn IsPro2();\n\t}\n\n'
     '\tpublic static bool IsPro2()\n\t{\n\t\treturn LocalSettings.Settings.IsPro;\n\t}\n\n'
     '\tpublic static bool IsUserValid()\n\t{\n\t\treturn IsPro();\n\t}',
     '\tpublic static bool IsPro()\n\t{\n\t\treturn true;\n\t}\n\n'
     '\tpublic static bool IsPro2()\n\t{\n\t\treturn true;\n\t}\n\n'
     '\tpublic static bool IsUserValid()\n\t{\n\t\treturn true;\n\t}')
])
edit('TickTick\\ticktick_WPF.Util\\ProChecker.cs', [
    ('\tpublic static async Task ShowUpgradeDialog(ProType type, Window owner = null, string teamId = null)\n\t{\n',
     '\tpublic static async Task ShowUpgradeDialog(ProType type, Window owner = null, string teamId = null)\n\t{\n\t\tawait Task.CompletedTask;\n\t\treturn;\n')
])
edit('TickTick\\ticktick_WPF.Service\\ABTestManager.cs', [
    ('\tpublic static async Task AssignGroupOnLaunch()\n\t{\n\t\tawait GetAssignedTestGroup();\n\t\tawait ApplyABTestGroup();\n\t}',
     '\tpublic static async Task AssignGroupOnLaunch()\n\t{\n\t\tawait Task.CompletedTask;\n\t}')
])
edit('TickTick\\ticktick_WPF\\BugsnagHelper.cs', [
    ('\tstatic BugsnagHelper()\n\t{\n'
     '\t\t_delaySender = new DelayActionHandler();\n'
     '\t\tif (LocalSettings.Settings?.Common != null && !_userSetted)\n'
     '\t\t{\n'
     '\t\t\t_userSetted = true;\n'
     '\t\t\tWPFClient.Config.SetUser(LocalSettings.Settings.LoginUserId, LocalSettings.Settings.LoginUserName, LocalSettings.Settings.LoginUserName);\n'
     '\t\t}\n'
     '\t\tWPFClient.Config.AutoNotify = false;\n'
     '\t\t_delaySender.SetAction(delegate\n'
     '\t\t{\n'
     '\t\t\tif (_exception != null)\n'
     '\t\t\t{\n'
     '\t\t\t\ttry\n'
     '\t\t\t\t{\n'
     '\t\t\t\t\tWPFClient.Notify(_exception);\n'
     '\t\t\t\t}\n'
     '\t\t\t\tcatch (Exception)\n'
     '\t\t\t\t{\n'
     '\t\t\t\t}\n'
     '\t\t\t}\n'
     '\t\t});\n'
     '\t}',
     '\tstatic BugsnagHelper()\n\t{\n\t\t_delaySender = new DelayActionHandler();\n\t}'),
    ('\tpublic static void SendException(Exception e)\n\t{\n\t\tif (!_shutDown)\n\t\t{\n\t\t\t_shutDown = true;\n\t\t\t_exception = e;\n\t\t\t_delaySender.TryDoAction();\n\t\t}\n\t}',
     '\tpublic static void SendException(Exception e)\n\t{\n\t}'),
    ('\tpublic static void SetUser(UserModel user)\n\t{\n\t\tif (user != null && !_userSetted)\n\t\t{\n\t\t\tWPFClient.Config.SetUser(user.userId, user.username, user.username);\n\t\t}\n\t}',
     '\tpublic static void SetUser(UserModel user)\n\t{\n\t}')
])

stub = open(root + r'\_stub.cs', encoding='utf-8').read()
with io.open(root + r'\TickTick\ticktick_WPF.Util\UserActCollectUtils.cs', 'w', encoding='utf-8') as f:
    f.write(stub)
print('UserActCollectUtils -> stub; PHASE3 PATCH DONE')
