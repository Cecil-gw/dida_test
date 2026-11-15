using System;
using System.Globalization;
using System.Windows.Media;

namespace ticktick_WPF.Views.MarkDown;

public class HighlightColorData
{
	public string OptionColor { get; set; }

	public string EditorColor { get; set; }

	public double LightOpacity { get; set; }

	public double DarkOpacity { get; set; }

	public SolidColorBrush OptionBrush { get; set; }

	public SolidColorBrush LightBrush { get; set; }

	public SolidColorBrush DarkBrush { get; set; }

	public HighlightColorData(string optionColor, string editorColor, double lightOpacity, double darkOpacity)
	{
		OptionColor = NormalizeRgbColor(optionColor);
		EditorColor = NormalizeRgbColor(editorColor);
		LightOpacity = lightOpacity;
		DarkOpacity = darkOpacity;
		OptionBrush = CreateBrush(OptionColor, 1.0);
		LightBrush = CreateBrush(EditorColor, lightOpacity);
		DarkBrush = CreateBrush(EditorColor, darkOpacity);
	}

	public SolidColorBrush GetEditorBrush(bool isDark)
	{
		if (!isDark)
		{
			return LightBrush;
		}
		return DarkBrush;
	}

	internal static SolidColorBrush CreateBrush(string color, double opacity)
	{
		SolidColorBrush solidColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color))
		{
			Opacity = opacity
		};
		if (solidColorBrush.CanFreeze)
		{
			solidColorBrush.Freeze();
		}
		return solidColorBrush;
	}

	private static string NormalizeRgbColor(string color)
	{
		if (string.IsNullOrWhiteSpace(color))
		{
			return null;
		}
		color = color.Trim();
		color = ((color[0] == '#') ? color.ToUpper(CultureInfo.InvariantCulture) : ("#" + color).ToUpper(CultureInfo.InvariantCulture));
		if (color.Length != 9 || !color.StartsWith("#FF", StringComparison.OrdinalIgnoreCase))
		{
			return color;
		}
		return "#" + color.Substring(3);
	}
}
