using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CartoonStyleColorConverter : IValueConverter
{
	private static readonly Dictionary<string, string> AToBMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["#5F85FF"] = "#FFC5C6",
		["#76A9FF"] = "#C9ECFF",
		["#4EDBCC"] = "#FFF8C9",
		["#56E8A1"] = "#D8FFBF",
		["#C0E575"] = "#BCFFEA",
		["#FFB15E"] = "#FFF0BE",
		["#F9966F"] = "#FFDDC8",
		["#FF7E7E"] = "#FFD8E0",
		["#8B8AF1"] = "#FFC2E1",
		["#243269"] = "#D6E5F4",
		["#5D423B"] = "#FAE5D4",
		["#2B2B2B"] = "#E4E4E4"
	};

	private static string GetBColor(string aColor)
	{
		if (!AToBMap.TryGetValue(aColor, out var value))
		{
			return "#FFC5C6";
		}
		return value;
	}

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string[] array && array.Length != 0)
		{
			return GetBColor(array[0]);
		}
		return ThemeUtil.GetColorInString("#FFC5C6");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
