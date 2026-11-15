using System;
using System.CodeDom.Compiler;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Framework.EmbeddedBrowser;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Views.Calendar;

public class BindOutlookWindow : Window, IComponentConnector
{
	private SubscribeCalendar _parent;

	private string _originAccount;

	private IEmbeddedBrowser _browser;

	private bool _closed;

	internal ContentControl BrowserHost;

	internal StackPanel RuntimeInstallPanel;

	internal TextBlock RuntimeStatusText;

	internal ProgressBar RuntimeDownloadProgress;

	private bool _contentLoaded;

	public BindOutlookWindow(SubscribeCalendar parent = null)
	{
		InitializeComponent();
		_parent = parent;
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		if (_browser == null)
		{
			InitializeBrowserAsync();
		}
	}

	private async Task InitializeBrowserAsync()
	{
		_ = 3;
		try
		{
			EmbeddedBrowserOptions options = new EmbeddedBrowserOptions
			{
				UseCase = "outlook_bind",
				OpenNewWindowsExternally = false,
				EnableHostObjects = false
			};
			IEmbeddedBrowser browser = (_browser = EmbeddedBrowserFactory.Create(options));
			if (browser.Engine == EmbeddedBrowserEngine.LegacyIe)
			{
				WinInetHelper.SupressCookiePersist();
			}
			browser.NavigationStarting += OnNavigationStarting;
			browser.NavigationCompleted += OnNavigationCompleted;
			browser.ProcessFailed += OnBrowserProcessFailed;
			BrowserHost.Content = browser.View;
			using (await EmbeddedBrowserLifecycle.EnterAsync(browser.Engine))
			{
				if (!IsBrowserActive(browser))
				{
					return;
				}
				EmbeddedBrowserPreparationResult embeddedBrowserPreparationResult = await EmbeddedBrowserRuntimeInstaller.PrepareAndInitializeAsync(browser, options, this, OnRuntimeInstallProgress);
				if (embeddedBrowserPreparationResult != EmbeddedBrowserPreparationResult.Ready)
				{
					if (!_closed)
					{
						if (embeddedBrowserPreparationResult == EmbeddedBrowserPreparationResult.RestartRequired)
						{
							Close();
						}
						else
						{
							ShowBrowserFailureAndClose();
						}
					}
					return;
				}
				if (!IsBrowserActive(browser))
				{
					return;
				}
				RuntimeInstallPanel.Visibility = Visibility.Collapsed;
				BrowserHost.Visibility = Visibility.Visible;
				await browser.DeleteAllCookiesAsync(new Uri("https://login.live.com/oauth20_authorize.srf"));
				if (!IsBrowserActive(browser))
				{
					return;
				}
				await browser.DeleteAllCookiesAsync(new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/authorize"));
				if (!IsBrowserActive(browser))
				{
					return;
				}
				browser.Navigate(new Uri(BaseUrl.GetApiDomain() + "/pub/api/v2/calendar/bind/outlook"));
			}
		}
		catch (EmbeddedBrowserRuntimeMissingException)
		{
			if (!_closed)
			{
				Close();
			}
		}
		catch (Exception ex2)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex2));
			if (!_closed)
			{
				Utils.Toast(Utils.GetString("VerificationFailedHaveTry"));
				Close();
			}
		}
	}

	private bool IsBrowserActive(IEmbeddedBrowser browser)
	{
		if (!_closed)
		{
			return _browser == browser;
		}
		return false;
	}

	private void OnRuntimeInstallProgress(EmbeddedBrowserRuntimeInstallProgress progress)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				OnRuntimeInstallProgress(progress);
			});
		}
		else if (!_closed)
		{
			BrowserHost.Visibility = Visibility.Collapsed;
			RuntimeInstallPanel.Visibility = Visibility.Visible;
			RuntimeStatusText.Text = progress.Message;
			RuntimeDownloadProgress.IsIndeterminate = progress.IsInstalling || !progress.Percentage.HasValue;
			if (progress.Percentage.HasValue)
			{
				RuntimeDownloadProgress.Value = progress.Percentage.Value;
			}
		}
	}

	private void OnNavigationStarting(object sender, EmbeddedBrowserNavigationStartingEventArgs e)
	{
		IEmbeddedBrowser browser = _browser;
		if (_closed || browser == null)
		{
			return;
		}
		Uri uri = new Uri("https://" + BaseUrl.GetDomain() + "/calendar/bind/outlook");
		if (!(e.Uri == null) && e.Uri.Scheme.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase) && e.Uri.Host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase) && e.Uri.Port == uri.Port && e.Uri.AbsolutePath.Equals(uri.AbsolutePath, StringComparison.Ordinal))
		{
			NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(e.Uri.Query);
			string text = nameValueCollection["code"];
			if (!string.IsNullOrEmpty(text) && string.Equals(nameValueCollection["state"], "outlook", StringComparison.Ordinal))
			{
				e.Cancel = true;
				browser.Stop();
				EmbeddedBrowserTelemetry.Record("outlook_bind", browser.Engine, browser.RuntimeVersion, "callback_success_outlook", null, null);
				BindOutlook(text);
				Close();
			}
		}
	}

	private void OnNavigationCompleted(object sender, EmbeddedBrowserNavigationCompletedEventArgs e)
	{
		if (!_closed && !e.IsSuccess)
		{
			ShowBrowserFailureAndClose();
		}
	}

	private void OnBrowserProcessFailed(object sender, EmbeddedBrowserProcessFailedEventArgs e)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				OnBrowserProcessFailed(sender, e);
			});
		}
		else if (!_closed)
		{
			ShowBrowserFailureAndClose();
		}
	}

	private void ShowBrowserFailureAndClose()
	{
		Utils.Toast(Utils.GetString("VerificationFailedHaveTry"));
		Close();
	}

	private async void OnWindowClosed(object sender, EventArgs e)
	{
		_closed = true;
		IEmbeddedBrowser browser = _browser;
		try
		{
			if (browser != null)
			{
				using (await EmbeddedBrowserLifecycle.EnterAsync(browser.Engine))
				{
					await browser.DisposeAsync();
				}
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		finally
		{
			_browser = null;
			BrowserHost.Content = null;
		}
	}

	private async void BindOutlook(string code)
	{
		BindCalendarAccountModel calendar = await Communicator.SubscribeOutlook(code);
		if (calendar != null)
		{
			if (!string.IsNullOrEmpty(calendar.Id))
			{
				await BindCalendarAccountDao.DeleteBindAccountById(calendar.Id);
			}
			if (!string.IsNullOrEmpty(_originAccount))
			{
				await BindCalendarAccountDao.DeleteBindAccountById(_originAccount);
			}
			await SubscribeCalendarHelper.SaveBindAccount(calendar);
			CalendarService.PullAccountEvents(calendar.Id, null, calendar.Site);
			_parent?.LoadData(needPull: false);
			ListViewContainer.ReloadProjectData();
		}
		else
		{
			Utils.Toast(Utils.GetString("VerificationFailedHaveTry"));
		}
	}

	public void SetOriginAccount(string accountId)
	{
		_originAccount = accountId;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/bindoutlookwindow.xaml", UriKind.Relative);
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
			((BindOutlookWindow)target).Loaded += OnWindowLoaded;
			((BindOutlookWindow)target).Closed += OnWindowClosed;
			break;
		case 2:
			BrowserHost = (ContentControl)target;
			break;
		case 3:
			RuntimeInstallPanel = (StackPanel)target;
			break;
		case 4:
			RuntimeStatusText = (TextBlock)target;
			break;
		case 5:
			RuntimeDownloadProgress = (ProgressBar)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
