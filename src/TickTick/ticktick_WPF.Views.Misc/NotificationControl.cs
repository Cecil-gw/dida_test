using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Misc;

public class NotificationControl : UserControl, IComponentConnector
{
	private readonly ObservableCollection<NotificationViewModel> _notificationShowList = new ObservableCollection<NotificationViewModel>();

	private readonly ObservableCollection<NotificationViewModel> _activitiesShowList = new ObservableCollection<NotificationViewModel>();

	private TempDataModel _notificationData;

	private bool _windowStart = true;

	private MarkType _markType;

	private double _notificationScrollOffset;

	private double _activityScrollOffset;

	public NotificationUnreadCount CountModel;

	private ScrollViewer _notificationScroller;

	private ScrollViewer _activityScroller;

	private static readonly List<string> Types = new List<string>
	{
		"team", "task", "attend", "projectPermission", "share", "PayReminder", "support", "TeamPayReminder", "comment", "upgrade",
		"forumTopic", "score", "permission", "assign", "project"
	};

	internal Grid SwitchGrid;

	internal AnimateGroupTitle SwitchTitle;

	internal Border UnreadNumber;

	internal Grid NotificationGrid;

	internal ListView NotificationItemsControl;

	internal Grid ActivitiesGrid;

	internal ListView ActivitiesItemsControl;

	internal ContentControl EmptyContainer;

	internal LoadingIndicator Loading;

	private bool _contentLoaded;

	public NotificationControl()
	{
		InitializeComponent();
		NotificationItemsControl.ItemsSource = _notificationShowList;
		ActivitiesItemsControl.ItemsSource = _activitiesShowList;
		base.Unloaded += OnUnloaded;
		EmptyContainer.Content = new EmptyWithTextView(140.0);
	}

	private void InitScroller()
	{
		if (_notificationScroller == null && NotificationItemsControl.Template.FindName("ScrollViewer", NotificationItemsControl) is ScrollViewer notificationScroller)
		{
			_notificationScroller = notificationScroller;
		}
		if (_activityScroller == null && ActivitiesItemsControl.Template.FindName("ScrollViewer", ActivitiesItemsControl) is ScrollViewer activityScroller)
		{
			_activityScroller = activityScroller;
			ActivitiesGrid.Visibility = Visibility.Collapsed;
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_notificationScrollOffset = _notificationScroller?.VerticalOffset ?? 0.0;
		_activityScrollOffset = _activityScroller?.VerticalOffset ?? 0.0;
		_notificationShowList.Clear();
		_activitiesShowList.Clear();
	}

	private async void InitViews(object sender, RoutedEventArgs routedEventArgs)
	{
		if (_windowStart)
		{
			_windowStart = false;
			return;
		}
		InitScroller();
		SwitchTitle.SelectedTitleChanged -= OnSelectedChanged;
		CountModel = await Communicator.GetNotificationCount();
		if (CountModel.Notification > 0)
		{
			UnreadNumber.Visibility = ((CountModel.Activity <= 0) ? Visibility.Hidden : Visibility.Visible);
			_markType = MarkType.notifications;
			CountModel.Notification = 0;
			Communicator.MarkReadNotification(_markType.ToString());
			_notificationScrollOffset = 0.0;
			if (CountModel.Activity > 0)
			{
				App.Window.LeftTabBar.ShowNotificationIndicator(CountModel);
				SwitchTitle.SetSelectedIndex(0);
				SwitchTitle.SelectedTitleChanged += OnSelectedChanged;
				return;
			}
		}
		else if (CountModel.Activity > 0)
		{
			_activityScrollOffset = 0.0;
			ShowAvtivities();
			UnreadNumber.Visibility = Visibility.Hidden;
			_markType = MarkType.activities;
			Communicator.MarkReadNotification(_markType.ToString());
			CountModel.Activity = 0;
		}
		else
		{
			UnreadNumber.Visibility = Visibility.Hidden;
			_markType = MarkType.none;
		}
		SwitchTitle.SetSelectedIndex((ActivitiesGrid.Visibility == Visibility.Visible) ? 1 : 0);
		SwitchTitle.SelectedTitleChanged += OnSelectedChanged;
		App.Window.LeftTabBar.HideNotificationIndicator();
	}

	private void ResetView(object sender, RoutedEventArgs e)
	{
		OnSwitchNotification(reset: true);
	}

	private void SwitchNotification()
	{
		OnSwitchNotification();
	}

	private void OnSwitchNotification(bool reset = false)
	{
		EmptyContainer.Visibility = Visibility.Collapsed;
		if (ActivitiesGrid.IsVisible || reset)
		{
			DoubleAnimation doubleAnimation = (DoubleAnimation)FindResource("HideAnimation");
			doubleAnimation.Completed -= ShowActivities;
			doubleAnimation.Completed -= ShowNotification;
			doubleAnimation.Completed += ShowNotification;
			ActivitiesGrid.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
		}
		if (CountModel == null || CountModel.Notification == 0)
		{
			(EmptyContainer.Content as EmptyWithTextView)?.SetData(Utils.GetString("NoNotifications"), Utils.GetString("NotificationsDesc"), new EmptyIconsSourceModel("NotificationEmpty", new List<int> { 1, 2, 3, 4, 6 }));
		}
	}

	private void SwitchActivities()
	{
		EmptyContainer.Visibility = Visibility.Collapsed;
		if (NotificationGrid.IsVisible)
		{
			_activityScroller?.ScrollToVerticalOffset(_activityScrollOffset);
			DoubleAnimation doubleAnimation = (DoubleAnimation)FindResource("HideAnimation");
			doubleAnimation.Completed -= ShowNotification;
			doubleAnimation.Completed -= ShowActivities;
			doubleAnimation.Completed += ShowActivities;
			NotificationGrid.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
			if (CountModel != null && CountModel.Activity > 0)
			{
				_markType = ((_markType != MarkType.none) ? MarkType.activities : MarkType.all);
				Communicator.MarkReadNotification("activities");
				CountModel.Activity = 0;
				Communicator.MarkReadNotification(_markType.ToString());
				App.Window.LeftTabBar.HideNotificationIndicator();
			}
			else
			{
				(EmptyContainer.Content as EmptyWithTextView)?.SetData(Utils.GetString("NoActives"), Utils.GetString("ActivesDesc"), new EmptyIconsSourceModel("NotificationEmpty", new List<int> { 1, 2, 3, 4, 6 }));
			}
			UnreadNumber.Visibility = Visibility.Hidden;
		}
	}

	private void ShowNotification(object sender, EventArgs e)
	{
		DoubleAnimation animation = (DoubleAnimation)FindResource("ShowAnimation");
		ActivitiesGrid.Visibility = Visibility.Collapsed;
		NotificationGrid.Visibility = Visibility.Visible;
		NotificationGrid.BeginAnimation(UIElement.OpacityProperty, animation);
		ShowOrHideView();
	}

	private void ShowActivities(object sender, EventArgs e)
	{
		DoubleAnimation animation = (DoubleAnimation)FindResource("ShowAnimation");
		NotificationGrid.Visibility = Visibility.Collapsed;
		ActivitiesGrid.Visibility = Visibility.Visible;
		ActivitiesGrid.BeginAnimation(UIElement.OpacityProperty, animation);
		ShowOrHideView();
	}

	private void ShowAvtivities()
	{
		NotificationGrid.Visibility = Visibility.Collapsed;
		ActivitiesGrid.Visibility = Visibility.Visible;
		ActivitiesGrid.BeginAnimation(UIElement.OpacityProperty, null);
		ActivitiesGrid.Opacity = 1.0;
		ShowOrHideView();
	}

	public async Task GetNotification()
	{
		ObservableCollection<NotificationModel> notificationModelList = null;
		EmptyContainer.Visibility = Visibility.Collapsed;
		NotificationViewModel notificationViewModel = _notificationShowList.FirstOrDefault();
		if (notificationViewModel != null)
		{
			NotificationItemsControl.ScrollIntoView(notificationViewModel);
		}
		Loading.Visibility = Visibility.Visible;
		((Storyboard)FindResource("ShowNotificationLoadingStoryboard")).Begin();
		if (_notificationShowList.Count == 0 && _activitiesShowList.Count == 0)
		{
			_notificationData = await TempDataDao.QueryTempDataModelListDbByTypeAndUserIdAsync("NOTIFICIATION", LocalSettings.Settings["LoginUserId"].ToString());
			if (_notificationData != null && !string.IsNullOrEmpty(_notificationData.Data))
			{
				_notificationData.ModifyTime = Utils.GetNowTimeStamp();
			}
			else if (_notificationData == null)
			{
				_notificationData = new TempDataModel
				{
					User_Id = LocalSettings.Settings["LoginUserId"].ToString(),
					DataType = "NOTIFICIATION",
					ModifyTime = Utils.GetNowTimeStamp()
				};
			}
		}
		else if (_notificationData == null)
		{
			_notificationData = new TempDataModel
			{
				User_Id = LocalSettings.Settings["LoginUserId"].ToString(),
				DataType = "NOTIFICIATION",
				ModifyTime = Utils.GetNowTimeStamp()
			};
		}
		string text = await Communicator.GetNotificationList();
		try
		{
			notificationModelList = JsonConvert.DeserializeObject<ObservableCollection<NotificationModel>>(text);
		}
		catch (Exception)
		{
			if (_notificationShowList.Count == 0 && _activitiesShowList.Count == 0)
			{
				try
				{
					notificationModelList = JsonConvert.DeserializeObject<ObservableCollection<NotificationModel>>(_notificationData.Data);
				}
				catch (Exception)
				{
					notificationModelList = null;
				}
			}
		}
		if (notificationModelList != null)
		{
			if (notificationModelList.Count > 0)
			{
				_notificationData.Data = text;
				await TempDataDao.UpdateOrInsertTempDataModelListDbByTypeAndUserIdAsync(_notificationData);
			}
			try
			{
				await GetViewModels(notificationModelList);
			}
			catch (Exception ex3)
			{
				UtilLog.Error(ex3);
			}
		}
		ShowOrHideView();
		Storyboard obj = (Storyboard)FindResource("HideNotificationLoadingStoryboard");
		obj.Begin();
		obj.Completed -= HideLoadingImage;
		obj.Completed += HideLoadingImage;
		_notificationScroller?.ScrollToVerticalOffset(_notificationScrollOffset);
		_activityScroller?.ScrollToVerticalOffset(_activityScrollOffset);
	}

	private void HideLoadingImage(object sender, EventArgs e)
	{
		Loading.Visibility = Visibility.Collapsed;
	}

	private void ShowOrHideView()
	{
		ListView notificationItemsControl = NotificationItemsControl;
		ObservableCollection<NotificationViewModel> notificationShowList = _notificationShowList;
		notificationItemsControl.Visibility = ((notificationShowList == null || notificationShowList.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
		ListView activitiesItemsControl = ActivitiesItemsControl;
		ObservableCollection<NotificationViewModel> activitiesShowList = _activitiesShowList;
		activitiesItemsControl.Visibility = ((activitiesShowList == null || activitiesShowList.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
		if (NotificationGrid.Visibility == Visibility.Visible)
		{
			ObservableCollection<NotificationViewModel> notificationShowList2 = _notificationShowList;
			if (notificationShowList2 != null && notificationShowList2.Count == 0)
			{
				goto IL_009a;
			}
		}
		if (ActivitiesGrid.Visibility == Visibility.Visible)
		{
			ObservableCollection<NotificationViewModel> activitiesShowList2 = _activitiesShowList;
			if (activitiesShowList2 != null && activitiesShowList2.Count == 0)
			{
				goto IL_009a;
			}
		}
		EmptyContainer.Visibility = Visibility.Collapsed;
		return;
		IL_009a:
		EmptyContainer.Visibility = Visibility.Visible;
		bool flag = NotificationGrid.Visibility == Visibility.Visible;
		(EmptyContainer.Content as EmptyWithTextView)?.SetData(Utils.GetString(flag ? "NoNotifications" : "NoActives"), Utils.GetString(flag ? "NotificationsDesc" : "ActivesDesc"), new EmptyIconsSourceModel("NotificationEmpty", new List<int> { 1, 2, 3, 4, 6 }));
	}

	private async Task GetViewModels(ObservableCollection<NotificationModel> notificationModelList)
	{
		List<NotificationViewModel> activities = new List<NotificationViewModel>();
		List<NotificationViewModel> notifications = new List<NotificationViewModel>();
		foreach (NotificationModel notificationModel in notificationModelList)
		{
			ProjectModel projectById = CacheManager.GetProjectById(notificationModel.notificationUserData.projectId);
			string type = notificationModel.type;
			if (!(type == "team"))
			{
				if (type == "task")
				{
					goto IL_0096;
				}
			}
			else if (notificationModel.actionStatus == 18 || notificationModel.actionStatus == 19)
			{
				goto IL_0096;
			}
			notifications.Add(new NotificationViewModel(notificationModel));
			continue;
			IL_0096:
			if (string.IsNullOrEmpty(projectById?.teamId) || !TeamDao.IsTeamExpired(projectById.teamId))
			{
				activities.Add(new NotificationViewModel(notificationModel));
			}
			else
			{
				notifications.Add(new NotificationViewModel(notificationModel));
			}
		}
		foreach (NotificationViewModel item in notifications.ToList())
		{
			string type = item.type;
			if (!(type == "permission"))
			{
				if (type == "projectPermission")
				{
					if (item.actionStatus == 1 || item.actionStatus == 4 || item.actionStatus == 5)
					{
						NotificationViewModel notificationViewModel = item;
						notificationViewModel.AvatarUrl = await Utils.GetAvatarUrl(item.notification.notificationUserData.applyUserCode);
						item.UserDisplayName = item.notification.notificationUserData.applyUserDisplayName;
					}
					else
					{
						NotificationViewModel notificationViewModel = item;
						notificationViewModel.AvatarUrl = await Utils.GetAvatarUrl(item.notification.notificationUserData.ownerUserCode);
						item.UserDisplayName = item.notification.notificationUserData.ownerUserDisplayName;
					}
				}
				else
				{
					NotificationViewModel notificationViewModel = item;
					notificationViewModel.AvatarUrl = await Utils.GetAvatarUrl(item.userCode);
				}
			}
			else if (item.actionStatus == 2 || item.actionStatus == 3)
			{
				NotificationViewModel notificationViewModel = item;
				notificationViewModel.AvatarUrl = await Utils.GetAvatarUrl(item.notification.notificationUserData.ownerUserCode);
			}
			else
			{
				NotificationViewModel notificationViewModel = item;
				notificationViewModel.AvatarUrl = await Utils.GetAvatarUrl(item.notification.notificationUserData.applyUserCode);
			}
		}
		foreach (NotificationViewModel item2 in activities.ToList())
		{
			NotificationViewModel item = item2;
			item.AvatarUrl = await Utils.GetAvatarUrl(item2.userCode);
		}
		ItemsSourceHelper.CopyTo(notifications, _notificationShowList);
		ItemsSourceHelper.CopyTo(activities, _activitiesShowList);
	}

	private void OnSelectedChanged(object sender, GroupTitleViewModel e)
	{
		if ("Notifications".Equals(e.Title))
		{
			SwitchNotification();
		}
		else
		{
			SwitchActivities();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/notificationcontrol.xaml", UriKind.Relative);
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
			((NotificationControl)target).Loaded += InitViews;
			((NotificationControl)target).Unloaded += ResetView;
			break;
		case 2:
			SwitchGrid = (Grid)target;
			break;
		case 3:
			SwitchTitle = (AnimateGroupTitle)target;
			break;
		case 4:
			UnreadNumber = (Border)target;
			break;
		case 5:
			NotificationGrid = (Grid)target;
			break;
		case 6:
			NotificationItemsControl = (ListView)target;
			break;
		case 7:
			ActivitiesGrid = (Grid)target;
			break;
		case 8:
			ActivitiesItemsControl = (ListView)target;
			break;
		case 9:
			EmptyContainer = (ContentControl)target;
			break;
		case 10:
			Loading = (LoadingIndicator)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
