using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class TaskDetailPriorityIconConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2)
		{
			object obj = values[0];
			if (obj is int)
			{
				switch ((int)obj)
				{
				case 0:
					if (values[1] is ResourceDictionary context)
					{
						return Utils.GetImageSource("NonePriorityDrawingImage", context);
					}
					return Utils.GetImageSource("NonePriorityDrawingImage");
				case 1:
					return Utils.GetImageSource("LowPriorityDrawingImage");
				case 3:
					return Utils.GetImageSource("MidPriorityDrawingImage");
				case 5:
					return Utils.GetImageSource("HighPriorityDrawingImage");
				}
			}
		}
		return Utils.GetImageSource("NonePriorityDrawingImage");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
