using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Config;

public class CalendarConnectInfoWindow : Window, IComponentConnector, IStyleConnector
{
	private CalendarConnectInfo _info;

	private object _mouseDownItem;

	internal TextBlock TitleText;

	internal StackPanel ItemsPanel;

	internal ItemsControl RelProjectItems;

	internal Grid EmptyGrid;

	internal Path More;

	internal Path X;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	private bool _contentLoaded;

	public CalendarConnectInfoWindow(CalendarConnectInfo info)
	{
		InitializeComponent();
		TitleText.Text = info.account;
		SetInfo(info);
	}

	private void SetInfo(CalendarConnectInfo info)
	{
		_info = info;
		ObservableCollection<RelProjectItemViewModel> observableCollection = new ObservableCollection<RelProjectItemViewModel>();
		if (info.relProjects != null)
		{
			foreach (CalendarContentProject project in info.relProjects)
			{
				CalendarContentCalendar calendar = info.calendars.FirstOrDefault((CalendarContentCalendar c) => c.outId == project.calendarId);
				observableCollection.Add(new RelProjectItemViewModel(calendar, project));
			}
		}
		RelProjectItems.ItemsSource = observableCollection;
		if (observableCollection.Count == 0)
		{
			ItemsPanel.Visibility = Visibility.Collapsed;
			EmptyGrid.Visibility = Visibility.Visible;
		}
		else
		{
			ItemsPanel.Visibility = Visibility.Visible;
			EmptyGrid.Visibility = Visibility.Collapsed;
		}
	}

	private void OnDragMove(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	private void CloseClick(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private async void OnEditClick(object sender, MouseButtonEventArgs e)
	{
		if (sender.Equals(_mouseDownItem) && sender is FrameworkElement { DataContext: var dataContext })
		{
			RelProjectItemViewModel model = dataContext as RelProjectItemViewModel;
			if (model != null)
			{
				LinkGoogleCalendarWindow linkGoogleCalendarWindow = new LinkGoogleCalendarWindow(_info, model.Project.Copy())
				{
					Owner = this
				};
				linkGoogleCalendarWindow.ShowDialog();
				if (linkGoogleCalendarWindow.RelProject != null && !linkGoogleCalendarWindow.RelProject.IsEquals(model.Project))
				{
					CalendarContentProject project = linkGoogleCalendarWindow.RelProject.Copy();
					model.Project = project;
					CalendarContentCalendar calendar = _info.calendars.FirstOrDefault((CalendarContentCalendar c) => c.outId == model.Project.calendarId);
					model.SetProperties(calendar, model.Project);
				}
				Reload();
			}
		}
		_mouseDownItem = null;
	}

	private async void Reload()
	{
		CalendarConnectInfo calendarConnectInfo = await CalendarConnectInfoDao.GetInfoByIdAsync(_info.id);
		if (calendarConnectInfo != null)
		{
			calendarConnectInfo.GetItems();
			SetInfo(calendarConnectInfo);
		}
	}

	private async void OnAddCalendarClick(object sender, MouseButtonEventArgs e)
	{
		if (!ProChecker.CheckPro(ProType.IntegrationWithGoogle) || !(await CalendarConnectService.CheckConnectProjectCount(_info.id, this)))
		{
			return;
		}
		List<CalendarConnectInfo> obj = await CalendarConnectService.GetConnectInfosByProjectId("all");
		CalendarConnectInfo item = obj?.FirstOrDefault((CalendarConnectInfo i) => i.id == _info.id);
		obj?.Remove(item);
		LinkGoogleCalendarWindow linkGoogleCalendarWindow = new LinkGoogleCalendarWindow(_info)
		{
			Owner = this
		};
		linkGoogleCalendarWindow.ShowDialog();
		if (linkGoogleCalendarWindow.RelProject != null)
		{
			CalendarContentProject project = linkGoogleCalendarWindow.RelProject.Copy();
			CalendarContentCalendar calendar = _info.calendars.FirstOrDefault((CalendarContentCalendar c) => c.outId == project.calendarId);
			if (RelProjectItems.ItemsSource is ObservableCollection<RelProjectItemViewModel> observableCollection)
			{
				observableCollection.Add(new RelProjectItemViewModel(calendar, project));
				ItemsPanel.Visibility = Visibility.Visible;
				EmptyGrid.Visibility = Visibility.Collapsed;
			}
		}
		if (linkGoogleCalendarWindow.IsSaved)
		{
			Reload();
		}
	}

	private void MoreClick(object sender, MouseButtonEventArgs e)
	{
		EscPopup popup = new EscPopup
		{
			StaysOpen = false,
			PlacementTarget = More,
			VerticalOffset = 12.0,
			Placement = PlacementMode.Left,
			HorizontalOffset = 32.0
		};
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"Disintegration", Utils.GetString("Disintegration"), (Geometry)null, "")
		}, popup);
		customMenuList.Operated += async delegate
		{
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("Disintegration"), Utils.GetString("DisintegrationDesc"), Utils.GetString("OK"), Utils.GetString("Cancel"), this);
			customerDialog.ShowDialog();
			if (customerDialog.DialogResult.HasValue && customerDialog.DialogResult.Value && await CalendarConnectService.Disintegration(_info.id))
			{
				Window owner = base.Owner;
				if (!(owner is SettingDialog settingDialog))
				{
					if (owner is IToastShowWindow toastShowWindow)
					{
						toastShowWindow.TryToastString(null, Utils.GetString("DisintegrationSucceed"));
					}
				}
				else
				{
					settingDialog.Toast(Utils.GetString("DisintegrationSucceed"));
				}
				Close();
			}
		};
		customMenuList.Show();
	}

	private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDownItem = sender;
	}

	public void Toast(string toastText)
	{
		ToastBorder.Visibility = Visibility.Visible;
		ToastText.Text = toastText;
		Storyboard storyboard = (Storyboard)FindResource("ShowToast");
		ToastBorder.BeginStoryboard(storyboard);
	}

	private void OnToasted(object sender, EventArgs e)
	{
		ToastBorder.Visibility = Visibility.Collapsed;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/calendarconnectinfowindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((Storyboard)target).Completed += OnToasted;
			break;
		case 2:
			((Border)target).MouseLeftButtonDown += OnDragMove;
			break;
		case 3:
			TitleText = (TextBlock)target;
			break;
		case 4:
			ItemsPanel = (StackPanel)target;
			break;
		case 5:
			RelProjectItems = (ItemsControl)target;
			break;
		case 7:
			((StackPanel)target).MouseLeftButtonUp += OnAddCalendarClick;
			break;
		case 8:
			EmptyGrid = (Grid)target;
			break;
		case 9:
			((Button)target).PreviewMouseLeftButtonUp += OnAddCalendarClick;
			break;
		case 10:
			((Border)target).MouseLeftButtonUp += MoreClick;
			break;
		case 11:
			More = (Path)target;
			break;
		case 12:
			((Border)target).MouseLeftButtonUp += CloseClick;
			break;
		case 13:
			X = (Path)target;
			break;
		case 14:
			ToastBorder = (Border)target;
			break;
		case 15:
			ToastText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 6)
		{
			((Border)target).MouseLeftButtonUp += OnEditClick;
			((Border)target).PreviewMouseLeftButtonDown += OnItemMouseDown;
		}
	}
}
