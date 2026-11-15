using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Navigation;
using ticktick_WPF.Util;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal sealed class LegacyIeEmbeddedBrowser : IEmbeddedBrowser
{
	private readonly System.Windows.Controls.WebBrowser _browser = new System.Windows.Controls.WebBrowser();

	private EmbeddedBrowserOptions _options;

	private WebBrowserNewWindowHandler _newWindowHandler;

	private DateTime _navigationStartedAt;

	private bool _disposed;

	public FrameworkElement View => _browser;

	public EmbeddedBrowserEngine Engine => EmbeddedBrowserEngine.LegacyIe;

	public string RuntimeVersion => "IE";

	public Uri Source => _browser.Source;

	public event EventHandler<EmbeddedBrowserNavigationStartingEventArgs> NavigationStarting;

	public event EventHandler<EmbeddedBrowserNavigationCompletedEventArgs> NavigationCompleted;

	public event EventHandler<EmbeddedBrowserNewWindowEventArgs> NewWindowRequested;

	public event EventHandler<EmbeddedBrowserProcessFailedEventArgs> ProcessFailed;

	public Task InitializeAsync(EmbeddedBrowserOptions options)
	{
		_options = options ?? throw new ArgumentNullException("options");
		DateTime utcNow = DateTime.UtcNow;
		_browser.Navigating += OnNavigating;
		_browser.LoadCompleted += OnLoadCompleted;
		_browser.Navigated += OnNavigated;
		if (_options.SuppressScriptErrors)
		{
			WebBrowserHelper.SetErrorSilent(_browser);
		}
		if (_options.ApplyLegacyDpiZoom)
		{
			WebBrowserZoomInvoker.AddZoomInvoker(_browser);
		}
		EmbeddedBrowserTelemetry.Record(_options.UseCase, Engine, RuntimeVersion, "initialized", (long)(DateTime.UtcNow - utcNow).TotalMilliseconds, null);
		return Task.CompletedTask;
	}

	public void Navigate(Uri uri)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		_navigationStartedAt = DateTime.UtcNow;
		string userAgent = GetUserAgent();
		if (!string.IsNullOrEmpty(userAgent))
		{
			_browser.Navigate(uri, "_self", null, "User-Agent: " + userAgent);
		}
		else
		{
			_browser.Navigate(uri);
		}
	}

	public Task SetCookieAsync(Uri uri, string name, string value, DateTime? expires = null, bool includeSubdomains = false)
	{
		CookieHelper.SetCookie(uri.GetLeftPart(UriPartial.Authority) + "/", BuildCookieValue(uri, name, value, includeSubdomains), expires ?? DateTime.UtcNow.AddDays(20.0));
		return Task.CompletedTask;
	}

	public Task DeleteCookieAsync(Uri uri, string name)
	{
		CookieHelper.SetCookieOutDate(uri.GetLeftPart(UriPartial.Authority) + "/", name + "=");
		if (CanUseDomainAttribute(uri))
		{
			CookieHelper.SetCookieOutDate(uri.GetLeftPart(UriPartial.Authority) + "/", BuildCookieValue(uri, name, string.Empty, includeSubdomains: true));
		}
		return Task.CompletedTask;
	}

	internal static string BuildCookieValue(Uri uri, string name, string value, bool includeSubdomains)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		string text = name + "=" + (value ?? string.Empty);
		if (!includeSubdomains || !CanUseDomainAttribute(uri))
		{
			return text;
		}
		return text + "; domain=." + uri.Host.TrimStart('.');
	}

	private static bool CanUseDomainAttribute(Uri uri)
	{
		if (Uri.CheckHostName(uri.Host) == UriHostNameType.Dns)
		{
			return uri.Host.IndexOf('.') > 0;
		}
		return false;
	}

	public Task DeleteAllCookiesAsync(Uri uri)
	{
		IReadOnlyCollection<string> cookieNames = CookieHelper.GetCookieNames(uri);
		IReadOnlyList<string> cookieExpirationPaths = GetCookieExpirationPaths(uri);
		foreach (string item in cookieNames)
		{
			foreach (string item2 in cookieExpirationPaths)
			{
				ExpireCookie(uri, item, null, item2);
				foreach (string cookieExpirationDomain in GetCookieExpirationDomains(uri))
				{
					ExpireCookie(uri, item, cookieExpirationDomain, item2);
				}
			}
		}
		return Task.CompletedTask;
	}

	internal static IReadOnlyList<string> GetCookieExpirationPaths(Uri uri)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		List<string> list = new List<string> { "/" };
		string[] array = uri.AbsolutePath.Trim('/').Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		string text = string.Empty;
		for (int i = 0; i < array.Length; i++)
		{
			text = text + "/" + array[i];
			list.Add(text);
			if (i < array.Length - 1)
			{
				list.Add(text + "/");
			}
		}
		return list;
	}

	internal static IReadOnlyList<string> GetCookieExpirationDomains(Uri uri)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		List<string> list = new List<string> { uri.Host };
		if (Uri.CheckHostName(uri.Host) != UriHostNameType.Dns)
		{
			return list;
		}
		string text = uri.Host;
		while (text.IndexOf('.') > 0)
		{
			text = text.Substring(text.IndexOf('.') + 1);
			if (text.IndexOf('.') > 0)
			{
				list.Add(text);
			}
		}
		return list;
	}

	private static void ExpireCookie(Uri uri, string name, string domain, string path)
	{
		try
		{
			string cookie = string.Format("{0}=; path={1}{2}", name, path, string.IsNullOrEmpty(domain) ? string.Empty : ("; domain=." + domain.TrimStart('.')));
			CookieHelper.SetCookieOutDate(uri.GetLeftPart(UriPartial.Authority) + "/", cookie, includeHttpOnly: false);
			CookieHelper.SetCookieOutDate(uri.GetLeftPart(UriPartial.Authority) + "/", cookie, includeHttpOnly: true);
		}
		catch (Exception)
		{
		}
	}

	public Task RegisterHostObjectAsync(string name, object hostObject)
	{
		if (!_options.EnableHostObjects)
		{
			throw new InvalidOperationException("Host objects are disabled for this browser session.");
		}
		_browser.ObjectForScripting = hostObject;
		return Task.CompletedTask;
	}

	public Task<string> ExecuteScriptAsync(string script)
	{
		try
		{
			return Task.FromResult(_browser.InvokeScript("eval", script)?.ToString());
		}
		catch
		{
			return Task.FromResult<string>(null);
		}
	}

	public void Stop()
	{
		try
		{
			_browser.InvokeScript("eval", "document.execCommand('Stop');");
		}
		catch
		{
		}
	}

	private void OnNavigating(object sender, NavigatingCancelEventArgs e)
	{
		_navigationStartedAt = DateTime.UtcNow;
		EmbeddedBrowserNavigationStartingEventArgs e2 = new EmbeddedBrowserNavigationStartingEventArgs(e.Uri);
		this.NavigationStarting?.Invoke(this, e2);
		if (!e2.Cancel && !_options.IsNavigationAllowed(e.Uri))
		{
			e2.Cancel = true;
			RaiseExternalNavigation(e.Uri);
		}
		e.Cancel = e2.Cancel;
	}

	private void OnLoadCompleted(object sender, NavigationEventArgs e)
	{
		Uri uri = e.Uri ?? _browser.Source;
		long? loadMilliseconds = ((_navigationStartedAt == default(DateTime)) ? ((long?)null) : new long?((long)(DateTime.UtcNow - _navigationStartedAt).TotalMilliseconds));
		this.NavigationCompleted?.Invoke(this, new EmbeddedBrowserNavigationCompletedEventArgs(uri, isSuccess: true, null));
		EmbeddedBrowserTelemetry.Record(_options.UseCase, Engine, RuntimeVersion, "navigation_success", null, loadMilliseconds);
	}

	private void OnNavigated(object sender, NavigationEventArgs e)
	{
		_newWindowHandler?.Dispose();
		_newWindowHandler = new WebBrowserNewWindowHandler(_browser, delegate(string url)
		{
			if (Uri.TryCreate(url, UriKind.Absolute, out var result))
			{
				RaiseExternalNavigation(result);
			}
		}, (string url, bool isTopLevel) => ShouldBlockFrameNavigation(_options, url, isTopLevel));
	}

	internal static bool ShouldBlockFrameNavigation(EmbeddedBrowserOptions options, string url, bool isTopLevel)
	{
		if (isTopLevel || options == null)
		{
			return false;
		}
		if (options.EnableHostObjects)
		{
			return true;
		}
		if (!Uri.TryCreate(url, UriKind.Absolute, out var result))
		{
			return false;
		}
		return !options.IsNavigationAllowed(result);
	}

	private void RaiseExternalNavigation(Uri uri)
	{
		EmbeddedBrowserNewWindowEventArgs e = new EmbeddedBrowserNewWindowEventArgs(uri);
		this.NewWindowRequested?.Invoke(this, e);
		if (!e.Handled && _options.OpenNewWindowsExternally && uri != null)
		{
			Utils.TryProcessStartUrl(uri.ToString());
			e.Handled = true;
		}
		else if (!e.Handled && uri != null)
		{
			Navigate(uri);
			e.Handled = true;
		}
	}

	private string GetUserAgent()
	{
		if (string.IsNullOrEmpty(_options.UserAgentSuffix))
		{
			return null;
		}
		try
		{
			using System.Windows.Forms.WebBrowser webBrowser = new System.Windows.Forms.WebBrowser();
			webBrowser.Navigate("about:blank");
			object obj = webBrowser.Document?.Window?.DomWindow;
			object obj2 = obj?.GetType().InvokeMember("navigator", BindingFlags.GetProperty, null, obj, new object[0]);
			return (obj2?.GetType().InvokeMember("userAgent", BindingFlags.GetProperty, null, obj2, new object[0]))?.ToString() + _options.UserAgentSuffix;
		}
		catch
		{
			return null;
		}
	}

	public Task DisposeAsync()
	{
		if (_disposed)
		{
			return Task.CompletedTask;
		}
		_disposed = true;
		_newWindowHandler?.Dispose();
		_newWindowHandler = null;
		_browser.Navigating -= OnNavigating;
		_browser.LoadCompleted -= OnLoadCompleted;
		_browser.Navigated -= OnNavigated;
		_browser.ObjectForScripting = null;
		_browser.Dispose();
		return Task.CompletedTask;
	}
}
