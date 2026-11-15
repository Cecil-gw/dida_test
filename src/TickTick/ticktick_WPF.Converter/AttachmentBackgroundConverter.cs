using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util.Attachment;

namespace ticktick_WPF.Converter;

public class AttachmentBackgroundConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			if (parameter is AttachmentConvertParam { IsFailed: not false } attachmentConvertParam)
			{
				if (attachmentConvertParam.IsDark)
				{
					return new SolidColorBrush(Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue))
					{
						Opacity = 0.1
					};
				}
				return new SolidColorBrush(Color.FromRgb(19, 19, 19))
				{
					Opacity = 0.03
				};
			}
			return AttachmentResourceHelper.GetFileTypeColor(value.ToString());
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
