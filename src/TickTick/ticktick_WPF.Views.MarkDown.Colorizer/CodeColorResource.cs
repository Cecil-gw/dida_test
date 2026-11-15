using System.Collections.Generic;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown.Colorizer;

public class CodeColorResource
{
	private static readonly Dictionary<string, string> CodeColorsLight = new Dictionary<string, string>
	{
		{ "Comment", "B8B8B8" },
		{ "String", "14C257" },
		{ "Atom", "EF42B5" },
		{ "Keyword", "FF871D" },
		{ "Variable", "3867E4" }
	};

	private static readonly Dictionary<string, string> CodeColorsDark = new Dictionary<string, string>
	{
		{ "Comment", "#535353" },
		{ "String", "#319A59" },
		{ "Atom", "#B4317E" },
		{ "Keyword", "#C4742E" },
		{ "Variable", "#3F62BF" }
	};

	private static readonly Dictionary<string, string> JavaDict = new Dictionary<string, string> { { "", "" } };

	public static SolidColorBrush GetColorByType(string lang, string type)
	{
		if (!JavaDict.TryGetValue(type, out var value))
		{
			value = type;
		}
		if (ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId))
		{
			if (!CodeColorsDark.ContainsKey(value))
			{
				return ThemeUtil.GetColor("BaseColorOpacity60");
			}
			return ThemeUtil.GetColor(CodeColorsDark[value]);
		}
		if (!CodeColorsLight.ContainsKey(value))
		{
			return ThemeUtil.GetColor("BaseColorOpacity60");
		}
		return ThemeUtil.GetColorInString(CodeColorsDark[value]);
	}
}
