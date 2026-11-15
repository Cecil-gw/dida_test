using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Config;

namespace ticktick_WPF.Views.Subscribe;

public class IntegratedCalendarsWindow : Window, IComponentConnector
{
	private readonly string _type;

	private bool _needCheck;

	internal TextBlock TitleText;

	internal Border NotionHelper;

	internal TextBlock MessageText;

	internal ItemsControl CalendarItems;

	internal StackPanel AddCalendarPanel;

	internal TextBlock AddCalendarText;

	private bool _contentLoaded;

	public SubscribeCalendar ParentView { get; set; }

	public IntegratedCalendarsWindow(string title, string type, List<SubscribeCalendarViewModel> models)
	{
		InitializeComponent();
		if (type == "Notion")
		{
			NotionHelper.Visibility = Visibility.Visible;
		}
		_type = type;
		TitleText.Text = title;
		SetMessage(_type);
		CalendarItems.ItemsSource = models;
		base.Loaded += delegate
		{
			DataChangedNotifier.CalendarChanged += OnCalendarChanged;
			DataChangedNotifier.CalendarConnectChanged += OnCalendarChanged;
			GlobalEventManager.ReloadCalendar += OnCalendarChanged;
		};
		base.Unloaded += delegate
		{
			DataChangedNotifier.CalendarChanged -= OnCalendarChanged;
			DataChangedNotifier.CalendarConnectChanged -= OnCalendarChanged;
			GlobalEventManager.ReloadCalendar -= OnCalendarChanged;
		};
	}

	private void SetMessage(string type)
	{
		string text = string.Empty;
		switch (_type)
		{
		case "Google":
			text = Utils.GetString("ViewGoogleCalendars");
			break;
		case "Outlook":
			text = Utils.GetString("ViewOutlookCalendars");
			break;
		case "iCloud":
			text = Utils.GetString("ViewiCloudCalendars");
			break;
		case "Exchange":
			text = Utils.GetString("ViewExchangeCalendars");
			break;
		case "FeiShu":
		case "WXWork":
		case "CalDAV":
		case "DingTalk":
			text = Utils.GetString("ViewCaldavCalendars");
			break;
		case "Holiday":
			text = string.Format(Utils.GetString("ViewHolidaysAddedIn"), Utils.GetAppName());
			AddCalendarText.Text = Utils.GetString("AddHolidays");
			if (Utils.IsWindows7())
			{
				AddCalendarPanel.Visibility = Visibility.Collapsed;
			}
			break;
		case "URL":
			text = Utils.GetString("ViewUrlCalendars");
			break;
		case "Notion":
			text = Utils.GetString("ViewNotionCalendars");
			AddCalendarText.Text = Utils.GetString("add_workspace");
			break;
		}
		if (text != null)
		{
			MessageText.Text = string.Format(text, Utils.GetAppName());
			MessageText.Visibility = Visibility.Visible;
		}
	}

	private async void OnCalendarChanged(object sender, EventArgs e)
	{
		List<SubscribeCalendarViewModel> list = await GetLocalCalendars();
		switch (_type)
		{
		case "Google":
			list = list.Where((SubscribeCalendarViewModel m) => m.Site == "google").ToList();
			break;
		case "Outlook":
			list = list.Where((SubscribeCalendarViewModel m) => m.Site == "outlook").ToList();
			break;
		case "iCloud":
			list = list.Where((SubscribeCalendarViewModel m) => m.Kind == "icloud").ToList();
			break;
		case "Exchange":
			list = list.Where((SubscribeCalendarViewModel m) => m.Kind == "exchange").ToList();
			break;
		case "FeiShu":
		case "WXWork":
		case "CalDAV":
		case "DingTalk":
			list = list.Where((SubscribeCalendarViewModel m) => m.Kind == "caldav").ToList();
			break;
		case "Holiday":
			list = list.Where((SubscribeCalendarViewModel m) => m.Kind == "holiday").ToList();
			break;
		case "URL":
			list = list.Where((SubscribeCalendarViewModel m) => m.Kind == "url").ToList();
			break;
		case "Notion":
			list = list.Where((SubscribeCalendarViewModel m) => m.Site == "notion").ToList();
			break;
		}
		if (list.Count == 0)
		{
			Close();
		}
		else
		{
			CalendarItems.ItemsSource = list;
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private static async Task<List<SubscribeCalendarViewModel>> GetLocalCalendars()
	{
		List<SubscribeCalendarViewModel> items = new List<SubscribeCalendarViewModel>();
		List<BindCalendarAccountModel> list = await BindCalendarAccountDao.GetBindCalendarAccounts();
		if (list != null && list.Count > 0)
		{
			HashSet<string> accountIds = new HashSet<string>();
			list.Sort(delegate(BindCalendarAccountModel a, BindCalendarAccountModel b)
			{
				int accountType = SubscribeCalendarHelper.GetAccountType(a);
				int accountType2 = SubscribeCalendarHelper.GetAccountType(b);
				return (accountType == accountType2 && a.CreatedTime.HasValue) ? a.CreatedTime.Value.CompareTo(b.CreatedTime) : accountType.CompareTo(accountType2);
			});
			list.ForEach(delegate(BindCalendarAccountModel account)
			{
				if (string.IsNullOrEmpty(account.Account) || accountIds.Contains(account.Id))
				{
					App.Connection.DeleteAsync(account);
				}
				else
				{
					items.Add(new SubscribeCalendarViewModel(account));
					accountIds.Add(account.Id);
				}
			});
		}
		List<CalendarConnectInfo> list2 = await CalendarConnectInfoDao.GetAllConnectsAsync();
		if (list2 != null && list2.Count > 0)
		{
			List<SubscribeCalendarViewModel> list3 = new List<SubscribeCalendarViewModel>();
			foreach (CalendarConnectInfo item in list2)
			{
				item.GetItems();
				list3.Add(new SubscribeCalendarViewModel(item));
			}
			foreach (SubscribeCalendarViewModel item2 in list3)
			{
				items.Add(item2);
			}
		}
		List<CalendarSubscribeProfileModel> list4 = await CalendarSubscribeProfileDao.GetProfiles();
		if (list4 != null && list4.Any())
		{
			list4.Sort((CalendarSubscribeProfileModel a, CalendarSubscribeProfileModel b) => a.CreatedTime?.CompareTo(b.CreatedTime) ?? (-1));
			list4.ForEach(delegate(CalendarSubscribeProfileModel sub)
			{
				items.Add(new SubscribeCalendarViewModel(sub));
			});
		}
		items = items.OrderBy((SubscribeCalendarViewModel x) => x.SortOrder).ToList();
		return items;
	}

	public void CheckOnActive()
	{
		_needCheck = true;
	}

	protected override void OnActivated(EventArgs e)
	{
		if (_needCheck)
		{
			_needCheck = false;
			CalendarService.PullCalendarConnects();
		}
		base.OnActivated(e);
	}

	private void OnNotionHelpClick(object sender, MouseButtonEventArgs e)
	{
		Utils.TryProcessStartUrl(Utils.IsDida() ? "https://help.dida365.com/articles/7263009022960205824" : "https://help.ticktick.com/articles/7263009660070789120");
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

	private void OnAddCalendarClick(object sender, MouseButtonEventArgs e)
	{
		OnAction(_type);
	}

	private void OnAction(string tag)
	{
		if (tag != "Google" && (!ProChecker.CheckPro(ProType.SubscribeCalendar) || !LimitCache.CheckCalendarCount(tag)))
		{
			return;
		}
		UserActCollectUtils.AddClickEvent("project_list_ui", "event", "add");
		if (tag == null)
		{
			return;
		}
		switch (tag.Length)
		{
		case 6:
			switch (tag[0])
			{
			case 'G':
				if (tag == "Google")
				{
					LinkGoogleCalendarWindow linkGoogleCalendarWindow = new LinkGoogleCalendarWindow();
					linkGoogleCalendarWindow.Owner = Window.GetWindow(this);
					linkGoogleCalendarWindow.ShowDialog();
				}
				break;
			case 'C':
				if (tag == "CalDAV")
				{
					BindAccountWindow bindAccountWindow2 = new BindAccountWindow(null, Constants.BindAccountType.CalDAV);
					bindAccountWindow2.Owner = Window.GetWindow(this);
					bindAccountWindow2.Show();
				}
				break;
			case 'i':
				if (tag == "iCloud")
				{
					BindICloudWindow bindICloudWindow = new BindICloudWindow(null);
					bindICloudWindow.Owner = Window.GetWindow(this);
					bindICloudWindow.Topmost = false;
					bindICloudWindow.Show();
				}
				break;
			case 'N':
				if (tag == "Notion")
				{
					Utils.TryProcessToNotionConnect(null);
				}
				break;
			}
			break;
		case 7:
			switch (tag[0])
			{
			case 'O':
				if (tag == "Outlook")
				{
					new BindOutlookWindow().ShowDialog();
				}
				break;
			case 'H':
				if (tag == "Holiday")
				{
					ParentView?.OpenHolidaySubscribeWebview();
				}
				break;
			}
			break;
		case 8:
			if (tag == "Exchange")
			{
				BindAccountWindow bindAccountWindow = new BindAccountWindow(null, Constants.BindAccountType.Exchange);
				bindAccountWindow.Owner = Window.GetWindow(this);
				bindAccountWindow.Show();
			}
			break;
		case 3:
			if (tag == "URL")
			{
				AddUrlCalendarWindow addUrlCalendarWindow = new AddUrlCalendarWindow();
				addUrlCalendarWindow.Owner = Window.GetWindow(this);
				addUrlCalendarWindow.Show();
			}
			break;
		case 4:
		case 5:
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/subscribe/integratedcalendarswindow.xaml", UriKind.Relative);
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
			((Border)target).MouseLeftButtonDown += OnDragMove;
			break;
		case 2:
			TitleText = (TextBlock)target;
			break;
		case 3:
			NotionHelper = (Border)target;
			NotionHelper.MouseLeftButtonUp += OnNotionHelpClick;
			break;
		case 4:
			MessageText = (TextBlock)target;
			break;
		case 5:
			CalendarItems = (ItemsControl)target;
			break;
		case 6:
			AddCalendarPanel = (StackPanel)target;
			AddCalendarPanel.MouseLeftButtonUp += OnAddCalendarClick;
			break;
		case 7:
			AddCalendarText = (TextBlock)target;
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
