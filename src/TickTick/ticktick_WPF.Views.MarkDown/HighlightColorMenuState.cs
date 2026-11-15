using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class HighlightColorMenuState
{
	public const string DefaultColor = "#FFEC6C";

	public const double DefaultLightOpacity = 0.4;

	public const double DefaultDarkOpacity = 0.5;

	private const double YellowGreenLightOpacity = 0.48;

	public static readonly IReadOnlyList<HighlightColorData> HighlightColors = new HighlightColorData[6]
	{
		new HighlightColorData("#FFA1A8", "#FD848D", 0.4, 0.5),
		new HighlightColorData("#FFEC6C", "#FFE500", 0.48, 0.5),
		new HighlightColorData("#BBEA84", "#6FF143", 0.48, 0.5),
		new HighlightColorData("#97EEF1", "#57DEE2", 0.4, 0.5),
		new HighlightColorData("#A3B8F9", "#7597FF", 0.4, 0.5),
		new HighlightColorData("#D3A9F3", "#B677E5", 0.4, 0.5)
	};

	private static string _selectedColor = "#FFEC6C";

	private static string _lastEffectiveColor = "#FFEC6C";

	public bool IsPopupOpen { get; set; }

	public string SelectedColor => _selectedColor;

	public static string GetSelectedColor()
	{
		return GetEditorColor(_selectedColor) ?? GetEditorColor(_lastEffectiveColor) ?? GetEditorColor("#FFEC6C");
	}

	public string SelectColor(string color)
	{
		string text = NormalizeColor(color);
		if (text == null)
		{
			_selectedColor = "#FFEC6C";
			_lastEffectiveColor = "#FFEC6C";
			return null;
		}
		_selectedColor = text;
		_lastEffectiveColor = text;
		return text;
	}

	public string GetMenuSelectedColor(string contextColor = null)
	{
		return NormalizeColor(contextColor) ?? _selectedColor ?? _lastEffectiveColor ?? "#FFEC6C";
	}

	public string GetApplyColor(string contextColor = null)
	{
		return NormalizeColor(contextColor) ?? _selectedColor ?? _lastEffectiveColor ?? "#FFEC6C";
	}

	public string GetIconBackground(string contextColor = null)
	{
		return GetOptionColor(contextColor) ?? GetOptionColor(_selectedColor) ?? GetOptionColor(_lastEffectiveColor) ?? GetOptionColor("#FFEC6C");
	}

	public static ColorExtra CreateHighlightColor(string color)
	{
		return new ColorExtra
		{
			Background = GetEditorColor(color),
			Color = null
		};
	}

	public static SolidColorBrush GetEditorHighlightBrush(string color)
	{
		bool flag = ThemeUtil.IsDark();
		HighlightColorData colorData = GetColorData(color);
		if (colorData != null)
		{
			return colorData.GetEditorBrush(flag);
		}
		string text = NormalizeColorValue(color);
		if (text == null)
		{
			return null;
		}
		return HighlightColorData.CreateBrush(text, flag ? 0.5 : 0.4);
	}

	public static string GetOptionColor(string color)
	{
		string text = NormalizeColorValue(color);
		if (text == null)
		{
			return null;
		}
		return GetColorData(text)?.OptionColor ?? text;
	}

	public static string GetEditorColor(string color)
	{
		string text = NormalizeColorValue(color);
		if (text == null)
		{
			return null;
		}
		return GetColorData(text)?.EditorColor ?? text;
	}

	public static string ExtractHighlightColor(string input)
	{
		input = MarkDownEditor.NormalizeDisplayTaskMarkersForPersistence(input ?? string.Empty);
		Match match = Regex.Match(input, "background:\\s*#([A-Fa-f0-9]{6,8});");
		Match match2 = Regex.Match(input, "(?:==|::)\\{(#(?:[A-Fa-f0-9]{6}|[A-Fa-f0-9]{8}))\\}");
		if (match2.Success)
		{
			return NormalizeColor(match2.Groups[1].Value);
		}
		if (match.Success)
		{
			return NormalizeColor("#" + match.Groups[1].Value);
		}
		return null;
	}

	public static string NormalizeColor(string color)
	{
		if (string.IsNullOrWhiteSpace(color))
		{
			return null;
		}
		color = color.Trim();
		color = ((color[0] == '#') ? color.ToUpperInvariant() : ("#" + color).ToUpperInvariant());
		return NormalizeKnownColor(color);
	}

	private static string GetEmptyIconBackground()
	{
		Color color = ThemeUtil.GetColor("BaseColorOpacity20").Color;
		return ColorHelper.ColorConverter.ConvertToString(color);
	}

	private static HighlightColorData GetColorData(string color)
	{
		string normalizedColor = NormalizeColorValue(color);
		if (normalizedColor == null)
		{
			return null;
		}
		return HighlightColors.FirstOrDefault((HighlightColorData item) => string.Equals(NormalizeColorValue(item.OptionColor), normalizedColor, StringComparison.OrdinalIgnoreCase) || string.Equals(NormalizeColorValue(item.EditorColor), normalizedColor, StringComparison.OrdinalIgnoreCase));
	}

	private static string NormalizeKnownColor(string color)
	{
		string normalizedColor = NormalizeColorValue(color);
		if (normalizedColor == null)
		{
			return null;
		}
		return HighlightColors.FirstOrDefault((HighlightColorData item) => string.Equals(NormalizeColorValue(item.OptionColor), normalizedColor, StringComparison.OrdinalIgnoreCase) || string.Equals(NormalizeColorValue(item.EditorColor), normalizedColor, StringComparison.OrdinalIgnoreCase))?.OptionColor ?? normalizedColor;
	}

	private static string NormalizeColorValue(string color)
	{
		if (string.IsNullOrWhiteSpace(color))
		{
			return null;
		}
		color = color.Trim();
		color = ((color[0] == '#') ? color.ToUpperInvariant() : ("#" + color).ToUpperInvariant());
		if (color.Length != 4 && color.Length != 5 && color.Length != 7 && color.Length != 9)
		{
			return null;
		}
		for (int i = 1; i < color.Length; i++)
		{
			char c = color[i];
			if ((c < '0' || c > '9') && (c < 'A' || c > 'F'))
			{
				return null;
			}
		}
		if (color.Length == 9 && color.StartsWith("#FF", StringComparison.OrdinalIgnoreCase))
		{
			return "#" + color.Substring(3);
		}
		return color;
	}
}
