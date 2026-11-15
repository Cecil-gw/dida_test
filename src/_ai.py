import io
root = r'C:\114514\dida\ticktick-wpf-custom'

# ---------- 1. LeftMenuBar: crown + avatar menu ----------
p = root + r'\TickTick\ticktick_WPF.Views.TabBar\LeftMenuBar.cs'
c = io.open(p, encoding='utf-8').read()
c = c.replace('''\tprivate void SetProCrown()
\t{
\t\tif (LocalSettings.Settings.IsPro && !UserManager.InGraceDate())
\t\t{
\t\t\tHeadProImage.Visibility = Visibility.Visible;
\t\t\tHeadFreeImage.Visibility = Visibility.Hidden;
\t\t}
\t\telse
\t\t{
\t\t\tHeadProImage.Visibility = Visibility.Hidden;
\t\t\tHeadFreeImage.Visibility = Visibility.Visible;
\t\t}
\t}''',
'''\tprivate void SetProCrown()
\t{
\t\tHeadProImage.Visibility = Visibility.Hidden;
\t\tHeadFreeImage.Visibility = Visibility.Hidden;
\t}''')
c = c.replace('''\t\t\tnew CustomMenuItemViewModel("Settings", Utils.GetString("Settings"), Utils.GetGeometry("Ic.Tabbar.SettingLinear")),
\t\t\tnew CustomMenuItemViewModel("Statistics", Utils.GetString("Statistics"), Utils.GetGeometry("Ic.Tabbar.Statistics")),
\t\t\tnew CustomMenuItemViewModel("Premium", Utils.GetString("Premium"), Utils.GetGeometry("Ic.Tabbar.VIP")),
\t\t\tnew CustomMenuItemViewModel("Logout", Utils.GetString("PublicLogout"), Utils.GetGeometry("Ic.Tabbar.Logout"))''',
'''\t\t\tnew CustomMenuItemViewModel("Settings", Utils.GetString("Settings"), Utils.GetGeometry("Ic.Tabbar.SettingLinear")),
\t\t\tnew CustomMenuItemViewModel("Statistics", Utils.GetString("Statistics"), Utils.GetGeometry("Ic.Tabbar.Statistics")),
\t\t\tnew CustomMenuItemViewModel("Logout", Utils.GetString("PublicLogout"), Utils.GetGeometry("Ic.Tabbar.Logout"))''')
c = c.replace('''\t\tcase "Premium":
\t\t\tUserActCollectUtils.AddClickEvent("tab_bar", "user", "premium");
\t\t\tUtils.StartUpgrade("account_info", null, toSettings: true);
\t\t\tbreak;''',
'''\t\tcase "Premium":
\t\t\tbreak;''')
c = c.replace('''\tpublic void ShowSettingDialog()
\t{
\t\tif (string.IsNullOrEmpty(LocalSettings.Settings.LoginUserId))
\t\t{
\t\t\tnew LoginDialog().ShowDialog();
\t\t\treturn;
\t\t}
\t\tSettingsHelper.PullRemoteSettings();
\t\tSettingDialog.ShowSettingDialog(SettingsType.Account, Window.GetWindow(this));
\t}''',
'''\tpublic void ShowSettingDialog()
\t{
\t\tSettingDialog.ShowSettingDialog(SettingsType.More, Window.GetWindow(this));
\t}''')
io.open(p, 'w', encoding='utf-8').write(c)
print('LeftMenuBar patched')

# ---------- 2. ExtraSettings: AI api fields ----------
p = root + r'\TickTick\ticktick_WPF.Resource\ExtraSettings.cs'
c = io.open(p, encoding='utf-8').read()
c = c.replace('''\tpublic string AppTheme { get; set; }''',
'''\tpublic string AppTheme { get; set; }

\tpublic string AI_ApiBaseUrl { get; set; } = "";

\tpublic string AI_ApiKey { get; set; } = "";

\tpublic string AI_Model { get; set; } = "gpt-4o-mini";''')
io.open(p, 'w', encoding='utf-8').write(c)
print('ExtraSettings patched')

# ---------- 3. MainWindow.SwitchAI -> local chat panel ----------
p = root + r'\TickTick\ticktick_WPF.Views\MainWindow.cs'
c = io.open(p, encoding='utf-8').read()
old = '''\tprivate async void SwitchAI()
\t{
\t\tSetViewVisible(AIBorder);
\t\tAIWorkspaceView orCreateAIWorkspaceView = GetOrCreateAIWorkspaceView();
\t\tSetInterfaceStyle();
\t\tSetMainWindowsMinSize(isList: true);
\t\tbool flag = !HasAIAssistantPrivacyConsent();
\t\torCreateAIWorkspaceView.SetPrivacyAuthorizationRequired(flag);
\t\tif (!flag)
\t\t{
\t\t\torCreateAIWorkspaceView.RefreshConversationsAsync();
\t\t\tGetOrCreateAIConversationView().RefreshGreetingAsync();
\t\t}
\t\ttry
\t\t{
\t\t\tawait AttachAIWebViewToMainWindowAsync(!flag);
\t\t}
\t\tcatch (Exception ex)
\t\t{
\t\t\tUtilLog.Info("Failed to show Agent WebView in main window. " + ExceptionUtils.BuildExceptionMessage(ex));
\t\t}
\t}'''
new = '''\tprivate async void SwitchAI()
\t{
\t\tSetViewVisible(AIBorder);
\t\tSetInterfaceStyle();
\t\tSetMainWindowsMinSize(isList: true);
\t\tawait Task.Delay(50);
\t\tLocalAIChatPanel.ShowIn(AIBorder);
\t}'''
assert old in c, 'SwitchAI body mismatch'
c = c.replace(old, new, 1)
io.open(p, 'w', encoding='utf-8').write(c)
print('SwitchAI patched')
print('AI PART DONE')
