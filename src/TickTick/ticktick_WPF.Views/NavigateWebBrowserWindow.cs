using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Framework.EmbeddedBrowser;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class NavigateWebBrowserWindow : Window, IComponentConnector
{
	private string _url;

	private readonly string _useCase;

	public string LogFileName = "FbLog";

	private Action<string> _closeAction;

	private JavaScriptTicketOptions _scriptOption;

	private IEmbeddedBrowser _browser;

	private Uri _browserUri;

	private readonly HashSet<Uri> _authenticationCookieUris = new HashSet<Uri>();

	private EmbeddedBrowserAuthenticationCookieLease _authenticationCookieLease;

	private Task _browserInitializationTask = Task.CompletedTask;

	private bool _closed;

	private bool _closeCleanupStarted;

	private bool _canClose;

	private bool _requiresBrowserRecreation;

	private int _automaticUnknownNavigationRecreationCount;

	private Task _browserRecreationTask = Task.CompletedTask;

	private const int MaxAutomaticUnknownNavigationRecreationCount = 1;

	internal Grid Container;

	internal ContentControl BrowserHost;

	internal Grid NoNetworkImage;

	internal Grid BrowserProgressPanel;

	internal LoadingIndicator BrowserLoading;

	internal TextBlock BrowserStatusText;

	internal ProgressBar RuntimeDownloadProgress;

	internal Popup ToastPopup;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	private bool _contentLoaded;

	public Action<string> ToastAction { get; set; }

	public NavigateWebBrowserWindow(string url, bool addLang = true, Action<string> closeAction = null)
	{
		try
		{
			int dotNetReleaseKey = Utils.GetDotNetReleaseKey();
			UtilLog.Info("LocalTimeZone:" + TimeZoneData.LocalTimeZoneModel.DisplayName + "   " + TimeZoneData.LocalTimeZoneModel.TimeZoneName);
			UtilLog.Info("taskCount : " + TaskCache.LocalTaskViewModels.Count);
			UtilLog.Info("db size : " + new FileInfo(AppPaths.TickTickDbPath).Length);
			UtilLog.Info("dotnet releaseKey : " + dotNetReleaseKey);
		}
		catch (Exception)
		{
		}
		_url = (addLang ? UriUtils.AddLangAttribute(url) : url);
		_url = UriUtils.AddAttribute(_url, "dark", ThemeUtil.IsDark().ToString().ToLower());
		DateFormatType sysDateFormatType = ticktick_WPF.Util.DateUtils.GetSysDateFormatType();
		string url2 = _url;
		int num = (int)sysDateFormatType;
		_url = UriUtils.AddAttribute(url2, "df", num.ToString());
		_useCase = GetUseCase(_url);
		InitializeComponent();
		AppLockWindowManager.TrackPopup(ToastPopup);
		_closeAction = closeAction;
		base.Closing += OnWindowClosing;
		base.Closed += OnWindowClosed;
	}

	private static string GetFeedbackDomain()
	{
		return BaseUrl.GetDomainUrl();
	}

	internal static string GetUseCase(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return "internal_web_other";
		}
		if (url.StartsWith("/v3/tickets/detail?id=", StringComparison.OrdinalIgnoreCase))
		{
			return "feedback_detail";
		}
		if (url.StartsWith("/v3/tickets/", StringComparison.OrdinalIgnoreCase))
		{
			return "feedback";
		}
		if (url.StartsWith("/webview/matrixRule", StringComparison.OrdinalIgnoreCase))
		{
			return "matrix_example";
		}
		if (url.IndexOf("/webview/holidayCalendar", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "holiday_subscription";
		}
		if (url.IndexOf("/webview/paymentHistory", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "order_history";
		}
		if (url.IndexOf("/webview/templates/project/", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "project_template";
		}
		return "internal_web_other";
	}

	internal static bool CanUseExternalFallback(string useCase)
	{
		if (!string.Equals(useCase, "feedback", StringComparison.Ordinal) && !string.Equals(useCase, "feedback_detail", StringComparison.Ordinal))
		{
			return string.Equals(useCase, "order_history", StringComparison.Ordinal);
		}
		return true;
	}

	internal static Uri GetAuthenticationCookieScopeUri(Uri pageUri, Uri applicationApiUri)
	{
		if (pageUri == null)
		{
			throw new ArgumentNullException("pageUri");
		}
		if (applicationApiUri == null)
		{
			throw new ArgumentNullException("applicationApiUri");
		}
		string host = applicationApiUri.Host;
		return new Uri(string.Concat(str2: IsHostOrSubdomain(host, "ticktick.com") ? "ticktick.com" : (IsHostOrSubdomain(host, "dida365.com") ? "dida365.com" : ((!IsHostOrSubdomain(host, "365dida.com")) ? pageUri.Host : "365dida.com")), str0: pageUri.Scheme, str1: "://", str3: "/"), UriKind.Absolute);
	}

	private static bool IsHostOrSubdomain(string host, string domain)
	{
		if (!string.Equals(host, domain, StringComparison.OrdinalIgnoreCase))
		{
			return host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		_browserInitializationTask = InitializeBrowserAsync();
	}

	private async Task InitializeBrowserAsync()
	{
		_ = 4;
		try
		{
			bool shouldOpenExternalFallback = false;
			Uri baseUri = new Uri(GetFeedbackDomain(), UriKind.Absolute);
			EmbeddedBrowserOptions options = new EmbeddedBrowserOptions
			{
				UseCase = _useCase,
				UserAgentSuffix = "/TickTick/PC",
				OpenNewWindowsExternally = true,
				EnableHostObjects = true,
				RestrictNavigationToTrustedOrigins = true
			};
			options.AddTrustedOrigin(baseUri);
			IEmbeddedBrowser browser = (_browser = EmbeddedBrowserFactory.Create(options));
			browser.NavigationCompleted += OnNavigationCompleted;
			browser.NewWindowRequested += OnNewWindowRequested;
			browser.ProcessFailed += OnProcessFailed;
			BrowserHost.Content = browser.View;
			_browserUri = new Uri(baseUri, _url);
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
							shouldOpenExternalFallback = true;
						}
					}
				}
				else
				{
					if (!IsBrowserActive(browser))
					{
						return;
					}
					string token = UserManager.GetToken();
					DateTime expires = DateTime.UtcNow.AddDays(20.0);
					_authenticationCookieUris.Clear();
					_authenticationCookieUris.Add(baseUri);
					Uri authenticationCookieScopeUri = GetAuthenticationCookieScopeUri(baseUri, new Uri(BaseUrl.GetApiDomain(), UriKind.Absolute));
					_authenticationCookieUris.Add(authenticationCookieScopeUri);
					await AcquireAuthenticationCookieLeaseAsync(browser, authenticationCookieScopeUri, token, expires);
					if (!IsBrowserActive(browser))
					{
						return;
					}
					_scriptOption = new JavaScriptTicketOptions(this);
					await browser.RegisterHostObjectAsync("external", _scriptOption);
					if (!IsBrowserActive(browser))
					{
						return;
					}
					browser.Navigate(_browserUri);
				}
			}
			if (shouldOpenExternalFallback && !_closed)
			{
				await OpenExternalFallbackOrShowErrorAsync();
			}
		}
		catch (EmbeddedBrowserRuntimeMissingException)
		{
			if (!_closed)
			{
				ShowLoadError(requiresBrowserRecreation: true);
			}
		}
		catch (Exception ex2)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex2));
			if (!_closed)
			{
				ShowLoadError(requiresBrowserRecreation: true);
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

	private async Task AcquireAuthenticationCookieLeaseAsync(IEmbeddedBrowser browser, Uri cookieScopeUri, string token, DateTime expires)
	{
		_authenticationCookieLease = await EmbeddedBrowserAuthenticationCookieLease.AcquireAsync(browser, cookieScopeUri, _authenticationCookieUris, token, expires, () => IsBrowserActive(browser));
	}

	private async Task ReleaseAuthenticationCookieLeaseAsync(IEmbeddedBrowser browser)
	{
		if (browser != null)
		{
			EmbeddedBrowserAuthenticationCookieLease authenticationCookieLease = _authenticationCookieLease;
			_authenticationCookieLease = null;
			if (authenticationCookieLease != null)
			{
				await authenticationCookieLease.ReleaseAsync();
			}
		}
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
			NoNetworkImage.Visibility = Visibility.Collapsed;
			BrowserProgressPanel.Visibility = Visibility.Visible;
			BrowserStatusText.Text = progress.Message;
			BrowserStatusText.Visibility = Visibility.Visible;
			RuntimeDownloadProgress.Visibility = ((!progress.Percentage.HasValue && !progress.IsInstalling) ? Visibility.Collapsed : Visibility.Visible);
			RuntimeDownloadProgress.IsIndeterminate = progress.IsInstalling;
			if (progress.Percentage.HasValue)
			{
				RuntimeDownloadProgress.Value = progress.Percentage.Value;
			}
		}
	}

	private void OnNavigationCompleted(object sender, EmbeddedBrowserNavigationCompletedEventArgs e)
	{
		IEmbeddedBrowser browser = sender as IEmbeddedBrowser;
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (IsBrowserActive(browser))
			{
				if (e.IsSuccess)
				{
					_automaticUnknownNavigationRecreationCount = 0;
					_requiresBrowserRecreation = false;
					BrowserProgressPanel.Visibility = Visibility.Collapsed;
					BrowserHost.Visibility = Visibility.Visible;
					NoNetworkImage.Visibility = Visibility.Collapsed;
				}
				else if (ShouldAutomaticallyRecreateBrowser(browser.Engine, e.Error, _automaticUnknownNavigationRecreationCount) && _browserUri != null)
				{
					_automaticUnknownNavigationRecreationCount++;
					_requiresBrowserRecreation = true;
					EmbeddedBrowserTelemetry.Record(_useCase, browser.Engine, browser.RuntimeVersion, "navigation_recreate_unknown", null, null, e.Error);
					StartBrowserRecreation(browser);
				}
				else
				{
					ShowLoadError(IsWebView2UnknownNavigationFailure(browser.Engine, e.Error));
				}
			}
		});
	}

	internal static bool ShouldAutomaticallyRecreateBrowser(EmbeddedBrowserEngine engine, string error, int recreationCount)
	{
		if (recreationCount < 1)
		{
			return IsWebView2UnknownNavigationFailure(engine, error);
		}
		return false;
	}

	private static bool IsWebView2UnknownNavigationFailure(EmbeddedBrowserEngine engine, string error)
	{
		if (engine == EmbeddedBrowserEngine.WebView2)
		{
			return string.Equals(error, "Unknown", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private void StartBrowserRecreation(IEmbeddedBrowser browser)
	{
		if (!_closed && browser != null && _browserRecreationTask.IsCompleted)
		{
			_browserRecreationTask = RecreateBrowserAsync(browser);
		}
	}

	private async Task RecreateBrowserAsync(IEmbeddedBrowser browser)
	{
		_ = 1;
		try
		{
			if (!IsBrowserActive(browser))
			{
				return;
			}
			ShowLoading();
			if (_browser == browser)
			{
				await DisposeBrowserAsync(browser);
				if (!_closed)
				{
					_requiresBrowserRecreation = false;
					_browserInitializationTask = InitializeBrowserAsync();
					await _browserInitializationTask;
				}
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			if (!_closed)
			{
				ShowLoadError(requiresBrowserRecreation: true);
			}
		}
	}

	private async Task DisposeBrowserAsync(IEmbeddedBrowser browser)
	{
		if (browser == null || _browser != browser)
		{
			return;
		}
		using (await EmbeddedBrowserLifecycle.EnterAsync(browser.Engine))
		{
			if (_browser != browser)
			{
				return;
			}
			browser.NavigationCompleted -= OnNavigationCompleted;
			browser.NewWindowRequested -= OnNewWindowRequested;
			browser.ProcessFailed -= OnProcessFailed;
			try
			{
				_scriptOption?.Dispose();
			}
			catch (Exception ex)
			{
				UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			}
			_scriptOption = null;
			try
			{
				try
				{
					await ReleaseAuthenticationCookieLeaseAsync(browser);
				}
				catch (Exception ex2)
				{
					UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex2));
				}
				await browser.DisposeAsync();
			}
			finally
			{
				_browser = null;
				BrowserHost.Content = null;
				_browserUri = null;
				_authenticationCookieUris.Clear();
			}
		}
	}

	private void OnNewWindowRequested(object sender, EmbeddedBrowserNewWindowEventArgs e)
	{
		if (e.Uri != null)
		{
			Utils.TryProcessStartUrl(e.Uri.AbsoluteUri);
		}
		e.Handled = true;
	}

	private void OnProcessFailed(object sender, EmbeddedBrowserProcessFailedEventArgs e)
	{
		IEmbeddedBrowser browser = sender as IEmbeddedBrowser;
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (IsBrowserActive(browser))
			{
				ShowLoadError(requiresBrowserRecreation: true);
			}
		});
	}

	private void ShowLoadError(bool requiresBrowserRecreation = false)
	{
		_requiresBrowserRecreation |= requiresBrowserRecreation;
		BrowserHost.Visibility = Visibility.Collapsed;
		BrowserProgressPanel.Visibility = Visibility.Collapsed;
		NoNetworkImage.Visibility = Visibility.Visible;
	}

	private async Task OpenExternalFallbackOrShowErrorAsync()
	{
		if (!CanUseExternalFallback(_useCase))
		{
			ShowLoadError(requiresBrowserRecreation: true);
			return;
		}
		string url = await Utils.GetAutoSignUrl(string.Equals(_useCase, "order_history", StringComparison.Ordinal) ? UriUtils.AddAttribute(_url, "from", "web") : _url);
		if (!_closed)
		{
			Utils.TryProcessStartUrl(url);
			Close();
		}
	}

	internal void RecordBridgeCallback(string callback)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				RecordBridgeCallback(callback);
			});
			return;
		}
		IEmbeddedBrowser browser = _browser;
		if (!_closed && browser != null)
		{
			EmbeddedBrowserTelemetry.Record(_useCase, browser.Engine, browser.RuntimeVersion, "callback_success_" + callback, null, null);
		}
	}

	private async void OnRetryClick(object sender, MouseButtonEventArgs e)
	{
		if (_closed || !_browserRecreationTask.IsCompleted)
		{
			return;
		}
		_automaticUnknownNavigationRecreationCount = 0;
		ShowLoading();
		IEmbeddedBrowser browser = _browser;
		if (_requiresBrowserRecreation && browser != null)
		{
			StartBrowserRecreation(browser);
			await _browserRecreationTask;
		}
		else if (!_closed)
		{
			if (_browser == null)
			{
				_browserInitializationTask = InitializeBrowserAsync();
				await _browserInitializationTask;
			}
			else if (_browserUri != null)
			{
				_browser.Navigate(_browserUri);
			}
		}
	}

	private void ShowLoading()
	{
		BrowserHost.Visibility = Visibility.Collapsed;
		NoNetworkImage.Visibility = Visibility.Collapsed;
		BrowserStatusText.Visibility = Visibility.Collapsed;
		RuntimeDownloadProgress.Visibility = Visibility.Collapsed;
		BrowserProgressPanel.Visibility = Visibility.Visible;
	}

	private async void OnWindowClosing(object sender, CancelEventArgs e)
	{
		if (_canClose || _browser == null)
		{
			_closed = true;
			return;
		}
		e.Cancel = true;
		if (_closeCleanupStarted)
		{
			return;
		}
		_closeCleanupStarted = true;
		_closed = true;
		_closeAction = null;
		try
		{
			await DisposeBrowserAsync(_browser);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			try
			{
				await DisposeBrowserAsync(_browser);
			}
			catch (Exception ex2)
			{
				UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex2));
			}
		}
		finally
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				if (!_canClose)
				{
					_canClose = true;
					Close();
				}
			});
		}
	}

	private void OnWindowClosed(object sender, EventArgs e)
	{
		_canClose = true;
		_closed = true;
		_closeAction = null;
		base.Owner?.Activate();
	}

	public void DoAction(bool needClose, string arg)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				DoAction(needClose, arg);
			});
			return;
		}
		_closeAction?.Invoke(arg);
		if (needClose)
		{
			Close();
		}
	}

	public async Task ShowToastPopup(string message)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			await base.Dispatcher.InvokeAsync(() => ShowToastPopup(message)).Task.Unwrap();
			return;
		}
		ToastText.Text = message;
		ToastPopup.IsOpen = true;
		await Task.Delay(1500);
		ToastPopup.IsOpen = false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/navigatewebbrowserwindow.xaml", UriKind.Relative);
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
			((NavigateWebBrowserWindow)target).Loaded += OnWindowLoaded;
			break;
		case 2:
			Container = (Grid)target;
			break;
		case 3:
			BrowserHost = (ContentControl)target;
			break;
		case 4:
			NoNetworkImage = (Grid)target;
			NoNetworkImage.MouseLeftButtonUp += OnRetryClick;
			break;
		case 5:
			BrowserProgressPanel = (Grid)target;
			break;
		case 6:
			BrowserLoading = (LoadingIndicator)target;
			break;
		case 7:
			BrowserStatusText = (TextBlock)target;
			break;
		case 8:
			RuntimeDownloadProgress = (ProgressBar)target;
			break;
		case 9:
			ToastPopup = (Popup)target;
			break;
		case 10:
			ToastBorder = (Border)target;
			break;
		case 11:
			ToastText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
