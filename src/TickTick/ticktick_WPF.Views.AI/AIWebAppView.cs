using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Framework.EmbeddedBrowser;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.AI;

public class AIWebAppView : UserControl, IComponentConnector
{
	private Task _backgroundDiagnosticsTask = Task.CompletedTask;

	private bool _backgroundCompatibilityChanging;

	private bool _backgroundHostIsFloating;

	private int _backgroundDiagnosticSequence;

	internal const string BackgroundDiagnosticScript = "(() => {\r\n            const nodes = [], seen = new Map();\r\n            let truncated = false;\r\n            const style = (el, pseudo) => {\r\n                const s = getComputedStyle(el, pseudo);\r\n                return { background: s.backgroundColor, image: s.backgroundImage !== 'none',\r\n                    color: s.color, opacity: s.opacity, blend: s.mixBlendMode,\r\n                    filter: s.filter !== 'none', backdrop: !!s.backdropFilter && s.backdropFilter !== 'none',\r\n                    generated: pseudo ? s.content !== 'none' && s.content !== 'normal' : false };\r\n            };\r\n            const describe = el => {\r\n                if (seen.has(el)) return seen.get(el);\r\n                if (nodes.length >= 128) { truncated = true; return null; }\r\n                const index = nodes.length;\r\n                seen.set(el, index);\r\n                nodes.push({ tag: el.tagName, style: style(el),\r\n                    before: style(el, '::before'), after: style(el, '::after') });\r\n                return index;\r\n            };\r\n            const chain = el => {\r\n                const ancestors = [];\r\n                for (; el && ancestors.length < 128; el = el.parentElement) ancestors.push(el);\r\n                const depthTruncated = !!el;\r\n                if (depthTruncated) truncated = true;\r\n                const indices = ancestors.reverse().map(describe);\r\n                return { nodes: indices, truncated: depthTruncated || indices.some(i => i === null) };\r\n            };\r\n            const points = [[.1,.1],[.5,.25],[.5,.5],[.9,.5],[.5,.9]];\r\n            return { ready: document.readyState, dark: matchMedia('(prefers-color-scheme: dark)').matches,\r\n                forcedColors: matchMedia('(forced-colors: active)').matches,\r\n                html: document.documentElement ? describe(document.documentElement) : null,\r\n                body: document.body ? describe(document.body) : null,\r\n                roots: document.body ? Array.from(document.body.children).slice(0,4).map(describe) : [],\r\n                samples: points.map(p => {\r\n                    const hits = document.elementsFromPoint(Math.floor(innerWidth*p[0]), Math.floor(innerHeight*p[1]));\r\n                    const hitsTruncated = hits.length > 8;\r\n                    if (hitsTruncated) truncated = true;\r\n                    return { point: p, hitsTruncated, layers: hits.slice(0,8).map(chain) };\r\n                }), nodes, truncated,\r\n                scope: 'viewport-hit-chains; excludes non-hit-testable overlays and iframe/shadow contents' };\r\n        })()";

	private readonly AIWebViewLifecycle _lifecycle = new AIWebViewLifecycle();

	private Uri _webAppUri;

	private readonly Uri _authenticationCookieScopeUri;

	private IEmbeddedBrowser _browser;

	private EmbeddedBrowserAuthenticationCookieLease _authenticationCookieLease;

	private AgentDsBridgeHost _agentBridge;

	private Task _initializationTask;

	private bool _isAgentBridgeReady;

	private bool _wasGenerating;

	private bool _isAgentBridgeVerificationRunning;

	private int _bridgeVerificationVersion;

	private bool _isRecoveryRunning;

	private bool _isWebPageFallbackPresented;

	private bool _isAgentPageStateResolved;

	private string _resolvedAgentPage;

	private string _resolvedConversationId;

	private bool _hasFontFolderMapping;

	private int _fontStyleUpdateVersion;

	private string _fontStyleScriptId;

	private string _fontStyleScript;

	private bool _shouldPresentStatusPanel;

	private bool _isBrowserFrameRequested;

	private bool _isBrowserInteractionRequested;

	private bool _isBrowserPresentationSuspendedForHostLayout;

	private bool _isBrowserPresentationSuspendedForTheme;

	private bool _isBrowserPresentationSuspendedForHostSwitch;

	private bool _isNativeHomePresented = true;

	private bool _isBrowserInitialized;

	private bool _membershipRefreshPending;

	private bool _hasSelectedConversation;

	private const int AgentBridgeVerificationAttempts = 40;

	private const int AgentBridgeVerificationDelayMilliseconds = 250;

	private const string WebAppChannel = "windows";

	private bool? _themeOverride;

	internal ContentControl BrowserHost;

	internal Grid StatusPanel;

	internal LoadingIndicator LoadingIndicator;

	internal TextBlock StatusText;

	internal ProgressBar RuntimeProgress;

	private bool _contentLoaded;

	internal AIWebViewLifecycleState LifecycleState => _lifecycle.State;

	internal bool IsAgentBridgeReady => _isAgentBridgeReady;

	internal bool IsAgentPageStateResolved => _isAgentPageStateResolved;

	internal string ResolvedAgentPage => _resolvedAgentPage;

	internal string ResolvedConversationId => _resolvedConversationId;

	internal bool ShouldPresentStatusPanel => _shouldPresentStatusPanel;

	internal bool HasPresentedBrowserContent { get; private set; }

	private static event EventHandler BackgroundCompatibilityChanged;

	internal event EventHandler AgentBridgeReadyChanged;

	internal event EventHandler<AgentBridgeNativeEventArgs> AgentNotificationReceived;

	internal event EventHandler StatusPresentationChanged;

	internal static void NotifyBackgroundCompatibilityChanged()
	{
		AIWebAppView.BackgroundCompatibilityChanged?.Invoke(null, EventArgs.Empty);
	}

	private void OnBackgroundCompatibilityChanged(object sender, EventArgs e)
	{
		ApplyBackgroundCompatibility();
	}

	internal void SetBackgroundHostMode(bool isFloating)
	{
		_backgroundHostIsFloating = isFloating;
		ApplyBackgroundCompatibility();
	}

	private void ApplyBackgroundCompatibility()
	{
		if (!(_browser is WebView2EmbeddedBrowser webView2EmbeddedBrowser))
		{
			return;
		}
		if (!LocalSettings.Settings.ExtraSettings.AIOpaqueBrowserBackground)
		{
			webView2EmbeddedBrowser.DefaultBackgroundColor = System.Drawing.Color.Transparent;
			return;
		}
		Window window = Window.GetWindow(this);
		if (window != null)
		{
			System.Windows.Media.Brush foreground = TryFindResource(_backgroundHostIsFloating ? "WindowBackgroundPrimary" : "ShowAreaBottomColor") as System.Windows.Media.Brush;
			System.Windows.Media.Brush background = window?.Background ?? (TryFindResource("MainWindowBrush") as System.Windows.Media.Brush);
			if (window?.FindName("WindowBackground") is Border { Visibility: Visibility.Visible, Opacity: >0.0, Background: ImageBrush { ImageSource: not null } background2 })
			{
				background = background2;
			}
			System.Windows.Media.Color color = AIBackgroundColor.Resolve(foreground, background);
			webView2EmbeddedBrowser.DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, color.R, color.G, color.B);
		}
	}

	internal void ShowBackgroundCompatibilityMenu(FrameworkElement placementTarget)
	{
		ContextMenu obj = new ContextMenu
		{
			PlacementTarget = placementTarget
		};
		Task captureTask = LogBackgroundDiagnosticsAsync("compatibility_menu_open");
		MenuItem compatibility = new MenuItem
		{
			Header = (Utils.IsCn() ? "AI 背景兼容模式（不透明）" : "AI background compatibility (opaque)"),
			ToolTip = (Utils.IsCn() ? "仅本机生效，立即应用；取消勾选恢复透明背景。" : "Applies immediately on this device. Uncheck to restore transparency."),
			IsCheckable = true,
			IsChecked = LocalSettings.Settings.ExtraSettings.AIOpaqueBrowserBackground,
			IsEnabled = (_browser is WebView2EmbeddedBrowser && !_backgroundCompatibilityChanging)
		};
		compatibility.Click += async delegate
		{
			if (_backgroundCompatibilityChanging)
			{
				return;
			}
			_backgroundCompatibilityChanging = true;
			try
			{
				await captureTask;
				await LogBackgroundDiagnosticsAsync("compatibility_before");
				LocalSettings.Settings.ExtraSettings.AIOpaqueBrowserBackground = compatibility.IsChecked;
				NotifyBackgroundCompatibilityChanged();
				await LocalSettings.Settings.Save(force: true);
				await LogBackgroundDiagnosticsAsync("compatibility_after");
			}
			catch (Exception ex)
			{
				UtilLog.Info("AgentBackground compatibility change failed: " + ex.GetType().Name);
				CustomerDialog customerDialog = new CustomerDialog(Utils.IsCn() ? "AI 背景兼容设置" : "AI background compatibility", Utils.IsCn() ? "设置未能完整应用或保存，请重新打开应用并检查此开关。" : "The setting could not be fully applied or saved. Restart the app and check this option.", MessageBoxButton.OK);
				customerDialog.Owner = Window.GetWindow(this);
				customerDialog.ShowDialog();
			}
			finally
			{
				_backgroundCompatibilityChanging = false;
			}
		};
		obj.Items.Add(compatibility);
		obj.IsOpen = true;
	}

	internal Task LogBackgroundDiagnosticsAsync(string reason)
	{
		return _backgroundDiagnosticsTask = QueueBackgroundDiagnosticsAsync(_backgroundDiagnosticsTask, reason);
	}

	private async Task QueueBackgroundDiagnosticsAsync(Task previous, string reason)
	{
		await previous;
		await LogBackgroundDiagnosticsCoreAsync(reason);
	}

	private static JObject DescribeBackgroundBrush(System.Windows.Media.Brush brush)
	{
		if (brush == null)
		{
			return new JObject { ["type"] = "unset" };
		}
		JObject jObject = new JObject
		{
			["type"] = brush.GetType().Name,
			["opacity"] = brush.Opacity
		};
		if (brush is SolidColorBrush solidColorBrush)
		{
			jObject["color"] = solidColorBrush.Color.ToString();
		}
		if (brush is GradientBrush gradientBrush)
		{
			JArray jArray = new JArray();
			foreach (GradientStop gradientStop in gradientBrush.GradientStops)
			{
				if (jArray.Count == 16)
				{
					break;
				}
				jArray.Add(new JObject
				{
					["offset"] = gradientStop.Offset,
					["color"] = gradientStop.Color.ToString()
				});
			}
			jObject["stops"] = jArray;
			jObject["truncated"] = gradientBrush.GradientStops.Count > jArray.Count;
		}
		if (brush is ImageBrush imageBrush)
		{
			jObject["hasImage"] = imageBrush.ImageSource != null;
		}
		return jObject;
	}

	private async Task LogBackgroundDiagnosticsCoreAsync(string reason)
	{
		IEmbeddedBrowser browser = _browser;
		if (!(browser is WebView2EmbeddedBrowser browser2))
		{
			return;
		}
		try
		{
			string text = reason;
			int num = ++_backgroundDiagnosticSequence;
			reason = text + "#" + num;
			int navigationVersion = _bridgeVerificationVersion;
			JArray jArray = new JArray();
			DependencyObject dependencyObject = browser2.View;
			while (dependencyObject != null && jArray.Count < 64)
			{
				System.Windows.Media.Brush brush = (dependencyObject as Panel)?.Background ?? (dependencyObject as Border)?.Background ?? (dependencyObject as Control)?.Background;
				UIElement uIElement = dependencyObject as UIElement;
				jArray.Add(new JObject
				{
					["type"] = dependencyObject.GetType().Name,
					["background"] = DescribeBackgroundBrush(brush),
					["opacity"] = uIElement?.Opacity,
					["visible"] = uIElement?.IsVisible
				});
				dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
			}
			System.Drawing.Color defaultBackgroundColor = browser2.DefaultBackgroundColor;
			Window window = Window.GetWindow(this);
			Border border = window?.FindName("WindowBackground") as Border;
			UtilLog.Info("AgentBackground native " + new JObject
			{
				["reason"] = reason,
				["opaqueMode"] = LocalSettings.Settings.ExtraSettings.AIOpaqueBrowserBackground,
				["dark"] = ThemeUtil.IsDark(),
				["control"] = browser2.View.GetType().Name,
				["argb"] = defaultBackgroundColor.ToArgb().ToString("X8"),
				["wpfRenderTier"] = RenderCapability.Tier >> 16,
				["highContrast"] = SystemParameters.HighContrast,
				["ancestors"] = jArray,
				["ancestorsTruncated"] = dependencyObject != null,
				["windowBackground"] = DescribeBackgroundBrush(window?.Background),
				["themeImage"] = new JObject
				{
					["visible"] = border?.Visibility.ToString(),
					["opacity"] = border?.Opacity,
					["brush"] = DescribeBackgroundBrush(border?.Background)
				}
			}.ToString(Formatting.None));
			if (!_isBrowserInitialized || browser2.Source == null || !string.Equals(browser2.Source.GetLeftPart(UriPartial.Authority), _webAppUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			Task<string> scriptTask = browser2.ExecuteScriptAsync("(() => {\r\n            const nodes = [], seen = new Map();\r\n            let truncated = false;\r\n            const style = (el, pseudo) => {\r\n                const s = getComputedStyle(el, pseudo);\r\n                return { background: s.backgroundColor, image: s.backgroundImage !== 'none',\r\n                    color: s.color, opacity: s.opacity, blend: s.mixBlendMode,\r\n                    filter: s.filter !== 'none', backdrop: !!s.backdropFilter && s.backdropFilter !== 'none',\r\n                    generated: pseudo ? s.content !== 'none' && s.content !== 'normal' : false };\r\n            };\r\n            const describe = el => {\r\n                if (seen.has(el)) return seen.get(el);\r\n                if (nodes.length >= 128) { truncated = true; return null; }\r\n                const index = nodes.length;\r\n                seen.set(el, index);\r\n                nodes.push({ tag: el.tagName, style: style(el),\r\n                    before: style(el, '::before'), after: style(el, '::after') });\r\n                return index;\r\n            };\r\n            const chain = el => {\r\n                const ancestors = [];\r\n                for (; el && ancestors.length < 128; el = el.parentElement) ancestors.push(el);\r\n                const depthTruncated = !!el;\r\n                if (depthTruncated) truncated = true;\r\n                const indices = ancestors.reverse().map(describe);\r\n                return { nodes: indices, truncated: depthTruncated || indices.some(i => i === null) };\r\n            };\r\n            const points = [[.1,.1],[.5,.25],[.5,.5],[.9,.5],[.5,.9]];\r\n            return { ready: document.readyState, dark: matchMedia('(prefers-color-scheme: dark)').matches,\r\n                forcedColors: matchMedia('(forced-colors: active)').matches,\r\n                html: document.documentElement ? describe(document.documentElement) : null,\r\n                body: document.body ? describe(document.body) : null,\r\n                roots: document.body ? Array.from(document.body.children).slice(0,4).map(describe) : [],\r\n                samples: points.map(p => {\r\n                    const hits = document.elementsFromPoint(Math.floor(innerWidth*p[0]), Math.floor(innerHeight*p[1]));\r\n                    const hitsTruncated = hits.length > 8;\r\n                    if (hitsTruncated) truncated = true;\r\n                    return { point: p, hitsTruncated, layers: hits.slice(0,8).map(chain) };\r\n                }), nodes, truncated,\r\n                scope: 'viewport-hit-chains; excludes non-hit-testable overlays and iframe/shadow contents' };\r\n        })()");
			if (await Task.WhenAny(scriptTask, Task.Delay(2000)) != scriptTask)
			{
				scriptTask.ContinueWith(delegate(Task<string> task)
				{
					_ = task.Exception;
				}, TaskContinuationOptions.OnlyOnFaulted);
				UtilLog.Info("AgentBackground web " + reason + ": timeout");
				return;
			}
			string text2 = await scriptTask;
			if (browser2 != _browser || navigationVersion != _bridgeVerificationVersion)
			{
				UtilLog.Info("AgentBackground web " + reason + ": discarded_navigation_changed");
			}
			else if (text2 != null && text2.Length <= 262144)
			{
				int num2 = (text2.Length + 6000 - 1) / 6000;
				for (int num3 = 0; num3 < num2; num3++)
				{
					UtilLog.Info("AgentBackground web " + reason + " part=" + (num3 + 1) + "/" + num2 + ": " + text2.Substring(num3 * 6000, Math.Min(6000, text2.Length - num3 * 6000)));
				}
			}
			else
			{
				UtilLog.Info("AgentBackground web " + reason + ": result_too_large");
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info("AgentBackground diagnostic failed: " + ex.GetType().Name);
		}
	}

	public AIWebAppView()
	{
		_webAppUri = BuildCurrentWebAppUri();
		_authenticationCookieScopeUri = NavigateWebBrowserWindow.GetAuthenticationCookieScopeUri(_webAppUri, new Uri(BaseUrl.GetApiDomain(), UriKind.Absolute));
		InitializeComponent();
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
		base.IsVisibleChanged += OnIsVisibleChanged;
		DataChangedNotifier.ThemeModeChanged += OnThemeChanged;
		BackgroundCompatibilityChanged += OnBackgroundCompatibilityChanged;
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnMembershipChanged, "IsPro");
	}

	internal void SetThemeOverride(bool isDark)
	{
		if (_themeOverride != isDark)
		{
			_themeOverride = isDark;
			OnThemeChanged(this, EventArgs.Empty);
		}
	}

	private static Uri BuildCurrentWebAppUri(bool? themeOverride = null)
	{
		bool flag = themeOverride ?? ThemeUtil.IsDark();
		return BuildWebAppUri(BaseUrl.GetAIAgentWebUrl(), Utils.GetLanguage(), flag, ThemeUtil.GetColorString("ColorPrimary", flag), (int)ticktick_WPF.Util.DateUtils.GetSysDateFormatType(), Utils.GetVersion());
	}

	internal static Uri BuildWebAppUri(string baseUrl, string language, bool isDark, string themeColor, int dateFormat, string version)
	{
		return new Uri(UriUtils.AddAttribute(UriUtils.AddAttribute(UriUtils.AddAttribute(UriUtils.AddAttribute(UriUtils.AddAttribute(UriUtils.AddAttribute(UriUtils.AddAttribute(baseUrl, "lang", language), "dark", isDark ? "true" : "false"), "df", dateFormat.ToString()), "channel", "windows"), "version", version), "backgroundColor", "transparent"), "themeColor", themeColor), UriKind.Absolute);
	}

	public async Task EnsureInitializedAsync()
	{
		if (_browser != null)
		{
			return;
		}
		if (_initializationTask == null)
		{
			if (!_lifecycle.TryBeginInitialization())
			{
				return;
			}
			_initializationTask = InitializeBrowserAsync();
		}
		Task initializationTask = _initializationTask;
		try
		{
			await initializationTask;
		}
		finally
		{
			if (initializationTask == _initializationTask)
			{
				_initializationTask = null;
			}
		}
	}

	public Task DisposeAsync()
	{
		base.Loaded -= OnLoaded;
		base.Unloaded -= OnUnloaded;
		base.IsVisibleChanged -= OnIsVisibleChanged;
		DataChangedNotifier.FontFamilyChanged -= OnFontFamilyChanged;
		DataChangedNotifier.ThemeModeChanged -= OnThemeChanged;
		BackgroundCompatibilityChanged -= OnBackgroundCompatibilityChanged;
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnMembershipChanged, "IsPro");
		_membershipRefreshPending = false;
		_lifecycle.Dispose();
		return DisposeBrowserAsync();
	}

	private async Task InitializeBrowserAsync()
	{
		IEmbeddedBrowser browser = null;
		try
		{
			UtilLog.Info("Agent WebView initialization started: loaded=" + base.IsLoaded + ", visible=" + base.IsVisible + ", size=" + base.ActualWidth + "x" + base.ActualHeight);
			ShowLoading();
			EmbeddedBrowserOptions options = new EmbeddedBrowserOptions
			{
				UseCase = "ai_webapp",
				UserAgentSuffix = "/TickTick/PC",
				OpenNewWindowsExternally = true,
				EnableHostObjects = true,
				RestrictNavigationToTrustedOrigins = true,
				UseTransparentBackground = true,
				DisableCompositionControl = LocalSettings.Settings.ExtraSettings.AIUseStandardWebView
			};
			UtilLog.Info("Agent WebView rendering mode: " + (options.DisableCompositionControl ? "standard_compatibility" : "default"));
			options.AddTrustedOrigin(_webAppUri);
			browser = EmbeddedBrowserFactory.Create(options);
			_browser = browser;
			ApplyBackgroundCompatibility();
			browser.NavigationStarting += OnNavigationStarting;
			browser.NavigationCompleted += OnNavigationCompleted;
			browser.NewWindowRequested += OnNewWindowRequested;
			browser.ProcessFailed += OnProcessFailed;
			BrowserHost.Content = browser.View;
			ApplyBrowserPresentation();
			EmbeddedBrowserPreparationResult result;
			using (await EmbeddedBrowserLifecycle.EnterAsync(browser.Engine))
			{
				UtilLog.Info("Agent WebView lifecycle acquired; initializing browser controller.");
				result = await EmbeddedBrowserRuntimeInstaller.PrepareAndInitializeAsync(browser, options, Window.GetWindow(this), OnRuntimeInstallProgress);
				UtilLog.Info("Agent WebView browser preparation completed: " + result);
				if (result == EmbeddedBrowserPreparationResult.Ready && browser == _browser)
				{
					_isBrowserInitialized = true;
					ApplyBackgroundCompatibility();
					ApplyBrowserPresentation();
					_authenticationCookieLease = await EmbeddedBrowserAuthenticationCookieLease.AcquireAsync(browser, _authenticationCookieScopeUri, new Uri[2] { _webAppUri, _authenticationCookieScopeUri }, UserManager.GetToken(), DateTime.UtcNow.AddDays(20.0), () => browser == _browser && _lifecycle.State != AIWebViewLifecycleState.Disposed);
					if (_authenticationCookieLease == null || browser != _browser)
					{
						UtilLog.Info("Agent WebView authentication cookie lease was canceled.");
						return;
					}
					UtilLog.Info("Agent WebView authentication cookie is ready.");
					if (browser.Engine == EmbeddedBrowserEngine.WebView2)
					{
						await UpdateFontStyleAsync(browser, applyToCurrentDocument: false);
						await InitializeAgentBridgeAsync(browser);
						UtilLog.Info("AgentDsBridge native host registered.");
					}
					browser.Navigate(_webAppUri);
					UtilLog.Info("Agent WebView navigation requested: " + _webAppUri.GetLeftPart(UriPartial.Authority));
				}
			}
			if (result != EmbeddedBrowserPreparationResult.Ready)
			{
				if (browser == _browser)
				{
					ShowError();
				}
				await DisposeBrowserAsync(browser);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			if (browser == null || browser == _browser)
			{
				ShowError();
			}
			if (browser != null)
			{
				await DisposeBrowserAsync(browser);
			}
		}
	}

	private void OnNavigationCompleted(object sender, EmbeddedBrowserNavigationCompletedEventArgs e)
	{
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (sender == _browser)
			{
				if (e.IsSuccess && e.Uri != null && (e.Uri.Scheme == Uri.UriSchemeHttp || e.Uri.Scheme == Uri.UriSchemeHttps))
				{
					if (_lifecycle.TryMarkReady() || _lifecycle.State == AIWebViewLifecycleState.Ready)
					{
						RequestBrowserPresentation(showFrame: true, enableInteraction: false);
						if (!TryRefreshMembershipPage())
						{
							if (_isAgentPageStateResolved)
							{
								ShowBrowserContent();
							}
							StartAgentBridgeVerification(sender as IEmbeddedBrowser, "navigation_completed");
						}
					}
				}
				else if (!(e.Error == "OperationCanceled"))
				{
					SetAgentBridgeReady(isReady: false);
					_lifecycle.TryMarkReady();
					ShowWebPageFallback();
				}
			}
		});
	}

	private void OnNavigationStarting(object sender, EmbeddedBrowserNavigationStartingEventArgs e)
	{
		if (sender == _browser && e.Uri != null && (e.Uri.Scheme == Uri.UriSchemeHttp || e.Uri.Scheme == Uri.UriSchemeHttps))
		{
			_bridgeVerificationVersion++;
			_isAgentBridgeVerificationRunning = false;
			_isWebPageFallbackPresented = false;
			SetAgentBridgeReady(isReady: false);
			UtilLog.Info("AgentDsBridge navigation started: " + e.Uri.GetLeftPart(UriPartial.Authority));
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
		if (!RequiresBrowserRecreation(e.Reason))
		{
			return;
		}
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (sender == _browser)
			{
				SetAgentBridgeReady(isReady: false);
				ShowError();
			}
		});
	}

	internal static bool RequiresBrowserRecreation(string reason)
	{
		if (!(reason == "BrowserProcessExited"))
		{
			return reason == "RenderProcessExited";
		}
		return true;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		ApplyBackgroundCompatibility();
		DataChangedNotifier.FontFamilyChanged -= OnFontFamilyChanged;
		DataChangedNotifier.FontFamilyChanged += OnFontFamilyChanged;
		RetryAgentBridgeVerificationWhenVisible("loaded");
		if (_lifecycle.State == AIWebViewLifecycleState.Ready)
		{
			await UpdateFontStyleAsync(_browser, applyToCurrentDocument: true);
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.FontFamilyChanged -= OnFontFamilyChanged;
	}

	private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!TryRefreshMembershipPage())
		{
			RetryAgentBridgeVerificationWhenVisible("view_visible");
		}
	}

	private void RetryAgentBridgeVerificationWhenVisible(string reason)
	{
		if (ShouldRetryAgentBridgeVerification(base.IsVisible, _browser != null, _lifecycle.State, _isAgentBridgeReady))
		{
			StartAgentBridgeVerification(_browser, reason);
		}
	}

	internal static bool ShouldRetryAgentBridgeVerification(bool isVisible, bool hasBrowser, AIWebViewLifecycleState lifecycleState, bool isAgentBridgeReady)
	{
		if (isVisible && hasBrowser && lifecycleState == AIWebViewLifecycleState.Ready)
		{
			return !isAgentBridgeReady;
		}
		return false;
	}

	private void OnMembershipChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				OnMembershipChanged(sender, e);
			});
		}
		else if (_lifecycle.State != AIWebViewLifecycleState.Disposed && _browser != null)
		{
			_membershipRefreshPending = true;
			TryRefreshMembershipPage();
		}
	}

	private bool TryRefreshMembershipPage()
	{
		if (!_membershipRefreshPending || (!base.IsVisible && !_hasSelectedConversation) || _browser == null || _lifecycle.State != AIWebViewLifecycleState.Ready)
		{
			return false;
		}
		_membershipRefreshPending = false;
		try
		{
			SetAgentBridgeReady(isReady: false);
			MarkAgentPageStateResolving();
			_browser.Navigate(_webAppUri);
			return true;
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent WebView membership refresh failed: " + ExceptionUtils.BuildExceptionMessage(ex));
			ShowWebPageFallback();
			return false;
		}
	}

	private async void OnFontFamilyChanged(object sender, EventArgs e)
	{
		await UpdateFontStyleAsync(_browser, applyToCurrentDocument: true);
	}

	private void OnThemeChanged(object sender, EventArgs e)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				OnThemeChanged(sender, e);
			});
			return;
		}
		_webAppUri = BuildCurrentWebAppUri(_themeOverride);
		IEmbeddedBrowser browser = _browser;
		if (browser == null || _lifecycle.State != AIWebViewLifecycleState.Ready)
		{
			return;
		}
		try
		{
			_isBrowserPresentationSuspendedForTheme = true;
			ApplyBackgroundCompatibility();
			ApplyBrowserPresentation();
			SetAgentBridgeReady(isReady: false);
			MarkAgentPageStateResolving();
			browser.Navigate(_webAppUri);
			UtilLog.Info("Agent WebView navigation refreshed for theme: dark=" + ThemeUtil.IsDark() + ", color=" + ThemeUtil.GetColorString("ColorPrimary", ThemeUtil.IsDark()));
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent WebView theme refresh failed: " + ExceptionUtils.BuildExceptionMessage(ex));
			ShowWebPageFallback();
		}
	}

	private async Task UpdateFontStyleAsync(IEmbeddedBrowser browser, bool applyToCurrentDocument)
	{
		if (!(browser is WebView2EmbeddedBrowser webView2) || browser != _browser)
		{
			return;
		}
		int updateVersion = ++_fontStyleUpdateVersion;
		try
		{
			AIWebFontStyleDefinition aIWebFontStyleDefinition = AIWebFontStyle.ResolveCurrent();
			if (!string.IsNullOrEmpty(aIWebFontStyleDefinition.MappedFolderPath))
			{
				webView2.SetVirtualHostNameToFolderMapping("ticktick-app-font.local", aIWebFontStyleDefinition.MappedFolderPath);
				_hasFontFolderMapping = true;
			}
			else if (_hasFontFolderMapping)
			{
				webView2.ClearVirtualHostNameToFolderMapping("ticktick-app-font.local");
				_hasFontFolderMapping = false;
			}
			string script = AIWebFontStyle.BuildInjectionScript(aIWebFontStyleDefinition, _webAppUri.GetLeftPart(UriPartial.Authority));
			if (!string.Equals(script, _fontStyleScript, StringComparison.Ordinal))
			{
				string text = await webView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
				if (updateVersion != _fontStyleUpdateVersion || browser != _browser)
				{
					webView2.RemoveScriptToExecuteOnDocumentCreated(text);
					return;
				}
				string fontStyleScriptId = _fontStyleScriptId;
				_fontStyleScriptId = text;
				_fontStyleScript = script;
				webView2.RemoveScriptToExecuteOnDocumentCreated(fontStyleScriptId);
			}
			if (applyToCurrentDocument && browser == _browser)
			{
				await ExecuteAgentScriptAsync(browser, script);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent WebView font style injection failed: " + ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	internal async Task<bool> StartNewConversationAsync()
	{
		return await CallBooleanAgentMethodAsync("agent.newConversation");
	}

	internal async Task<JObject> GetAgentStateAsync()
	{
		JToken jToken = await CallAgentMethodAsync("agent.getState");
		if (jToken is JObject result)
		{
			return result;
		}
		if (jToken == null || jToken.Type != JTokenType.String)
		{
			return null;
		}
		try
		{
			return JObject.Parse(jToken.Value<string>());
		}
		catch (JsonException)
		{
			return null;
		}
	}

	internal void MarkAgentPageStateResolved(string page, string conversationId)
	{
		_hasSelectedConversation = string.Equals(page, "conversation", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(conversationId);
		_isAgentPageStateResolved = true;
		_resolvedAgentPage = page;
		_resolvedConversationId = conversationId;
		if (_lifecycle.State == AIWebViewLifecycleState.Ready)
		{
			ShowBrowserContent();
		}
	}

	internal void MarkAgentPageStateResolving()
	{
		if (!_isWebPageFallbackPresented)
		{
			ShowLoading();
		}
	}

	internal async Task<bool> SwitchConversationAsync(string conversationId)
	{
		if (string.IsNullOrWhiteSpace(conversationId))
		{
			return false;
		}
		return await CallBooleanAgentMethodAsync("agent.switchConversation", new JObject { ["conversationId"] = conversationId });
	}

	internal async Task<bool> SubmitMessageAsync(string content, string messageId)
	{
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		if (!Utils.IsNetworkAvailable())
		{
			Utils.Toast(Utils.GetString("NoNetwork"));
			return false;
		}
		JObject jObject = new JObject { ["content"] = content };
		string methodName = "agent.sendMessage";
		if (!string.IsNullOrWhiteSpace(messageId))
		{
			jObject["messageId"] = messageId;
			methodName = "agent.editMessage";
		}
		return await CallBooleanAgentMethodAsync(methodName, jObject);
	}

	internal async Task<bool> StopGeneratingAsync()
	{
		return await CallBooleanAgentMethodAsync("agent.stopGenerating");
	}

	internal async Task<bool> ScrollToBottomAsync()
	{
		return await CallBooleanAgentMethodAsync("agent.scrollToBottom");
	}

	internal async Task<JObject> GetApprovalsAsync()
	{
		JToken jToken = await CallAgentMethodAsync("agent.getApprovals");
		if (jToken is JObject result)
		{
			return result;
		}
		if (jToken == null || jToken.Type != JTokenType.String)
		{
			return null;
		}
		try
		{
			return JObject.Parse(jToken.Value<string>());
		}
		catch (JsonException)
		{
			return null;
		}
	}

	internal async Task<bool> RespondToApprovalAsync(string conversationId, string messageId, string decision, bool approveRemaining = false)
	{
		if (string.IsNullOrWhiteSpace(conversationId) || string.IsNullOrWhiteSpace(messageId) || !AgentApproval.IsSupportedDecision(decision) || (approveRemaining && decision != "approved"))
		{
			return false;
		}
		return await CallBooleanAgentMethodAsync("agent.respondToApproval", new JObject
		{
			["conversationId"] = conversationId,
			["messageId"] = messageId,
			["decision"] = decision,
			["approveRemaining"] = approveRemaining
		});
	}

	internal async Task<bool> SubmitRatingAsync(AgentRatingSubmission submission)
	{
		if (submission == null || !submission.IsValid)
		{
			return false;
		}
		return await CallBooleanAgentMethodAsync("agent.submitRating", submission.ToPayload());
	}

	internal async Task<bool> DebugNonBlockingAppUpdateRequiredAsync()
	{
		return await CallBooleanAgentMethodAsync("agent.debugNonBlockingAppUpdateRequired");
	}

	internal async Task<bool> DebugBlockingAppUpdateRequiredAsync()
	{
		return await CallBooleanAgentMethodAsync("agent.debugBlockingAppUpdateRequired");
	}

	private async Task<bool> CallBooleanAgentMethodAsync(string methodName, JToken payload = null)
	{
		JToken jToken = await CallAgentMethodAsync(methodName, payload);
		return jToken != null && jToken.Type == JTokenType.Boolean && jToken.Value<bool>();
	}

	private async Task<JToken> CallAgentMethodAsync(string methodName, JToken payload = null)
	{
		AgentDsBridgeHost agentBridge = _agentBridge;
		if (agentBridge == null || !_isAgentBridgeReady)
		{
			return null;
		}
		try
		{
			return await agentBridge.CallWebAsync(methodName, payload);
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		catch (Exception ex2)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex2));
			return null;
		}
	}

	private async Task InitializeAgentBridgeAsync(IEmbeddedBrowser browser)
	{
		AgentDsBridgeHost bridge = new AgentDsBridgeHost((string script) => ExecuteAgentScriptAsync(browser, script));
		bridge.WebReady += OnAgentBridgeWebReady;
		bridge.RegisterNativeMethod("getStatusBarHeight", (JToken payload) => 0.0);
		bridge.RegisterNativeMethod("toast", ShowNativeToast);
		bridge.RegisterNativeMethod("track", delegate(JToken payload)
		{
			UtilLog.Info("Agent Native track payload: " + (payload?.ToString(Formatting.None) ?? "null"));
			if (payload is JObject jObject)
			{
				JToken? jToken = jObject["category"];
				if (jToken != null && jToken.Type == JTokenType.String)
				{
					JToken? jToken2 = jObject["event"];
					if (jToken2 != null && jToken2.Type == JTokenType.String)
					{
						JToken? jToken3 = jObject["label"];
						if (jToken3 != null && jToken3.Type == JTokenType.String)
						{
							UserActCollectUtils.AddClickEvent((string?)jObject["category"], (string?)jObject["event"], (string?)jObject["label"]);
							return true;
						}
					}
				}
			}
			return false;
		});
		RegisterAgentNotification(bridge, "agent.fillComposer");
		RegisterAgentNotification(bridge, "agent.approvalsChanged");
		RegisterAgentNotification(bridge, "agent.showFeedback");
		RegisterAgentNotification(bridge, "agent.ratingResult");
		RegisterAgentNotification(bridge, "agent.conversationCreated");
		RegisterAgentNotification(bridge, "agent.refreshConversationTitle");
		RegisterAgentNotification(bridge, "agent.pageChanged");
		RegisterAgentNotification(bridge, "agent.tokenLimitReached");
		RegisterAgentNotification(bridge, "agent.appUpdateRequired");
		RegisterAgentNotification(bridge, "agent.cardClick");
		RegisterAgentNotification(bridge, "agent.viewFullCardListMode");
		RegisterAgentNotification(bridge, "agent.stateChanged");
		RegisterAgentNotification(bridge, "agent.scrollToBottomButtonVisibilityChanged");
		_agentBridge = bridge;
		try
		{
			await browser.RegisterHostObjectAsync("_dsbridge", bridge);
		}
		catch
		{
			if (bridge == _agentBridge)
			{
				_agentBridge = null;
			}
			bridge.WebReady -= OnAgentBridgeWebReady;
			bridge.Dispose();
			throw;
		}
	}

	private void RegisterAgentNotification(AgentDsBridgeHost bridge, string methodName)
	{
		bridge.RegisterNativeMethod(methodName, delegate(JToken payload)
		{
			if (methodName == "agent.showFeedback" || methodName == "agent.ratingResult")
			{
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					if (bridge != _agentBridge)
					{
						return;
					}
					try
					{
						RaiseAgentNotification(methodName, payload);
					}
					catch (Exception ex)
					{
						UtilLog.Info("Agent feedback notification failed: " + methodName + ", " + ex);
					}
				});
				return (object)null;
			}
			RaiseAgentNotification(methodName, payload);
			return (object)null;
		});
	}

	private object ShowNativeToast(JToken payload)
	{
		string message = ExtractNativeMessage(payload);
		if (string.IsNullOrWhiteSpace(message))
		{
			return false;
		}
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			Utils.Toast(message);
		});
		return true;
	}

	internal static string ExtractNativeMessage(JToken payload)
	{
		if (payload != null && payload.Type == JTokenType.String)
		{
			return payload.Value<string>()?.Trim();
		}
		return (((string?)payload?["message"]) ?? ((string?)payload?["text"]) ?? ((string?)payload?["content"]))?.Trim();
	}

	private void RaiseAgentNotification(string methodName, JToken payload)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.Invoke(delegate
			{
				RaiseAgentNotification(methodName, payload);
			});
			return;
		}
		this.AgentNotificationReceived?.Invoke(this, new AgentBridgeNativeEventArgs(methodName, payload));
		if (ShouldRefreshConversationLists(methodName, payload, ref _wasGenerating))
		{
			AIConversationService.NotifyConversationsChanged(this);
		}
	}

	internal static bool ShouldRefreshConversationLists(string methodName, JToken payload, ref bool wasGenerating)
	{
		if (methodName == "agent.stateChanged")
		{
			bool? flag = (bool?)payload?["isGenerating"];
			if (flag.HasValue)
			{
				bool valueOrDefault = flag == true;
				bool result = wasGenerating && !valueOrDefault;
				wasGenerating = valueOrDefault;
				return result;
			}
		}
		if (!(methodName == "agent.refreshConversationTitle"))
		{
			if (methodName == "agent.conversationCreated")
			{
				return (bool?)payload?["isLocalOnly"] != true;
			}
			return false;
		}
		return true;
	}

	private void OnAgentBridgeWebReady(object sender, EventArgs e)
	{
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (sender == _agentBridge)
			{
				UtilLog.Info("AgentDsBridge dsinit received; verifying registered Web methods.");
				StartAgentBridgeVerification(_browser, "dsinit");
			}
		});
	}

	private async void StartAgentBridgeVerification(IEmbeddedBrowser browser, string reason)
	{
		if (browser == null || browser != _browser || _agentBridge == null || _isAgentBridgeVerificationRunning || _lifecycle.State != AIWebViewLifecycleState.Ready)
		{
			return;
		}
		_isAgentBridgeVerificationRunning = true;
		int verificationVersion = ++_bridgeVerificationVersion;
		JObject lastProbe = null;
		try
		{
			UtilLog.Info("AgentDsBridge verification started: " + reason);
			await ExecuteAgentProbeAsync(browser, WebView2EmbeddedBrowser.BuildHostObjectInjectionScript("_dsbridge", new string[1] { _webAppUri.GetLeftPart(UriPartial.Authority) }));
			for (int attempt = 1; attempt <= 40; attempt++)
			{
				if (browser != _browser || _agentBridge == null || verificationVersion != _bridgeVerificationVersion)
				{
					return;
				}
				string result = await ExecuteAgentProbeAsync(browser, BuildAgentBridgeProbeScript());
				if (browser != _browser || verificationVersion != _bridgeVerificationVersion)
				{
					return;
				}
				lastProbe = ParseAgentBridgeProbeResult(result);
				if (IsAgentBridgeProbeReady(lastProbe))
				{
					SetAgentBridgeReady(isReady: true);
					UtilLog.Info("AgentDsBridge verification succeeded: attempt=" + attempt + ", state=" + lastProbe.ToString(Formatting.None));
					return;
				}
				if (attempt < 40)
				{
					await Task.Delay(250);
				}
			}
			if (browser == _browser && verificationVersion == _bridgeVerificationVersion)
			{
				SetAgentBridgeReady(isReady: false);
				UtilLog.Info("AgentDsBridge verification failed: state=" + (lastProbe?.ToString(Formatting.None) ?? "null"));
				if (base.IsVisible)
				{
					ShowWebPageFallback();
				}
			}
		}
		catch (Exception ex)
		{
			if (browser == _browser && verificationVersion == _bridgeVerificationVersion)
			{
				SetAgentBridgeReady(isReady: false);
				UtilLog.Info("AgentDsBridge verification exception: " + ExceptionUtils.BuildExceptionMessage(ex));
				if (base.IsVisible)
				{
					ShowWebPageFallback();
				}
			}
		}
		finally
		{
			if (verificationVersion == _bridgeVerificationVersion)
			{
				_isAgentBridgeVerificationRunning = false;
			}
		}
	}

	private async Task<string> ExecuteAgentProbeAsync(IEmbeddedBrowser browser, string script)
	{
		Task<string> execution = ExecuteAgentScriptAsync(browser, script);
		if (await Task.WhenAny(execution, Task.Delay(5000)) != execution)
		{
			execution.ContinueWith(delegate(Task<string> task)
			{
				_ = task.Exception;
			}, TaskContinuationOptions.OnlyOnFaulted);
			throw new TimeoutException("Agent WebView bridge probe timed out.");
		}
		return await execution;
	}

	internal static string BuildAgentBridgeProbeScript()
	{
		string arg = JsonConvert.SerializeObject("agent.sendMessage");
		return string.Format("\r\n(function () {{\r\n    function hasHandler(registry, methodName) {{\r\n        if (!registry) return false;\r\n        if (typeof registry[methodName] === 'function') return true;\r\n        var parts = methodName.split('.');\r\n        if (parts.length < 2 || !registry._obs) return false;\r\n        var method = parts.pop();\r\n        var namespace = parts.join('.');\r\n        return !!registry._obs[namespace] &&\r\n               typeof registry._obs[namespace][method] === 'function';\r\n    }}\r\n\r\n    var state = {{\r\n        origin: window.location.origin,\r\n        hasNativeHost: false,\r\n        hasDsBridge: typeof window.dsBridge === 'object',\r\n        hasMessageHandler: typeof window._handleMessageFromNative === 'function',\r\n        hasSendMessage: hasHandler(window._dsf, {0}) || hasHandler(window._dsaf, {0}),\r\n        initRequested: false,\r\n        error: null\r\n    }};\r\n    try {{\r\n        state.hasNativeHost = !!window._dsbridge &&\r\n                              typeof window._dsbridge.call === 'function';\r\n        if (state.hasNativeHost && state.hasDsBridge && state.hasSendMessage) {{\r\n            window.dsBridge.call('_dsb.dsinit');\r\n            state.initRequested = true;\r\n        }}\r\n    }} catch (error) {{\r\n        state.error = String(error && error.message || error);\r\n    }}\r\n    return JSON.stringify(state);\r\n}})();", arg);
	}

	internal static JObject ParseAgentBridgeProbeResult(string result)
	{
		if (string.IsNullOrWhiteSpace(result))
		{
			return null;
		}
		try
		{
			JToken jToken = JToken.Parse(result);
			if (jToken.Type == JTokenType.String)
			{
				return JObject.Parse(jToken.Value<string>());
			}
			return jToken as JObject;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	internal static bool IsAgentBridgeProbeReady(JObject state)
	{
		if ((bool?)state?["hasNativeHost"] == true && (bool?)state?["hasDsBridge"] == true && (bool?)state?["hasMessageHandler"] == true && (bool?)state?["hasSendMessage"] == true && (bool?)state?["initRequested"] == true)
		{
			if (state == null)
			{
				return false;
			}
			return state["error"]?.Type == JTokenType.Null;
		}
		return false;
	}

	private Task<string> ExecuteAgentScriptAsync(IEmbeddedBrowser browser, string script)
	{
		if (base.Dispatcher.CheckAccess())
		{
			if (browser != _browser)
			{
				return Task.FromException<string>(new ObjectDisposedException("AIWebAppView"));
			}
			return browser.ExecuteScriptAsync(script);
		}
		return base.Dispatcher.InvokeAsync(() => ExecuteAgentScriptAsync(browser, script)).Task.Unwrap();
	}

	private void SetAgentBridgeReady(bool isReady)
	{
		if (!isReady)
		{
			_wasGenerating = false;
		}
		if (_isAgentBridgeReady != isReady)
		{
			_isAgentBridgeReady = isReady;
			this.AgentBridgeReadyChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private void OnRuntimeInstallProgress(EmbeddedBrowserRuntimeInstallProgress progress)
	{
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (progress != null)
			{
				SetShouldPresentStatusPanel(progress.Percentage.HasValue || progress.IsInstalling);
				StatusText.Text = progress.Message;
				RuntimeProgress.Visibility = ((!progress.Percentage.HasValue && !progress.IsInstalling) ? Visibility.Collapsed : Visibility.Visible);
				RuntimeProgress.IsIndeterminate = progress.IsInstalling;
				if (progress.Percentage.HasValue)
				{
					RuntimeProgress.Value = progress.Percentage.Value;
				}
			}
		});
	}

	private void ShowLoading()
	{
		if (!_lifecycle.CanRetry && _lifecycle.State != AIWebViewLifecycleState.Disposed)
		{
			SetShouldPresentStatusPanel(shouldPresent: false);
			_isAgentPageStateResolved = false;
			_resolvedAgentPage = null;
			_resolvedConversationId = null;
			if (_lifecycle.State == AIWebViewLifecycleState.Ready)
			{
				LoadingIndicator.IsActive = false;
				LoadingIndicator.Visibility = Visibility.Collapsed;
				RuntimeProgress.IsIndeterminate = false;
				RuntimeProgress.Visibility = Visibility.Collapsed;
				StatusPanel.Visibility = Visibility.Collapsed;
				RequestBrowserPresentation(showFrame: true, enableInteraction: true);
			}
			else
			{
				RequestBrowserPresentation(showFrame: false, enableInteraction: false);
				LoadingIndicator.Visibility = Visibility.Visible;
				LoadingIndicator.IsActive = true;
				StatusText.Text = Utils.GetString("Loading");
				RuntimeProgress.IsIndeterminate = false;
				RuntimeProgress.Visibility = Visibility.Collapsed;
				StatusPanel.Visibility = Visibility.Visible;
			}
		}
	}

	private void ShowBrowserContent()
	{
		_isWebPageFallbackPresented = false;
		_isBrowserPresentationSuspendedForTheme = false;
		bool num = !HasPresentedBrowserContent;
		HasPresentedBrowserContent = true;
		SetShouldPresentStatusPanel(shouldPresent: false);
		LoadingIndicator.IsActive = false;
		LoadingIndicator.Visibility = Visibility.Collapsed;
		RuntimeProgress.IsIndeterminate = false;
		RuntimeProgress.Visibility = Visibility.Collapsed;
		StatusPanel.Visibility = Visibility.Collapsed;
		RequestBrowserPresentation(showFrame: true, enableInteraction: true);
		if (num)
		{
			this.StatusPresentationChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private void ShowWebPageFallback()
	{
		if (_lifecycle.State == AIWebViewLifecycleState.Ready)
		{
			ShowBrowserContent();
			_isWebPageFallbackPresented = true;
			SetShouldPresentStatusPanel(shouldPresent: true);
		}
	}

	private void ShowError()
	{
		_isBrowserPresentationSuspendedForTheme = false;
		ApplyBrowserPresentation();
		if (_lifecycle.TryMarkFailed() || _lifecycle.TryMarkRuntimeFailed())
		{
			_bridgeVerificationVersion++;
			_isAgentBridgeVerificationRunning = false;
			SetAgentBridgeReady(isReady: false);
			SetShouldPresentStatusPanel(shouldPresent: true);
			_isAgentPageStateResolved = false;
			_resolvedAgentPage = null;
			_resolvedConversationId = null;
			RequestBrowserPresentation(showFrame: false, enableInteraction: false);
			LoadingIndicator.IsActive = false;
			LoadingIndicator.Visibility = Visibility.Collapsed;
			StatusText.Text = Utils.GetString("LoadFailed") + " · " + Utils.GetString("Retry");
			RuntimeProgress.IsIndeterminate = false;
			RuntimeProgress.Visibility = Visibility.Collapsed;
			StatusPanel.Visibility = Visibility.Visible;
		}
	}

	internal void SetHostSwitchPresentationSuspended(bool suspended)
	{
		_isBrowserPresentationSuspendedForHostSwitch = suspended;
		ApplyBrowserPresentation();
	}

	internal async Task WaitForHostSwitchLayoutAsync()
	{
		IEmbeddedBrowser browser = _browser;
		if (browser == null || !HasPresentedBrowserContent)
		{
			return;
		}
		Task deadline = Task.Delay(1500);
		string previous = null;
		int stableSamples = 0;
		try
		{
			while (!deadline.IsCompleted && browser == _browser)
			{
				DpiScale dpi = VisualTreeHelper.GetDpi(browser.View);
				Task<string> probe = ExecuteAgentScriptAsync(browser, AIWebScrollPosition.ProbeLayout(browser.View.ActualWidth * dpi.DpiScaleX, browser.View.ActualHeight * dpi.DpiScaleY));
				if (await Task.WhenAny(probe, deadline) != probe)
				{
					probe.ContinueWith(delegate(Task<string> task)
					{
						_ = task.Exception;
					}, TaskContinuationOptions.OnlyOnFaulted);
					break;
				}
				string text = await probe;
				if (!string.IsNullOrEmpty(text) && text != "null" && text == previous)
				{
					int num = stableSamples + 1;
					stableSamples = num;
					if (num >= 2)
					{
						await WaitForHostSwitchRenderingAsync();
						return;
					}
				}
				else
				{
					stableSamples = 0;
				}
				previous = text;
				await Task.Delay(32);
			}
			UtilLog.Info("Agent host layout presentation wait timed out.");
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent host layout presentation wait failed: " + ex.Message);
		}
	}

	private static async Task WaitForHostSwitchRenderingAsync()
	{
		TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
		int frames = 0;
		EventHandler handler = delegate
		{
			if (++frames >= 2)
			{
				completion.TrySetResult(result: true);
			}
		};
		CompositionTarget.Rendering += handler;
		try
		{
			await Task.WhenAny(completion.Task, Task.Delay(100));
		}
		finally
		{
			CompositionTarget.Rendering -= handler;
		}
	}

	internal async Task PreserveHostScrollPositionAsync(bool restore)
	{
		IEmbeddedBrowser browser = _browser;
		if (browser == null || !HasPresentedBrowserContent)
		{
			return;
		}
		try
		{
			DpiScale dpi = VisualTreeHelper.GetDpi(browser.View);
			Task<string> capture = ExecuteAgentScriptAsync(browser, restore ? AIWebScrollPosition.Restore(browser.View.ActualWidth * dpi.DpiScaleX, browser.View.ActualHeight * dpi.DpiScaleY) : "\r\n(() => {\r\n    const key = '__ticktickHostScroll';\r\n    window[key]?.cancel();\r\n    const items = [];\r\n    const root = document.scrollingElement;\r\n    for (const el of new Set([root, ...document.querySelectorAll('*')])) {\r\n        if (!el || el.clientHeight <= 0) continue;\r\n        if (el !== root && !/(auto|scroll)/.test(getComputedStyle(el).overflowY)) continue;\r\n        const rect = el.getBoundingClientRect();\r\n        if (rect.bottom <= 0 || rect.top >= innerHeight) continue;\r\n        const viewportTop = el === root ? 0 : Math.max(0, rect.top);\r\n        let anchor = null;\r\n        const walker = document.createTreeWalker(el, NodeFilter.SHOW_TEXT);\r\n        while (walker.nextNode()) {\r\n            const node = walker.currentNode;\r\n            if (!node.textContent.trim() || /^(SCRIPT|STYLE)$/.test(node.parentElement?.tagName)) continue;\r\n            const range = document.createRange();\r\n            range.selectNodeContents(node);\r\n            const boxes = range.getClientRects();\r\n            if (!boxes.length) continue;\r\n            const box = boxes[0];\r\n            if (box.top >= viewportTop && box.top < Math.min(rect.bottom, innerHeight)) {\r\n                anchor = { node, offset: box.top - viewportTop };\r\n                break;\r\n            }\r\n        }\r\n        items.push({ el, top: el.scrollTop, left: el.scrollLeft,\r\n            bottom: el.scrollHeight - el.clientHeight - el.scrollTop <= 4, anchor });\r\n    }\r\n    const events = ['wheel', 'pointerdown', 'touchstart', 'keydown'];\r\n    const state = { items, url: location.href, cancelled: false, timer: 0, frame: 0, cancel() {\r\n        state.cancelled = true;\r\n        clearTimeout(state.timer);\r\n        cancelAnimationFrame(state.frame);\r\n        for (const name of events) document.removeEventListener(name, state.cancel, true);\r\n        if (window[key] === state) delete window[key];\r\n    }};\r\n    for (const name of events) document.addEventListener(name, state.cancel, true);\r\n    state.timer = setTimeout(state.cancel, 2000);\r\n    window[key] = state;\r\n    return true;\r\n})();");
			if (await Task.WhenAny(capture, Task.Delay(250)) != capture)
			{
				capture.ContinueWith(delegate(Task<string> task)
				{
					_ = task.Exception;
				}, TaskContinuationOptions.OnlyOnFaulted);
			}
			else
			{
				await capture;
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent host scroll preservation failed: " + ex.Message);
		}
	}

	internal void SuspendBrowserPresentationForHostLayout()
	{
		_isBrowserPresentationSuspendedForHostLayout = true;
		ApplyBrowserPresentation();
	}

	internal void ResumeBrowserPresentationAfterHostLayout()
	{
		_isBrowserPresentationSuspendedForHostLayout = false;
		ApplyBrowserPresentation();
	}

	private void RequestBrowserPresentation(bool showFrame, bool enableInteraction)
	{
		_isBrowserFrameRequested = showFrame;
		_isBrowserInteractionRequested = enableInteraction;
		ApplyBrowserPresentation();
	}

	internal void SetNativeHomePresented(bool isPresented)
	{
		_isNativeHomePresented = isPresented;
		ApplyBrowserPresentation();
	}

	private void ApplyBrowserPresentation()
	{
		bool flag = _isBrowserFrameRequested && !_isBrowserPresentationSuspendedForHostSwitch && !_isBrowserPresentationSuspendedForTheme && !_isBrowserPresentationSuspendedForHostLayout;
		bool flag2 = _browser?.View is HwndHost;
		bool flag3 = _isBrowserInitialized && HasPresentedBrowserContent;
		bool flag4 = flag2 && !flag3 && (_isNativeHomePresented || !flag);
		BrowserHost.Width = (flag4 ? 1.0 : double.NaN);
		BrowserHost.Height = (flag4 ? 1.0 : double.NaN);
		BrowserHost.HorizontalAlignment = ((!flag4) ? HorizontalAlignment.Stretch : HorizontalAlignment.Left);
		BrowserHost.VerticalAlignment = ((!flag4) ? VerticalAlignment.Stretch : VerticalAlignment.Top);
		BrowserHost.Visibility = ((flag2 && _isNativeHomePresented && flag3) ? Visibility.Hidden : Visibility.Visible);
		BrowserHost.Opacity = (flag ? 1.0 : 0.0);
		BrowserHost.IsHitTestVisible = flag && _isBrowserInteractionRequested;
	}

	private void SetShouldPresentStatusPanel(bool shouldPresent)
	{
		if (_shouldPresentStatusPanel != shouldPresent)
		{
			_shouldPresentStatusPanel = shouldPresent;
			this.StatusPresentationChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private async void OnRetry(object sender, MouseButtonEventArgs e)
	{
		await RecoverBrowserAsync();
	}

	private async Task RecoverBrowserAsync()
	{
		if (!_lifecycle.CanRetry || _initializationTask != null || _isRecoveryRunning)
		{
			return;
		}
		_isRecoveryRunning = true;
		try
		{
			await DisposeBrowserAsync();
			await EnsureInitializedAsync();
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent WebView recovery failed: " + ExceptionUtils.BuildExceptionMessage(ex));
			ShowError();
		}
		finally
		{
			_isRecoveryRunning = false;
		}
	}

	private async Task DisposeBrowserAsync(IEmbeddedBrowser expectedBrowser = null)
	{
		IEmbeddedBrowser browser = _browser;
		if (browser == null || (expectedBrowser != null && browser != expectedBrowser))
		{
			return;
		}
		_browser = null;
		_isBrowserInitialized = false;
		_isWebPageFallbackPresented = false;
		_bridgeVerificationVersion++;
		_isAgentBridgeVerificationRunning = false;
		_isBrowserPresentationSuspendedForTheme = false;
		HasPresentedBrowserContent = false;
		_fontStyleUpdateVersion++;
		_fontStyleScriptId = null;
		_fontStyleScript = null;
		_hasFontFolderMapping = false;
		AgentDsBridgeHost agentBridge = _agentBridge;
		_agentBridge = null;
		if (agentBridge != null)
		{
			agentBridge.WebReady -= OnAgentBridgeWebReady;
			agentBridge.Dispose();
		}
		SetAgentBridgeReady(isReady: false);
		browser.NavigationCompleted -= OnNavigationCompleted;
		browser.NavigationStarting -= OnNavigationStarting;
		browser.NewWindowRequested -= OnNewWindowRequested;
		browser.ProcessFailed -= OnProcessFailed;
		BrowserHost.Content = null;
		using (await EmbeddedBrowserLifecycle.EnterAsync(browser.Engine))
		{
			try
			{
				EmbeddedBrowserAuthenticationCookieLease authenticationCookieLease = _authenticationCookieLease;
				_authenticationCookieLease = null;
				if (authenticationCookieLease != null)
				{
					await authenticationCookieLease.ReleaseAsync();
				}
			}
			catch (Exception ex)
			{
				UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			}
			await browser.DisposeAsync();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/ai/aiwebappview.xaml", UriKind.Relative);
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
			BrowserHost = (ContentControl)target;
			break;
		case 2:
			StatusPanel = (Grid)target;
			StatusPanel.MouseLeftButtonUp += OnRetry;
			break;
		case 3:
			LoadingIndicator = (LoadingIndicator)target;
			break;
		case 4:
			StatusText = (TextBlock)target;
			break;
		case 5:
			RuntimeProgress = (ProgressBar)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
