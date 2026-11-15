using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Views.CountDown.Infrastructure;

public class CardSizeConverter : IMultiValueConverter
{
	private const double SmallCardHeight = 171.0;

	private const double LargeCardHeight = 232.0;

	private const double MaxAspectRatio = 1.6;

	private const double MinAspectRatio = 1.3333333333333333;

	private const double MinSmallCardWidth = 228.0;

	private const double MaxSmallCardWidth = 310.0;

	private const double MinLargeCardWidth = 251.0;

	private const double MaxLargeCardWidth = 310.0;

	private const double SingleCardMaxWidth = 310.0;

	private const double BreakpointWidth = 1591.0;

	private const double TotalHorizontalMargin = 20.0;

	private const double CardMargin = 10.0;

	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length < 2 || !(values[0] is double num) || !(values[1] is int num2))
		{
			return (parameter as string == "height") ? 171.0 : 228.0;
		}
		int num3 = num2;
		int num4;
		double num5;
		if (num > 1591.0)
		{
			num4 = ((num3 >= 6) ? 1 : 0);
			if (num4 != 0)
			{
				num5 = 232.0;
				goto IL_008f;
			}
		}
		else
		{
			num4 = 0;
		}
		num5 = 171.0;
		goto IL_008f;
		IL_008f:
		double num6 = num5;
		double num7 = ((num4 != 0) ? 251.0 : 228.0);
		double val = ((num4 != 0) ? 310.0 : 310.0);
		double num8 = num - 20.0;
		int num9 = (int)Math.Floor((num8 + 10.0) / (num7 + 10.0));
		if (num9 < 1)
		{
			num9 = 1;
		}
		int num10 = Math.Min(num9, num3);
		double num11 = ((num10 <= 1) ? Math.Min(num8, val) : (num8 / (double)num10));
		if (num10 <= 3)
		{
			num11 = Math.Min(num11, 310.0);
		}
		if (num3 <= num10)
		{
			num11 = Math.Min(num11, 310.0);
		}
		if (num3 == 1)
		{
			num11 = 310.0;
		}
		double num12 = (num11 - 20.0) / (num6 - 20.0 - 5.0);
		double num13 = num6;
		if (num12 > 1.6)
		{
			num13 = (num11 - 20.0) / 1.6 + 20.0 + 5.0;
		}
		if (num12 < 1.3333333333333333)
		{
			num13 = (num11 - 20.0) / 1.3333333333333333 + 20.0;
		}
		if (num3 == 1)
		{
			num13 = 193.75;
		}
		if (parameter as string == "height")
		{
			return (num13 > 0.0) ? num13 : 0.0;
		}
		return (num11 > 0.0) ? num11 : 0.0;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
