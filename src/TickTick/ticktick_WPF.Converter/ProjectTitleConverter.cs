using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class ProjectTitleConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2 && values[0] is string text)
		{
			object obj = values[1];
			if (obj is bool)
			{
				if (!(bool)obj)
				{
					return text;
				}
				string emojiIcon = EmojiHelper.GetEmojiIcon(text);
				if (!text.StartsWith(emojiIcon))
				{
					return text;
				}
				return text.Substring(emojiIcon.Length);
			}
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
