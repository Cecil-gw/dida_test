using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Calendar;

public class CalArrangeDateFilterPopup
{
	private EscPopup _popup;

	public Action OnClosed { get; set; }

	public CalArrangeDateFilterPopup(FrameworkElement target)
	{
		_popup = new EscPopup
		{
			StaysOpen = false,
			PopupAnimation = PopupAnimation.Fade,
			Placement = ((target == null) ? PlacementMode.Mouse : PlacementMode.Bottom),
			PlacementTarget = target,
			VerticalOffset = -5.0,
			HorizontalOffset = -140.0,
			MinWidth = 180.0
		};
		_popup.Closed += _popup_Closed;
	}

	private void _popup_Closed(object sender, EventArgs e)
	{
		OnClosed?.Invoke();
	}

	public void UnRegistEvent()
	{
		_popup.Closed -= _popup_Closed;
	}

	public void Show()
	{
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("overdue", Utils.GetString("OverdueTask"), Utils.GetGeometry("Ic.CalendarOM.Expired"))
			{
				Selected = (LocalSettings.Settings.ArrangeTaskDateType == 1),
				ImageMargin = new Thickness(11.0, 0.0, 2.0, 0.0)
			},
			new CustomMenuItemViewModel("undated", Utils.GetString("NoDate"), Utils.GetGeometry("Ic.CalendarOM.NoDate"))
			{
				Selected = (LocalSettings.Settings.ArrangeTaskDateType == 0),
				ImageMargin = new Thickness(11.0, 0.0, 2.0, 0.0)
			},
			new CustomMenuItemViewModel(null),
			new CustomMenuItemViewModel("showNote", Utils.GetString(LocalSettings.Settings.ExtraSettings.ShowNoteInCalArrange ? "HideNote" : "ShowNote"), Utils.GetGeometry(LocalSettings.Settings.ExtraSettings.ShowNoteInCalArrange ? "Ic.TaskOM.HideNote" : "Ic.TaskOM.ShowNote"))
			{
				ShowSelected = false,
				ImageMargin = new Thickness(11.0, 0.0, 2.0, 0.0)
			},
			new CustomMenuItemViewModel("showParent", Utils.GetString(LocalSettings.Settings.ExtraSettings.ShowParentInCalArrange ? "HideParent" : "ShowParent"), Utils.GetGeometry(LocalSettings.Settings.ExtraSettings.ShowParentInCalArrange ? "Ic.CalendarOM.HideTaskLevel" : "Ic.CalendarOM.ShowTaskLevel"))
			{
				ShowSelected = false,
				ImageMargin = new Thickness(11.0, 0.0, 2.0, 0.0)
			}
		}, _popup);
		TextBlock textBlock = new TextBlock
		{
			Margin = new Thickness(12.0, 4.0, 0.0, 4.0)
		};
		textBlock.SetResourceReference(TextBlock.TextProperty, "Show");
		textBlock.SetResourceReference(FrameworkElement.StyleProperty, "Tag03");
		customMenuList.TopItem.Content = textBlock;
		customMenuList.Operated += OnDateSelected;
		customMenuList.Show();
	}

	private void OnDateSelected(object sender, object e)
	{
		string text = e as string;
		switch (text)
		{
		case "showNote":
			LocalSettings.Settings.ExtraSettings.ShowNoteInCalArrange = !LocalSettings.Settings.ExtraSettings.ShowNoteInCalArrange;
			break;
		case "showParent":
			LocalSettings.Settings.ExtraSettings.ShowParentInCalArrange = !LocalSettings.Settings.ExtraSettings.ShowParentInCalArrange;
			break;
		default:
			UserActCollectUtils.AddClickEvent("calendar", "arrangement_show", text);
			LocalSettings.Settings.ArrangeTaskDateType = ((text == "overdue") ? 1 : 0);
			break;
		}
		GlobalEventManager.NotifyArrangeTaskDateTypeChanged();
	}
}
