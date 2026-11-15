using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ticktick_WPF.Views.Timeline;

public class MainScrollClipConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length >= 5 && values[0] is double num && values[1] is double height && values[2] is bool flag && values[3] is double num2 && values[4] is bool flag2)
		{
			bool flag3 = default(bool);
			int num3;
			if (values.Length == 6)
			{
				object obj = values[5];
				if (obj is bool)
				{
					flag3 = (bool)obj;
					num3 = 1;
				}
				else
				{
					num3 = 0;
				}
			}
			else
			{
				num3 = 0;
			}
			if (((uint)num3 & (flag3 ? 1u : 0u)) != 0)
			{
				flag2 = false;
			}
			return new RectangleGeometry(new Rect(new Point(flag ? num2 : 0.0, 0.0), new Size(Math.Max(0.0, num - (flag ? num2 : 0.0) - (flag2 ? 215.0 : 0.0)), height)));
		}
		return null;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
