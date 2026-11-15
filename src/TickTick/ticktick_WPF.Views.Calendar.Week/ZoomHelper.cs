using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ticktick_WPF.Views.Calendar.Week;

public static class ZoomHelper
{
	public static void Zoom(double delta, double actualHeight, ScrollViewer scroller)
	{
		double hourHeight = CalendarGeoHelper.HourHeight;
		double num = (actualHeight * 1.0 - (double)(CalendarGeoHelper.TopFolded ? 64 : 0)) / (double)(24 - CalendarGeoHelper.GetCollapsedHours());
		bool flag = delta > 0.0;
		double num2 = ((hourHeight <= num && !flag) ? num : GetNextZoomHeight(flag, hourHeight));
		if (num2 <= num)
		{
			num2 = GetValidAutoHeight(num);
		}
		if (Math.Abs(num2 - hourHeight) > 0.0)
		{
			CalendarGeoHelper.SetCalendarHourHeight(num2);
			Point position = Mouse.GetPosition(scroller);
			scroller.ScrollToVerticalOffset((scroller.VerticalOffset + position.Y) / hourHeight * num2 - position.Y);
		}
	}

	private static int GetValidAutoHeight(double height)
	{
		int num = (int)height;
		switch (num % 4)
		{
		case 1:
			num--;
			break;
		case 2:
			num += 2;
			break;
		case 3:
			num++;
			break;
		}
		return num;
	}

	private static double GetNextZoomHeight(bool zoomIn, double checkHeight)
	{
		List<double> hourZoomHeight = CalendarGeoHelper.HourZoomHeight;
		if (checkHeight >= hourZoomHeight.Last())
		{
			if (!zoomIn)
			{
				return hourZoomHeight[hourZoomHeight.Count - 2];
			}
			return hourZoomHeight.Last();
		}
		if (checkHeight <= hourZoomHeight.First())
		{
			if (zoomIn)
			{
				return hourZoomHeight[1];
			}
			return hourZoomHeight.First();
		}
		int num = 0;
		for (int i = 0; i < hourZoomHeight.Count - 1; i++)
		{
			double num2 = hourZoomHeight[i];
			double num3 = hourZoomHeight[i + 1];
			if (num2 >= checkHeight && num2 < num3)
			{
				num = i;
				break;
			}
		}
		if (!zoomIn)
		{
			return hourZoomHeight[Math.Max(0, num - 1)];
		}
		return hourZoomHeight[num + 1];
	}
}
