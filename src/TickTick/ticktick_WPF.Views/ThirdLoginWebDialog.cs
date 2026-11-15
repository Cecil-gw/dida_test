using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Framework.EmbeddedBrowser;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Network;
using ticktick_WPF.Util.Twitter;

namespace ticktick_WPF.Views;

public class ThirdLoginWebDialog : Window, IComponentConnector
{
	private string FBClient_id = "687713684576416";

	private string FBRedirect_uri = "https://ticktick.com/sign/facebook";

	private string FBUrl = "https://www.facebook.com/dialog/oauth?client_id={0}&redirect_uri={1}&state=THc&response_type=token&scope=email";

	private string GOOGLE_ACCESS_TOKEN_TAG = "GOOGLE_ACCESS_TOKEN_TAG_";

	private string GOOGLE_REFRESH_TOKEN_TAG = "GOOGLE_REFRESH_TOKEN_TAG_";

	private string GRANT_TYPE = "authorization_code";

	private string GRANT_TYPE_REFRESH = "refresh_token";

	private string OAUTH_SCOPE = "https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile";

	private string OAUTH_URL = "https://accounts.google.com/o/oauth2/auth";

	private string qqClient_id = "101139917";

	private string qqRedirect_uri = "https://dida365.com/sign/qq";

	private string qqUrl = "https://graph.qq.com/oauth2.0/show?which=Login&display=pc&client_id={0}&response_type=token&redirect_uri={1}&state=Lw";

	private string REDIRECT_URI = "http://localhost";

	private string REQUEST_EMIAL_ADDRESS_BASE_URL = "https://www.googleapis.com/oauth2/v1/userinfo?alt=json&access_token=";

	private string TOKEN_URL = "https://accounts.google.com/o/oauth2/token";

	private string TwitterCallBackUri = "oob";

	private string TwitterConsumerKey = "cQVZO67miyWT5xDhOtcuOVUYH";

	private string TwitterConsumerSecret = "rI6OxEvpGy0XfjTw995uOgagQHhQ4Nk0vqdk8IOHp5pL2QjppK";

	private string WeiBoClient_id = "18974718";

	private string WeiBoRedirect_uri = "https://www.dida365.com/sign/weibo";

	private string WeiBoUrl = "https://api.weibo.com/oauth2/authorize?client_id={0}&response_type=code&redirect_uri={1}";

	private string WXAppid = "wxf1429a73d311aad4";

	private string WXRedirect_uri = "https://dida365.com/sign/wechat";

	private string WXUrl = "https://open.weixin.qq.com/connect/qrconnect?appid={0}&redirect_uri={1}&response_type=code&scope=snsapi_login";

	private readonly LoginDialog _loginDialog;

	private readonly string _mode;

	private IEmbeddedBrowser _browser;

	private bool _closed;

	internal Grid webGrid;

	internal ContentControl BrowserHost;

	internal ProgressBar loginProgressBar;

	internal StackPanel RuntimeInstallPanel;

	internal TextBlock RuntimeStatusText;

	internal ProgressBar RuntimeDownloadProgress;

	internal TextBlock LoginErrorText;

	internal Grid PinGrid;

	internal TextBox pinTextBox;

	internal Button PinLoginButton;

	internal StackPanel resultGrid;

	internal ProgressBar resultProgressBar;

	internal Image logoImage;

	private bool _contentLoaded;

	public ThirdLoginWebDialog()
	{
		InitializeComponent();
	}

	public ThirdLoginWebDialog(string mode, LoginDialog loginDialog)
	{
		InitializeComponent();
		_loginDialog = loginDialog;
		_mode = mode;
		logoImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/logo_dida.png"));
	}

	private void OnDialogLoaded(object sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrEmpty(_mode) && _browser == null)
		{
			InitializeBrowserAsync();
		}
	}

	private async Task InitializeBrowserAsync()
	{
		_ = 3;
		try
		{
			bool shouldStartTwitterLogin = false;
			EmbeddedBrowserOptions options = new EmbeddedBrowserOptions
			{
				UseCase = "third_login_" + _mode,
				OpenNewWindowsExternally = false,
				EnableHostObjects = false
			};
			IEmbeddedBrowser browser = (_browser = EmbeddedBrowserFactory.Create(options));
			browser.NavigationCompleted += OnBrowserLoaded;
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
							ShowLoginInitializationError();
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
				await ClearLoginCookiesAsync(browser);
				if (!IsBrowserActive(browser))
				{
					return;
				}
				switch (_mode)
				{
				case "login_Facebook":
					browser.NavigationStarting += FB_LoginWebBrowser_Navigating;
					browser.Navigate(new Uri(string.Format(FBUrl, FBClient_id, FBRedirect_uri)));
					LocalSettings.Settings.AccountType = "facebook";
					break;
				case "login_Twitter":
					browser.NavigationStarting += TW_LoginWebBrowser_Navigating;
					shouldStartTwitterLogin = true;
					break;
				case "login_Weibo":
					browser.NavigationStarting += WeiBo_LoginWebBrowser_Navigating;
					browser.Navigate(new Uri(string.Format(WeiBoUrl, WeiBoClient_id, WeiBoRedirect_uri)));
					LocalSettings.Settings.AccountType = "weibo";
					base.Topmost = false;
					break;
				case "login_QQ":
					browser.NavigationStarting += QQ_LoginWebBrowser_Navigating;
					browser.Navigate(new Uri(string.Format(qqUrl, qqClient_id, qqRedirect_uri)));
					LocalSettings.Settings.AccountType = "tecent";
					break;
				case "login_Wechat":
					browser.NavigationStarting += WX_LoginWebBrowser_Navigating;
					browser.Navigate(new Uri(string.Format(WXUrl, WXAppid, WXRedirect_uri)));
					LocalSettings.Settings.AccountType = "wechat";
					break;
				}
			}
			if (shouldStartTwitterLogin)
			{
				await LoginTwitterAsync(browser);
				if (IsBrowserActive(browser))
				{
					LocalSettings.Settings.AccountType = "twitter";
				}
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
				ShowLoginInitializationError();
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
			loginProgressBar.Visibility = Visibility.Collapsed;
			RuntimeInstallPanel.Visibility = Visibility.Visible;
			RuntimeStatusText.Text = progress.Message;
			RuntimeDownloadProgress.IsIndeterminate = progress.IsInstalling || !progress.Percentage.HasValue;
			if (progress.Percentage.HasValue)
			{
				RuntimeDownloadProgress.Value = progress.Percentage.Value;
			}
		}
	}

	private async Task ClearLoginCookiesAsync(IEmbeddedBrowser browser)
	{
		foreach (Uri loginCookieDomain in GetLoginCookieDomains(_mode))
		{
			if (!IsBrowserActive(browser))
			{
				return;
			}
			await browser.DeleteAllCookiesAsync(loginCookieDomain);
		}
	}

	internal static IReadOnlyList<Uri> GetLoginCookieDomains(string mode)
	{
		return mode switch
		{
			"login_Facebook" => new Uri[1]
			{
				new Uri("https://www.facebook.com/dialog/oauth")
			}, 
			"login_Twitter" => new Uri[2]
			{
				new Uri("https://www.twitter.com"),
				new Uri("https://api.twitter.com/oauth/authorize")
			}, 
			"login_Weibo" => new Uri[1]
			{
				new Uri("https://api.weibo.com/oauth2/authorize")
			}, 
			"login_QQ" => new Uri[1]
			{
				new Uri("https://graph.qq.com/oauth2.0/show")
			}, 
			"login_Wechat" => new Uri[1]
			{
				new Uri("https://open.weixin.qq.com/connect/qrconnect")
			}, 
			_ => Array.Empty<Uri>(), 
		};
	}

	private async void OnBrowserLoaded(object sender, EmbeddedBrowserNavigationCompletedEventArgs e)
	{
		if (_closed)
		{
			return;
		}
		if (!e.IsSuccess)
		{
			ShowLoginInitializationError();
			return;
		}
		loginProgressBar.Visibility = Visibility.Collapsed;
		await Task.Delay(20);
		if (!_closed)
		{
			base.Width -= 1.0;
			await Task.Delay(20);
			if (!_closed)
			{
				base.Width += 1.0;
			}
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
			ShowLoginInitializationError();
		}
	}

	private void ShowLoginInitializationError()
	{
		BrowserHost.Visibility = Visibility.Collapsed;
		RuntimeInstallPanel.Visibility = Visibility.Collapsed;
		loginProgressBar.Visibility = Visibility.Collapsed;
		PinGrid.Visibility = Visibility.Collapsed;
		LoginErrorText.Visibility = Visibility.Visible;
	}

	private async Task LoginTwitterAsync(IEmbeddedBrowser browser)
	{
		Uri uri = new Uri("https://api.twitter.com/oauth/authorize?oauth_token=" + await new TwitterClient(new OAuthInfo
		{
			ConsumerKey = TwitterConsumerKey,
			ConsumerSecret = TwitterConsumerSecret
		}).GetTwitterRequestTokenAsync(TwitterCallBackUri));
		if (IsBrowserActive(browser))
		{
			browser.Navigate(uri);
		}
	}

	private void TW_LoginWebBrowser_Navigating(object sender, EmbeddedBrowserNavigationStartingEventArgs e)
	{
		loginProgressBar.Visibility = Visibility.Visible;
		if (IsTwitterPinPage(e.Uri))
		{
			PinGrid.Visibility = Visibility.Visible;
		}
	}

	private void pinTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (pinTextBox != null && PinLoginButton != null)
		{
			if (pinTextBox.Text.Length != 0)
			{
				PinLoginButton.IsEnabled = true;
			}
			else
			{
				PinLoginButton.IsEnabled = false;
			}
		}
	}

	private async void PinLoginButton_Click(object sender, RoutedEventArgs e)
	{
		if (pinTextBox.Text.Length == 0)
		{
			return;
		}
		loginProgressBar.Visibility = Visibility.Visible;
		string auth = $"Authorization: OAuth realm=\"\",oauth_consumer_key=\"{TwitterConsumerKey}\",oauth_nonce=\"{BaseRequest.oauth_nonce}\",oauth_signature_method=\"HMAC - SHA1\",oauth_timestamp=\"{BaseRequest.oauth_timestamp}\",oauth_token=\"{BaseRequest.request_token}\",oauth_verifier=\"{pinTextBox.Text}\",oauth_version=\"1.0\",oauth_signature=\"{BaseRequest.signature}\"";
		string text = await Network.GetHttpWebRequest("https://api.twitter.com/oauth/access_token", "", null, auth, "POST", fulluri: true, isNeedErrorReturn: true);
		if (text.Contains("oauth_token_secret"))
		{
			string access_token = text.Substring(text.IndexOf("oauth_token") + 12, text.IndexOf("&", text.IndexOf("oauth_token")) - (text.IndexOf("oauth_token") + 12));
			string accessTokenSecret = text.Substring(text.IndexOf("oauth_token_secret") + 19, text.IndexOf("&", text.IndexOf("oauth_token_secret")) - (text.IndexOf("oauth_token_secret") + 19));
			loginProgressBar.Visibility = Visibility.Collapsed;
			webGrid.Visibility = Visibility.Collapsed;
			resultGrid.Visibility = Visibility.Visible;
			RecordOAuthCallback("twitter");
			ThirdLoginSuccess("login_Twitter", "", "", access_token, "", accessTokenSecret);
			return;
		}
		pinTextBox.Text = "";
		CustomerDialog customerDialog = new CustomerDialog(Application.Current?.FindResource("LoginFailed").ToString(), Application.Current?.FindResource("VerificationFailedHaveTry").ToString(), MessageBoxButton.OK);
		customerDialog.Owner = Window.GetWindow(this);
		customerDialog.ShowDialog();
		IEmbeddedBrowser browser = _browser;
		if (browser != null && !_closed)
		{
			await LoginTwitterAsync(browser);
		}
	}

	private void FB_LoginWebBrowser_Navigating(object sender, EmbeddedBrowserNavigationStartingEventArgs e)
	{
		string oAuthParameter = GetOAuthParameter(e.Uri, "access_token");
		if (IsOAuthCallback(e.Uri, FBRedirect_uri) && !string.IsNullOrEmpty(oAuthParameter))
		{
			UtilLog.Info("Facebook OAuth callback received.");
			_browser.NavigationStarting -= FB_LoginWebBrowser_Navigating;
			e.Cancel = true;
			_browser.Stop();
			webGrid.Visibility = Visibility.Collapsed;
			resultGrid.Visibility = Visibility.Visible;
			RecordOAuthCallback("facebook");
			ThirdLoginSuccess("login_Facebook", "", "", oAuthParameter);
		}
	}

	private void WeiBo_LoginWebBrowser_Navigating(object sender, EmbeddedBrowserNavigationStartingEventArgs e)
	{
		string oAuthParameter = GetOAuthParameter(e.Uri, "code");
		if (IsOAuthCallback(e.Uri, WeiBoRedirect_uri) && !string.IsNullOrEmpty(oAuthParameter))
		{
			_browser.NavigationStarting -= WeiBo_LoginWebBrowser_Navigating;
			e.Cancel = true;
			_browser.Stop();
			webGrid.Visibility = Visibility.Collapsed;
			resultGrid.Visibility = Visibility.Visible;
			RecordOAuthCallback("weibo");
			ThirdLoginSuccess("login_Weibo", oAuthParameter);
		}
	}

	private async void QQ_LoginWebBrowser_Navigating(object sender, EmbeddedBrowserNavigationStartingEventArgs e)
	{
		string accessToken = GetOAuthParameter(e.Uri, "access_token");
		if (IsOAuthCallback(e.Uri, qqRedirect_uri) && !string.IsNullOrEmpty(accessToken))
		{
			_browser.NavigationStarting -= QQ_LoginWebBrowser_Navigating;
			e.Cancel = true;
			_browser.Stop();
			webGrid.Visibility = Visibility.Collapsed;
			resultGrid.Visibility = Visibility.Visible;
			RecordOAuthCallback("qq");
			ThirdLoginSuccess("login_QQ", "", "qq.com", accessToken, await Communicator.GetTencentOpenId(accessToken));
		}
	}

	private void WX_LoginWebBrowser_Navigating(object sender, EmbeddedBrowserNavigationStartingEventArgs e)
	{
		string oAuthParameter = GetOAuthParameter(e.Uri, "code");
		if (IsOAuthCallback(e.Uri, WXRedirect_uri) && !string.IsNullOrEmpty(oAuthParameter))
		{
			_browser.NavigationStarting -= WX_LoginWebBrowser_Navigating;
			e.Cancel = true;
			_browser.Stop();
			RecordOAuthCallback("wechat");
			ThirdLoginSuccess("login_Wechat", oAuthParameter);
		}
	}

	private void RecordOAuthCallback(string provider)
	{
		IEmbeddedBrowser browser = _browser;
		if (!_closed && browser != null)
		{
			EmbeddedBrowserTelemetry.Record("third_login_" + _mode, browser.Engine, browser.RuntimeVersion, "callback_success_" + provider, null, null);
		}
	}

	private async void ThirdLoginSuccess(string mode, string code = "", string site = "", string access_token = "", string openId = "", string accessTokenSecret = "")
	{
		Close();
		UserModel user = new UserModel();
		switch (mode)
		{
		case "login_Google":
			user = await Communicator.ThirdSignon(mode, "", site, access_token, openId);
			break;
		case "login_Facebook":
			user = await Communicator.ThirdSignon(mode, "", "", access_token);
			break;
		case "login_Twitter":
			user = await Communicator.ThirdSignon(mode, "", "", access_token, "", accessTokenSecret);
			break;
		case "login_Weibo":
			user = await Communicator.ThirdSignon(mode, code);
			break;
		case "login_QQ":
			user = await Communicator.ThirdSignon(mode, "", site, access_token, openId);
			break;
		case "login_Wechat":
			user = await Communicator.ThirdSignon(mode, code);
			break;
		}
		if (!string.IsNullOrEmpty(user?.authId))
		{
			_loginDialog.BeginTwoFactorVerify(user);
			return;
		}
		if (!string.IsNullOrEmpty(user?.userId))
		{
			bool isNew = await UserDao.UpdateOrInsertUserModelListDbAsync(user);
			await LocalSettings.ResetUserSettings(user, null);
			await UserManager.PullUserInfo();
			await UserActCollectUtils.OnDeviceDataChanged();
			UserManager.OnUserDbChange(user, isNew);
			JumpHelper.InitJumpList();
			JumpHelper.InitChangeEvents();
			bool flag = await Communicator.IsNewUser();
			if (flag)
			{
				LocalSettings.Settings.NeedShowTutorial = true;
				LocalSettings.Settings.ExtraSettings.ShowProjectTimes = 3;
			}
			_loginDialog?.Hide();
			if (LoginDialog.FirstDownload)
			{
				UserActCollectUtils.AddClickEvent("register&login", "for_new_download", flag ? "register" : "login");
			}
			await App.Instance.ShowMainWindow();
			_loginDialog?.Close();
			return;
		}
		try
		{
			ErrorModel error = JsonConvert.DeserializeObject<ErrorModel>(user?.username ?? "");
			if (!(error.errorCode == "need_upgrade_client"))
			{
				return;
			}
			UpdateModel updateModel = await UpdateHelper.CheckUpdate();
			if (updateModel != null)
			{
				string forceText = null;
				if (error.errorMessage == "mfa")
				{
					forceText = Utils.GetString("TwoFactorAuthenticationErrorMsg");
				}
				MainWindow.ShowNewVersionWindow(updateModel, force: true, base.Owner, forceText);
			}
		}
		catch
		{
			UtilLog.Info(user?.username);
		}
	}

	private void Restart()
	{
		string location = Assembly.GetExecutingAssembly().Location;
		base.Dispatcher.Invoke((ThreadStart)delegate
		{
			Application.Current?.Shutdown();
		});
		MessageBox.Show("");
		Process.Start(location);
	}

	internal static bool IsOAuthCallback(Uri uri, string expectedUrl)
	{
		if (uri == null || !Uri.TryCreate(expectedUrl, UriKind.Absolute, out var result))
		{
			return false;
		}
		if (string.Equals(uri.Scheme, result.Scheme, StringComparison.OrdinalIgnoreCase) && string.Equals(uri.Host, result.Host, StringComparison.OrdinalIgnoreCase) && uri.Port == result.Port)
		{
			return string.Equals(uri.AbsolutePath.TrimEnd('/'), result.AbsolutePath.TrimEnd('/'), StringComparison.Ordinal);
		}
		return false;
	}

	internal static bool IsTwitterPinPage(Uri uri)
	{
		if (IsOAuthCallback(uri, "https://api.twitter.com/oauth/authorize") && string.IsNullOrEmpty(uri.Query))
		{
			return string.IsNullOrEmpty(uri.Fragment);
		}
		return false;
	}

	internal static string GetOAuthParameter(Uri uri, string name)
	{
		if (uri == null)
		{
			return null;
		}
		string text = HttpUtility.ParseQueryString(uri.Query.TrimStart('?'))[name];
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return HttpUtility.ParseQueryString(uri.Fragment.TrimStart('#'))[name];
	}

	private async void OnDialogClosed(object sender, EventArgs e)
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

	private void OnLoginFailClick(object sender, MouseButtonEventArgs e)
	{
		Utils.TryProcessStartUrl("https://help.dida365.com/forum/topic/8276/pc-%E7%AC%AC%E4%B8%89%E6%96%B9%E7%99%BB%E9%99%86%E5%A4%B1%E8%B4%A5%E8%A7%A3%E5%86%B3%E5%8A%9E%E6%B3%95");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/thirdloginwebdialog.xaml", UriKind.Relative);
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
			((ThirdLoginWebDialog)target).Loaded += OnDialogLoaded;
			((ThirdLoginWebDialog)target).Closed += OnDialogClosed;
			break;
		case 2:
			webGrid = (Grid)target;
			break;
		case 3:
			BrowserHost = (ContentControl)target;
			break;
		case 4:
			loginProgressBar = (ProgressBar)target;
			break;
		case 5:
			RuntimeInstallPanel = (StackPanel)target;
			break;
		case 6:
			RuntimeStatusText = (TextBlock)target;
			break;
		case 7:
			RuntimeDownloadProgress = (ProgressBar)target;
			break;
		case 8:
			LoginErrorText = (TextBlock)target;
			LoginErrorText.MouseLeftButtonUp += OnLoginFailClick;
			break;
		case 9:
			PinGrid = (Grid)target;
			break;
		case 10:
			pinTextBox = (TextBox)target;
			pinTextBox.TextChanged += pinTextBox_TextChanged;
			break;
		case 11:
			PinLoginButton = (Button)target;
			PinLoginButton.Click += PinLoginButton_Click;
			break;
		case 12:
			resultGrid = (StackPanel)target;
			break;
		case 13:
			resultProgressBar = (ProgressBar)target;
			break;
		case 14:
			logoImage = (Image)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
