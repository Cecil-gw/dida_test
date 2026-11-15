using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class DopamineColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string[] array && array.Length != 0)
		{
			bool flag = false;
			string text = array[0];
			if (text == "#21213D" || text == "#242424")
			{
				flag = true;
			}
			if (parameter != null)
			{
				if (string.Equals(parameter.ToString(), "header"))
				{
					return flag ? 0.06 : 0.1;
				}
				if (string.Equals(parameter.ToString(), "bar"))
				{
					return flag ? 0.1 : 0.3;
				}
			}
			if (!flag)
			{
				return ThemeUtil.GetColorInString("#191919");
			}
			return new SolidColorBrush(Colors.White);
		}
		if (parameter != null)
		{
			if (string.Equals(parameter.ToString(), "header"))
			{
				return 0.1;
			}
			if (string.Equals(parameter.ToString(), "bar"))
			{
				return 0.3;
			}
		}
		return ThemeUtil.GetColorInString("#191919");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
