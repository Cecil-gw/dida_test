using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Views.Calendar;

public class TimelineArrangeFilterPopup
{
	private readonly TimelineViewModel _model;

	private EscPopup _popup;

	public TimelineArrangeFilterPopup(TimelineViewModel model, FrameworkElement target)
	{
		_model = model;
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
	}

	public void Show()
	{
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("overdue", Utils.GetString("OverdueTask"), Utils.GetGeometry("Ic.CalendarOM.Expired"))
			{
				Selected = _model.IsOverDue,
				ImageMargin = new Thickness(11.0, 0.0, 2.0, 0.0)
			},
			new CustomMenuItemViewModel("undated", Utils.GetString("NoDate"), Utils.GetGeometry("Ic.CalendarOM.NoDate"))
			{
				Selected = !_model.IsOverDue,
				ImageMargin = new Thickness(11.0, 0.0, 2.0, 0.0)
			},
			new CustomMenuItemViewModel(null),
			new CustomMenuItemViewModel("showNote", Utils.GetString(_model.ShowNoteInArrange ? "HideNote" : "ShowNote"), Utils.GetGeometry(_model.ShowNoteInArrange ? "Ic.TaskOM.HideNote" : "Ic.TaskOM.ShowNote"))
			{
				ShowSelected = false,
				ImageMargin = new Thickness(11.0, 0.0, 2.0, 0.0)
			},
			new CustomMenuItemViewModel("showParent", Utils.GetString(_model.ShowParentInArrange ? "HideParent" : "ShowParent"), Utils.GetGeometry(_model.ShowParentInArrange ? "Ic.CalendarOM.HideTaskLevel" : "Ic.CalendarOM.ShowTaskLevel"))
			{
				ShowSelected = false,
				ImageMargin = new Thickness(11.0, 0.0, 2.0, 0.0)
			}
		}, _popup);
		TextBlock textBlock = new TextBlock
		{
			Margin = new Thickness(12.0, -2.0, 0.0, 4.0)
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
			_model.ShowNoteInArrange = !_model.ShowNoteInArrange;
			break;
		case "showParent":
			_model.ShowParentInArrange = !_model.ShowParentInArrange;
			break;
		default:
			UserActCollectUtils.AddClickEvent("calendar", "arrangement_show", text);
			_model.IsOverDue = text == "overdue";
			break;
		}
	}
}
