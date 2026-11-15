using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Newtonsoft.Json;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views;

public class LoginDialog : Window, IComponentConnector
{
	private bool isLogging;

	private bool _isSignIn = true;

	private string _captchaKey;

	private string _authId;

	private long _expireTime;

	private string _mfaType;

	private string _mfaName;

	private bool _verifyMfa;

	private string _mfaOid;

	private MFAModel _mfaModel;

	private bool _inSignInView;

	internal Grid Container;

	internal CarouselControl Carousel;

	internal StackPanel ReturnGrid;

	internal StackPanel InputGrid;

	internal TextBlock SignTitle;

	internal TextBlock VerifyMessage;

	internal Grid AccountGrid;

	internal TextBox emailTextBlock;

	internal TextBlock AccountRemainderTimesText;

	internal PasswordBox passwordPasswordBox;

	internal TextBlock PasswordRemainderTimesText;

	internal Grid CaptchaGrid;

	internal TextBox CaptchaTextBlock;

	internal Image Captcha;

	internal TextBlock CaptchaErrorText;

	internal Grid VerifyGrid;

	internal TextBox VerifyCodeTextBlock;

	internal TextBlock SendVerifyText;

	internal TextBlock VerifyCodeErrorText;

	internal Grid OkButton;

	internal LoadingIndicator SignInIndicator;

	internal TextBlock OkText;

	internal TextBlock SwitchAnotherWayText;

	internal Grid OtherWayGrid;

	internal TextBlock AnotherWayText;

	internal Grid OtherWayContainer;

	internal ContentControl TwoStepPhone;

	internal ContentControl TwoStepMail;

	internal ContentControl TwoStepApp;

	internal ContentControl TwoStepBackup;

	internal Border ForgetAndSignupGrid;

	internal StackPanel AgreePanel;

	internal CheckBox AgreeCheckBox;

	internal TextBlock AgreeText;

	internal StackPanel AgreePopup;

	internal TextBlock SwitchSignUpText;

	internal TextBlock SwitchSignInText;

	internal StackPanel homeGrid;

	internal Image DidaLogoImage;

	internal TextBlock WelcomeTitle;

	internal Button WeChatLogin;

	internal Button GoogleLogin;

	internal Button AccountLogin;

	internal Button EmailLogin;

	internal Grid MoreLoginType;

	internal Grid ThirdLoginGrid;

	internal Grid ThirdLoginEnGrid;

	internal Grid FacebookGrid;

	internal Grid AppleEnGrid;

	internal Grid ThirdLoginZnGrid;

	internal Grid AppleCNGrid;

	internal Grid WeiboGrid;

	internal Grid QQGrid;

	internal Border ProxySetting;

	private bool _contentLoaded;

	private static Dictionary<string, DateTime> sendMfaTimeDict { get; set; } = new Dictionary<string, DateTime>();

	public static bool FirstDownload { get; set; }

	public LoginDialog()
	{
		InitializeComponent();
		TickDbHelper.CreateDbAsync(logFirst: false);
		base.Title = Utils.GetAppName();
		UpdateMainView();
		Carousel.InitData(CarouselItemViewModel.GetLoginCarouselModels());
		InitApi();
		InitAgreeText();
		if (FirstDownload)
		{
			UserActCollectUtils.AddClickEvent("register&login", "for_new_download", "page_show");
			base.Closed += delegate
			{
				FirstDownload = false;
			};
		}
		Carousel.SlideButton.Background = new SolidColorBrush(Color.FromRgb(71, 114, 250));
	}

	private void UpdateMainView()
	{
		_verifyMfa = false;
		_inSignInView = false;
		WelcomeTitle.Text = string.Format(Utils.GetString("WelcomeToTickTick"), Utils.GetAppName());
		OtherWayGrid.Visibility = Visibility.Collapsed;
		SwitchAnotherWayText.Visibility = Visibility.Collapsed;
		VerifyMessage.Visibility = Visibility.Collapsed;
		InputGrid.Visibility = Visibility.Collapsed;
		homeGrid.Visibility = Visibility.Visible;
		WeChatLogin.Visibility = Visibility.Visible;
		ThirdLoginZnGrid.Visibility = Visibility.Visible;
		GoogleLogin.Visibility = Visibility.Collapsed;
		ThirdLoginEnGrid.Visibility = Visibility.Collapsed;
		emailTextBlock.Tag = Utils.GetString("PhoneNumberAndEmail");
	}

	private void InitAgreeText()
	{
		string title = Utils.GetString(Utils.IsDida() ? "AgreePrivacyPolicy" : "TickAgreePrivacyPolicy");
		AgreeText.Inlines.Clear();
		foreach (string item in GetRunTextList(title))
		{
			if (!(item == "{0}"))
			{
				if (item == "{1}")
				{
					Run run = new Run
					{
						Cursor = Cursors.Hand
					};
					run.Text = Utils.GetString("PrivacyPolicy");
					run.Background = Brushes.Transparent;
					run.TextDecorations = TextDecorations.Underline;
					run.MouseLeftButtonUp += GotoPolicy;
					AgreeText.Inlines.Add(run);
				}
				else
				{
					AgreeText.Inlines.Add(new Run(item));
				}
			}
			else
			{
				Run run2 = new Run
				{
					Cursor = Cursors.Hand
				};
				run2.Text = Utils.GetString("TermsofService");
				run2.Background = Brushes.Transparent;
				run2.TextDecorations = TextDecorations.Underline;
				run2.MouseLeftButtonUp += GotoTos;
				AgreeText.Inlines.Add(run2);
			}
		}
		static List<string> GetRunTextList(string text)
		{
			List<string> list = new List<string>();
			if (text.Contains("{"))
			{
				int num = text.IndexOf("{", StringComparison.Ordinal);
				list.Add(text.Substring(0, num));
				list.Add(text.Substring(num, 3));
				list.AddRange(GetRunTextList(text.Substring(num + 3, text.Length - num - 3)));
			}
			else
			{
				list.Add(text);
			}
			return list;
		}
	}

	private void GotoTos(object sender, MouseButtonEventArgs e)
	{
		Utils.TryProcessStartUrl("https://dida365.com/tos");
	}

	private void GotoPolicy(object sender, MouseButtonEventArgs e)
	{
		Utils.TryProcessStartUrl("https://dida365.com/privacy");
	}

	private void InitApi()
	{
	}

	private void InputKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return && !HandleEnterKeyDown(sender))
		{
			OkButton_Click(null, null);
		}
	}

	private bool HandleEnterKeyDown(object sender)
	{
		if (sender.Equals(emailTextBlock))
		{
			bool num = Utils.IsDida() && Utils.IsMobilePhone(emailTextBlock.Text.Trim());
			bool flag = Utils.IsValidEmail(emailTextBlock.Text.Trim());
			if (!num && !flag)
			{
				AccountRemainderTimesText.Text = Utils.GetString("InvalidInput");
				return true;
			}
			if (string.IsNullOrEmpty(emailTextBlock.Text))
			{
				return true;
			}
			if (string.IsNullOrEmpty(passwordPasswordBox.Password))
			{
				FocusBox(passwordPasswordBox);
				return true;
			}
		}
		if (sender.Equals(emailTextBlock) || sender.Equals(passwordPasswordBox))
		{
			if (string.IsNullOrEmpty(passwordPasswordBox.Password))
			{
				return true;
			}
			if (sender.Equals(passwordPasswordBox) && (passwordPasswordBox.Password.Length > 64 || passwordPasswordBox.Password.Length < 6))
			{
				PasswordRemainderTimesText.Text = Utils.GetString("InvalidatePasswordLength");
				return true;
			}
			if (CaptchaTextBlock.IsVisible && string.IsNullOrEmpty(CaptchaTextBlock.Text))
			{
				FocusBox(CaptchaTextBlock);
				return true;
			}
		}
		if (VerifyCodeTextBlock.IsVisible && string.IsNullOrEmpty(VerifyCodeTextBlock.Text))
		{
			FocusBox(VerifyCodeTextBlock);
			return true;
		}
		return false;
	}

	private async Task signUp()
	{
		if (isLogging)
		{
			return;
		}
		if (VerifyGrid.Visibility == Visibility.Visible && VerifyCodeTextBlock.Text.Length < 1)
		{
			VerifyCodeErrorText.Text = Utils.GetString("VerificationCodeNotInput");
			FocusBox(VerifyCodeTextBlock);
			return;
		}
		if (passwordPasswordBox.Password.Length > 64 || passwordPasswordBox.Password.Length < 6)
		{
			PasswordRemainderTimesText.Text = Utils.GetString("InvalidatePasswordLength");
			FocusBox(passwordPasswordBox);
			return;
		}
		if (AgreeCheckBox.Visibility == Visibility.Visible && AgreeCheckBox.IsChecked != true)
		{
			AgreePopup.Visibility = Visibility.Visible;
			return;
		}
		isLogging = true;
		SignInIndicator.Visibility = Visibility.Visible;
		var (userModel, text) = await LoginManager.SignUp(null, emailTextBlock.Text, VerifyCodeTextBlock.Text, passwordPasswordBox.Password, VerifyGrid.Visibility == Visibility.Visible, CaptchaGrid.IsVisible ? _captchaKey : "", CaptchaGrid.IsVisible ? CaptchaTextBlock.Text : "");
		if (userModel != null)
		{
			Hide();
			LocalSettings.Settings.NeedShowTutorial = true;
			LocalSettings.Settings.ExtraSettings.ShowProjectTimes = 3;
			LocalSettings.Settings.LastLoginAccount = emailTextBlock.Text.Trim();
			if (FirstDownload)
			{
				UserActCollectUtils.AddClickEvent("register&login", "for_new_download", "register");
			}
			await App.Instance.ShowMainWindow();
			Close();
			return;
		}
		if (!string.IsNullOrEmpty(text))
		{
			switch (text)
			{
			case "invalidate_email":
				if (!AccountGrid.IsVisible)
				{
					SwitchSignUp();
				}
				AccountRemainderTimesText.Text = Utils.GetString("InvalidInput");
				FocusBox(emailTextBlock);
				break;
			case "invalidate_password_length":
				if (!AccountGrid.IsVisible)
				{
					SwitchSignUp();
				}
				PasswordRemainderTimesText.Text = Utils.GetString("InvalidatePasswordLength");
				FocusBox(passwordPasswordBox);
				break;
			case "username_exist":
				if (!AccountGrid.IsVisible)
				{
					SwitchSignUp();
				}
				AccountRemainderTimesText.Text = Utils.GetString("UsernameExist");
				FocusBox(emailTextBlock);
				break;
			case "user_verify_sms_failed":
				if (!AccountGrid.IsVisible)
				{
					SwitchSignUp();
				}
				VerifyCodeErrorText.Text = Utils.GetString("WrongVerificationCode");
				FocusBox(VerifyCodeTextBlock);
				break;
			case "need_upgrade_client":
			{
				UpdateModel updateModel = await UpdateHelper.CheckUpdate();
				if (updateModel == null)
				{
					return;
				}
				MainWindow.ShowNewVersionWindow(updateModel, force: true, this);
				break;
			}
			case "need_recaptcha":
			case "captcha_wrong":
				ShowCaptcha(text);
				SetOkButtonEnable();
				break;
			}
		}
		SignInIndicator.Visibility = Visibility.Collapsed;
		isLogging = false;
	}

	private void FocusBox(IInputElement textBox)
	{
		textBox.Focus();
		Keyboard.Focus(textBox);
	}

	private void ShowCaptcha(string errorCode)
	{
		SignTitle.Margin = new Thickness(0.0, 130.0, 0.0, 0.0);
		bool num = "captcha_wrong" == errorCode && !AccountGrid.IsVisible;
		SignTitle.Text = Utils.GetString("SafetyVerification");
		OkText.Text = Utils.GetString("NextStep1");
		AccountGrid.Visibility = Visibility.Collapsed;
		passwordPasswordBox.Visibility = Visibility.Collapsed;
		VerifyGrid.Visibility = Visibility.Collapsed;
		CaptchaGrid.Visibility = Visibility.Visible;
		ForgetAndSignupGrid.Visibility = Visibility.Collapsed;
		SwitchSignUpText.Visibility = Visibility.Collapsed;
		SwitchSignInText.Visibility = Visibility.Collapsed;
		AgreePanel.Visibility = Visibility.Collapsed;
		GetCaptcha();
		if (num)
		{
			CaptchaErrorText.Text = Utils.GetString("WrongCaptchaCode");
			FocusBox(CaptchaTextBlock);
		}
		else
		{
			CaptchaTextBlock.Text = "";
			FocusBox(CaptchaTextBlock);
		}
	}

	private async Task Login()
	{
		if (!isLogging)
		{
			isLogging = true;
			SignInIndicator.Visibility = Visibility.Visible;
			var (userModel, apiEx) = await LoginManager.Login(emailTextBlock.Text, passwordPasswordBox.Password, CaptchaGrid.IsVisible ? _captchaKey : "", CaptchaGrid.IsVisible ? CaptchaTextBlock.Text : "");
			SignInIndicator.Visibility = Visibility.Collapsed;
			isLogging = false;
			if (userModel != null && userModel.authId != null)
			{
				BeginTwoFactorVerify(userModel);
			}
			else
			{
				HandleLoginResult(userModel, apiEx);
			}
		}
	}

	public void BeginTwoFactorVerify(UserModel user)
	{
		_authId = user.authId;
		_expireTime = user.expireTime;
		ShowTwoFactorView();
	}

	private async Task HandleLoginResult(UserModel user, ApiException apiEx)
	{
		if (user != null)
		{
			LocalSettings.Settings.LastLoginAccount = emailTextBlock.Text;
			Hide();
			if (FirstDownload)
			{
				UserActCollectUtils.AddClickEvent("register&login", "for_new_download", "login");
			}
			await App.Instance.ShowMainWindow();
			Close();
			return;
		}
		if (apiEx != null)
		{
			switch (apiEx.ErrorCode)
			{
			case "username_password_not_match":
			{
				if (int.TryParse(apiEx.Data["remainderTimes"]?.ToString(), out var result))
				{
					if (result == 0)
					{
						PasswordRemainderTimesText.Text = string.Empty;
						CustomerDialog customerDialog2 = new CustomerDialog(Utils.GetString("AccountAtRisk"), Utils.GetString("AccountAtRiskMessage"), MessageBoxButton.OK);
						customerDialog2.Owner = this;
						customerDialog2.ShowDialog();
						break;
					}
					if (result <= 3)
					{
						SwitchSignIn(resetInput: false);
						PasswordRemainderTimesText.Text = string.Format(Utils.GetString((result > 1) ? "PasswordRemainderTimes" : "PasswordRemainderTime"), result);
						FocusBox(passwordPasswordBox);
						break;
					}
				}
				if (!AccountGrid.IsVisible)
				{
					SwitchSignIn(resetInput: false);
				}
				PasswordRemainderTimesText.Text = Utils.GetString("UsernamePasswordNotMatch");
				FocusBox(passwordPasswordBox);
				break;
			}
			case "incorrect_password_too_many_times":
			{
				PasswordRemainderTimesText.Text = string.Empty;
				CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("AccountAtRisk"), Utils.GetString("AccountAtRiskMessage"), Utils.GetString("GotIt"), "");
				customerDialog.Owner = this;
				customerDialog.ShowDialog();
				if (!AccountGrid.IsVisible)
				{
					SwitchSignIn(resetInput: false);
				}
				break;
			}
			case "username_not_exist":
				if (!AccountGrid.IsVisible)
				{
					SwitchSignIn(resetInput: false);
				}
				AccountRemainderTimesText.Text = Utils.GetString("UsernameNotExist");
				FocusBox(emailTextBlock);
				break;
			case "need_upgrade_client":
			{
				UpdateModel updateModel = await UpdateHelper.CheckUpdate();
				if (updateModel == null)
				{
					return;
				}
				string forceText = null;
				if (apiEx.ErrorMessage == "mfa")
				{
					forceText = Utils.GetString("TwoFactorAuthenticationErrorMsg");
				}
				MainWindow.ShowNewVersionWindow(updateModel, force: true, this, forceText);
				break;
			}
			case "need_recaptcha":
			case "captcha_wrong":
				ShowCaptcha(apiEx.ErrorCode);
				SetOkButtonEnable();
				break;
			default:
				ShowLoginFailedDialog();
				MessageBox.Show(apiEx.ErrorCode);
				break;
			}
		}
		else
		{
			ShowLoginFailedDialog();
		}
		SignInIndicator.Visibility = Visibility.Collapsed;
	}

	private async Task ShowTwoFactorView()
	{
		MFAModel mFAModel = (_mfaModel = await Communicator.GetMfaSettings(_authId, isSign: true));
		if (mFAModel?.phoneOption != null && mFAModel.phoneOption.enabled)
		{
			ShowPhoneMfaView(mFAModel.phoneOption.name);
		}
		else if (mFAModel?.mailOption != null && mFAModel.mailOption.enabled)
		{
			ShowMailMfaView(mFAModel.mailOption.name);
		}
		else if (mFAModel?.appOption != null && mFAModel.appOption.enabled)
		{
			ShowAppMfaView();
		}
		else if (mFAModel?.backupOption != null && mFAModel.backupOption.enabled)
		{
			ShowBackupCodeMfaView();
		}
		else if (mFAModel?.wechatOption != null)
		{
			_ = mFAModel.wechatOption.enabled;
		}
		SetOtherWay();
	}

	private void ShowBackupCodeMfaView()
	{
		_mfaType = "backup";
		ShowMfaView(showSendText: false);
		VerifyMessage.Text = Utils.GetString("EnterBackupCode");
	}

	private void ShowAppMfaView()
	{
		_mfaType = "app";
		ShowMfaView(showSendText: false);
		VerifyMessage.Text = Utils.GetString("EnterOneTimeCode");
	}

	private void ShowMfaView(bool showSendText)
	{
		_verifyMfa = true;
		_isSignIn = true;
		SignTitle.Margin = new Thickness(0.0, 130.0, 0.0, 0.0);
		InputGrid.Visibility = Visibility.Visible;
		homeGrid.Visibility = Visibility.Collapsed;
		SwitchSignInText.Visibility = Visibility.Collapsed;
		SwitchSignUpText.Visibility = Visibility.Collapsed;
		CaptchaGrid.Visibility = Visibility.Collapsed;
		AgreePopup.Visibility = Visibility.Collapsed;
		AgreePanel.Visibility = Visibility.Collapsed;
		AccountGrid.Visibility = Visibility.Collapsed;
		passwordPasswordBox.Visibility = Visibility.Collapsed;
		CaptchaTextBlock.Text = string.Empty;
		_captchaKey = null;
		VerifyMessage.Visibility = Visibility.Visible;
		VerifyGrid.Visibility = Visibility.Visible;
		SendVerifyText.MinWidth = 0.0;
		VerifyCodeTextBlock.Tag = Utils.GetString("VCode");
		VerifyCodeTextBlock.Text = string.Empty;
		FocusBox(VerifyCodeTextBlock);
		if (!showSendText)
		{
			SendVerifyText.Visibility = Visibility.Collapsed;
		}
		else
		{
			SendVerifyText.Visibility = Visibility.Visible;
			SendVerifyText.MouseLeftButtonUp -= OnSendVerifyClick;
			SendVerifyText.MouseLeftButtonUp -= OnSendMfaVerifyClick;
			SendVerifyText.MouseLeftButtonUp += OnSendMfaVerifyClick;
			DateTime value = default(DateTime);
			sendMfaTimeDict.TryGetValue(_mfaType + "_" + _mfaName, out value);
			if ((DateTime.Now - value).TotalSeconds >= 60.0)
			{
				OnSendMfaVerifyClick(null, null);
			}
		}
		PasswordRemainderTimesText.Text = string.Empty;
		AccountRemainderTimesText.Text = string.Empty;
		SignTitle.Text = Utils.GetString("TwoFactorAuthentication");
		OkText.Text = Utils.GetString("Verification");
		SignInIndicator.Visibility = Visibility.Collapsed;
		ForgetAndSignupGrid.Visibility = Visibility.Collapsed;
		SetOkButtonEnable();
	}

	private void ShowMailMfaView(string mail)
	{
		_mfaType = "mail";
		_mfaName = mail;
		ShowMfaView(showSendText: true);
		SendVerifyText.Text = Utils.GetString("SendVerifyCode");
		SendVerifyText.IsEnabled = true;
		VerifyMessage.Text = string.Format(Utils.GetString("EnterVCodeSentTo"), mail);
	}

	private void ShowPhoneMfaView(string phone)
	{
		_mfaType = "phone";
		_mfaName = phone;
		ShowMfaView(showSendText: true);
		SendVerifyText.Text = Utils.GetString("SendVerifyCode");
		SendVerifyText.IsEnabled = true;
		if (phone.Length > 11)
		{
			phone = phone.Substring(phone.Length - 11);
		}
		VerifyMessage.Text = string.Format(Utils.GetString("EnterVCodeSentTo"), ((phone.Length > 3) ? phone.Substring(0, 3) : "") + "****" + ((phone.Length > 7) ? phone.Substring(7) : ""));
	}

	private void ShowLoginFailedDialog()
	{
		CustomerDialog dialog = new CustomerDialog(Utils.GetString("LoginFailed"), Utils.GetString("CheckNetworkProxyPre"), Utils.GetString("CheckNetworkProxyCenter"), string.Empty, Utils.GetString("OK"));
		dialog.TextClick += delegate
		{
			dialog.Close();
			try
			{
				ProxyWindow proxyWindow = new ProxyWindow();
				proxyWindow.Owner = this;
				proxyWindow.Show();
			}
			catch (Exception)
			{
			}
		};
		dialog.ShowDialog();
	}

	private void SwitchSignUp()
	{
		_isSignIn = false;
		SignTitle.Text = Utils.GetString("SignUpAccount");
		OkText.Text = Utils.GetString("PublicSignUp");
		passwordPasswordBox.Tag = Utils.GetString("PasswordChars");
		AccountGrid.Visibility = Visibility.Visible;
		passwordPasswordBox.Visibility = Visibility.Visible;
		AgreePanel.Visibility = Visibility.Visible;
		SwitchSignInText.Visibility = Visibility.Visible;
		SwitchSignUpText.Visibility = Visibility.Collapsed;
		ForgetAndSignupGrid.Visibility = Visibility.Collapsed;
		PasswordRemainderTimesText.Text = string.Empty;
		AccountRemainderTimesText.Text = string.Empty;
		CheckAccountValid();
	}

	private async void SwitchPhoneOrEmailSignup(bool isPhone)
	{
		SignTitle.Margin = new Thickness(0.0, isPhone ? 86 : 130, 0.0, 0.0);
		if ((!isPhone || !VerifyGrid.IsVisible) && (isPhone || VerifyGrid.IsVisible))
		{
			Keyboard.Focus(null);
			await Task.Delay(10);
			UpdateLayout();
			VerifyGrid.Visibility = ((!isPhone) ? Visibility.Collapsed : Visibility.Visible);
			CaptchaGrid.Visibility = ((!isPhone) ? Visibility.Collapsed : Visibility.Visible);
			CaptchaErrorText.Text = string.Empty;
			VerifyCodeErrorText.Text = string.Empty;
			if (isPhone)
			{
				GetCaptcha();
				SendVerifyText.MinWidth = 67.0;
				SendVerifyText.MouseLeftButtonUp -= OnSendMfaVerifyClick;
				SendVerifyText.MouseLeftButtonUp -= OnSendVerifyClick;
				SendVerifyText.MouseLeftButtonUp += OnSendVerifyClick;
				SendVerifyText.Visibility = Visibility.Visible;
				SendVerifyText.Text = Utils.GetString("SendVerifyCode");
				VerifyCodeTextBlock.Tag = Utils.GetString("VerificationCode");
				CaptchaTextBlock.Text = "";
				SendVerifyText.IsEnabled = false;
				VerifyCodeTextBlock.Text = "";
				_captchaKey = null;
			}
			FocusBox(passwordPasswordBox);
		}
	}

	private void SwitchSignIn(bool resetInput = true)
	{
		_inSignInView = true;
		_isSignIn = true;
		SignTitle.Margin = new Thickness(0.0, 130.0, 0.0, 0.0);
		InputGrid.Visibility = Visibility.Visible;
		homeGrid.Visibility = Visibility.Collapsed;
		SwitchSignInText.Visibility = Visibility.Collapsed;
		SwitchSignUpText.Visibility = Visibility.Visible;
		VerifyGrid.Visibility = Visibility.Collapsed;
		CaptchaGrid.Visibility = Visibility.Collapsed;
		AgreePopup.Visibility = Visibility.Collapsed;
		AgreePanel.Visibility = Visibility.Collapsed;
		if (resetInput)
		{
			emailTextBlock.Text = (string.IsNullOrEmpty(emailTextBlock.Text) ? LocalSettings.Settings.LastLoginAccount : emailTextBlock.Text);
			passwordPasswordBox.Password = string.Empty;
			Keyboard.Focus(emailTextBlock);
		}
		CaptchaTextBlock.Text = string.Empty;
		_captchaKey = null;
		AccountGrid.Visibility = Visibility.Visible;
		passwordPasswordBox.Visibility = Visibility.Visible;
		PasswordRemainderTimesText.Text = string.Empty;
		AccountRemainderTimesText.Text = string.Empty;
		SignTitle.Text = Utils.GetString("SignInAccount");
		OkText.Text = Utils.GetString("PublicSignIn");
		passwordPasswordBox.Tag = Utils.GetString("Password");
		ForgetAndSignupGrid.Visibility = Visibility.Visible;
		SetOkButtonEnable();
	}

	private void SetOkButtonEnable()
	{
		OkButton.IsEnabled = (!emailTextBlock.IsVisible || !string.IsNullOrEmpty(emailTextBlock.Text)) && (!CaptchaTextBlock.IsVisible || !string.IsNullOrEmpty(CaptchaTextBlock.Text)) && (!VerifyCodeTextBlock.IsVisible || !string.IsNullOrEmpty(VerifyCodeTextBlock.Text)) && (!passwordPasswordBox.IsVisible || !string.IsNullOrEmpty(passwordPasswordBox.Password));
	}

	private void SignWithAccountClick(object sender, RoutedEventArgs e)
	{
		SwitchSignIn();
		Keyboard.Focus(emailTextBlock);
		emailTextBlock.SelectionStart = emailTextBlock.Text.Length;
	}

	private async void ReturnClick(object sender, MouseButtonEventArgs e)
	{
		VerifyCodeErrorText.Text = null;
		CaptchaErrorText.Text = null;
		SendVerifyText.Text = Utils.GetString("SendVerifyCode");
		SendVerifyText.IsEnabled = true;
		if (!string.IsNullOrEmpty(_authId))
		{
			_authId = null;
			_mfaModel = null;
			_verifyMfa = false;
			OtherWayGrid.Visibility = Visibility.Collapsed;
			SwitchAnotherWayText.Visibility = Visibility.Collapsed;
			VerifyMessage.Visibility = Visibility.Collapsed;
			if (_inSignInView)
			{
				SwitchSignIn(resetInput: false);
				return;
			}
		}
		UpdateMainView();
	}

	private void forgetPasswordButton_Click(object sender, RoutedEventArgs e)
	{
		ResetPassword();
	}

	private void ResetPassword()
	{
		Utils.ResetPassword(emailTextBlock.Text);
	}

	private void OkButton_Click(object sender, RoutedEventArgs e)
	{
		if (_verifyMfa)
		{
			VerifyMfaCode();
			return;
		}
		if (CaptchaGrid.Visibility == Visibility.Visible && string.IsNullOrEmpty(CaptchaTextBlock.Text.Trim()))
		{
			CaptchaErrorText.Text = Utils.GetString("CaptchaNotInput");
			FocusBox(CaptchaTextBlock);
			return;
		}
		bool flag = Utils.IsDida() && Utils.IsMobilePhone(emailTextBlock.Text.Trim());
		bool flag2 = Utils.IsValidEmail(emailTextBlock.Text.Trim());
		if (!flag && !flag2)
		{
			AccountRemainderTimesText.Text = Utils.GetString("InvalidInput");
		}
		else if (_isSignIn)
		{
			Login();
		}
		else if ((flag && !VerifyGrid.IsVisible) || (!flag && VerifyGrid.IsVisible))
		{
			SwitchPhoneOrEmailSignup(flag);
		}
		else
		{
			signUp();
		}
	}

	private async Task VerifyMfaCode()
	{
		if (isLogging)
		{
			return;
		}
		isLogging = true;
		SignInIndicator.Visibility = Visibility.Visible;
		var (user, ex) = await LoginManager.VerifyMfa(_mfaOid, _mfaType, VerifyCodeTextBlock.Text.Trim(), _authId);
		SignInIndicator.Visibility = Visibility.Collapsed;
		isLogging = false;
		if (ex != null)
		{
			switch (ex.ErrorCode)
			{
			case "expired_code":
				VerifyCodeErrorText.Text = Utils.GetString("VCodeExpired");
				FocusBox(VerifyCodeTextBlock);
				break;
			case "verify_failed":
				VerifyCodeErrorText.Text = Utils.GetString("VerificationFailed");
				FocusBox(VerifyCodeTextBlock);
				break;
			case "expired_verify":
				UpdateMainView();
				break;
			default:
				VerifyCodeErrorText.Text = Utils.GetString("VerificationFailed");
				FocusBox(VerifyCodeTextBlock);
				break;
			}
		}
		else
		{
			HandleLoginResult(user, ex);
		}
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (LocalSettings.Settings.LoginUserId == "")
		{
			Application.Current?.Shutdown();
		}
		base.OnClosing(e);
	}

	private void ThirdLoginMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		ThirdLoginWebDialog thirdLoginWebDialog = new ThirdLoginWebDialog((sender as FrameworkElement)?.Tag as string, this);
		thirdLoginWebDialog.Owner = Window.GetWindow(this);
		thirdLoginWebDialog.ShowDialog();
	}

	private void WeChatSignClick(object sender, RoutedEventArgs e)
	{
		ThirdLoginWebDialog thirdLoginWebDialog = new ThirdLoginWebDialog("login_Wechat", this);
		thirdLoginWebDialog.Owner = Window.GetWindow(this);
		thirdLoginWebDialog.ShowDialog();
	}

	private void TextBlock_GotFocus(object sender, RoutedEventArgs e)
	{
	}

	private void OnProxySettingClick(object sender, MouseButtonEventArgs e)
	{
		ProxyWindow proxyWindow = new ProxyWindow();
		proxyWindow.Owner = this;
		proxyWindow.Show();
	}

	private void DragMove(object sender, MouseButtonEventArgs e)
	{
		DragMove();
	}

	private void CloseWindow(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private async void GoogleLoginClick(object sender, RoutedEventArgs e)
	{
		GoogleLogin login = new GoogleLogin(this);
		try
		{
			base.Closing += delegate
			{
				login.ClearLogin();
			};
			await login.PerformLogin();
		}
		catch (Exception)
		{
			Utils.Toast(Utils.GetString("LoginFailed"));
		}
	}

	public async Task PerformGoogleByAccessToken(string code, string accessToken)
	{
		UserModel user = await Communicator.ThirdSignon("login_Google", "", "google.com", accessToken, code);
		if (!string.IsNullOrEmpty(user?.authId))
		{
			await Task.Delay(200);
			try
			{
				Show();
				Activate();
				base.WindowState = WindowState.Normal;
			}
			catch (Exception)
			{
			}
			BeginTwoFactorVerify(user);
		}
		else if (!string.IsNullOrEmpty(user?.userId))
		{
			if (string.IsNullOrEmpty(user.code))
			{
				await SaveThirdLogin(user);
				return;
			}
			LoginBindThirdDialog loginBindThirdDialog = new LoginBindThirdDialog(user);
			loginBindThirdDialog.Owner = this;
			loginBindThirdDialog.ShowDialog();
			await SaveThirdLogin(user);
		}
		else
		{
			OnLoginFailed(user?.username);
		}
	}

	private async Task OnLoginFailed(string userUsername)
	{
		try
		{
			ErrorModel error = JsonConvert.DeserializeObject<ErrorModel>(userUsername ?? "");
			if (error?.errorCode == "need_upgrade_client")
			{
				UpdateModel updateModel = await UpdateHelper.CheckUpdate();
				if (updateModel != null)
				{
					string forceText = null;
					if (error.errorMessage == "mfa")
					{
						forceText = Utils.GetString("TwoFactorAuthenticationErrorMsg");
					}
					MainWindow.ShowNewVersionWindow(updateModel, force: true, this, forceText);
				}
			}
			else
			{
				MessageBox.Show(error?.errorCode);
			}
		}
		catch
		{
			UtilLog.Info(userUsername);
		}
	}

	public async Task SaveThirdLogin(UserModel user)
	{
		if (!string.IsNullOrEmpty(user.userId))
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
			Hide();
			if (FirstDownload)
			{
				UserActCollectUtils.AddClickEvent("register&login", "for_new_download", flag ? "register" : "login");
			}
			await App.Instance.ShowMainWindow();
			Close();
		}
	}

	private void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!string.IsNullOrEmpty(PasswordRemainderTimesText.Text))
		{
			PasswordRemainderTimesText.Text = string.Empty;
		}
		if (!string.IsNullOrEmpty(AccountRemainderTimesText.Text))
		{
			AccountRemainderTimesText.Text = string.Empty;
		}
		SetOkButtonEnable();
	}

	private void OnPasswordChanged(object sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrEmpty(PasswordRemainderTimesText.Text))
		{
			PasswordRemainderTimesText.Text = string.Empty;
		}
		SetOkButtonEnable();
	}

	private void ShowMoreLoginType(object sender, MouseButtonEventArgs e)
	{
		ShowOrHideElement(ThirdLoginGrid, show: true);
	}

	private void HideMoreSignGrid(object sender, MouseButtonEventArgs e)
	{
		ShowOrHideElement(ThirdLoginGrid, show: false);
	}

	private void ShowOrHideElement(UIElement element, bool show)
	{
		element.IsHitTestVisible = show;
		element.Visibility = Visibility.Visible;
		Storyboard obj = (Storyboard)FindResource(show ? "ShowStory" : "HideStory");
		((DoubleAnimation)obj.Children[0]).SetValue(Storyboard.TargetProperty, element);
		obj.Begin();
		Task.Delay(120);
		if (!show)
		{
			element.Visibility = Visibility.Collapsed;
		}
	}

	private void SignInWithApple(object sender, MouseButtonEventArgs e)
	{
		SignInWithApple apple = ticktick_WPF.Views.SignInWithApple.GetInstance(this);
		base.Closing += delegate
		{
			apple.ClearLogin();
		};
		apple.Start();
	}

	public async Task TryLoginApple(SignInWithAppleParma e)
	{
		UserModel user = await Communicator.LoginApple(e.accessToken, e.uId, e.email, e.name);
		try
		{
			Show();
			Activate();
			base.WindowState = WindowState.Normal;
		}
		catch (Exception)
		{
		}
		await Task.Delay(500);
		if (!string.IsNullOrEmpty(user?.authId))
		{
			try
			{
				Show();
				Activate();
				base.WindowState = WindowState.Normal;
			}
			catch (Exception)
			{
			}
			BeginTwoFactorVerify(user);
		}
		else if (user?.userId != null && user.userId != "")
		{
			if (!string.IsNullOrEmpty(user.code))
			{
				LoginBindThirdDialog loginBindThirdDialog = new LoginBindThirdDialog(user);
				loginBindThirdDialog.Owner = this;
				loginBindThirdDialog.ShowDialog();
			}
			await UserDao.UpdateOrInsertUserModelListDbAsync(user);
			SaveThirdLogin(user);
		}
		else if (user != null && !string.IsNullOrEmpty(user.username))
		{
			OnLoginFailed(user.username);
		}
	}

	private async void GetCaptcha()
	{
		string captchaKey;
		string text;
		(captchaKey, text) = await Communicator.GetCaptcha();
		if (!string.IsNullOrEmpty(text))
		{
			_captchaKey = captchaKey;
			text = text.Split(',').LastOrDefault();
			try
			{
				Captcha.Source = ImageUtils.ConvertBase64ToImageSource(text);
			}
			catch (Exception ex)
			{
				UtilLog.Info(ex.Message + "  " + text);
			}
		}
	}

	private async void OnSendMfaVerifyClick(object sender, MouseButtonEventArgs e)
	{
		sendMfaTimeDict[_mfaType + "_" + _mfaName] = DateTime.Now;
		(string, int) tuple = await Communicator.SendMfaVerifyCode(_mfaType, _mfaName, 3, _authId, isSign: true);
		if (tuple.Item2 == 0)
		{
			_mfaOid = tuple.Item1;
			string sendCode = Utils.GetString("SendVerifyCode");
			SendVerifyText.IsEnabled = false;
			for (int second = 59; second > 0; second--)
			{
				SendVerifyText.Text = string.Format(Utils.GetString("NSecondResend"), second);
				await Task.Delay(1000);
				if (SendVerifyText.Text == sendCode)
				{
					break;
				}
			}
			SendVerifyText.Text = sendCode;
			SendVerifyText.IsEnabled = true;
			return;
		}
		var (text, _) = tuple;
		if (!(text == "user_send_sms_failed"))
		{
			if (text == "user_send_sms_exceed_limit")
			{
				VerifyCodeErrorText.Text = Utils.GetString((tuple.Item2 >= 20) ? "TooOftenAgainTomorrow" : ((tuple.Item2 >= 10) ? "TooOftenAgainHourLater" : "TooOften"));
				FocusBox(VerifyCodeTextBlock);
			}
		}
		else
		{
			VerifyCodeErrorText.Text = Utils.GetString("SendFailed");
			FocusBox(VerifyCodeTextBlock);
		}
	}

	private async void OnSendVerifyClick(object sender, MouseButtonEventArgs e)
	{
		if (CaptchaGrid.Visibility == Visibility.Visible && string.IsNullOrEmpty(CaptchaTextBlock.Text.Trim()))
		{
			CaptchaErrorText.Text = Utils.GetString("CaptchaNotInput");
			FocusBox(CaptchaTextBlock);
			return;
		}
		string text = await Communicator.SendVerifyCode(emailTextBlock.Text, _captchaKey, CaptchaTextBlock.Text);
		_captchaKey = null;
		if (string.IsNullOrEmpty(text))
		{
			string sendCode = Utils.GetString("SendVerifyCode");
			SendVerifyText.IsEnabled = false;
			for (int second = 59; second > 0; second--)
			{
				SendVerifyText.Text = string.Format(Utils.GetString("NSecondResend"), second);
				await Task.Delay(1000);
				if (SendVerifyText.Text == sendCode)
				{
					break;
				}
			}
			SendVerifyText.Text = sendCode;
			SendVerifyText.IsEnabled = !string.IsNullOrEmpty(CaptchaTextBlock.Text);
			return;
		}
		GetCaptcha();
		switch (text)
		{
		case "unsupported_phone_num":
			AccountRemainderTimesText.Text = Utils.GetString("ErrorPhone");
			FocusBox(emailTextBlock);
			break;
		case "user_send_sms_failed":
			VerifyCodeErrorText.Text = Utils.GetString("SendFailed");
			FocusBox(VerifyCodeTextBlock);
			break;
		case "user_send_sms_exceed_limit":
			VerifyCodeErrorText.Text = Utils.GetString("TooOften");
			FocusBox(VerifyCodeTextBlock);
			break;
		case "user_phone_bound_error":
			AccountRemainderTimesText.Text = Utils.GetString("PhoneSignedUp");
			FocusBox(emailTextBlock);
			break;
		case "captcha_wrong":
			CaptchaErrorText.Text = Utils.GetString("WrongCaptchaCode");
			FocusBox(CaptchaTextBlock);
			break;
		}
	}

	private void OnCaptchaClick(object sender, MouseButtonEventArgs e)
	{
		GetCaptcha();
	}

	private void OnAgreeChecked(object sender, RoutedEventArgs e)
	{
		AgreePopup.Visibility = Visibility.Collapsed;
	}

	private void VerifyCodeTextChanged(object sender, TextChangedEventArgs e)
	{
		VerifyCodeErrorText.Text = null;
		SetOkButtonEnable();
	}

	private void CaptchaTextChanged(object sender, TextChangedEventArgs e)
	{
		CaptchaErrorText.Text = null;
		if (SendVerifyText.Text == Utils.GetString("SendVerifyCode"))
		{
			SendVerifyText.IsEnabled = !string.IsNullOrEmpty(CaptchaTextBlock.Text);
		}
		SetOkButtonEnable();
	}

	private void SwitchToSignUp(object sender, MouseButtonEventArgs e)
	{
		SwitchSignUp();
	}

	private void SwitchToSignIn(object sender, MouseButtonEventArgs e)
	{
		SwitchSignIn();
		CheckAccountValid();
	}

	private async void OnAccountLostFocus(object sender, RoutedEventArgs e)
	{
		CheckAccountValid();
	}

	private void CheckAccountValid()
	{
		if (!string.IsNullOrEmpty(emailTextBlock.Text.Trim()))
		{
			bool flag = Utils.IsDida() && Utils.IsMobilePhone(emailTextBlock.Text.Trim());
			bool flag2 = Utils.IsValidEmail(emailTextBlock.Text.Trim());
			if (!flag && !flag2)
			{
				AccountRemainderTimesText.Text = Utils.GetString("InvalidInput");
			}
			else if (!_isSignIn)
			{
				SwitchPhoneOrEmailSignup(flag);
			}
		}
	}

	private void OnPasswordLostFocus(object sender, RoutedEventArgs e)
	{
		if (passwordPasswordBox.Password.Length != 0 && (passwordPasswordBox.Password.Length > 64 || passwordPasswordBox.Password.Length < 6))
		{
			PasswordRemainderTimesText.Text = Utils.GetString("InvalidatePasswordLength");
		}
	}

	private async void SetOtherWay()
	{
		MFAModel mfaModel = _mfaModel;
		if (mfaModel == null)
		{
			OtherWayGrid.Visibility = Visibility.Collapsed;
			SwitchAnotherWayText.Visibility = Visibility.Collapsed;
			return;
		}
		string text = null;
		if (mfaModel.phoneOption != null && mfaModel.phoneOption.enabled && _mfaType != "phone")
		{
			TwoStepPhone.Visibility = Visibility.Visible;
			text = "phone";
		}
		else
		{
			TwoStepPhone.Visibility = Visibility.Collapsed;
		}
		if (mfaModel.mailOption != null && mfaModel.mailOption.enabled && _mfaType != "mail")
		{
			TwoStepMail.Visibility = Visibility.Visible;
			text = (string.IsNullOrEmpty(text) ? "mail" : "-1");
		}
		else
		{
			TwoStepMail.Visibility = Visibility.Collapsed;
		}
		if (mfaModel.appOption != null && mfaModel.appOption.enabled && _mfaType != "app")
		{
			TwoStepApp.Visibility = Visibility.Visible;
			text = (string.IsNullOrEmpty(text) ? "app" : "-1");
		}
		else
		{
			TwoStepApp.Visibility = Visibility.Collapsed;
		}
		if (mfaModel.backupOption != null && mfaModel.backupOption.enabled && _mfaType != "backup")
		{
			TwoStepBackup.Visibility = Visibility.Visible;
			text = (string.IsNullOrEmpty(text) ? "backup" : "-1");
		}
		else
		{
			TwoStepBackup.Visibility = Visibility.Collapsed;
		}
		if (text == "-1")
		{
			SwitchAnotherWayText.Visibility = Visibility.Collapsed;
			OtherWayGrid.IsHitTestVisible = false;
			OtherWayGrid.Visibility = Visibility.Visible;
			await Task.Delay(300);
			OtherWayGrid.IsHitTestVisible = true;
		}
		else if (!string.IsNullOrEmpty(text))
		{
			OtherWayGrid.Visibility = Visibility.Collapsed;
			SwitchAnotherWayText.Visibility = Visibility.Visible;
			SwitchAnotherWayText.Tag = text;
			switch (text)
			{
			case "phone":
				SwitchAnotherWayText.Text = Utils.GetString("SwitchPhoneVerification");
				break;
			case "mail":
				SwitchAnotherWayText.Text = Utils.GetString("SwitchEmailVerification");
				break;
			case "app":
				SwitchAnotherWayText.Text = Utils.GetString("SwitchAppVerification");
				break;
			case "backup":
				SwitchAnotherWayText.Text = Utils.GetString("SwitchBackupVerification");
				break;
			}
		}
	}

	private void OnOtherWayShow(object sender, EventArgs e)
	{
		OtherWayContainer.IsHitTestVisible = true;
	}

	private void OnOtherWayHide(object sender, EventArgs e)
	{
		OtherWayContainer.IsHitTestVisible = false;
	}

	private void OnSwitchAnotherWayClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement frameworkElement)
		{
			switch (frameworkElement.Tag as string)
			{
			case "phone":
				ShowPhoneMfaView(_mfaModel.phoneOption.name);
				break;
			case "mail":
				ShowMailMfaView(_mfaModel.mailOption.name);
				break;
			case "app":
				ShowAppMfaView();
				break;
			case "backup":
				ShowBackupCodeMfaView();
				break;
			}
			VerifyCodeErrorText.Text = null;
			SetOtherWay();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/logindialog.xaml", UriKind.Relative);
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
			((Border)target).MouseLeftButtonDown += DragMove;
			break;
		case 2:
			Container = (Grid)target;
			break;
		case 3:
			((Border)target).MouseLeftButtonDown += DragMove;
			break;
		case 4:
			Carousel = (CarouselControl)target;
			break;
		case 5:
			((Border)target).MouseLeftButtonUp += CloseWindow;
			break;
		case 6:
			ReturnGrid = (StackPanel)target;
			ReturnGrid.MouseLeftButtonUp += ReturnClick;
			break;
		case 7:
			InputGrid = (StackPanel)target;
			break;
		case 8:
			SignTitle = (TextBlock)target;
			break;
		case 9:
			VerifyMessage = (TextBlock)target;
			break;
		case 10:
			AccountGrid = (Grid)target;
			break;
		case 11:
			emailTextBlock = (TextBox)target;
			emailTextBlock.TextChanged += OnTextChanged;
			emailTextBlock.LostFocus += OnAccountLostFocus;
			emailTextBlock.GotFocus += TextBlock_GotFocus;
			emailTextBlock.KeyDown += InputKeyDown;
			break;
		case 12:
			AccountRemainderTimesText = (TextBlock)target;
			break;
		case 13:
			passwordPasswordBox = (PasswordBox)target;
			passwordPasswordBox.LostFocus += OnPasswordLostFocus;
			passwordPasswordBox.KeyDown += InputKeyDown;
			passwordPasswordBox.PasswordChanged += OnPasswordChanged;
			break;
		case 14:
			PasswordRemainderTimesText = (TextBlock)target;
			break;
		case 15:
			CaptchaGrid = (Grid)target;
			break;
		case 16:
			CaptchaTextBlock = (TextBox)target;
			CaptchaTextBlock.TextChanged += CaptchaTextChanged;
			CaptchaTextBlock.KeyDown += InputKeyDown;
			break;
		case 17:
			Captcha = (Image)target;
			Captcha.MouseLeftButtonUp += OnCaptchaClick;
			break;
		case 18:
			CaptchaErrorText = (TextBlock)target;
			break;
		case 19:
			VerifyGrid = (Grid)target;
			break;
		case 20:
			VerifyCodeTextBlock = (TextBox)target;
			VerifyCodeTextBlock.KeyDown += InputKeyDown;
			VerifyCodeTextBlock.TextChanged += VerifyCodeTextChanged;
			break;
		case 21:
			SendVerifyText = (TextBlock)target;
			SendVerifyText.MouseLeftButtonUp += OnSendVerifyClick;
			break;
		case 22:
			VerifyCodeErrorText = (TextBlock)target;
			break;
		case 23:
			OkButton = (Grid)target;
			break;
		case 24:
			((Button)target).Click += OkButton_Click;
			break;
		case 25:
			SignInIndicator = (LoadingIndicator)target;
			break;
		case 26:
			OkText = (TextBlock)target;
			break;
		case 27:
			SwitchAnotherWayText = (TextBlock)target;
			SwitchAnotherWayText.MouseLeftButtonUp += OnSwitchAnotherWayClick;
			break;
		case 28:
			OtherWayGrid = (Grid)target;
			break;
		case 29:
			AnotherWayText = (TextBlock)target;
			break;
		case 30:
			OtherWayContainer = (Grid)target;
			break;
		case 31:
			TwoStepPhone = (ContentControl)target;
			TwoStepPhone.MouseLeftButtonUp += OnSwitchAnotherWayClick;
			break;
		case 32:
			TwoStepMail = (ContentControl)target;
			TwoStepMail.MouseLeftButtonUp += OnSwitchAnotherWayClick;
			break;
		case 33:
			TwoStepApp = (ContentControl)target;
			TwoStepApp.MouseLeftButtonUp += OnSwitchAnotherWayClick;
			break;
		case 34:
			TwoStepBackup = (ContentControl)target;
			TwoStepBackup.MouseLeftButtonUp += OnSwitchAnotherWayClick;
			break;
		case 35:
			ForgetAndSignupGrid = (Border)target;
			break;
		case 36:
			((TextBlock)target).MouseLeftButtonUp += forgetPasswordButton_Click;
			break;
		case 37:
			AgreePanel = (StackPanel)target;
			break;
		case 38:
			AgreeCheckBox = (CheckBox)target;
			AgreeCheckBox.Checked += OnAgreeChecked;
			break;
		case 39:
			AgreeText = (TextBlock)target;
			break;
		case 40:
			AgreePopup = (StackPanel)target;
			break;
		case 41:
			SwitchSignUpText = (TextBlock)target;
			break;
		case 42:
			((Run)target).MouseLeftButtonUp += SwitchToSignUp;
			break;
		case 43:
			SwitchSignInText = (TextBlock)target;
			break;
		case 44:
			((Run)target).MouseLeftButtonUp += SwitchToSignIn;
			break;
		case 45:
			homeGrid = (StackPanel)target;
			break;
		case 46:
			DidaLogoImage = (Image)target;
			break;
		case 47:
			WelcomeTitle = (TextBlock)target;
			break;
		case 48:
			WeChatLogin = (Button)target;
			WeChatLogin.Click += WeChatSignClick;
			break;
		case 49:
			GoogleLogin = (Button)target;
			GoogleLogin.Click += GoogleLoginClick;
			break;
		case 50:
			AccountLogin = (Button)target;
			AccountLogin.Click += SignWithAccountClick;
			break;
		case 51:
			EmailLogin = (Button)target;
			EmailLogin.Click += SignWithAccountClick;
			break;
		case 52:
			MoreLoginType = (Grid)target;
			break;
		case 53:
			ThirdLoginGrid = (Grid)target;
			break;
		case 54:
			ThirdLoginEnGrid = (Grid)target;
			break;
		case 55:
			FacebookGrid = (Grid)target;
			FacebookGrid.MouseLeftButtonUp += ThirdLoginMouseLeftButtonUp;
			break;
		case 56:
			AppleEnGrid = (Grid)target;
			AppleEnGrid.MouseLeftButtonUp += SignInWithApple;
			break;
		case 57:
			ThirdLoginZnGrid = (Grid)target;
			break;
		case 58:
			AppleCNGrid = (Grid)target;
			AppleCNGrid.MouseLeftButtonUp += SignInWithApple;
			break;
		case 59:
			WeiboGrid = (Grid)target;
			WeiboGrid.MouseLeftButtonUp += ThirdLoginMouseLeftButtonUp;
			break;
		case 60:
			QQGrid = (Grid)target;
			QQGrid.MouseLeftButtonUp += ThirdLoginMouseLeftButtonUp;
			break;
		case 61:
			ProxySetting = (Border)target;
			ProxySetting.MouseLeftButtonUp += OnProxySettingClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
