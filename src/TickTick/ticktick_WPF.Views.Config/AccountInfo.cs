using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Config;

public class AccountInfo : UserControl, IComponentConnector
{
	private readonly SettingDialog _parent;

	internal ImageBrush HeadimgImageBrush;

	internal TextBlock email;

	internal Grid NextBillingDateTextPanel;

	internal TextBlock NextBillingDateText;

	internal Border GraceHelper;

	internal ToolTip Tips;

	internal Button RenewButton;

	internal Button LogoutButton;

	private bool _contentLoaded;

	public AccountInfo(SettingDialog settingDialog)
	{
		InitializeComponent();
		InitData();
		_parent = settingDialog;
	}

	private void InitData()
	{
		InitProStatus();
		CheckLoginStatus();
	}

	private async void InitProStatus()
	{
		UserModel userModel = await UserDao.QueryUserModelListDbByIdAsync(LocalSettings.Settings.LoginUserId);
		TeamModel teamModel = CacheManager.GetTeams()?.FirstOrDefault((TeamModel t) => t.IsPro());
		bool flag = teamModel != null;
		if (userModel != null)
		{
			if (userModel.pro || flag)
			{
				DateTime? dateTime = (flag ? new DateTime?(teamModel.expiredDate) : userModel.proEndDate);
				NextBillingDateTextPanel.Visibility = Visibility.Visible;
				NextBillingDateText.Text = string.Format(Utils.GetString(flag ? "UsingBusiness" : "NextBillingDate"), dateTime.HasValue ? DateUtils.FormatYearMonthDay(dateTime.Value, DateFormatLen.Short) : string.Empty);
				if (!flag && UserManager.InGraceDate())
				{
					GraceHelper.Visibility = Visibility.Visible;
					DateTime? userGraceDate = UserManager.GetUserGraceDate();
					if (userGraceDate.HasValue)
					{
						NextBillingDateText.Text = string.Format(Utils.GetString("NextBillingDate"), DateUtils.FormatYearMonthDay(userGraceDate.Value, DateFormatLen.Short));
						int val = (int)(DateTime.Now.Date - userGraceDate.Value.Date).TotalDays;
						string arg = ((UserManager.GetSubscribeType() == "apple") ? "Apple" : "Google");
						Tips.Content = string.Format(Utils.GetString("SubscriptionGraceDateMessage"), arg, Math.Max(1, val));
					}
				}
			}
			else
			{
				NextBillingDateTextPanel.Visibility = Visibility.Collapsed;
			}
		}
		if (flag || (userModel != null && !userModel.needSubscribe))
		{
			RenewButton.Visibility = Visibility.Collapsed;
		}
		else
		{
			RenewButton.Content = Utils.GetString((userModel != null && userModel.pro) ? "RenewNow" : "UpdateNow");
		}
	}

	private async Task CheckLoginStatus()
	{
		UserInfoModel userInfo = await UserManager.GetUserInfo();
		if (userInfo != null)
		{
			ImageBrush headimgImageBrush = HeadimgImageBrush;
			headimgImageBrush.ImageSource = await AvatarHelper.GetAvatarByUrlAsync("avatar-new.png", 256);
			if (!userInfo.fakedEmail)
			{
				userInfo.displayEmail = userInfo.username;
			}
			if (userInfo.displayEmail == null)
			{
				email.Visibility = Visibility.Collapsed;
			}
			if (userInfo.fakedEmail && string.IsNullOrEmpty(userInfo.name))
			{
				UserModel userModel = await UserDao.GetUserModelByName(userInfo.username);
				if (userModel != null)
				{
					userInfo.name = Utils.GetMaskPhone(userModel.phone);
				}
			}
			if (string.IsNullOrEmpty(userInfo.displayEmail) && !string.IsNullOrEmpty(userInfo.name))
			{
				UserModel userModel2 = await UserDao.GetUserModelByName(userInfo.username);
				if (userModel2 != null)
				{
					userInfo.displayEmail = Utils.GetMaskPhone(userModel2.phone);
					if (!string.IsNullOrEmpty(userInfo.displayEmail))
					{
						email.Text = Utils.GetString("PhoneNumber");
						email.Visibility = Visibility.Visible;
					}
				}
			}
			base.DataContext = userInfo;
			if (!string.IsNullOrEmpty(userInfo.picture))
			{
				headimgImageBrush = HeadimgImageBrush;
				headimgImageBrush.ImageSource = await AvatarHelper.GetAvatarByUrlAsync(userInfo.picture, 256);
			}
		}
		else
		{
			email.Visibility = Visibility.Collapsed;
			HeadimgImageBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Assets/avatar-new.png"));
		}
	}

	private void OnRenewClick(object sender, RoutedEventArgs e)
	{
		Utils.StartUpgrade("settings");
	}

	private void OnLogoutClick(object sender, RoutedEventArgs e)
	{
		_parent.Close();
		LogoutButton.IsEnabled = false;
		App.Window?.Logout();
	}

	private async void GoToWebSetting(object sender, RoutedEventArgs e)
	{
		Utils.TryProcessStartUrlWithToken("/webapp/#settings/profile");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/accountinfo.xaml", UriKind.Relative);
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
			HeadimgImageBrush = (ImageBrush)target;
			break;
		case 2:
			email = (TextBlock)target;
			break;
		case 3:
			NextBillingDateTextPanel = (Grid)target;
			break;
		case 4:
			NextBillingDateText = (TextBlock)target;
			break;
		case 5:
			GraceHelper = (Border)target;
			break;
		case 6:
			Tips = (ToolTip)target;
			break;
		case 7:
			((Button)target).Click += GoToWebSetting;
			break;
		case 8:
			RenewButton = (Button)target;
			RenewButton.Click += OnRenewClick;
			break;
		case 9:
			LogoutButton = (Button)target;
			LogoutButton.Click += OnLogoutClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
