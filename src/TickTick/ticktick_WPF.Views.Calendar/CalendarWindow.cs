using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.TouchPad;

namespace ticktick_WPF.Views.Calendar;

public class CalendarWindow : IndependentWindow
{
	public new static CalendarWindow Window;

	public new static string Name = DisplayModule.Calendar.ToString();

	private CalendarControl _calendar;

	public static bool IsShowing => Window != null;

	private CalendarWindow(WindowModel windowModel)
		: base(windowModel)
	{
		base.MinWidth = 580.0;
		base.MinHeight = 462.0;
		base.KeyUp += OnWindowKeyUp;
		Id = Name;
		_calendar = new CalendarControl(CalendarViewSourceType.ExtraWindow);
		Container.Children.Add(_calendar);
		base.Title = Utils.GetString("Calendar");
		Container.SetResourceReference(Control.BackgroundProperty, "ShowAreaBottomColor");
		base.Loaded += OnWindowLoaded;
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
		{
			hwndSource.AddHook(Hook);
			if (TouchPadHelper.TouchpadHelper.Exists())
			{
				TouchPadHelper.TouchpadHelper.RegisterInput(hwndSource.Handle);
			}
		}
	}

	private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		switch (msg)
		{
		case 255:
		{
			TouchpadContact[] array = TouchPadHelper.TouchpadHelper.ParseInput(lParam);
			if (array != null && array.Any((TouchpadContact c) => c.ContactId == 1))
			{
				OnDoubleFingerTouch();
			}
			break;
		}
		case 526:
		{
			int num = 0;
			num = LOWORD(wParam);
			OnMouseTilt(num);
			return (IntPtr)1;
		}
		}
		return IntPtr.Zero;
	}

	private void OnDoubleFingerTouch()
	{
		_calendar?.OnDoubleFingerTouch();
	}

	private static int HIWORD(IntPtr ptr)
	{
		return (ptr.ToInt32() >> 16) & 0xFFFF;
	}

	private static int LOWORD(IntPtr ptr)
	{
		return ((int)ptr.ToInt64() >> 16) % 256;
	}

	private void OnMouseTilt(int offset)
	{
		if (base.IsActive)
		{
			_calendar?.OnTouchScroll(offset);
		}
	}

	private void OnWindowKeyUp(object sender, KeyEventArgs e)
	{
		_calendar?.OnKeyUp(sender, e);
	}

	public static async Task ShowWindow(bool force = true)
	{
		WindowModel windowModel = await WindowModelDao.GetModelByIdAsync(Name);
		if (!force && (windowModel == null || windowModel.Closed))
		{
			return;
		}
		if (Window != null)
		{
			Window.Show();
			Window.Activate();
			Window.Reload();
			return;
		}
		if (windowModel == null || windowModel.Closed)
		{
			IndependentWindow.AddShowEvent("calendar");
		}
		if (windowModel != null)
		{
			await WindowModelDao.OpenWindow(windowModel);
		}
		else
		{
			windowModel = await IndependentWindow.GetNewWindowModel(Name);
		}
		Window = new CalendarWindow(windowModel);
		Window.Show();
		Window.SetBackBrush();
	}

	private void Reload()
	{
		_calendar?.Reload();
	}

	protected override void OnClosing()
	{
		Container.Children.Clear();
		Window = null;
	}

	protected override void ClearCopiedTaskSelectionOnWindowMinimized()
	{
		_calendar?.ClearCopiedTaskSelection();
	}

	public override void Print()
	{
		_calendar?.Print(isAll: false);
	}

	public static void OpenOrCloseWindow(Window window)
	{
		if (Window != null)
		{
			Window.Close();
		}
		else if (ProChecker.CheckPro(ProType.CalendarView) && IndependentWindow.CheckCount(window))
		{
			ShowWindow();
		}
	}

	public static void CheckProEnable()
	{
		if (Window != null && !UserDao.IsPro())
		{
			Window.Close();
		}
	}
}
