using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.Theme;

public class AppearanceModel
{
	public string IconKey { get; }

	public string FontKey { get; }

	public string ThemeKey { get; }

	public int FontSize { get; }

	public int NumDisplayType { get; }

	public int ShowCompleteLine { get; }

	public AppearanceModel()
	{
		IconKey = LocalSettings.Settings.Common.AppIconKey;
		FontKey = LocalSettings.Settings.ExtraSettings.AppFontFamily;
		ThemeKey = LocalSettings.Settings.ExtraSettings.AppTheme;
		FontSize = LocalSettings.Settings.BaseFontSize;
		NumDisplayType = LocalSettings.Settings.ExtraSettings.NumDisplayType;
		ShowCompleteLine = LocalSettings.Settings.ExtraSettings.ShowCompleteLine;
	}
}
