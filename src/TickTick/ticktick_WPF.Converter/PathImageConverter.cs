using System;
using System.Globalization;
using System.Windows.Data;
using TickTickUtils.Lang;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Files;

namespace ticktick_WPF.Converter;

public class PathImageConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is string text && values[1] is double num)
		{
			bool flag = false;
			int width = ((int)(num * 1.5) / 1000 + 1) * 1000;
			if (!FileUtils.FileEmptyOrNotExists(text))
			{
				try
				{
					return ThemeUtil.GetImage(text, width);
				}
				catch (Exception arg)
				{
					UtilLog.Error($"PathImageConverter.Convert Read Image Failed, {arg}");
					flag = true;
				}
			}
			return ThemeUtil.GetPlaceholderAttachmentImage(text.StartsWith("failed", StringComparison.OrdinalIgnoreCase) || flag);
		}
		return ThemeUtil.GetPlaceholderAttachmentImage();
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
