using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Widget;

public class WidgetMorePopup
{
	private EscPopup _popup;

	private Action _closeAction;

	private readonly bool _enable = true;

	public event EventHandler<WidgetMoreAction> MoreAction;

	public event EventHandler<string> Action;

	public WidgetMorePopup()
	{
		_popup = new EscPopup
		{
			StaysOpen = false,
			PopupAnimation = PopupAnimation.Fade,
			Placement = PlacementMode.Mouse
		};
		_popup.Closed += delegate
		{
			_closeAction?.Invoke();
			PopupStateManager.OnViewPopupClosed();
		};
	}

	public WidgetMorePopup(Action closeAction, bool enable = true)
		: this()
	{
		_closeAction = closeAction;
		_enable = enable;
	}

	public void Show(Point point, WidgetType widgetType)
	{
		_popup.Placement = PlacementMode.Bottom;
		_popup.HorizontalOffset = point.X;
		_popup.VerticalOffset = point.Y;
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
		if (!_enable)
		{
			list = new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel("Settings", Utils.GetString("WidgetSettings"), Utils.GetGeometry("Ic.Tabbar.SettingLinear"))
				{
					ShowSelected = false
				},
				new CustomMenuItemViewModel("Close", Utils.GetString("Close"), Utils.GetGeometry("Ic.Common.Power"))
				{
					ShowSelected = false
				}
			};
		}
		else
		{
			switch (widgetType)
			{
			case WidgetType.Calendar:
				list = new List<CustomMenuItemViewModel>
				{
					new CustomMenuItemViewModel("DisplaySetting", Utils.GetString("DisplaySetting"), Utils.GetGeometry("Ic.TaskOM.ViewSetting"))
					{
						ShowSelected = false
					},
					new CustomMenuItemViewModel("ArrangeTask", Utils.GetString("ArrangeTask"), Utils.GetGeometry("Ic.CalendarOM.CalendarFreeTasks"))
					{
						ShowSelected = false
					},
					new CustomMenuItemViewModel("Sync", Utils.GetString("Sync"), Utils.GetGeometry("Ic.Tabbar.Sync"))
					{
						ShowSelected = false
					}
				};
				break;
			case WidgetType.List:
			case WidgetType.Matrix:
				list = new List<CustomMenuItemViewModel>
				{
					new CustomMenuItemViewModel("Sync", Utils.GetString("Sync"), Utils.GetGeometry("Ic.Tabbar.Sync"))
					{
						ShowSelected = false
					}
				};
				break;
			}
			list.AddRange(new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel("Settings", Utils.GetString("WidgetSettings"), Utils.GetGeometry("Ic.Tabbar.SettingLinear"))
				{
					ShowSelected = false
				},
				new CustomMenuItemViewModel("Lock", Utils.GetString("Lock"), Utils.GetGeometry("Ic.Common.Lock"))
				{
					ShowSelected = false
				},
				new CustomMenuItemViewModel("Close", Utils.GetString("Close"), Utils.GetGeometry("Ic.Common.Power"))
				{
					ShowSelected = false
				}
			});
		}
		CustomMenuList customMenuList = new CustomMenuList(list, _popup);
		customMenuList.Operated += OnItemSelected;
		customMenuList.Show();
		PopupStateManager.OnViewPopupOpened();
	}

	private void OnItemSelected(object sender, object e)
	{
		PopupStateManager.OnViewPopupClosed();
		switch (e as string)
		{
		case "Sync":
			this.MoreAction?.Invoke(this, WidgetMoreAction.Sync);
			break;
		case "Settings":
			this.MoreAction?.Invoke(this, WidgetMoreAction.Setting);
			break;
		case "Lock":
			this.MoreAction?.Invoke(this, WidgetMoreAction.Lock);
			break;
		case "Close":
			this.MoreAction?.Invoke(this, WidgetMoreAction.Exit);
			break;
		default:
			this.Action?.Invoke(this, e as string);
			break;
		}
	}

	public void SetResources(ResourceDictionary resources)
	{
		_popup.Resources = resources;
	}

	public void SetPlaceTarget(UIElement moreButton)
	{
		_popup.PlacementTarget = moreButton;
	}
}
