using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace ticktick_WPF.Util;

public static class PopupLocationCalculator
{
	private enum Alignment
	{
		Vertical,
		Horizontal
	}

	private enum AlignDirection
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	public static System.Windows.Point GetMousePoint(bool isDefault)
	{
		if (isDefault)
		{
			return default(System.Windows.Point);
		}
		System.Drawing.Point mousePosition = Control.MousePosition;
		return new System.Windows.Point(mousePosition.X, mousePosition.Y);
	}

	public static System.Windows.Point GetTaskPopupLocation(UIElement target, double targetWidth, double width, System.Windows.Point point, double addHeight)
	{
		bool flag = point != default(System.Windows.Point);
		if (flag)
		{
			targetWidth = 0.0;
		}
		else
		{
			try
			{
				point = target.PointToScreen(new System.Windows.Point(0.0, 0.0));
			}
			catch (Exception)
			{
				flag = true;
			}
		}
		if (flag)
		{
			System.Drawing.Point mousePosition = Control.MousePosition;
			point = new System.Windows.Point(mousePosition.X, mousePosition.Y);
			targetWidth = 0.0;
		}
		try
		{
			CompositionTarget compositionTarget = PresentationSource.FromVisual(target)?.CompositionTarget;
			if (compositionTarget != null)
			{
				Matrix transformFromDevice = compositionTarget.TransformFromDevice;
				if (addHeight != 0.0)
				{
					addHeight /= transformFromDevice.M22;
				}
				Rectangle pointLocationScreen = ScreenPositionUtils.GetPointLocationScreen(point, new System.Windows.Point(point.X + targetWidth, point.Y + addHeight));
				point = transformFromDevice.Transform(point);
				System.Windows.Point point2 = transformFromDevice.Transform(new System.Windows.Point(pointLocationScreen.X, pointLocationScreen.Y));
				System.Windows.Point point3 = transformFromDevice.Transform(new System.Windows.Point(pointLocationScreen.Width, pointLocationScreen.Height));
				pointLocationScreen = new Rectangle((int)point2.X, (int)point2.Y, (int)point3.X, (int)point3.Y);
				if (point.X + targetWidth + width - 10.0 > (double)(pointLocationScreen.X + pointLocationScreen.Width))
				{
					return new System.Windows.Point(point.X - width + (double)(flag ? (-10) : 4), point.Y - (double)((!flag) ? 10 : 0));
				}
			}
			return new System.Windows.Point(point.X + targetWidth - (double)(flag ? (-10) : 10), point.Y - (double)((!flag) ? 10 : 0));
		}
		catch (Exception)
		{
			return point;
		}
	}

	public static TaskPopupArgs GetPopupLocation(UIElement target, double targetWidth, double width, bool byMouse, double addHeight)
	{
		TaskPopupArgs result = new TaskPopupArgs
		{
			IsRight = true,
			ByMouse = byMouse
		};
		if (byMouse)
		{
			return result;
		}
		System.Windows.Point point;
		try
		{
			point = target.PointToScreen(new System.Windows.Point(0.0, 0.0));
		}
		catch (Exception)
		{
			result.ByMouse = true;
			return result;
		}
		try
		{
			CompositionTarget compositionTarget = PresentationSource.FromVisual(target)?.CompositionTarget;
			if (compositionTarget != null)
			{
				Matrix transformFromDevice = compositionTarget.TransformFromDevice;
				if (addHeight != 0.0)
				{
					addHeight /= transformFromDevice.M22;
				}
				Rectangle pointLocationScreen = ScreenPositionUtils.GetPointLocationScreen(point, new System.Windows.Point(point.X + targetWidth, point.Y + addHeight));
				point = transformFromDevice.Transform(point);
				System.Windows.Point point2 = transformFromDevice.Transform(new System.Windows.Point(pointLocationScreen.X, pointLocationScreen.Y));
				System.Windows.Point point3 = transformFromDevice.Transform(new System.Windows.Point(pointLocationScreen.Width, pointLocationScreen.Height));
				pointLocationScreen = new Rectangle((int)point2.X, (int)point2.Y, (int)point3.X, (int)point3.Y);
				width = (double.IsNaN(width) ? 0.0 : width);
				if (point.X + targetWidth + width - 10.0 > (double)(pointLocationScreen.X + pointLocationScreen.Width))
				{
					result.IsRight = false;
				}
			}
		}
		catch (Exception)
		{
		}
		return result;
	}

	public static double FixVerticalLocation(double left, double top, double height, UIElement element)
	{
		if (element != null)
		{
			Matrix? matrix = (PresentationSource.FromVisual(element)?.CompositionTarget)?.TransformFromDevice;
			Rectangle rectangle = ScreenPositionUtils.GetPointLocationScreen(new System.Windows.Point(left / (matrix?.M11 ?? 1.0), top / (matrix?.M22 ?? 1.0)), null);
			if (matrix.HasValue)
			{
				System.Windows.Point point = matrix.Value.Transform(new System.Windows.Point(rectangle.X, rectangle.Y));
				System.Windows.Point point2 = matrix.Value.Transform(new System.Windows.Point(rectangle.Width, rectangle.Height));
				rectangle = new Rectangle((int)point.X, (int)point.Y, (int)point2.X, (int)point2.Y);
			}
			if (top + height > (double)(rectangle.Y + rectangle.Height))
			{
				return (double)(rectangle.Y + rectangle.Height) - height;
			}
		}
		return top;
	}

	public static System.Windows.Point GetPopupLocation(Window context, double width, double height, double leftMargin = 0.0, double rightMargin = 0.0, double topMargin = 0.0)
	{
		System.Drawing.Point mousePosition = Control.MousePosition;
		System.Windows.Point point = new System.Windows.Point(mousePosition.X, mousePosition.Y);
		CompositionTarget compositionTarget = PresentationSource.FromVisual(context)?.CompositionTarget;
		if (compositionTarget != null)
		{
			System.Windows.Point point2 = compositionTarget.TransformFromDevice.Transform(point);
			AlignDirection alignDirection = CalculateDirection(point2.X, point2.Y, width, height, leftMargin, rightMargin);
			double num = point2.X;
			double num2 = point2.Y;
			switch (alignDirection)
			{
			case AlignDirection.TopLeft:
				num = num - width + 10.0 - leftMargin;
				num2 = num2 - height + 10.0 + 10.0;
				break;
			case AlignDirection.TopRight:
				num = num - 10.0 + rightMargin;
				num2 = num2 - height + 10.0 + 10.0;
				break;
			case AlignDirection.BottomLeft:
				num = num - width + 10.0 - leftMargin;
				num2 = num2 - 10.0 - 10.0 - topMargin;
				break;
			case AlignDirection.BottomRight:
				num = num - 10.0 + rightMargin;
				num2 = num2 - 10.0 - 10.0 - topMargin;
				break;
			}
			return new System.Windows.Point(num, num2);
		}
		return new System.Windows.Point(mousePosition.X, mousePosition.Y);
	}

	private static Alignment CalculateAlignment(double virtualWidth, double virtualHeight, double primaryWidth, double primaryHeight)
	{
		if (virtualHeight == primaryHeight && virtualWidth > primaryWidth)
		{
			return Alignment.Horizontal;
		}
		if (virtualWidth == primaryWidth && virtualHeight > primaryHeight)
		{
			return Alignment.Vertical;
		}
		return Alignment.Horizontal;
	}

	private static AlignDirection CalculateDirection(double x, double y, double width, double height, double lm, double rm)
	{
		double virtualScreenWidth = SystemParameters.VirtualScreenWidth;
		double virtualScreenHeight = SystemParameters.VirtualScreenHeight;
		double primaryScreenWidth = SystemParameters.PrimaryScreenWidth;
		double primaryScreenHeight = SystemParameters.PrimaryScreenHeight;
		return CalculateAlignment(virtualScreenWidth, virtualScreenHeight, primaryScreenWidth, primaryScreenHeight) switch
		{
			Alignment.Horizontal => CalculateHorizontalDirection(x, y, width, height, virtualScreenWidth, virtualScreenHeight, primaryScreenWidth, primaryScreenHeight, lm, rm), 
			Alignment.Vertical => CalculateVerticalDirection(x, y, width, height, virtualScreenWidth, virtualScreenHeight, primaryScreenWidth, primaryScreenHeight, lm, rm), 
			_ => AlignDirection.BottomRight, 
		};
	}

	private static AlignDirection CalculateHorizontalDirection(double x, double y, double width, double height, double vw, double vh, double pw, double ph, double lm, double rm)
	{
		double num = vw;
		if (x >= 0.0 && x <= pw)
		{
			num = pw;
		}
		bool left = x + width + rm > num;
		bool top = y + height > Math.Min(vh, ph);
		return GetAlignDirection(left, top);
	}

	private static AlignDirection CalculateVerticalDirection(double x, double y, double width, double height, double vw, double vh, double pw, double ph, double lm, double rm)
	{
		double num = vh;
		if (y >= 0.0 && y <= ph)
		{
			num = ph;
		}
		bool top = y + height > num;
		return GetAlignDirection(x + width + rm > Math.Min(vw, pw), top);
	}

	private static AlignDirection GetAlignDirection(bool left, bool top)
	{
		if (left && !top)
		{
			return AlignDirection.BottomLeft;
		}
		if (!left && !top)
		{
			return AlignDirection.BottomRight;
		}
		if (left)
		{
			return AlignDirection.TopLeft;
		}
		return AlignDirection.TopRight;
	}

	public static System.Windows.Point GetTimeDialogLocation(UIElement target, double width, double height, double hOffset, double vOffset)
	{
		System.Windows.Point point;
		if (target == null)
		{
			System.Drawing.Point mousePosition = Control.MousePosition;
			point = new System.Windows.Point(mousePosition.X, mousePosition.Y);
		}
		else
		{
			try
			{
				point = target.PointToScreen(new System.Windows.Point(0.0, 0.0));
			}
			catch (Exception)
			{
				System.Drawing.Point mousePosition2 = Control.MousePosition;
				point = new System.Windows.Point(mousePosition2.X, mousePosition2.Y);
			}
		}
		try
		{
			Rectangle rectangle = ScreenPositionUtils.GetPointLocationScreen(point, new System.Windows.Point(point.X, point.Y));
			CompositionTarget compositionTarget = PresentationSource.FromVisual(target ?? App.Window)?.CompositionTarget;
			if (compositionTarget != null)
			{
				Matrix transformFromDevice = compositionTarget.TransformFromDevice;
				point = transformFromDevice.Transform(point);
				System.Windows.Point point2 = transformFromDevice.Transform(new System.Windows.Point(rectangle.X, rectangle.Y));
				System.Windows.Point point3 = transformFromDevice.Transform(new System.Windows.Point(rectangle.Width, rectangle.Height));
				rectangle = new Rectangle((int)point2.X, (int)point2.Y, (int)point3.X, (int)point3.Y);
			}
			double x = ((!(point.X + hOffset + width + 10.0 > (double)(rectangle.X + rectangle.Width))) ? (point.X + hOffset) : ((double)(rectangle.X + rectangle.Width) - width - 10.0));
			double y = ((!(point.Y + vOffset + height + 10.0 > (double)(rectangle.Y + rectangle.Height))) ? (point.Y + vOffset) : ((double)(rectangle.Y + rectangle.Height) - height - 10.0));
			return new System.Windows.Point(x, y);
		}
		catch (Exception)
		{
			return point;
		}
	}
}
