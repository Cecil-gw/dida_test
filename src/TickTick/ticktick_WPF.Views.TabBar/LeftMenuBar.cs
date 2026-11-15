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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Framework.EmbeddedBrowser;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Views.TabBar;

public class LeftMenuBar : UserControl, IComponentConnector
{
	private const string FeedbackMail = "mailto:3485446086@qq.com?subject=dida_test%20feedback";

	private const string GitHubUrl = "https://github.com/Cecil-gw/dida_test";

	public DateTime LoadTime;

	private string _pic;

	private NetworkError _errorType;

	public List<TabBarItemViewModel> TabBarItems;

	private bool _dragChanged;

	internal LeftMenuBar Root;

	internal Border BackBorder;

	internal Line RightLine;

	internal Path Avatar;

	internal ImageBrush HeadimgImage;

	internal Image HeadProImage;

	internal Image HeadFreeImage;

	internal EscPopup MenuPopup;

	internal ItemsControl TopTabBar;

	internal Popup DragPopup;

	internal Path PopupPath;

	internal Image NetworkErrorImage;

	internal ToolTip SyncToolTip;

	internal Path SyncPath;

	internal Grid NotificationButton;

	internal Border NotificationBackBorder;

	internal Path NotificationDot;

	internal Border NotificationDotBorder;

	internal TextBlock NotificationDotText;

	internal EscPopup NotificationPopup;

	internal NotificationControl NotificationPanel;

	internal Grid MoreGrid;

	internal Border UpgradeBackBorder;

	internal Path MorePath;

	internal Grid UpdateEllipse;

	internal ContentControl MorePopupPlacement;

	internal EscPopup MorePopup;

	private bool _contentLoaded;

	public static bool CanUpdate { get; set; }

	public string SelectedModule
	{
		get
		{
			return TabBarItems?.FirstOrDefault((TabBarItemViewModel t) => t.Selected)?.Module;
		}
		set
		{
			TabBarItems?.ForEach(delegate(TabBarItemViewModel item)
			{
				item.Selected = item.Module == (LocalSettings.Settings.InSearch ? "Search" : value);
			});
		}
	}

	public LeftMenuBar()
	{
		InitializeComponent();
		DataChangedNotifier.IsDarkChanged += OnIsDarkChanged;
		DataChangedNotifier.InterfaceStyleChanged += OnInterfaceStyleChanged;
		SetTabBarItems("List");
		OnInterfaceStyleChanged(null, null);
	}

	private void OnInterfaceStyleChanged(object sender, EventArgs e)
	{
		switch (LocalSettings.Settings.ExtraSettings.InterfaceStyle)
		{
		case InterfaceStyle.Flat:
			BackBorder.SetResourceReference(Control.BackgroundProperty, "LinearBackground");
			RightLine.Visibility = Visibility.Visible;
			break;
		case InterfaceStyle.Card:
			BackBorder.Background = Brushes.Transparent;
			RightLine.Visibility = Visibility.Collapsed;
			break;
		}
	}

	private void OnIsDarkChanged(object sender, EventArgs e)
	{
		RefreshHead();
	}

	public async Task LoadUserInfo(bool force)
	{
		await UserManager.CheckUserP(force);
		try
		{
			await UserManager.PullUserInfo();
		}
		catch (Exception ex)
		{
			UtilLog.Warn("LeftMenuBar.LoadUserInfo PullUserInfo ex: " + ex);
		}
		await base.Dispatcher.InvokeAsync(delegate
		{
			LoadTime = DateTime.Now;
			RefreshHead();
			RefreshIcon();
			RefreshToolTip();
		});
	}

	private void RefreshIcon()
	{
		LocalSettings.Settings.NotifyPropertyChanged("ShowMatrix");
	}

	public void RefreshToolTip()
	{
		List<TabBarItemViewModel> list = ((TopTabBar.ItemsSource as ObservableCollection<TabBarItemViewModel>) ?? new ObservableCollection<TabBarItemViewModel>()).Where((TabBarItemViewModel tabBarItemViewModel) => tabBarItemViewModel.Show).ToList();
		list.Sort((TabBarItemViewModel a, TabBarItemViewModel b) => a.SortOrder.CompareTo(b.SortOrder));
		List<TabBarModel> navigationTabBars = TabBarModel.GetNavigationTabBars();
		foreach (TabBarItemViewModel item in list)
		{
			string text = string.Empty;
			if (item.Module == "Search")
			{
				text = new HotkeyModel(LocalSettings.Settings.ShortCutModel.SearchTask).ToString();
			}
			else if (!KeyBindingManager.HasCtrlNumKeyBinding())
			{
				int num = navigationTabBars.FindIndex((TabBarModel bar) => string.Equals(bar.name, TabBarModel.GetConfigName(item.Module), StringComparison.OrdinalIgnoreCase));
				if (num >= 0 && num < 9)
				{
					text = new HotkeyModel(LocalSettings.Settings.ShortCutModel.GetPropertyValue($"Tab0{num + 1}")).ToString();
				}
			}
			item.ToolTip = (string.IsNullOrEmpty(text) ? item.Name : (item.Name + " (" + text + ")"));
		}
	}

	public async void RefreshHead()
	{
		_ = 1;
		try
		{
			UserInfoModel userInfo = await UserManager.GetUserInfo();
			if (userInfo != null)
			{
				BitmapImage imageSource = await AvatarHelper.GetAvatarByUrlAsync(string.IsNullOrEmpty(userInfo.picture) ? "avatar-new.png" : userInfo.picture);
				HeadimgImage.ImageSource = imageSource;
			}
			if (_pic != userInfo?.picture || string.IsNullOrEmpty(userInfo?.picture))
			{
				_pic = userInfo?.picture;
				UtilLog.Info($"LeftMenu.RefreshHead : {userInfo?.picture} ,isPro {LocalSettings.Settings.IsPro}");
			}
			SetProCrown();
		}
		catch (Exception)
		{
		}
	}

	private void SetProCrown()
	{
		ApplySquareAvatar();
	}

	private void ApplySquareAvatar()
	{
		HeadProImage.Visibility = Visibility.Collapsed;
		HeadFreeImage.Visibility = Visibility.Collapsed;
		Avatar.Data = new RectangleGeometry(new Rect(0, 0, 64, 64), 12, 12);
		Avatar.Stretch = Stretch.Fill;
		Avatar.Stroke = null;
		Avatar.StrokeThickness = 0;
		Avatar.Visibility = Visibility.Visible;
	}


	private void SetTabBarItems(string module)
	{
		TopTabBar.ItemsSource = new ObservableCollection<TabBarItemViewModel>();
		TabBarItems = new List<TabBarItemViewModel>
		{
			new TabBarItemViewModel("Task", module),
			new TabBarItemViewModel("Calendar", module),
			new TabBarItemViewModel("Matrix", module),
			new TabBarItemViewModel("Habit", module),
			new TabBarItemViewModel("Pomo", module),
			new TabBarItemViewModel("Search", module),
			new TabBarItemViewModel("CountDown", module),
			new TabBarItemViewModel("AI", module)
		};
		SetItems();
		TopTabBar.ItemsSource = new ObservableCollection<TabBarItemViewModel>(TabBarItems);
	}

	public void SetItems()
	{
		List<TabBarModel> list = LocalSettings.Settings.UserPreference?.desktopTabBars?.bars ?? TabBarModel.InitTabBars();
		foreach (TabBarItemViewModel tabBarItem in TabBarItems)
		{
			string configName = TabBarModel.GetConfigName(tabBarItem.Module);
			if (tabBarItem.CanHide())
			{
				tabBarItem.Show = TabBarModel.IsActive(configName, list);
			}
			tabBarItem.SortOrder = TabBarModel.GetTabBarSort(configName, list);
		}
		List<TabBarItemViewModel> list2 = TabBarItems.Where((TabBarItemViewModel t) => t.Show).ToList();
		list2.Sort((TabBarItemViewModel a, TabBarItemViewModel b) => a.SortOrder.CompareTo(b.SortOrder));
		foreach (TabBarItemViewModel item in list2)
		{
			item.SortOrder = list2.IndexOf(item);
		}
		TopTabBar.Height = list2.Count * 48;
		RefreshToolTip();
		UtilLog.Info($"SetTabItems settings is null : {LocalSettings.Settings.UserPreference?.desktopTabBars?.bars == null}, {JsonConvert.SerializeObject(list)}");
		UtilLog.Info(TabBarItems.Aggregate(string.Empty, (string text, TabBarItemViewModel item) => text + $"{item.Name}, {item.Show}, {item.SortOrder} | "));
	}

	private void HeadClick(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		InitMenuOptions();
	}

	private void InitMenuOptions()
	{
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("Settings", Utils.GetString("Settings"), Utils.GetGeometry("Ic.Tabbar.SettingLinear")),
			new CustomMenuItemViewModel("Logout", Utils.GetString("PublicLogout"), Utils.GetGeometry("Ic.Tabbar.Logout"))
		};
		if (CacheManager.GetTeams().Count > 0)
		{
			list.Insert(list.Count - 1, new CustomMenuItemViewModel("ManageTeam", Utils.GetString("ManageTeam"), Utils.GetGeometry("Ic.Sharing.ManageGroup")));
		}
		CustomMenuList customMenuList = new CustomMenuList(list, MenuPopup);
		customMenuList.Operated += OnActionSelected;
		customMenuList.Show();
	}

	private void ShowMoreOptions()
	{
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("Shortcut", Utils.GetString("Shortcut"), Utils.GetGeometry("Ic.Tabbar.HotKey")),
			new CustomMenuItemViewModel("Feedback", Utils.GetString("PublicFeedback"), Utils.GetGeometry("Ic.Tabbar.FeedBack")),
			new CustomMenuItemViewModel("Changelog", Utils.GetString("ChangeLog"), Utils.GetGeometry("Ic.Tabbar.UpgradeLog"))
		}, MorePopup);
		customMenuList.Operated += OnActionSelected;
		customMenuList.Show();
	}

	private async void OnActionSelected(object sender, object e)
	{
		await Task.Delay(50);
		string text = e as string;
		switch (text)
		{
		case "Sync":
			UserActCollectUtils.AddClickEvent("tab_bar", "click", "sync");
			break;
		case "Settings":
			UserActCollectUtils.AddClickEvent("tab_bar", "user", "settings");
			ShowSettingDialog();
			break;
		case "CheckUpdate":
			UserActCollectUtils.AddClickEvent("tab_bar", "three_dots", "check_update");
			CheckUpdate();
			break;
		case "Feedback":
			UserActCollectUtils.AddClickEvent("tab_bar", "three_dots", "feedback");
			Utils.TryProcessStartUrl(FeedbackMail);
			break;
		case "ManageTeam":
		{
			List<TeamModel> teams = CacheManager.GetTeams();
			if (teams.Count > 0)
			{
				Utils.TryProcessStartUrlWithToken("/webapp/#settings/team/members/" + teams[0].id);
			}
			return;
		}
		case "OpenOnWeb":
			UserActCollectUtils.AddClickEvent("tab_bar", "three_dots", "home");
			ViewOnWeb();
			break;
		case "Premium":
			break;
		case "Changelog":
			UserActCollectUtils.AddClickEvent("tab_bar", "three_dots", "changelog");
			Utils.TryProcessStartUrl(GitHubUrl);
			break;
		case "Logout":
			UserActCollectUtils.AddClickEvent("tab_bar", "user", "signout");
			Logout();
			break;
		case "Shortcut":
			UserActCollectUtils.AddClickEvent("tab_bar", "three_dots", "shortcut");
			MainWindowManager.ShowShortCutPanel();
			break;
		case "Debug":
			DebugClick();
			break;
		}
		UtilLog.Info("LeftMenu.MoreAction : " + text);
	}

	private void DebugClick()
	{
		Utils.StartUpgrade("settings");
	}

	private void OnMoreClick(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		UserActCollectUtils.AddClickEvent("tab_bar", "click", "three_dots");
		ShowMoreOptions();
	}

	public void ShowSettingDialog()
	{
		SettingDialog.ShowSettingDialog(SettingsType.More, Window.GetWindow(this));
	}

	private async void SyncButtonClick()
	{
		App.Window.SyncButtonClick();
		UserActCollectUtils.AddClickEvent("tab_bar", "click", "sync");
	}

	private void Logout()
	{
		App.Window.Logout();
	}

	private void OpenChangeLog()
	{
		Utils.FindParent<MainWindow>(this).OpenChangeLog();
	}

	private static void ViewOnWeb()
	{
		MainWindow.ViewOnWeb();
	}

	private void CheckUpdate()
	{
		App.Window.CheckUpdate();
	}

	private void OpenTicket()
	{
		ShowFeedBackDialog();
	}

	private async void ShowFeedBackDialog()
	{
		if (EmbeddedBrowserFactory.CanOpenEmbeddedBrowser(Environment.OSVersion.Version, InternetExplorerBrowserEmulation.IsVersion11()))
		{
			NavigateWebBrowserWindow navigateWebBrowserWindow = new NavigateWebBrowserWindow("/v3/tickets/");
			navigateWebBrowserWindow.Owner = App.Window;
			navigateWebBrowserWindow.Show();
		}
		else
		{
			JumpWebFeedBack();
		}
	}

	private async void JumpWebFeedBack()
	{
		string url = BaseUrl.GetDomain() + "/sign/autoSignOn?token={0}&dest={1}";
		url = string.Format(url, await Communicator.GetSignOnToken(), "/v3/tickets/");
		Utils.TryProcessStartUrl(url);
	}

	public void Reset()
	{
		try
		{
			HeadimgImage.ImageSource = AvatarHelper.GetDefaultAvatar();
		}
		catch (Exception)
		{
			HeadimgImage.ImageSource = null;
		}
		ApplySquareAvatar();
	}

	private void OnItemClick(object sender, TabBarItemViewModel e)
	{
		switch (e.Module)
		{
		case "Task":
			UserActCollectUtils.AddClickEvent("tab_bar", "click", "task");
			break;
		case "Calendar":
			UserActCollectUtils.AddClickEvent("tab_bar", "click", "calendar");
			EventArchiveSyncService.PullArchivedModels();
			CourseArchiveSyncService.PullArchivedModels();
			break;
		case "Matrix":
			SettingsHelper.PullRemotePreference();
			UserActCollectUtils.AddClickEvent("tab_bar", "click", "matrix");
			break;
		case "Habit":
			UserActCollectUtils.AddClickEvent("tab_bar", "click", "habit");
			break;
		case "Search":
			App.Window.StartSearch();
			UserActCollectUtils.AddClickEvent("tab_bar", "click", "search");
			break;
		case "CountDown":
			UserActCollectUtils.AddClickEvent("tab_bar", "click", "countdown");
			break;
		case "AI":
			UserActCollectUtils.AddClickEvent("tab_bar", "click", "ai_webapp");
			break;
		}
		UtilLog.Info("LeftMenu.SwitchView : " + e.Module);
		if (Enum.TryParse<DisplayModule>(e.Module, ignoreCase: true, out var result))
		{
			bool inSearch = LocalSettings.Settings.InSearch;
			SearchHelper.InSearch = false;
			App.Window.SwitchModule(result, inSearch, click: true);
		}
	}

	private void NotificationButtonClick(object sender, MouseButtonEventArgs e)
	{
		UtilLog.Info("LeftMenu.NotificationClick");
		UserActCollectUtils.AddClickEvent("tab_bar", "click", "notification");
		if (Utils.IsNetworkAvailable())
		{
			NotificationPopup.IsOpen = !NotificationPopup.IsOpen;
			if (NotificationPopup.IsOpen)
			{
				NotificationPanel.GetNotification();
			}
		}
		else
		{
			Utils.Toast(Utils.GetString("NoNetwork"));
		}
	}

	public void ShowNotificationIndicator(NotificationUnreadCount notificationCount)
	{
		int num = notificationCount.Activity + notificationCount.Notification;
		if (num > 0)
		{
			string text = "99+";
			if (num <= 99)
			{
				text = num.ToString();
			}
			NotificationDotText.Text = text;
			NotificationDotBorder.Visibility = Visibility.Visible;
			NotificationBackBorder.Clip = TryFindResource("NotifiClip") as Geometry;
		}
	}

	public void HideNotificationIndicator()
	{
		NotificationDotBorder.Visibility = Visibility.Collapsed;
		NotificationBackBorder.Clip = null;
	}

	private void TryMoveWindow(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			App.Window.DragMove();
		}
	}

	private void OnSyncClick(object sender, MouseButtonEventArgs e)
	{
		UtilLog.Info("LeftMenu.SyncClick");
		SyncButtonClick();
	}

	public void StartSyncStory()
	{
		try
		{
			if (FindResource("SyncStory") is Storyboard storyboard)
			{
				storyboard.Begin();
			}
		}
		catch
		{
		}
	}

	public void SetUpgradeDisplay(int level)
	{
		CanUpdate = level > 0;
		if (level >= 1)
		{
			UpdateEllipse.Visibility = Visibility.Visible;
			UpgradeBackBorder.Clip = TryFindResource("UpgradeClip") as Geometry;
		}
		else
		{
			UpdateEllipse.Visibility = Visibility.Collapsed;
			UpgradeBackBorder.Clip = null;
		}
	}

	public void SetNetworkStatus(NetworkError errorType)
	{
		base.Dispatcher.Invoke(delegate
		{
			try
			{
				if (_errorType != errorType)
				{
					_errorType = errorType;
					base.Dispatcher.Invoke(delegate
					{
						switch (errorType)
						{
						case NetworkError.None:
							NetworkErrorImage.Visibility = Visibility.Collapsed;
							SyncToolTip.Content = Utils.GetString("Sync");
							break;
						case NetworkError.NoNetwork:
							NetworkErrorImage.Visibility = Visibility.Visible;
							NetworkErrorImage.SetResourceReference(Image.SourceProperty, "NoNetworkDrawingImage");
							SyncToolTip.Content = Utils.GetString("LeftBarNoNetwork");
							break;
						case NetworkError.CheckProxy:
							NetworkErrorImage.Visibility = Visibility.Visible;
							NetworkErrorImage.SetResourceReference(Image.SourceProperty, "NoConnectionDrawingImage");
							SyncToolTip.Content = Utils.GetString("LeftBarCheckProxy");
							break;
						case NetworkError.Error:
							NetworkErrorImage.Visibility = Visibility.Visible;
							NetworkErrorImage.SetResourceReference(Image.SourceProperty, "NoConnectionDrawingImage");
							SyncToolTip.Content = Utils.GetString("LeftBarNoConnection");
							break;
						}
					});
				}
			}
			catch (Exception ex)
			{
				UtilLog.Info(ex.Message);
			}
		});
	}

	public void TryDragModel(TabBarItemViewModel model)
	{
		if (model != null)
		{
			DragPopup.Tag = model;
			PopupPath.Data = model.Icon;
			PopupPath.Opacity = (model.Selected ? 1.0 : 0.4);
			PopupPath.SetResourceReference(Shape.FillProperty, model.Selected ? "LeftBarSelectedIconColor" : "LeftBarColorOpacity100");
			model.Dragging = true;
			CaptureMouse();
			base.MouseLeftButtonUp += ReleaseDrag;
			base.MouseMove += DragMove;
			_dragChanged = false;
		}
	}

	private void DragMove(object sender, MouseEventArgs e)
	{
		DragPopup.IsOpen = true;
		if (e.LeftButton != MouseButtonState.Pressed)
		{
			return;
		}
		Point position = e.GetPosition(TopTabBar);
		DragPopup.HorizontalOffset = position.X - 36.0;
		DragPopup.VerticalOffset = position.Y - 36.0;
		ContentPresenter mousePointVisibleItem = Utils.GetMousePointVisibleItem<ContentPresenter>(e, TopTabBar);
		if (mousePointVisibleItem == null || !(mousePointVisibleItem.DataContext is TabBarItemViewModel tabBarItemViewModel) || !(DragPopup.Tag is TabBarItemViewModel tabBarItemViewModel2) || tabBarItemViewModel.Equals(tabBarItemViewModel2) || Math.Abs((double)mousePointVisibleItem.GetValue(Canvas.TopProperty) - (double)(tabBarItemViewModel.SortOrder * 48)) > 5.0 || !(TopTabBar.ItemsSource is ObservableCollection<TabBarItemViewModel> source))
		{
			return;
		}
		List<TabBarItemViewModel> list = source.Where((TabBarItemViewModel i) => i.Show).ToList();
		list.Sort((TabBarItemViewModel a, TabBarItemViewModel b) => a.SortOrder.CompareTo(b.SortOrder));
		Point position2 = e.GetPosition(mousePointVisibleItem);
		list.Remove(tabBarItemViewModel2);
		int num = list.IndexOf(tabBarItemViewModel);
		if (num >= 0)
		{
			list.Insert((position2.Y > 20.0) ? (num + 1) : num, tabBarItemViewModel2);
		}
		else
		{
			list.Insert(0, tabBarItemViewModel2);
		}
		_dragChanged = true;
		foreach (TabBarItemViewModel item in list)
		{
			item.SortOrder = list.IndexOf(item);
		}
	}

	private void ReleaseDrag(object sender, MouseButtonEventArgs e)
	{
		ReleaseMouseCapture();
		base.MouseLeftButtonUp -= ReleaseDrag;
		base.MouseMove -= DragMove;
		DragPopup.IsOpen = false;
		if (DragPopup.Tag is TabBarItemViewModel tabBarItemViewModel)
		{
			tabBarItemViewModel.Dragging = false;
			DragPopup.Tag = null;
		}
		if (!_dragChanged || !(TopTabBar.ItemsSource is ObservableCollection<TabBarItemViewModel> source))
		{
			return;
		}
		List<TabBarItemViewModel> list = source.Where((TabBarItemViewModel i) => i.Show).ToList();
		list.Sort((TabBarItemViewModel a, TabBarItemViewModel b) => a.SortOrder.CompareTo(b.SortOrder));
		List<(string, long)> list2 = new List<(string, long)>();
		foreach (TabBarItemViewModel item in list)
		{
			list2.Add((TabBarModel.GetConfigName(item.Module).ToUpper(), item.SortOrder));
		}
		LocalSettings.Settings.SaveTabBarOrder(list2);
		SettingsHelper.PushLocalPreference();
		RefreshToolTip();
	}

	private void OnTestClick(object sender, MouseButtonEventArgs e)
	{
		App.Window.ShowNewUserGuide();
	}

	public int GetListIndex()
	{
		List<TabBarItemViewModel> obj = TabBarItems?.ToList();
		obj?.Sort((TabBarItemViewModel a, TabBarItemViewModel b) => a.SortOrder.CompareTo(b.SortOrder));
		return obj?.FindIndex((TabBarItemViewModel i) => i.Module == "Task") ?? 0;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tabbar/leftmenubar.xaml", UriKind.Relative);
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
			Root = (LeftMenuBar)target;
			break;
		case 2:
			BackBorder = (Border)target;
			break;
		case 3:
			RightLine = (Line)target;
			break;
		case 4:
			Avatar = (Path)target;
			break;
		case 5:
			HeadimgImage = (ImageBrush)target;
			break;
		case 6:
			HeadProImage = (Image)target;
			break;
		case 7:
			HeadFreeImage = (Image)target;
			break;
		case 8:
			((Button)target).Click += HeadClick;
			break;
		case 9:
			MenuPopup = (EscPopup)target;
			break;
		case 10:
			TopTabBar = (ItemsControl)target;
			break;
		case 11:
			DragPopup = (Popup)target;
			break;
		case 12:
			PopupPath = (Path)target;
			break;
		case 13:
			((Border)target).MouseLeftButtonDown += TryMoveWindow;
			break;
		case 14:
			NetworkErrorImage = (Image)target;
			break;
		case 15:
			((Grid)target).MouseLeftButtonUp += OnSyncClick;
			break;
		case 16:
			SyncToolTip = (ToolTip)target;
			break;
		case 17:
			SyncPath = (Path)target;
			break;
		case 18:
			NotificationButton = (Grid)target;
			NotificationButton.MouseLeftButtonUp += NotificationButtonClick;
			break;
		case 19:
			NotificationBackBorder = (Border)target;
			break;
		case 20:
			NotificationDot = (Path)target;
			break;
		case 21:
			NotificationDotBorder = (Border)target;
			break;
		case 22:
			NotificationDotText = (TextBlock)target;
			break;
		case 23:
			NotificationPopup = (EscPopup)target;
			break;
		case 24:
			NotificationPanel = (NotificationControl)target;
			break;
		case 25:
			MoreGrid = (Grid)target;
			break;
		case 26:
			((Button)target).Click += OnMoreClick;
			break;
		case 27:
			UpgradeBackBorder = (Border)target;
			break;
		case 28:
			MorePath = (Path)target;
			break;
		case 29:
			UpdateEllipse = (Grid)target;
			break;
		case 30:
			MorePopupPlacement = (ContentControl)target;
			break;
		case 31:
			MorePopup = (EscPopup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}


