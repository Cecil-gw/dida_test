using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace ticktick_WPF.Views.Widget;

public class WidgetLocationHelper
{
	public static WidgetLocationModel GetLocationSafely(double left, double top, double width, double height, Matrix? transform)
	{
		WidgetLocationModel widgetLocationModel = new WidgetLocationModel
		{
			Left = left,
			Top = top,
			Width = width,
			Height = height
		};
		double num = transform?.M11 ?? 1.0;
		double num2 = transform?.M22 ?? 1.0;
		System.Windows.Point point = new System.Windows.Point(left / num, top / num2);
		System.Windows.Point point2 = new System.Windows.Point((left + width) / num, top / num2);
		System.Windows.Point point3 = new System.Windows.Point(left / num, (top + height) / num2);
		System.Windows.Point point4 = new System.Windows.Point((left + width) / num, (top + height) / num2);
		Rectangle[] array = new Rectangle[4]
		{
			Rectangle.Empty,
			Rectangle.Empty,
			Rectangle.Empty,
			Rectangle.Empty
		};
		Screen[] allScreens = Screen.AllScreens;
		for (int i = 0; i < allScreens.Length; i++)
		{
			Rectangle bounds = allScreens[i].Bounds;
			if (point.X <= (double)(bounds.X + bounds.Width) && point.X >= (double)bounds.X && point.Y <= (double)(bounds.Y + bounds.Height) && point.Y >= (double)bounds.Y)
			{
				array[0] = bounds;
			}
			if (point2.X <= (double)(bounds.X + bounds.Width) && point2.X >= (double)bounds.X && point2.Y <= (double)(bounds.Y + bounds.Height) && point2.Y >= (double)bounds.Y)
			{
				array[1] = bounds;
			}
			if (point3.X <= (double)(bounds.X + bounds.Width) && point3.X >= (double)bounds.X && point3.Y <= (double)(bounds.Y + bounds.Height) && point3.Y >= (double)bounds.Y)
			{
				array[2] = bounds;
			}
			if (point4.X <= (double)(bounds.X + bounds.Width) && point4.X >= (double)bounds.X && point4.Y <= (double)(bounds.Y + bounds.Height) && point4.Y >= (double)bounds.Y)
			{
				array[3] = bounds;
			}
		}
		if (array.All((Rectangle r) => r.IsEmpty))
		{
			widgetLocationModel.Left = 100.0;
			widgetLocationModel.Top = 100.0;
			widgetLocationModel.HideType = HideType.Reset;
			return widgetLocationModel;
		}
		if ((array[0].IsEmpty && array[1].IsEmpty) || Math.Abs(point.Y - (double)array[0].Y) < 1.0 || Math.Abs(point2.Y - (double)array[1].Y) < 1.0)
		{
			if (!array[2].IsEmpty && !array[2].Equals(array[3]))
			{
				widgetLocationModel.SetScreen(array[2], num, num2);
			}
			else if (array[2].IsEmpty && !array[3].IsEmpty)
			{
				widgetLocationModel.SetScreen(array[3], num, num2);
			}
			else
			{
				widgetLocationModel.SetScreen(array[2], num, num2);
			}
			widgetLocationModel.HideType = HideType.Top;
			return widgetLocationModel;
		}
		if ((array[0].IsEmpty && array[2].IsEmpty) || (Math.Abs(point.X - (double)array[0].X) < 1.0 && Math.Abs(point3.X - (double)array[2].X) < 1.0))
		{
			if (!array[1].IsEmpty && !array[1].Equals(array[3]))
			{
				widgetLocationModel.SetScreen(array[1], num, num2);
			}
			else if (array[1].IsEmpty && !array[3].IsEmpty)
			{
				widgetLocationModel.SetScreen(array[3], num, num2);
			}
			else
			{
				widgetLocationModel.SetScreen(array[1], num, num2);
			}
			widgetLocationModel.HideType = HideType.Left;
			return widgetLocationModel;
		}
		if ((array[1].IsEmpty && array[3].IsEmpty) || (Math.Abs(point2.X - (double)array[1].X - (double)array[1].Width) < 1.0 && Math.Abs(point4.X - (double)array[3].X - (double)array[3].Width) < 1.0))
		{
			if (!array[0].IsEmpty && !array[0].Equals(array[2]))
			{
				widgetLocationModel.SetScreen(array[0], num, num2);
			}
			else if (array[0].IsEmpty && !array[2].IsEmpty)
			{
				widgetLocationModel.SetScreen(array[2], num, num2);
			}
			else
			{
				widgetLocationModel.SetScreen(array[0], num, num2);
			}
			widgetLocationModel.HideType = HideType.Right;
			return widgetLocationModel;
		}
		if (array[0].Equals(array[1]))
		{
			widgetLocationModel.SetScreen(array[0], num, num2);
		}
		return widgetLocationModel;
	}

	public static System.Windows.Point GetPomoLocationSafely(double left, double top, double width, double height, Matrix? transform, System.Windows.Point defaultPoint)
	{
		System.Windows.Point result = new System.Windows.Point(left, top);
		double num = transform?.M11 ?? 1.0;
		double num2 = transform?.M22 ?? 1.0;
		System.Windows.Point point = new System.Windows.Point(left / num, top / num2);
		System.Windows.Point point2 = new System.Windows.Point((left + width) / num, top / num2);
		System.Windows.Point point3 = new System.Windows.Point(left / num, (top + height) / num2);
		System.Windows.Point point4 = new System.Windows.Point((left + width) / num, (top + height) / num2);
		Rectangle[] array = new Rectangle[4]
		{
			Rectangle.Empty,
			Rectangle.Empty,
			Rectangle.Empty,
			Rectangle.Empty
		};
		Screen[] allScreens = Screen.AllScreens;
		for (int i = 0; i < allScreens.Length; i++)
		{
			Rectangle bounds = allScreens[i].Bounds;
			if (point.X <= (double)(bounds.X + bounds.Width) && point.X >= (double)bounds.X && point.Y <= (double)(bounds.Y + bounds.Height) && point.Y >= (double)bounds.Y)
			{
				array[0] = bounds;
			}
			if (point2.X <= (double)(bounds.X + bounds.Width) && point2.X >= (double)bounds.X && point2.Y <= (double)(bounds.Y + bounds.Height) && point2.Y >= (double)bounds.Y)
			{
				array[1] = bounds;
			}
			if (point3.X <= (double)(bounds.X + bounds.Width) && point3.X >= (double)bounds.X && point3.Y <= (double)(bounds.Y + bounds.Height) && point3.Y >= (double)bounds.Y)
			{
				array[2] = bounds;
			}
			if (point4.X <= (double)(bounds.X + bounds.Width) && point4.X >= (double)bounds.X && point4.Y <= (double)(bounds.Y + bounds.Height) && point4.Y >= (double)bounds.Y)
			{
				array[3] = bounds;
			}
		}
		if (array.All((Rectangle r) => r.IsEmpty))
		{
			return defaultPoint;
		}
		if ((array[0].IsEmpty && array[1].IsEmpty) || Math.Abs(point.Y - (double)array[0].Y - (double)array[0].Width) < 1.0 || Math.Abs(point2.Y - (double)array[1].Y - (double)array[1].Width) < 1.0)
		{
			if (!array[2].IsEmpty && !array[2].Equals(array[3]))
			{
				result.X = (double)(array[2].X + array[2].Width) * num - width;
				result.Y = (double)array[2].Y * num2;
			}
			else if (array[2].IsEmpty && !array[3].IsEmpty)
			{
				result.X = (double)array[3].X * num;
				result.Y = (double)array[3].Y * num2;
			}
			else
			{
				result.X = left;
				result.Y = (double)array[2].Y * num2;
			}
			return result;
		}
		if ((array[2].IsEmpty && array[3].IsEmpty) || (Math.Abs(point3.Y - (double)array[2].Y) < 1.0 && Math.Abs(point4.Y - (double)array[3].Y) < 1.0))
		{
			if (!array[0].IsEmpty && !array[0].Equals(array[1]))
			{
				result.X = (double)(array[0].X + array[0].Width) * num - width;
				result.Y = (double)(array[0].Y + array[0].Height) * num2 - height - 20.0;
			}
			else if (array[0].IsEmpty && !array[1].IsEmpty)
			{
				result.X = (double)array[1].X * num;
				result.Y = (double)(array[1].Y + array[1].Height) * num2 - height - 20.0;
			}
			else
			{
				result.X = left;
				result.Y = (double)(array[0].Y + array[0].Height) * num2 - height - 20.0;
			}
			return result;
		}
		return result;
	}

	public static System.Windows.Point GetLocationWithinWorkingAreaSafely(double left, double top, double width, double height, Matrix? transform)
	{
		double validScale = GetValidScale(transform?.M11);
		double validScale2 = GetValidScale(transform?.M22);
		Rect windowBounds = new Rect(left / validScale, top / validScale2, width / validScale, height / validScale2);
		Screen[] allScreens = Screen.AllScreens;
		Rect[] workingAreas = allScreens.Select((Screen screen2) => ToWindowsRect(screen2.WorkingArea)).ToArray();
		Screen screen = Screen.PrimaryScreen ?? allScreens.FirstOrDefault();
		Rect fallbackWorkingArea = ((screen != null) ? ToWindowsRect(screen.WorkingArea) : new Rect(0.0, 0.0, Math.Max(windowBounds.Width, 1.0), Math.Max(windowBounds.Height, 1.0)));
		System.Windows.Point locationWithinWorkingAreas = GetLocationWithinWorkingAreas(windowBounds, workingAreas, fallbackWorkingArea);
		return new System.Windows.Point(locationWithinWorkingAreas.X * validScale, locationWithinWorkingAreas.Y * validScale2);
	}

	public static System.Windows.Point GetLocationWithinWorkingAreas(Rect windowBounds, IEnumerable<Rect> workingAreas, Rect fallbackWorkingArea)
	{
		List<Rect> list = (workingAreas ?? Enumerable.Empty<Rect>()).Where(IsUsableRect).ToList();
		if (!IsUsableRect(fallbackWorkingArea))
		{
			fallbackWorkingArea = list.FirstOrDefault();
		}
		if (!IsUsableRect(fallbackWorkingArea))
		{
			fallbackWorkingArea = new Rect(0.0, 0.0, 1.0, 1.0);
		}
		if (list.Count == 0)
		{
			list.Add(fallbackWorkingArea);
		}
		double num = (IsPositiveFinite(windowBounds.Width) ? windowBounds.Width : 1.0);
		double num2 = (IsPositiveFinite(windowBounds.Height) ? windowBounds.Height : 1.0);
		int num3;
		Rect rect;
		if (IsFinite(windowBounds.X))
		{
			num3 = (IsFinite(windowBounds.Y) ? 1 : 0);
			if (num3 != 0)
			{
				rect = GetBestWorkingArea(new Rect(windowBounds.X, windowBounds.Y, num, num2), list);
				goto IL_00f3;
			}
		}
		else
		{
			num3 = 0;
		}
		rect = fallbackWorkingArea;
		goto IL_00f3;
		IL_00f3:
		Rect rect2 = rect;
		double val = ((num3 != 0) ? windowBounds.X : (rect2.Left + Math.Max(0.0, (rect2.Width - num) / 2.0)));
		double val2 = ((num3 != 0) ? windowBounds.Y : (rect2.Top + Math.Max(0.0, (rect2.Height - num2) / 2.0)));
		val = ((num <= rect2.Width) ? Math.Max(rect2.Left, Math.Min(val, rect2.Right - num)) : rect2.Left);
		val2 = ((num2 <= rect2.Height) ? Math.Max(rect2.Top, Math.Min(val2, rect2.Bottom - num2)) : rect2.Top);
		return new System.Windows.Point(val, val2);
	}

	private static Rect GetBestWorkingArea(Rect windowBounds, IList<Rect> areas)
	{
		Rect result = areas[0];
		double num = -1.0;
		double num2 = double.MaxValue;
		foreach (Rect area in areas)
		{
			Rect rect = Rect.Intersect(windowBounds, area);
			double num3 = (rect.IsEmpty ? 0.0 : (rect.Width * rect.Height));
			double distanceSquared = GetDistanceSquared(windowBounds, area);
			if (num3 > num || (Math.Abs(num3 - num) < 0.01 && distanceSquared < num2))
			{
				result = area;
				num = num3;
				num2 = distanceSquared;
			}
		}
		return result;
	}

	private static double GetDistanceSquared(Rect windowBounds, Rect area)
	{
		double num = windowBounds.Left + windowBounds.Width / 2.0;
		double num2 = windowBounds.Top + windowBounds.Height / 2.0;
		double num3 = ((num < area.Left) ? (area.Left - num) : ((num > area.Right) ? (num - area.Right) : 0.0));
		double num4 = ((num2 < area.Top) ? (area.Top - num2) : ((num2 > area.Bottom) ? (num2 - area.Bottom) : 0.0));
		return num3 * num3 + num4 * num4;
	}

	private static Rect ToWindowsRect(Rectangle rectangle)
	{
		return new Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
	}

	private static bool IsUsableRect(Rect rect)
	{
		if (IsFinite(rect.X) && IsFinite(rect.Y) && IsPositiveFinite(rect.Width))
		{
			return IsPositiveFinite(rect.Height);
		}
		return false;
	}

	private static double GetValidScale(double? scale)
	{
		if (!scale.HasValue || !IsPositiveFinite(scale.Value))
		{
			return 1.0;
		}
		return scale.Value;
	}

	private static bool IsPositiveFinite(double value)
	{
		if (value > 0.0)
		{
			return IsFinite(value);
		}
		return false;
	}

	private static bool IsFinite(double value)
	{
		if (!double.IsNaN(value))
		{
			return !double.IsInfinity(value);
		}
		return false;
	}
}
