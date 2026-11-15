using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using TickTickUtils.Lang;
using ticktick_WPF.Util;

internal class PopupLocationInfo
{
	private bool _enable = true;

	private bool _popupRight = true;

	private Popup _popup;

	private Point Trigger { get; set; }

	private Point Top { get; set; }

	private Point Bottom { get; set; }

	public bool IsSafeShowing()
	{
		if (_enable && _popup != null && _popup.IsOpen)
		{
			return IsInSafeArea();
		}
		return false;
	}

	public async void Mark()
	{
		if (Utils.IsWindows7())
		{
			_enable = false;
			return;
		}
		Point mousePoint = PopupLocationCalculator.GetMousePoint(isDefault: false);
		if (Trigger != default(Point) && ((mousePoint.X < Trigger.X && _popupRight) || (mousePoint.X > Trigger.X && !_popupRight)))
		{
			_enable = false;
		}
		else
		{
			Trigger = mousePoint;
		}
	}

	public bool IsInSafeArea()
	{
		if (!_enable)
		{
			return false;
		}
		return Utils.IsPointInTriangle(PopupLocationCalculator.GetMousePoint(isDefault: false), Trigger, Top, Bottom);
	}

	public async Task Bind(Popup popup, bool delay = false)
	{
		try
		{
			_enable = true;
			if (Utils.IsWindows7())
			{
				_enable = false;
				return;
			}
			_popup = popup;
			if (_popup?.Child == null)
			{
				_enable = false;
				return;
			}
			Trigger = PopupLocationCalculator.GetMousePoint(isDefault: false);
			if (delay)
			{
				await Task.Delay(80);
			}
			Point point = popup.Child.PointToScreen(new Point(0.0, 0.0));
			Point bottom = popup.Child.PointToScreen(new Point(0.0, 0.0));
			double pointDpi = ScreenPositionUtils.GetPointDpi(point);
			bottom.Y += popup.Child.RenderSize.Height * pointDpi;
			if (Trigger.X < point.X)
			{
				Top = popup.Child.PointToScreen(new Point(0.0, 0.0));
				Bottom = bottom;
				_popupRight = true;
			}
			else
			{
				Top = new Point(point.X + popup.Child.RenderSize.Width, point.Y);
				Bottom = new Point(bottom.X + popup.Child.RenderSize.Width, bottom.Y);
				_popupRight = false;
			}
		}
		catch (Exception ex)
		{
			UtilLog.Warn("PopupLocationInfo Init error" + ex);
		}
	}
}
