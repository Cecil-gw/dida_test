using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
using ticktick_WPF.Views.Subscribe;

namespace ticktick_WPF.Views.Config;

public class SubscribeCalendar : UserControl, IComponentConnector
{
	private List<SubscribeCalendarViewModel> _items;

	internal IntegrationsAndImportTypeSelector TypeSelector;

	private bool _contentLoaded;

	public SubscribeCalendar()
	{
		InitializeComponent();
		LoadData();
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

	private void OnCalendarChanged(object sender, EventArgs e)
	{
		LoadData(needPull: false);
	}

	public async Task LoadData(bool needPull = true)
	{
		await LoadLocalCalendars();
		if (needPull)
		{
			await PullRemoteCalendars();
		}
	}

	private async Task PullRemoteCalendars()
	{
		await PullSubscribeCalendars();
		await CalendarService.PullAccountCalendarsAndEvents(DateTime.Today, DateTime.Today);
		LoadLocalCalendars();
	}

	private async Task LoadLocalCalendars()
	{
		List<SubscribeCalendarViewModel> integratedCals = (_items = await GetLocalCalendars());
		TypeSelector.LoadIntegratedItems(integratedCals);
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
		return items;
	}

	private static async Task PullSubscribeCalendars()
	{
		await CalendarService.PullSubscribeCalendars();
	}

	private async void OnBindCalendarClick(object sender, SubscribeTypeItemViewModel model)
	{
		if (model == null)
		{
			return;
		}
		if (model.IsSubscribed)
		{
			ShowSubscribedWindow(model);
			return;
		}
		switch (model.Value)
		{
		case "WeChat":
			AddClickEvent("wechat");
			Utils.TryProcessStartUrl("https://help.dida365.com/articles/6950366347985420288");
			return;
		case "EmailHelper":
		{
			AddClickEvent("ticktick_email");
			EmailTaskCodeWindow emailTaskCodeWindow = new EmailTaskCodeWindow();
			emailTaskCodeWindow.Owner = Window.GetWindow(this);
			emailTaskCodeWindow.ShowDialog();
			return;
		}
		case "Zapier":
			Utils.TryProcessStartUrl("https://blog.ticktick.com/2020/03/11/streamline-to-do-list-ticktick-zapier/");
			AddClickEvent("zapier");
			return;
		case "IFTTT":
			Utils.TryProcessStartUrl("https://blog.ticktick.com/2020/04/03/stay-connected-with-ticktick-ifttt/");
			AddClickEvent("ifttt");
			return;
		case "Gmail":
			Utils.TryProcessStartUrl("https://blog.ticktick.com/2020/06/03/handle-inbox-with-ticktick-for-gmail/");
			AddClickEvent("gmail");
			return;
		case "Spark":
			Utils.TryProcessStartUrl("https://blog.ticktick.com/2020/06/18/connect-ticktick-to-your-spark-inbox/");
			AddClickEvent("spark");
			return;
		case "OutlookEmail":
			Utils.TryProcessStartUrl("https://ticktick.com/integrations");
			AddClickEvent("outlook_email");
			return;
		case "Shortcuts":
			Utils.TryProcessStartUrl("https://help.ticktick.com/articles/7055781515422072832#siri-%26-shortcuts");
			AddClickEvent("shortcuts");
			return;
		case "Slack":
			Utils.TryProcessStartUrl("https://slack.com/marketplace/A018QN0KF53-ticktick");
			AddClickEvent("slack");
			return;
		case "Telegram":
		{
			AddClickEvent("telegram");
			TelegramAccountWindow telegramAccountWindow = new TelegramAccountWindow();
			telegramAccountWindow.Owner = Window.GetWindow(this);
			telegramAccountWindow.ShowDialog();
			return;
		}
		case "Alexa":
			Utils.TryProcessStartUrl("https://blog.ticktick.com/2020/03/11/stay-hands-free-with-ticktick-alexa/");
			AddClickEvent("amazon_alexa");
			return;
		case "Chatmind":
			Utils.TryProcessStartUrl("https://ticktick.com/integrations");
			AddClickEvent("chatmind");
			return;
		case "TickCalendar":
		{
			AddClickEvent("subscribe_ticktick");
			GetTickUrlLinkDialog getTickUrlLinkDialog = new GetTickUrlLinkDialog();
			getTickUrlLinkDialog.Owner = Window.GetWindow(this);
			getTickUrlLinkDialog.ShowDialog();
			return;
		}
		case "WXWork":
			AddClickEvent("wecom");
			break;
		default:
			AddClickEvent(model.Value.ToLower());
			break;
		}
		if (model.Value != "Google" && ((model.Value == "Notion" && !ProChecker.CheckPro(ProType.IntegrationWithNotion)) || !ProChecker.CheckPro(ProType.SubscribeCalendar)))
		{
			return;
		}
		string value = model.Value;
		if (value == null)
		{
			return;
		}
		switch (value.Length)
		{
		case 6:
			switch (value[0])
			{
			case 'G':
				if (value == "Google")
				{
					LinkGoogleCalendarWindow linkGoogleCalendarWindow = new LinkGoogleCalendarWindow();
					linkGoogleCalendarWindow.Owner = Window.GetWindow(this);
					linkGoogleCalendarWindow.ShowDialog();
				}
				return;
			case 'i':
				if (value == "iCloud" && LimitCache.CheckCalendarCount("iCloud"))
				{
					BindICloudWindow bindICloudWindow = new BindICloudWindow(this);
					bindICloudWindow.Owner = Window.GetWindow(this);
					bindICloudWindow.Topmost = false;
					bindICloudWindow.Show();
				}
				return;
			case 'W':
				if (!(value == "WXWork"))
				{
					return;
				}
				break;
			case 'F':
				if (!(value == "FeiShu"))
				{
					return;
				}
				break;
			case 'C':
				if (!(value == "CalDAV"))
				{
					return;
				}
				break;
			case 'N':
			{
				if (!(value == "Notion"))
				{
					return;
				}
				int num = (await CalendarConnectInfoDao.GetAllConnectsAsync()).Count((CalendarConnectInfo c) => c.site == "notion");
				if (num >= 3)
				{
					new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("CalendarConnectExceed"), 3), MessageBoxButton.OK, Window.GetWindow(this)).ShowDialog();
					return;
				}
				AddClickEvent("show_feature", "notion");
				if (num > 0)
				{
					Utils.TryProcessToNotionConnect(null);
					return;
				}
				NotionFeatureWindow notionFeatureWindow = new NotionFeatureWindow();
				notionFeatureWindow.Owner = Window.GetWindow(this);
				notionFeatureWindow.ShowDialog();
				return;
			}
			default:
				return;
			}
			goto IL_06e6;
		case 7:
			switch (value[0])
			{
			case 'O':
				if (value == "Outlook" && LimitCache.CheckCalendarCount("Outlook"))
				{
					BindOutlookWindow bindOutlookWindow = new BindOutlookWindow(this);
					bindOutlookWindow.Owner = Window.GetWindow(this);
					bindOutlookWindow.ShowDialog();
				}
				break;
			case 'H':
				if (value == "Holiday" && LimitCache.CheckCalendarCount("Holiday"))
				{
					AddClickEvent("holidays");
					OpenHolidaySubscribeWebview();
				}
				break;
			}
			break;
		case 8:
			switch (value[0])
			{
			default:
				return;
			case 'E':
				if (value == "Exchange" && LimitCache.CheckCalendarCount("Exchange"))
				{
					BindAccountWindow bindAccountWindow = new BindAccountWindow(this, Constants.BindAccountType.Exchange);
					bindAccountWindow.Owner = Window.GetWindow(this);
					bindAccountWindow.Show();
				}
				return;
			case 'D':
				break;
			}
			if (!(value == "DingTalk"))
			{
				break;
			}
			goto IL_06e6;
		case 3:
			if (value == "URL" && LimitCache.CheckCalendarCount("URL"))
			{
				AddUrlCalendarWindow addUrlCalendarWindow = new AddUrlCalendarWindow(this);
				addUrlCalendarWindow.Owner = Window.GetWindow(this);
				addUrlCalendarWindow.Show();
			}
			break;
		case 4:
		case 5:
			break;
			IL_06e6:
			if (LimitCache.CheckCalendarCount("CalDAV"))
			{
				BindAccountWindow bindAccountWindow2 = new BindAccountWindow(this, Constants.BindAccountType.CalDAV, (model.Value != "CalDAV") ? model.Value.ToLower() : null);
				bindAccountWindow2.Owner = Window.GetWindow(this);
				bindAccountWindow2.Show();
			}
			break;
		}
	}

	public void OpenHolidaySubscribeWebview()
	{
		string url = "/webview/holidayCalendar?lang=" + Utils.GetLanguage().ToLower() + "&dark=" + ThemeUtil.IsDark().ToString().ToLower() + "&hl=" + Utils.GetLanguage();
		NavigateWebBrowserWindow webBrowserWindow = new NavigateWebBrowserWindow(url, addLang: true, FinishHolidaySubsAction)
		{
			Title = Utils.GetString("AddHolidays"),
			Width = 455.0,
			Height = 560.0,
			Owner = Window.GetWindow(this)
		};
		webBrowserWindow.ToastAction = delegate(string message)
		{
			webBrowserWindow.ShowToastPopup(message);
		};
		webBrowserWindow.ShowDialog();
	}

	private async void FinishHolidaySubsAction(string data)
	{
		if (await SubscribeCalendarHelper.OnHolidaySubscribeSelect(data, ToastHolidaySubs))
		{
			LoadData();
		}
	}

	private void ToastHolidaySubs(string msg)
	{
		if (!SettingDialog.TryToast(SettingsType.Calendar, msg))
		{
			Utils.Toast(msg);
		}
	}

	private void AddClickEvent(string label, string ctype = "click")
	{
		UserActCollectUtils.AddClickEvent("import_integration", ctype, label);
	}

	private void ShowSubscribedWindow(SubscribeTypeItemViewModel model)
	{
		string value = model.Value;
		if (value == null)
		{
			return;
		}
		List<SubscribeCalendarViewModel> models4;
		IntegratedCalendarsWindow integratedCalendarsWindow4;
		switch (value.Length)
		{
		case 6:
			switch (value[0])
			{
			case 'G':
				if (value == "Google")
				{
					List<SubscribeCalendarViewModel> models7 = _items.Where((SubscribeCalendarViewModel m) => m.Site == "google").ToList();
					IntegratedCalendarsWindow integratedCalendarsWindow7 = new IntegratedCalendarsWindow(model.Name, model.Value, models7);
					integratedCalendarsWindow7.Owner = Window.GetWindow(this);
					integratedCalendarsWindow7.ShowDialog();
				}
				return;
			case 'i':
				if (value == "iCloud")
				{
					List<SubscribeCalendarViewModel> models8 = _items.Where((SubscribeCalendarViewModel m) => m.Kind == "icloud").ToList();
					IntegratedCalendarsWindow integratedCalendarsWindow8 = new IntegratedCalendarsWindow(model.Name, model.Value, models8);
					integratedCalendarsWindow8.Owner = Window.GetWindow(this);
					integratedCalendarsWindow8.ShowDialog();
				}
				return;
			case 'F':
				if (!(value == "FeiShu"))
				{
					return;
				}
				break;
			case 'W':
				if (!(value == "WXWork"))
				{
					return;
				}
				break;
			case 'C':
				if (!(value == "CalDAV"))
				{
					return;
				}
				break;
			case 'N':
				if (value == "Notion")
				{
					List<SubscribeCalendarViewModel> models6 = _items.Where((SubscribeCalendarViewModel m) => m.Site == "notion").ToList();
					IntegratedCalendarsWindow integratedCalendarsWindow6 = new IntegratedCalendarsWindow(model.Name, model.Value, models6);
					integratedCalendarsWindow6.Owner = Window.GetWindow(this);
					integratedCalendarsWindow6.ShowDialog();
				}
				return;
			default:
				return;
			}
			goto IL_02d3;
		case 7:
			switch (value[0])
			{
			case 'O':
				if (value == "Outlook")
				{
					List<SubscribeCalendarViewModel> models3 = _items.Where((SubscribeCalendarViewModel m) => m.Site == "outlook").ToList();
					IntegratedCalendarsWindow integratedCalendarsWindow3 = new IntegratedCalendarsWindow(model.Name, model.Value, models3);
					integratedCalendarsWindow3.Owner = Window.GetWindow(this);
					integratedCalendarsWindow3.ShowDialog();
				}
				break;
			case 'H':
				if (value == "Holiday")
				{
					List<SubscribeCalendarViewModel> models2 = _items.Where((SubscribeCalendarViewModel m) => m.Kind == "holiday").ToList();
					IntegratedCalendarsWindow integratedCalendarsWindow2 = new IntegratedCalendarsWindow(model.Name, model.Value, models2);
					integratedCalendarsWindow2.Owner = Window.GetWindow(this);
					integratedCalendarsWindow2.ParentView = this;
					integratedCalendarsWindow2.ShowDialog();
				}
				break;
			}
			break;
		case 8:
			switch (value[0])
			{
			default:
				return;
			case 'E':
				if (value == "Exchange")
				{
					List<SubscribeCalendarViewModel> models5 = _items.Where((SubscribeCalendarViewModel m) => m.Kind == "exchange").ToList();
					IntegratedCalendarsWindow integratedCalendarsWindow5 = new IntegratedCalendarsWindow(model.Name, model.Value, models5);
					integratedCalendarsWindow5.Owner = Window.GetWindow(this);
					integratedCalendarsWindow5.ShowDialog();
				}
				return;
			case 'D':
				break;
			}
			if (!(value == "DingTalk"))
			{
				break;
			}
			goto IL_02d3;
		case 3:
			if (value == "URL")
			{
				List<SubscribeCalendarViewModel> models = (from x in _items
					where x.Kind == "url"
					orderby x.SortOrder
					select x).ToList();
				IntegratedCalendarsWindow integratedCalendarsWindow = new IntegratedCalendarsWindow(model.Name, model.Value, models);
				integratedCalendarsWindow.Owner = Window.GetWindow(this);
				integratedCalendarsWindow.ShowDialog();
			}
			break;
		case 4:
		case 5:
			break;
			IL_02d3:
			models4 = (from x in _items
				where x.Kind == "caldav"
				orderby x.SortOrder
				select x).ToList();
			integratedCalendarsWindow4 = new IntegratedCalendarsWindow(model.Name, "CalDAV", models4);
			integratedCalendarsWindow4.Owner = Window.GetWindow(this);
			integratedCalendarsWindow4.ShowDialog();
			break;
		}
	}

	public async void ShowCalDetail(SubscribeCalendarViewModel model)
	{
		if (model.IsConnect)
		{
			if (model.Site == "google")
			{
				CalendarConnectInfo calendarConnectInfo = await CalendarConnectInfoDao.GetInfoByIdAsync(model.CalendarId);
				if (calendarConnectInfo != null)
				{
					calendarConnectInfo.GetItems();
					CalendarConnectInfoWindow calendarConnectInfoWindow = new CalendarConnectInfoWindow(calendarConnectInfo);
					calendarConnectInfoWindow.Owner = Window.GetWindow(this);
					calendarConnectInfoWindow.ShowDialog();
				}
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
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/subscribecalendar.xaml", UriKind.Relative);
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
		if (connectionId == 1)
		{
			TypeSelector = (IntegrationsAndImportTypeSelector)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
