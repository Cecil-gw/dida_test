namespace ticktick_WPF.Models;

public class CustomThemeSetting
{
	public double ThemeImageOpacity { get; set; } = 1.0;

	public int ThemeImageBlurRadius { get; set; }

	public double ShowAreaOpacity { get; set; } = 0.9;

	public string CustomThemeLocation { get; set; }

	public string CustomThemeColor { get; set; }

	public void Reset()
	{
		ThemeImageOpacity = 1.0;
		ThemeImageBlurRadius = 0;
		ShowAreaOpacity = 0.9;
		CustomThemeLocation = string.Empty;
	}
}
