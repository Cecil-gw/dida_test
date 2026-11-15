using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using Emoji.Wpf;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Upgrade;

namespace ticktick_WPF.Views.Setting;

public class PremiumConfig : UserControl, IComponentConnector
{
	internal EmjTextBlock TitleText;

	internal TextBlock DescText;

	internal Border GraceHelper;

	internal ToolTip Tips;

	internal Popup HintPopup;

	internal ArrowHintUserControl HintArrow;

	internal ItemsControl ItemsControl;

	internal Button UpgradeBtn;

	internal TextBlock PrivacyText;

	internal TextBlock ServiceText;

	internal TextBlock ManageText;

	private bool _contentLoaded;

	private UserModel user { get; set; }

	public PremiumConfig()
	{
		InitializeComponent();
		ItemsControl.ItemsSource = PremiumViewModel.BuildViewModels();
		InitData();
		UpgradeBtn.Margin = new Thickness(0.0, Utils.IsCn() ? 10 : 4, 0.0, 0.0);
	}

	private void InitData()
	{
		InitProStatus();
	}

	private async void InitProStatus(bool force = false)
	{
		user = await UserManager.GetCurrentUser(force);
		TeamModel teamModel = CacheManager.GetTeams()?.FirstOrDefault((TeamModel t) => t.IsPro());
		bool flag = teamModel != null;
		if (user == null)
		{
			return;
		}
		if (user.pro || flag)
		{
			DateTime? dateTime = (flag ? new DateTime?(teamModel.expiredDate) : user.proEndDate);
			TitleText.Text = string.Format(Utils.GetString(flag ? "UsingBusiness1" : "YouArePremium"), Utils.GetAppName());
			DescText.Text = string.Format(Utils.GetString("ExpiresOn"), dateTime.HasValue ? DateUtils.FormatYearMonthDay(dateTime.Value, DateFormatLen.Short) : string.Empty);
			if (flag || !user.needSubscribe)
			{
				UpgradeBtn.Visibility = Visibility.Collapsed;
				PrivacyText.Visibility = Visibility.Collapsed;
				ServiceText.Visibility = Visibility.Collapsed;
				ManageText.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
			}
			else
			{
				UpgradeBtn.Visibility = Visibility.Visible;
				UpgradeBtn.Content = Utils.GetString("RenewNow");
				ManageText.Visibility = Visibility.Collapsed;
				PrivacyText.Visibility = Visibility.Visible;
				ServiceText.Visibility = Visibility.Visible;
			}
			if (UserManager.InGraceDate())
			{
				GraceHelper.Visibility = Visibility.Visible;
				TitleText.Text = Utils.GetString("PremiumHasExpired");
				DateTime? userGraceDate = UserManager.GetUserGraceDate();
				if (userGraceDate.HasValue)
				{
					DescText.Text = string.Format(Utils.GetString("ExpiresOn"), DateUtils.FormatYearMonthDay(userGraceDate.Value, DateFormatLen.Short));
					int val = (int)(DateTime.Now.Date - userGraceDate.Value.Date).TotalDays;
					string arg = ((UserManager.GetSubscribeType() == "apple") ? "Apple" : "Google");
					Tips.Content = string.Format(Utils.GetString("SubscriptionGraceDateMessage"), arg, Math.Max(1, val));
				}
			}
			if (!(user.errorType > 0))
			{
				return;
			}
			GraceHelper.Visibility = Visibility.Visible;
			int? errorType = user.errorType;
			if (errorType.HasValue && errorType == 2)
			{
				string text = Utils.GetString("DuplicateSubscriptionContent");
				if (App.Ci.TwoLetterISOLanguageName == "zh")
				{
					text = text.Replace(" {0} ", "{0}");
				}
				ToolTip tips = Tips;
				string content = (HintArrow.Text = string.Format(text, Utils.GetString("ManageSubscriptions")));
				tips.Content = content;
			}
			if (!force)
			{
				HintPopup.IsOpen = true;
			}
		}
		else if (user.errorType == 1)
		{
			TitleText.Text = Utils.GetString("PremiumHasExpired");
			DateTime? dateTime2 = user?.noGraceDate ?? user?.proEndDate;
			if (dateTime2.HasValue)
			{
				DescText.Text = string.Format(Utils.GetString("ExpiresOn"), DateUtils.FormatYearMonthDay(dateTime2.Value, DateFormatLen.Short));
			}
			else
			{
				DescText.Text = null;
			}
			GraceHelper.Visibility = Visibility.Visible;
			string text3 = Utils.GetString("PaymentFailedContent");
			if (App.Ci.TwoLetterISOLanguageName == "zh")
			{
				text3 = text3.Replace(" {0} ", "{0}");
			}
			string subscribeTypeText = PremiumUtils.GetSubscribeTypeText(user.subscribeType);
			subscribeTypeText = (string.IsNullOrEmpty(subscribeTypeText) ? "PayPal" : subscribeTypeText);
			ToolTip tips2 = Tips;
			string content = (HintArrow.Text = string.Format(text3, subscribeTypeText));
			tips2.Content = content;
			if (!force)
			{
				HintPopup.IsOpen = true;
			}
			UpgradeBtn.Visibility = Visibility.Collapsed;
			PrivacyText.Visibility = Visibility.Collapsed;
			ServiceText.Visibility = Visibility.Collapsed;
			ManageText.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		}
		else
		{
			TitleText.Text = Utils.GetString("DoubleYourAchievement");
			DescText.Text = string.Format(Utils.GetString("WithPremium"), Utils.GetAppName());
			UpgradeBtn.Visibility = Visibility.Visible;
			UpgradeBtn.Content = Utils.GetString("UpdateNow");
			ManageText.Visibility = Visibility.Collapsed;
			PrivacyText.Visibility = Visibility.Visible;
			ServiceText.Visibility = Visibility.Visible;
		}
	}

	private void OnUpgradeClick(object sender, RoutedEventArgs e)
	{
		Utils.StartUpgrade("settings");
	}

	private void OpenPrivacyPolicy(object sender, MouseButtonEventArgs e)
	{
		Utils.TryProcessStartUrl("https://dida365.com/privacy");
	}

	private void OpenTermsofService(object sender, MouseButtonEventArgs e)
	{
		Utils.TryProcessStartUrl("https://dida365.com/tos");
	}

	private async void OpenManageSubscriptions(object sender, MouseButtonEventArgs e)
	{
		if (user != null && user.subscribeType != "order")
		{
			ManageProSubscribeWindow manageProSubscribeWindow = new ManageProSubscribeWindow(await Communicator.GetSubscribeList());
			manageProSubscribeWindow.Owner = Window.GetWindow(this);
			manageProSubscribeWindow.ShowDialog();
		}
		else
		{
			Utils.TryProcessStartUrlWithToken("webapp/#settings/premium");
		}
	}

	private void OpenOrderHistory(object sender, MouseButtonEventArgs e)
	{
		if (Utils.IsWindows7())
		{
			Utils.TryProcessStartUrlWithToken("/webview/paymentHistory?lang=" + Utils.GetLanguage().ToLower() + "&dark=" + ThemeUtil.IsDark().ToString().ToLower() + "&from=web");
		}
		else
		{
			NavigateWebBrowserWindow navigateWebBrowserWindow = new NavigateWebBrowserWindow("/webview/paymentHistory?lang=" + Utils.GetLanguage().ToLower() + "&dark=" + ThemeUtil.IsDark().ToString().ToLower(), addLang: false);
			navigateWebBrowserWindow.Title = Utils.GetString("OrderHistory");
			navigateWebBrowserWindow.Width = 496.0;
			navigateWebBrowserWindow.Height = 540.0;
			navigateWebBrowserWindow.Owner = Window.GetWindow(this);
			navigateWebBrowserWindow.ShowDialog();
			navigateWebBrowserWindow.Topmost = true;
		}
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.PaymentUpdated += OnPaymentUpdated;
	}

	private void OnPaymentUpdated(object sender, EventArgs e)
	{
		InitProStatus(force: true);
	}

	private void UserControl_Unloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.PaymentUpdated -= OnPaymentUpdated;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/setting/premiumconfig.xaml", UriKind.Relative);
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
			((PremiumConfig)target).Loaded += UserControl_Loaded;
			((PremiumConfig)target).Unloaded += UserControl_Unloaded;
			break;
		case 2:
			TitleText = (EmjTextBlock)target;
			break;
		case 3:
			DescText = (TextBlock)target;
			break;
		case 4:
			GraceHelper = (Border)target;
			break;
		case 5:
			Tips = (ToolTip)target;
			break;
		case 6:
			HintPopup = (Popup)target;
			break;
		case 7:
			HintArrow = (ArrowHintUserControl)target;
			break;
		case 8:
			ItemsControl = (ItemsControl)target;
			break;
		case 9:
			UpgradeBtn = (Button)target;
			UpgradeBtn.Click += OnUpgradeClick;
			break;
		case 10:
			PrivacyText = (TextBlock)target;
			break;
		case 11:
			((Run)target).MouseLeftButtonUp += OpenPrivacyPolicy;
			break;
		case 12:
			ServiceText = (TextBlock)target;
			break;
		case 13:
			((Run)target).MouseLeftButtonUp += OpenTermsofService;
			break;
		case 14:
			ManageText = (TextBlock)target;
			break;
		case 15:
			((Run)target).MouseLeftButtonUp += OpenManageSubscriptions;
			break;
		case 16:
			((TextBlock)target).MouseLeftButtonUp += OpenOrderHistory;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
