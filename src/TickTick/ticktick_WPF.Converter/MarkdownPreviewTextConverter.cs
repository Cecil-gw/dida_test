using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class MarkdownPreviewTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string contentMarkdownDisplayText = TaskUtils.GetContentMarkdownDisplayText(value as string);
		if (parameter is string s && int.TryParse(s, out var result))
		{
			if (contentMarkdownDisplayText.Length <= result)
			{
				return contentMarkdownDisplayText;
			}
			return contentMarkdownDisplayText.Substring(0, result);
		}
		return contentMarkdownDisplayText;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
