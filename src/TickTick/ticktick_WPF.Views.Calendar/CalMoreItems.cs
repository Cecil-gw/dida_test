using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Calendar;

public class CalMoreItems
{
	private CustomMenuList _moreMenu;

	public event EventHandler<string> Action;

	public event WeekMonthSwitch.SwitchModeDelegate SwitchMode;

	public CalMoreItems(UIElement ui, bool showClose, bool showAddTask, string switchMode, bool showPrint)
	{
		EscPopup popup = new EscPopup
		{
			StaysOpen = false,
			PopupAnimation = PopupAnimation.Fade,
			Placement = PlacementMode.Left,
			PlacementTarget = ui,
			HorizontalOffset = 44.0,
			VerticalOffset = 24.0
		};
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("DisplaySetting", Utils.GetString("DisplaySetting"), Utils.GetGeometry("Ic.CalendarOM.ViewSetting")),
			new CustomMenuItemViewModel("ArrangeTask", Utils.GetString("ArrangeTask"), Utils.GetGeometry("Ic.CalendarOM.CalendarFreeTasks")),
			new CustomMenuItemViewModel("Subscribe", Utils.GetString("SubscribeCalendar"), Utils.GetGeometry("Ic.CalendarOM.CalendarSubscribe"))
			{
				ImageMargin = new Thickness(12.0, 2.0, 0.0, 0.0)
			}
		};
		if (showPrint)
		{
			list.Add(new CustomMenuItemViewModel("Print", Utils.GetString("Print"), Utils.GetGeometry("Ic.CalendarOM.Print"))
			{
				SubActions = new List<CustomMenuItemViewModel>
				{
					new CustomMenuItemViewModel((object)"PrintCalendar", Utils.GetString("PrintCalendar"), (Geometry)null, ""),
					new CustomMenuItemViewModel((object)"PrintDetail", Utils.GetString("PrintDetail"), (Geometry)null, "")
				},
				ShowSelected = false
			});
		}
		if (showAddTask)
		{
			list.Insert(0, new CustomMenuItemViewModel("AddTask", Utils.GetString("AddTaskTitle"), Utils.GetGeometry("Ic.CalendarOM.Add")));
			list.Insert(1, new CustomMenuItemViewModel(null));
		}
		if (showClose)
		{
			list.Add(new CustomMenuItemViewModel("ExitParallel", Utils.GetString("Close"), Utils.GetGeometry("Ic.Common.Close")));
		}
		WeekMonthSwitchContent weekMonthSwitchContent = (string.IsNullOrEmpty(switchMode) ? null : new WeekMonthSwitchContent(moreMode: true));
		if (weekMonthSwitchContent != null)
		{
			weekMonthSwitchContent.InitSelectMode(switchMode);
			weekMonthSwitchContent.SwitchMode += OnSwitchMode;
			list.Insert(0, new CustomMenuItemViewModel(null));
		}
		_moreMenu = new CustomMenuList(list, popup, weekMonthSwitchContent);
		_moreMenu.Operated += OnSelected;
	}

	private void OnSwitchMode(string from, string to, bool closePopup)
	{
		if (closePopup)
		{
			_moreMenu?.Close();
		}
		this.SwitchMode?.Invoke(from, to, closePopup);
	}

	public void Show()
	{
		_moreMenu.Show();
	}

	private void OnSelected(object sender, object e)
	{
		switch (e as string)
		{
		case "ArrangeTask":
			this.Action?.Invoke(this, "ArrangeTask");
			break;
		case "DisplaySetting":
			this.Action?.Invoke(this, "DisplaySetting");
			break;
		case "Subscribe":
			this.Action?.Invoke(this, "Subscribe");
			break;
		case "PrintCalendar":
			this.Action?.Invoke(this, "Print");
			UserActCollectUtils.AddClickEvent("calendar", "toolbar", "print");
			break;
		case "PrintDetail":
			this.Action?.Invoke(this, "PrintDetail");
			UserActCollectUtils.AddClickEvent("calendar", "toolbar", "print");
			break;
		default:
			this.Action?.Invoke(this, e as string);
			break;
		}
	}
}
