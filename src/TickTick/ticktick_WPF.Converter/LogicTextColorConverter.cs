using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class LogicTextColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is LogicType logicType && parameter is string text && logicType.ToString() == text)
		{
			switch (logicType)
			{
			case LogicType.And:
				return ThemeUtil.GetColor("LogicAndColor");
			case LogicType.Or:
				return ThemeUtil.GetColor("LogicOrColor");
			case LogicType.Not:
				return ThemeUtil.GetColor("LogicNotColor");
			}
		}
		return ThemeUtil.GetColor("BaseColorOpacity40");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
