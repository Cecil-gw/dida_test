using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Config;

public class EditBindAccountWindow : Window, IComponentConnector
{
	internal Grid Container;

	internal Border ExpireIcon;

	internal Path More;

	private bool _contentLoaded;

	private string rawDisplayName { get; set; }

	public static async Task<EditBindAccountWindow> Build(string calendarId)
	{
		if (!string.IsNullOrEmpty(calendarId))
		{
			BindCalendarAccountModel bindCalendarAccountModel = await BindCalendarAccountDao.GetBindCalendarAccount(calendarId);
			if (bindCalendarAccountModel != null)
			{
				return new EditBindAccountWindow(BindAccountViewModel.Build(bindCalendarAccountModel));
			}
		}
		return null;
	}

	public EditBindAccountWindow(BindAccountViewModel model)
	{
		InitializeComponent();
		base.DataContext = model;
		rawDisplayName = model.DisplayName;
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.Owner?.Activate();
		base.OnClosing(e);
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		if (base.DataContext != null)
		{
			object dataContext = base.DataContext;
			if (dataContext is BindAccountViewModel model)
			{
				foreach (BindCalendarViewModel cal in model.Calendars)
				{
					string status = (cal.StatusItems.FirstOrDefault((ComboBoxViewModel i) => i.Selected)?.Value as string) ?? "show";
					if (await BindCalendarAccountDao.SaveCalendarShowStatus(cal.Id, status))
					{
						Communicator.CalendarSetVisible(model.Id, cal.Id, status);
					}
				}
				if (model.DisplayName != rawDisplayName)
				{
					await BindCalendarAccountDao.UpdateDisplayName(model.Id, model.DisplayName);
				}
			}
		}
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void OnUnsubscribeClicked()
	{
		if (base.DataContext is BindAccountViewModel bindAccountViewModel)
		{
			await SubscribeCalendarHelper.UnbindCalendar(bindAccountViewModel.Id);
			Close();
		}
	}

	private void OnDragMove(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	private void MoreClick(object sender, MouseButtonEventArgs e)
	{
		ShowMorePopup(More);
	}

	private void CloseClick(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void OnExpireIconClick(object sender, MouseButtonEventArgs e)
	{
		ShowMorePopup(ExpireIcon, right: true);
	}

	private void ShowMorePopup(FrameworkElement target, bool right = false)
	{
		object dataContext = base.DataContext;
		BindAccountViewModel model = dataContext as BindAccountViewModel;
		if (model == null)
		{
			return;
		}
		EscPopup popup = new EscPopup
		{
			StaysOpen = false,
			PlacementTarget = target,
			VerticalOffset = 12.0,
			Placement = (right ? PlacementMode.Right : PlacementMode.Left),
			HorizontalOffset = (right ? (-32) : 32)
		};
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"Muted", Utils.GetString("Muted"), (Geometry)null, "")
			{
				Selected = LocalSettings.Settings.ExtraSettings.DoNotDisturbInCalendar
			},
			new CustomMenuItemViewModel((object)"Unsubscribe", Utils.GetString("Unsubscribe"), (Geometry)null, "")
		};
		if (model.IsExpired)
		{
			list.Insert(1, new CustomMenuItemViewModel((object)"Resubscribe", Utils.GetString("Resubscribe"), (Geometry)null, ""));
		}
		CustomMenuList customMenuList = new CustomMenuList(list, popup);
		customMenuList.Operated += async delegate(object o, object o1)
		{
			switch (o1.ToString())
			{
			case "Unsubscribe":
			{
				CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("Unsubscribe"), Utils.GetString("CancelSubscribeMessage"), Utils.GetString("OK"), Utils.GetString("Cancel"), this);
				customerDialog.ShowDialog();
				if (customerDialog.DialogResult.HasValue && customerDialog.DialogResult.Value)
				{
					await SubscribeCalendarHelper.UnbindCalendar(model.Id);
					Close();
				}
				break;
			}
			case "Muted":
			{
				bool doNotDisturbInCalendar = LocalSettings.Settings.ExtraSettings.DoNotDisturbInCalendar;
				bool? flag = new CustomerDialog(Utils.GetString(doNotDisturbInCalendar ? "ComfirmCloseNotDisturb" : "ComfirmOpenNotDisturb"), Utils.GetString(doNotDisturbInCalendar ? "ComfirmCloseNotDisturbDesc" : "ComfirmOpenNotDisturbDesc"), Utils.GetString(doNotDisturbInCalendar ? "Close" : "Enable"), Utils.GetString("Cancel"), this).ShowDialog();
				if (flag.HasValue && flag.Value)
				{
					LocalSettings.Settings.ExtraSettings.DoNotDisturbInCalendar = !LocalSettings.Settings.ExtraSettings.DoNotDisturbInCalendar;
					LocalSettings.Settings.Save(force: true);
					UiUtils.Toast(Container, Utils.GetString(LocalSettings.Settings.ExtraSettings.DoNotDisturbInCalendar ? "Enabled" : "Disabled"));
				}
				break;
			}
			case "Resubscribe":
			{
				Action<string> toast = null;
				if (base.Owner is SettingDialog settingDialog)
				{
					toast = settingDialog.Toast;
				}
				CalendarService.ResubscribeCalendar(model.Account, this, toast);
				break;
			}
			}
		};
		customMenuList.Show();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/editbindaccountwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Container = (Grid)target;
			break;
		case 2:
			((Border)target).MouseLeftButtonDown += OnDragMove;
			break;
		case 3:
			ExpireIcon = (Border)target;
			ExpireIcon.MouseLeftButtonUp += OnExpireIconClick;
			break;
		case 4:
			((Button)target).Click += OnSaveClick;
			break;
		case 5:
			((Button)target).Click += OnCancelClick;
			break;
		case 6:
			((Border)target).MouseLeftButtonUp += MoreClick;
			break;
		case 7:
			More = (Path)target;
			break;
		case 8:
			((Border)target).MouseLeftButtonUp += CloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
