using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Subscribe;

namespace ticktick_WPF.Views.Config;

public class CalendarInfoItem : UserControl, IComponentConnector
{
	internal Border Container;

	internal TextBlock PopupShowText;

	private bool _contentLoaded;

	public CalendarInfoItem()
	{
		InitializeComponent();
	}

	private async void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		SubscribeCalendarViewModel model = dataContext as SubscribeCalendarViewModel;
		if (model == null)
		{
			return;
		}
		switch (model.SubType)
		{
		case SubscribeCalendarType.Url:
			if (model.Kind != "holiday")
			{
				EditUrlWindow editUrlWindow = new EditUrlWindow(model);
				editUrlWindow.Owner = Window.GetWindow(this);
				editUrlWindow.ShowDialog();
			}
			else
			{
				EditHolidaySubsWindow editHolidaySubsWindow = new EditHolidaySubsWindow(model);
				editHolidaySubsWindow.Owner = Window.GetWindow(this);
				editHolidaySubsWindow.ShowDialog();
			}
			break;
		case SubscribeCalendarType.Account:
			if (string.IsNullOrEmpty(model.CalendarId))
			{
				break;
			}
			if (model.IsExpired)
			{
				PopupShowText.Visibility = Visibility.Visible;
				List<CustomMenuItemViewModel> types = new List<CustomMenuItemViewModel>
				{
					new CustomMenuItemViewModel((object)"Resubscribe", Utils.GetString(model.IsConnect ? "Reintegrate" : "Resubscribe"), (Geometry)null, ""),
					new CustomMenuItemViewModel((object)"Unsubscribe", Utils.GetString(model.IsConnect ? "Disintegration" : "Unsubscribe"), (Geometry)null, "")
				};
				EscPopup escPopup = new EscPopup
				{
					StaysOpen = false,
					PlacementTarget = (sender as UIElement),
					Placement = PlacementMode.Mouse
				};
				escPopup.Closed += delegate
				{
					PopupShowText.Visibility = Visibility.Collapsed;
				};
				CustomMenuList customMenuList = new CustomMenuList(types, escPopup);
				customMenuList.Operated += async delegate(object o, object val)
				{
					switch (val as string)
					{
					case "Resubscribe":
						if (model.IsConnect && model.Site == "notion")
						{
							Utils.TryProcessToNotionConnect(null);
							Utils.FindParent<IntegratedCalendarsWindow>(this)?.CheckOnActive();
						}
						else if (model.Account?.IsBindAccountPassword() ?? false)
						{
							BindAccountWindow bindAccountWindow = new BindAccountWindow(model.Account);
							bindAccountWindow.Owner = Utils.GetParentWindow(this);
							bindAccountWindow.ShowDialog();
						}
						else if (model.Site == "outlook")
						{
							BindOutlookWindow bindOutlookWindow = new BindOutlookWindow();
							bindOutlookWindow.SetOriginAccount(model.Account?.Id);
							bindOutlookWindow.ShowDialog();
						}
						else if (model.Site == "google")
						{
							BindGoogleAccount instance = BindGoogleAccount.GetInstance(!model.IsConnect);
							instance.Start(model.CalendarId);
							instance.CalendarConnected += delegate
							{
								Utils.FindParent<SettingDialog>(this)?.Toast(Utils.GetString("Integrated"));
							};
							instance.ToastText += delegate(object obj, string s)
							{
								Utils.FindParent<SettingDialog>(this)?.Toast(s);
							};
						}
						else if (model.Kind == "icloud")
						{
							BindICloudWindow bindICloudWindow = new BindICloudWindow(null, model.Account);
							bindICloudWindow.Owner = Utils.FindParent<SettingDialog>(this);
							bindICloudWindow.Topmost = false;
							bindICloudWindow.ShowDialog();
						}
						break;
					case "Unsubscribe":
					{
						bool? flag = new CustomerDialog(Utils.GetString(model.IsConnect ? "Disintegration" : "Unsubscribe"), Utils.GetString(model.IsConnect ? "DisintegrationDesc" : "CancelSubscribeMessage"), MessageBoxButton.OKCancel, Window.GetWindow(this)).ShowDialog();
						if (flag.HasValue && flag.Value)
						{
							if (model.IsConnect)
							{
								if (await CalendarConnectService.Disintegration(model.CalendarId) && Window.GetWindow(this) is SettingDialog settingDialog)
								{
									settingDialog.Toast(Utils.GetString("DisintegrationSucceed"));
								}
							}
							else
							{
								SubscribeCalendarHelper.UnbindCalendar(model.CalendarId);
							}
						}
						break;
					}
					}
				};
				customMenuList.Show();
			}
			else if (model.IsConnect)
			{
				string site = model.Site;
				if (!(site == "google"))
				{
					if (site == "notion")
					{
						model.NavigateNotionUrl();
						Utils.FindParent<IntegratedCalendarsWindow>(this)?.CheckOnActive();
					}
					break;
				}
				CalendarConnectInfo calendarConnectInfo = await CalendarConnectInfoDao.GetInfoByIdAsync(model.CalendarId);
				if (calendarConnectInfo != null)
				{
					calendarConnectInfo.GetItems();
					CalendarConnectInfoWindow calendarConnectInfoWindow = new CalendarConnectInfoWindow(calendarConnectInfo);
					calendarConnectInfoWindow.Owner = Window.GetWindow(this);
					calendarConnectInfoWindow.ShowDialog();
				}
			}
			else
			{
				EditBindAccountWindow editBindAccountWindow = await EditBindAccountWindow.Build(model.CalendarId);
				if (editBindAccountWindow != null)
				{
					editBindAccountWindow.Owner = Window.GetWindow(this);
					editBindAccountWindow.ShowDialog();
				}
			}
			break;
		}
	}

	private void OnEditClick(object sender, RoutedEventArgs e)
	{
		OnItemClick(sender, null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/calendarinfoitem.xaml", UriKind.Relative);
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
			Container = (Border)target;
			Container.MouseLeftButtonUp += OnItemClick;
			break;
		case 2:
			PopupShowText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
