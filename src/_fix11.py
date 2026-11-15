import io
p = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views\MainWindow.cs'
with io.open(p, encoding='utf-8') as f: c = f.read()

# 1. SetViewVisible: reset z-index for non-AI modules
old = '''	private void SetViewVisible(Border border)
	{
		FocusBorder.Visibility = ((FocusBorder != border) ? Visibility.Hidden : Visibility.Visible);'''
new = '''	private void SetViewVisible(Border border)
	{
		Panel.SetZIndex(AIBorder, (border == AIBorder) ? 10 : 0);
		FocusBorder.Visibility = ((FocusBorder != border) ? Visibility.Hidden : Visibility.Visible);'''
assert old in c
c = c.replace(old, new, 1)

# 2. SwitchAI: raise AIBorder above HeaderGrid
old2 = '''	private async void SwitchAI()
	{
		SetViewVisible(AIBorder);
		SetInterfaceStyle();
		SetMainWindowsMinSize(isList: true);
		await Task.Delay(50);
		LocalAIChatPanel.ShowIn(AIBorder);
	}'''
new2 = '''	private async void SwitchAI()
	{
		SetViewVisible(AIBorder);
		Panel.SetZIndex(AIBorder, 10);
		SetInterfaceStyle();
		SetMainWindowsMinSize(isList: true);
		await Task.Delay(50);
		LocalAIChatPanel.ShowIn(AIBorder);
	}'''
assert old2 in c
c = c.replace(old2, new2, 1)
with io.open(p, 'w', encoding='utf-8') as f: f.write(c)
print('z-index patched')

# 3. panel: send enabled when legacy config exists
p2 = r'C:\114514\dida\ticktick-wpf-custom\TickTick\ticktick_WPF.Views.AI\LocalAIChatPanel.cs'
with io.open(p2, encoding='utf-8') as f: c2 = f.read()
old3 = '''    private void RefreshSendState()
    {
        var a = AIProviderStore.Active;
        bool ok = a != null && !string.IsNullOrEmpty(a.base_url);
        _sendBtn.IsEnabled = ok;'''
new3 = '''    private void RefreshSendState()
    {
        var a = AIProviderStore.Active;
        bool ok = (a != null && !string.IsNullOrEmpty(a.base_url))
            || !string.IsNullOrEmpty(LocalSettings.Settings.ExtraSettings.AI_ApiBaseUrl);
        _sendBtn.IsEnabled = ok;'''
assert old3 in c2
c2 = c2.replace(old3, new3, 1)
with io.open(p2, 'w', encoding='utf-8') as f: f.write(c2)
print('send state patched')
