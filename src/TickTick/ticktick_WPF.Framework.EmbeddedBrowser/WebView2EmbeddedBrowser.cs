using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Util;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal sealed class WebView2EmbeddedBrowser : IEmbeddedBrowser
{
	private static readonly SemaphoreSlim ControllerLifecycleGate = new SemaphoreSlim(1, 1);

	private const int CookieDeletionVerificationAttempts = 20;

	private const int CookieDeletionVerificationDelayMilliseconds = 25;

	private static readonly MethodInfo CompositionSendMouseInputMethod = ResolveCompositionSendMouseInputMethod(typeof(WebView2CompositionControl));

	private readonly IWebView2 _browser;

	private readonly FrameworkElement _browserView;

	private readonly IDisposable _disposableBrowser;

	private EmbeddedBrowserOptions _options;

	private readonly Dictionary<ulong, Stopwatch> _navigationStopwatches = new Dictionary<ulong, Stopwatch>();

	private object _hostObject;

	private string _hostObjectName;

	private bool _disposed;

	private bool _eventsAttached;

	private readonly HashSet<ulong> _canceledNavigationIds = new HashSet<ulong>();

	private readonly bool _forwardsCompositionMouseLeave;

	private readonly WebView2CompositionMouseCapture _compositionMouseCapture;

	private bool _compositionMouseInputForwardingFailed;

	public FrameworkElement View => _browserView;

	public EmbeddedBrowserEngine Engine => EmbeddedBrowserEngine.WebView2;

	public string RuntimeVersion { get; private set; }

	public Uri Source => _browser.Source;

	internal Color DefaultBackgroundColor
	{
		get
		{
			return _browser.DefaultBackgroundColor;
		}
		set
		{
			_browser.DefaultBackgroundColor = value;
		}
	}

	public event EventHandler<EmbeddedBrowserNavigationStartingEventArgs> NavigationStarting;

	public event EventHandler<EmbeddedBrowserNavigationCompletedEventArgs> NavigationCompleted;

	public event EventHandler<EmbeddedBrowserNewWindowEventArgs> NewWindowRequested;

	public event EventHandler<EmbeddedBrowserProcessFailedEventArgs> ProcessFailed;

	public WebView2EmbeddedBrowser(bool useCompositionControl = false, bool useTransparentBackground = false)
	{
		if (useCompositionControl)
		{
			WebView2CompositionMonitorFallback.EnsureInstalled();
		}
		IWebView2 browser;
		if (!useCompositionControl)
		{
			IWebView2 webView = new WebView2();
			browser = webView;
		}
		else
		{
			IWebView2 webView = new WebView2CompositionControl();
			browser = webView;
		}
		_browser = browser;
		if (useTransparentBackground)
		{
			_browser.DefaultBackgroundColor = Color.Transparent;
		}
		_browserView = (FrameworkElement)_browser;
		if (useCompositionControl)
		{
			_browserView.MinWidth = 1.0;
			_browserView.MinHeight = 1.0;
		}
		_disposableBrowser = _browser;
		_forwardsCompositionMouseLeave = useCompositionControl && RequiresCompositionMouseLeaveWorkaround(_browser.GetType()) && CompositionSendMouseInputMethod != null;
		if (_forwardsCompositionMouseLeave)
		{
			_browserView.MouseLeave += OnCompositionBrowserMouseLeave;
		}
		if (useCompositionControl && CompositionSendMouseInputMethod != null)
		{
			_compositionMouseCapture = new WebView2CompositionMouseCapture(_browserView, () => !_disposed && _browser.CoreWebView2 != null, delegate
			{
				SendCompositionMouseInput(CoreWebView2MouseEventKind.LeftButtonUp, new System.Drawing.Point(-1, -1));
			});
		}
	}

	internal static bool RequiresCompositionMouseLeaveWorkaround(Type compositionControlType)
	{
		if (compositionControlType == null)
		{
			return false;
		}
		MethodInfo method = compositionControlType.GetMethod("OnMouseLeave", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[1] { typeof(MouseEventArgs) }, null);
		if (!(method == null))
		{
			return method.DeclaringType != compositionControlType;
		}
		return true;
	}

	private static MethodInfo ResolveCompositionSendMouseInputMethod(Type compositionControlType)
	{
		return compositionControlType?.GetMethod("SendMouseInput", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[4]
		{
			typeof(CoreWebView2MouseEventKind),
			typeof(CoreWebView2MouseEventVirtualKeys),
			typeof(uint),
			typeof(System.Drawing.Point)
		}, null);
	}

	private void OnCompositionBrowserMouseLeave(object sender, MouseEventArgs e)
	{
		SendCompositionMouseInput(CoreWebView2MouseEventKind.Leave, System.Drawing.Point.Empty);
	}

	private void SendCompositionMouseInput(CoreWebView2MouseEventKind kind, System.Drawing.Point point)
	{
		if (_disposed || _compositionMouseInputForwardingFailed || _browser.CoreWebView2 == null)
		{
			return;
		}
		try
		{
			CompositionSendMouseInputMethod.Invoke(_browser, new object[4]
			{
				kind,
				CoreWebView2MouseEventVirtualKeys.None,
				0u,
				point
			});
		}
		catch (Exception ex)
		{
			_compositionMouseInputForwardingFailed = true;
			Exception ex2 = ((ex is TargetInvocationException { InnerException: not null } ex3) ? ex3.InnerException : ex);
			UtilLog.Info("Failed to forward WebView2CompositionControl mouse input " + kind.ToString() + ". " + ex2.GetType().Name + ": " + ex2.Message);
		}
	}

	public async Task InitializeAsync(EmbeddedBrowserOptions options)
	{
		_options = options ?? throw new ArgumentNullException("options");
		Stopwatch stopwatch = Stopwatch.StartNew();
		await ControllerLifecycleGate.WaitAsync();
		try
		{
			int num = 0;
			_ = num;
			_ = 1;
			try
			{
				if (_disposed)
				{
					throw new ObjectDisposedException("WebView2EmbeddedBrowser");
				}
				if (!EmbeddedBrowserFactory.IsWebView2RuntimeAvailable())
				{
					throw new EmbeddedBrowserRuntimeMissingException(null);
				}
				UtilLog.Info("EmbeddedBrowser controller initialization: environment_start, use_case=" + _options.UseCase);
				CoreWebView2Environment environment = await WebView2EnvironmentProvider.GetAsync();
				UtilLog.Info("EmbeddedBrowser controller initialization: environment_ready, use_case=" + _options.UseCase);
				UtilLog.Info("EmbeddedBrowser controller initialization: ensure_core_start, use_case=" + _options.UseCase + ", loaded=" + _browserView.IsLoaded + ", visible=" + _browserView.IsVisible + ", size=" + _browserView.ActualWidth + "x" + _browserView.ActualHeight);
				await _browser.EnsureCoreWebView2Async(environment);
				UtilLog.Info("EmbeddedBrowser controller initialization: ensure_core_ready, use_case=" + _options.UseCase);
				RuntimeVersion = _browser.CoreWebView2.Environment.BrowserVersionString;
				if (!_eventsAttached)
				{
					_browser.NavigationStarting += OnNavigationStarting;
					_browser.NavigationCompleted += OnNavigationCompleted;
					_browser.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
					_browser.CoreWebView2.ProcessFailed += OnProcessFailed;
					_eventsAttached = true;
				}
				CoreWebView2Settings settings = _browser.CoreWebView2.Settings;
				settings.AreHostObjectsAllowed = _options.EnableHostObjects;
				settings.IsWebMessageEnabled = false;
				settings.AreDefaultContextMenusEnabled = false;
				settings.AreDevToolsEnabled = false;
				if (!string.IsNullOrEmpty(_options.UserAgentSuffix) && !settings.UserAgent.EndsWith(_options.UserAgentSuffix, StringComparison.Ordinal))
				{
					settings.UserAgent += _options.UserAgentSuffix;
				}
				stopwatch.Stop();
				EmbeddedBrowserTelemetry.Record(_options.UseCase, Engine, RuntimeVersion, "initialized", stopwatch.ElapsedMilliseconds, null);
			}
			catch (EmbeddedBrowserRuntimeMissingException)
			{
				EmbeddedBrowserTelemetry.Record(_options.UseCase, Engine, RuntimeVersion, "runtime_missing", stopwatch.ElapsedMilliseconds, null);
				throw;
			}
			catch (WebView2RuntimeNotFoundException ex2)
			{
				EmbeddedBrowserTelemetry.Record(_options.UseCase, Engine, RuntimeVersion, "runtime_missing", stopwatch.ElapsedMilliseconds, null, ex2.GetType().Name);
				throw new EmbeddedBrowserRuntimeMissingException(ex2);
			}
			catch (Exception ex3)
			{
				EmbeddedBrowserTelemetry.Record(_options.UseCase, Engine, RuntimeVersion, "init_failed", stopwatch.ElapsedMilliseconds, null, ex3.GetType().Name);
				throw;
			}
		}
		finally
		{
			ControllerLifecycleGate.Release();
		}
	}

	public void Navigate(Uri uri)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		_browser.CoreWebView2.Navigate(uri.AbsoluteUri);
	}

	public async Task SetCookieAsync(Uri uri, string name, string value, DateTime? expires = null, bool includeSubdomains = false)
	{
		CoreWebView2CookieManager cookieManager = _browser.CoreWebView2.CookieManager;
		string domain = (includeSubdomains ? ("." + uri.Host.TrimStart('.')) : uri.Host);
		CoreWebView2Cookie coreWebView2Cookie = cookieManager.CreateCookie(name, value ?? string.Empty, domain, "/");
		coreWebView2Cookie.IsSecure = uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
		if (expires.HasValue)
		{
			coreWebView2Cookie.Expires = expires.Value;
		}
		cookieManager.AddOrUpdateCookie(coreWebView2Cookie);
		if (!(await cookieManager.GetCookiesAsync(uri.GetLeftPart(UriPartial.Authority) + "/")).Any((CoreWebView2Cookie item) => string.Equals(item.Name, name, StringComparison.Ordinal) && string.Equals(item.Value, value ?? string.Empty, StringComparison.Ordinal) && string.Equals(item.Domain.TrimStart('.'), uri.Host.TrimStart('.'), StringComparison.OrdinalIgnoreCase) && (!includeSubdomains || item.Domain.StartsWith(".", StringComparison.Ordinal))))
		{
			throw new InvalidOperationException("WebView2 failed to store the requested cookie.");
		}
	}

	public async Task DeleteCookieAsync(Uri uri, string name)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		CoreWebView2CookieManager manager = _browser.CoreWebView2.CookieManager;
		List<CoreWebView2Cookie> list = (await manager.GetCookiesAsync(null)).Where((CoreWebView2Cookie cookie) => string.Equals(cookie.Name, name, StringComparison.Ordinal) && IsCookieInDomainScope(cookie.Domain, uri)).ToList();
		foreach (CoreWebView2Cookie item in list)
		{
			manager.DeleteCookie(item);
		}
		if (list.Count > 0)
		{
			await WaitForCookieDeletionAsync(manager, null, (CoreWebView2Cookie cookie) => string.Equals(cookie.Name, name, StringComparison.Ordinal) && IsCookieInDomainScope(cookie.Domain, uri));
		}
	}

	public async Task DeleteAllCookiesAsync(Uri uri)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		CoreWebView2CookieManager manager = _browser.CoreWebView2.CookieManager;
		List<CoreWebView2Cookie> list = (await manager.GetCookiesAsync(null)).Where((CoreWebView2Cookie cookie) => IsCookieInDomainScope(cookie.Domain, uri)).ToList();
		foreach (CoreWebView2Cookie item in list)
		{
			manager.DeleteCookie(item);
		}
		if (list.Count > 0)
		{
			await WaitForCookieDeletionAsync(manager, null, (CoreWebView2Cookie cookie) => IsCookieInDomainScope(cookie.Domain, uri));
		}
	}

	private static async Task WaitForCookieDeletionAsync(CoreWebView2CookieManager manager, string uri, Func<CoreWebView2Cookie, bool> isTargetCookie)
	{
		for (int attempt = 0; attempt < 20; attempt++)
		{
			if (!(await manager.GetCookiesAsync(uri)).Any(isTargetCookie))
			{
				return;
			}
			if (attempt < 19)
			{
				await Task.Delay(25);
			}
		}
		throw new InvalidOperationException("WebView2 failed to delete the requested cookie in time.");
	}

	internal static bool IsCookieInDomainScope(string cookieDomain, Uri scopeUri)
	{
		if (scopeUri == null || string.IsNullOrWhiteSpace(cookieDomain))
		{
			return false;
		}
		string text = scopeUri.Host.Trim().TrimStart('.');
		string text2 = cookieDomain.Trim().TrimStart('.');
		if (!string.Equals(text, text2, StringComparison.OrdinalIgnoreCase) && !text.EndsWith("." + text2, StringComparison.OrdinalIgnoreCase))
		{
			return text2.EndsWith("." + text, StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public async Task RegisterHostObjectAsync(string name, object hostObject)
	{
		if (!_options.EnableHostObjects)
		{
			throw new InvalidOperationException("Host objects are disabled for this browser session.");
		}
		_hostObject = hostObject ?? throw new ArgumentNullException("hostObject");
		_hostObjectName = name;
		_browser.CoreWebView2.AddHostObjectToScript(name, _hostObject);
		string javaScript = BuildHostObjectInjectionScript(name, _options.TrustedOrigins);
		await _browser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(javaScript);
	}

	internal static string BuildHostObjectInjectionScript(string name, IEnumerable<string> trustedOrigins)
	{
		string arg = JsonConvert.SerializeObject((trustedOrigins ?? Enumerable.Empty<string>()).ToArray());
		string arg2 = JsonConvert.SerializeObject(name);
		return $"\r\n(function () {{\r\n    if (window.top !== window || {arg}.indexOf(window.location.origin) < 0) return;\r\n    var globalName = {arg2};\r\n    var hostObject = window.chrome.webview.hostObjects.sync[globalName];\r\n    if (globalName === '_dsbridge') {{\r\n        // WebView2 Host Object 代理继承自 Function，直接访问 hostObject.call\r\n        // 会命中 Function.prototype.call，而不是 Native 的同名 COM 成员。\r\n        // Web 协议仍暴露 _dsbridge.call，内部转发到不冲突的 invoke 成员。\r\n        var dsBridgeObject = {{\r\n            call: function (methodName, argumentsJson) {{\r\n                return hostObject.invoke(methodName, argumentsJson);\r\n            }}\r\n        }};\r\n        try {{\r\n            Object.defineProperty(window, globalName,\r\n                {{ value: dsBridgeObject, configurable: false }});\r\n        }} catch (_) {{\r\n            window[globalName] = dsBridgeObject;\r\n        }}\r\n        return;\r\n    }}\r\n    var nativeObject = new Proxy(hostObject, {{\r\n        get: function (target, property) {{\r\n            var member = target[property];\r\n            if (typeof property !== 'string' || typeof member !== 'function') {{\r\n                return member;\r\n            }}\r\n            return function () {{\r\n                var values = Array.prototype.map.call(arguments, function (value) {{\r\n                    return Array.isArray(value) ? String(value) : value;\r\n                }});\r\n                return member.apply(target, values);\r\n            }};\r\n        }}\r\n    }});\r\n    try {{\r\n        Object.defineProperty(window, globalName, {{ value: nativeObject, configurable: false }});\r\n    }} catch (_) {{\r\n        window[globalName] = nativeObject;\r\n    }}\r\n}})();";
	}

	public Task<string> ExecuteScriptAsync(string script)
	{
		return _browser.CoreWebView2.ExecuteScriptAsync(script);
	}

	internal void SetVirtualHostNameToFolderMapping(string hostName, string folderPath)
	{
		if (string.IsNullOrWhiteSpace(hostName))
		{
			throw new ArgumentException("Virtual host name cannot be empty.", "hostName");
		}
		if (string.IsNullOrWhiteSpace(folderPath))
		{
			throw new ArgumentException("Mapped folder path cannot be empty.", "folderPath");
		}
		_browser.CoreWebView2.SetVirtualHostNameToFolderMapping(hostName, Path.GetFullPath(folderPath), CoreWebView2HostResourceAccessKind.Allow);
	}

	internal void ClearVirtualHostNameToFolderMapping(string hostName)
	{
		_browser.CoreWebView2.ClearVirtualHostNameToFolderMapping(hostName);
	}

	internal Task<string> AddScriptToExecuteOnDocumentCreatedAsync(string script)
	{
		return _browser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
	}

	internal void RemoveScriptToExecuteOnDocumentCreated(string scriptId)
	{
		if (!string.IsNullOrEmpty(scriptId))
		{
			_browser.CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(scriptId);
		}
	}

	public void Stop()
	{
		_browser.CoreWebView2?.Stop();
	}

	private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
	{
		if (!_navigationStopwatches.ContainsKey(e.NavigationId))
		{
			_navigationStopwatches[e.NavigationId] = Stopwatch.StartNew();
		}
		Uri.TryCreate(e.Uri, UriKind.Absolute, out var result);
		EmbeddedBrowserNavigationStartingEventArgs e2 = new EmbeddedBrowserNavigationStartingEventArgs(result);
		this.NavigationStarting?.Invoke(this, e2);
		if (!e2.Cancel && !_options.IsNavigationAllowed(result))
		{
			e2.Cancel = true;
			RaiseExternalNavigation(result);
		}
		e.Cancel = e2.Cancel;
		if (e2.Cancel)
		{
			_canceledNavigationIds.Add(e.NavigationId);
		}
	}

	private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
	{
		_navigationStopwatches.TryGetValue(e.NavigationId, out var value);
		_navigationStopwatches.Remove(e.NavigationId);
		value?.Stop();
		string error = (e.IsSuccess ? null : e.WebErrorStatus.ToString());
		if (_canceledNavigationIds.Remove(e.NavigationId) && !e.IsSuccess && e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled)
		{
			EmbeddedBrowserTelemetry.Record(_options.UseCase, Engine, RuntimeVersion, "navigation_canceled", null, value?.ElapsedMilliseconds);
			return;
		}
		this.NavigationCompleted?.Invoke(this, new EmbeddedBrowserNavigationCompletedEventArgs(Source, e.IsSuccess, error));
		EmbeddedBrowserTelemetry.Record(_options.UseCase, Engine, RuntimeVersion, e.IsSuccess ? "navigation_success" : "navigation_failed", null, value?.ElapsedMilliseconds, error);
	}

	private void OnNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
	{
		Uri.TryCreate(e.Uri, UriKind.Absolute, out var result);
		EmbeddedBrowserNewWindowEventArgs e2 = new EmbeddedBrowserNewWindowEventArgs(result);
		this.NewWindowRequested?.Invoke(this, e2);
		if (!e2.Handled && _options.OpenNewWindowsExternally)
		{
			if (result != null)
			{
				Utils.TryProcessStartUrl(result.AbsoluteUri);
			}
			e2.Handled = true;
		}
		else if (!e2.Handled && result != null)
		{
			e.Handled = true;
			Navigate(result);
			e2.Handled = true;
		}
		else if (!e2.Handled)
		{
			e2.Handled = true;
		}
		e.Handled = e2.Handled;
	}

	private void RaiseExternalNavigation(Uri uri)
	{
		EmbeddedBrowserNewWindowEventArgs e = new EmbeddedBrowserNewWindowEventArgs(uri);
		this.NewWindowRequested?.Invoke(this, e);
		if (!e.Handled && _options.OpenNewWindowsExternally && uri != null)
		{
			Utils.TryProcessStartUrl(uri.AbsoluteUri);
			e.Handled = true;
		}
	}

	private void OnProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
	{
		string text = e.ProcessFailedKind.ToString();
		this.ProcessFailed?.Invoke(this, new EmbeddedBrowserProcessFailedEventArgs(text));
		EmbeddedBrowserTelemetry.Record(_options.UseCase, Engine, RuntimeVersion, "process_failed", null, null, text);
	}

	public async Task DisposeAsync()
	{
		await ControllerLifecycleGate.WaitAsync();
		try
		{
			if (!_disposed)
			{
				DisposeCore();
			}
		}
		finally
		{
			ControllerLifecycleGate.Release();
		}
	}

	private void DisposeCore()
	{
		_compositionMouseCapture?.Dispose();
		_disposed = true;
		if (_forwardsCompositionMouseLeave)
		{
			_browserView.MouseLeave -= OnCompositionBrowserMouseLeave;
		}
		if (_eventsAttached)
		{
			_browser.NavigationStarting -= OnNavigationStarting;
			_browser.NavigationCompleted -= OnNavigationCompleted;
			if (_browser.CoreWebView2 != null)
			{
				_browser.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
				_browser.CoreWebView2.ProcessFailed -= OnProcessFailed;
			}
			_eventsAttached = false;
		}
		if (_browser.CoreWebView2 != null && !string.IsNullOrEmpty(_hostObjectName))
		{
			try
			{
				_browser.CoreWebView2.RemoveHostObjectFromScript(_hostObjectName);
			}
			catch (Exception)
			{
			}
		}
		_hostObject = null;
		_hostObjectName = null;
		_canceledNavigationIds.Clear();
		_navigationStopwatches.Clear();
		_disposableBrowser.Dispose();
	}
}
