using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Subscribe;

namespace ticktick_WPF.Views.Config;

public class IntegrationsAndImportTypeSelectWindow : Window, IComponentConnector
{
	private readonly SubscribeCalendar _parent;

	internal ScrollViewer ScrollViewer;

	internal IntegrationsAndImportTypeSelector TypeSelector;

	private bool _contentLoaded;

	public IntegrationsAndImportTypeSelectWindow(SubscribeCalendar sc, double scrollTo = 0.0)
	{
		InitializeComponent();
		_parent = sc;
		ScrollViewer.ScrollToVerticalOffset(scrollTo);
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnBindCalendarClick(object sender, SubscribeTypeItemViewModel model)
	{
		if (model == null || (model.Name != "Google" && !ProChecker.CheckPro(ProType.SubscribeCalendar)))
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
			switch (value[4])
			{
			default:
				return;
			case 'l':
				if (value == "Google")
				{
					LinkGoogleCalendarWindow linkGoogleCalendarWindow = new LinkGoogleCalendarWindow();
					linkGoogleCalendarWindow.Owner = Window.GetWindow(this);
					linkGoogleCalendarWindow.ShowDialog();
				}
				return;
			case 'u':
				if (value == "iCloud" && LimitCache.CheckCalendarCount("iCloud"))
				{
					BindICloudWindow bindICloudWindow = new BindICloudWindow(_parent);
					bindICloudWindow.Owner = Window.GetWindow(this);
					bindICloudWindow.Show();
				}
				return;
			case 'r':
				if (!(value == "WXWork"))
				{
					return;
				}
				break;
			case 'h':
				if (!(value == "FeiShu"))
				{
					return;
				}
				break;
			case 'A':
				if (!(value == "CalDAV"))
				{
					return;
				}
				break;
			case 'o':
				if (value == "Notion")
				{
					Utils.TryProcessToNotionConnect(null);
				}
				return;
			case 'a':
				if (value == "WeChat")
				{
					WeChatWindow weChatWindow = new WeChatWindow();
					weChatWindow.Owner = Window.GetWindow(this);
					weChatWindow.ShowDialog();
				}
				return;
			}
			goto IL_0265;
		case 8:
			switch (value[0])
			{
			default:
				return;
			case 'E':
				if (value == "Exchange" && LimitCache.CheckCalendarCount("Exchange"))
				{
					BindAccountWindow bindAccountWindow = new BindAccountWindow(_parent, Constants.BindAccountType.Exchange);
					bindAccountWindow.Owner = Window.GetWindow(this);
					bindAccountWindow.Show();
				}
				return;
			case 'D':
				break;
			case 'T':
				if (value == "Telegram")
				{
					TelegramAccountWindow telegramAccountWindow = new TelegramAccountWindow();
					telegramAccountWindow.Owner = Window.GetWindow(this);
					telegramAccountWindow.ShowDialog();
				}
				return;
			}
			if (!(value == "DingTalk"))
			{
				break;
			}
			goto IL_0265;
		case 7:
			if (value == "Outlook" && LimitCache.CheckCalendarCount("Outlook"))
			{
				BindOutlookWindow bindOutlookWindow = new BindOutlookWindow(_parent);
				bindOutlookWindow.Owner = Window.GetWindow(this);
				bindOutlookWindow.ShowDialog();
			}
			break;
		case 3:
			if (value == "URL" && LimitCache.CheckCalendarCount("URL"))
			{
				AddUrlCalendarWindow addUrlCalendarWindow = new AddUrlCalendarWindow(_parent);
				addUrlCalendarWindow.Owner = Window.GetWindow(this);
				addUrlCalendarWindow.Show();
			}
			break;
		case 12:
			if (value == "TickCalendar")
			{
				GetTickUrlLinkDialog getTickUrlLinkDialog = new GetTickUrlLinkDialog();
				getTickUrlLinkDialog.Owner = Window.GetWindow(this);
				getTickUrlLinkDialog.ShowDialog();
			}
			break;
		case 11:
			if (value == "EmailHelper")
			{
				EmailTaskCodeWindow emailTaskCodeWindow = new EmailTaskCodeWindow();
				emailTaskCodeWindow.Owner = Window.GetWindow(this);
				emailTaskCodeWindow.ShowDialog();
			}
			break;
		case 5:
			if (value == "Other")
			{
				Utils.TryProcessStartUrlWithToken("/webapp/#settings/profile");
			}
			break;
		case 4:
		case 9:
		case 10:
			break;
			IL_0265:
			if (LimitCache.CheckCalendarCount("CalDAV"))
			{
				BindAccountWindow bindAccountWindow2 = new BindAccountWindow(_parent, Constants.BindAccountType.CalDAV);
				bindAccountWindow2.Owner = Window.GetWindow(this);
				bindAccountWindow2.Show();
			}
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
			Uri resourceLocator = new Uri("/TickTick;component/views/config/integrationsandimporttypeselectwindow.xaml", UriKind.Relative);
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
			ScrollViewer = (ScrollViewer)target;
			break;
		case 2:
			TypeSelector = (IntegrationsAndImportTypeSelector)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
