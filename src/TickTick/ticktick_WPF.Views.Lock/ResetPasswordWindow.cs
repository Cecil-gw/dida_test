using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Lock;

public class ResetPasswordWindow : Window, IComponentConnector
{
	private enum SaveMode
	{
		Disabled,
		Verifing,
		Normal
	}

	private ResetMode _resetMode;

	internal TextBlock ResetHint;

	internal PasswordBox PasswordText;

	internal TextBlock PasswordInvalidText;

	internal Button SaveButton;

	private bool _contentLoaded;

	public ResetPasswordWindow(ResetMode mode)
	{
		InitializeComponent();
		InitData(mode);
	}

	private async Task InitData(ResetMode mode)
	{
		_resetMode = mode;
		PasswordText.Focus();
		SetSaveMode(SaveMode.Disabled);
		_ = string.Empty;
		UserInfoModel userInfoModel = await UserManager.GetUserInfo();
		string account;
		if (userInfoModel != null)
		{
			account = userInfoModel.displayEmail;
			if (string.IsNullOrEmpty(account))
			{
				if (!userInfoModel.fakedEmail)
				{
					account = userInfoModel.username;
				}
				else
				{
					UserModel userModel = await UserDao.GetUserModelByName(userInfoModel.username);
					if (userModel != null)
					{
						account = Utils.GetMaskPhone(userModel.phone);
					}
				}
			}
			if (account.Contains("@"))
			{
				string[] array = account.Split('@');
				int num = (int)Math.Floor((double)array[0].Length / 2.0);
				account = array[0].Substring(0, num) + new string('*', array[0].Length - num) + "@" + array[1];
			}
		}
		else
		{
			account = LocalSettings.Settings.LastLoginAccount;
			if (account.Contains("@"))
			{
				string[] array2 = account.Split('@');
				int num2 = (int)Math.Floor((double)array2[0].Length / 2.0);
				account = array2[0].Substring(0, num2) + new string('*', array2[0].Length - num2) + "@" + array2[1];
			}
			else if (account.Length >= 3)
			{
				int num3 = account.Length % 3;
				int num4 = account.Length / 3;
				switch (num3)
				{
				case 0:
					account = account.Substring(0, num4) + new string('*', num4) + account.Substring(account.Length - num4, num4);
					break;
				case 1:
					account = account.Substring(0, num4) + new string('*', num4) + account.Substring(account.Length - num4 - 1, num4 + 1);
					break;
				case 2:
					account = account.Substring(0, num4) + new string('*', num4 + 1) + account.Substring(account.Length - num4 - 1, num4 + 1);
					break;
				}
			}
		}
		switch (mode)
		{
		case ResetMode.Unlock:
			base.Title = Utils.GetString("UnlockTickTick");
			ResetHint.Text = string.Format(Utils.GetString("ResetPasswordHint"), account);
			break;
		case ResetMode.Modify:
			base.Title = Utils.GetString("ChangePassword");
			ResetHint.Text = string.Format(Utils.GetString("ModifyResetPassword"), account);
			break;
		case ResetMode.Clear:
			base.Title = Utils.GetString("DisablePassword");
			ResetHint.Text = string.Format(Utils.GetString("ClearResetPassword"), account);
			break;
		}
	}

	private void OnPasswordChanged(object sender, RoutedEventArgs e)
	{
		PasswordInvalidText.Visibility = Visibility.Collapsed;
		SetSaveMode((!string.IsNullOrEmpty(PasswordText.Password)) ? SaveMode.Normal : SaveMode.Disabled);
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		if (!Utils.IsNetworkAvailable())
		{
			Utils.Toast(Utils.GetString("NoNetwork"));
			return;
		}
		SetSaveMode(SaveMode.Verifing);
		string password = PasswordText.Password;
		if (string.IsNullOrEmpty(password))
		{
			return;
		}
		string username = LocalSettings.Settings.LoginUserName;
		try
		{
			UserModel userModel = await Communicator.SignOn(username, password);
			if (userModel == null || !(userModel.username == username))
			{
				throw new Exception();
			}
			await AppLockDao.ClearPassword();
			Close();
			if (_resetMode != ResetMode.Clear)
			{
				new SetPasswordWindow(SetPasswordMode.Reset).ShowDialog();
				await App.Unlock();
				App.ShowMainWindow(string.Empty, string.Empty, skiplock: true);
			}
		}
		catch (Exception)
		{
			SetSaveMode(SaveMode.Normal);
			PasswordInvalidText.Visibility = Visibility.Visible;
			PasswordText.Focus();
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnForgetPasswordClick(object sender, MouseButtonEventArgs e)
	{
		Close();
		new ForgetPasswordWindow(thirdPartyUser: false).ShowDialog();
	}

	private void SetSaveMode(SaveMode mode)
	{
		SaveButton.IsEnabled = mode == SaveMode.Normal;
		switch (mode)
		{
		case SaveMode.Normal:
			SaveButton.Opacity = 1.0;
			SaveButton.Content = Utils.GetString("Confirm");
			PasswordText.IsEnabled = true;
			break;
		case SaveMode.Disabled:
			SaveButton.Opacity = 0.36000001430511475;
			SaveButton.Content = Utils.GetString("Confirm");
			PasswordText.IsEnabled = true;
			break;
		default:
			SaveButton.Opacity = 0.36000001430511475;
			SaveButton.Content = Utils.GetString("Verifying");
			PasswordText.IsEnabled = false;
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
			Uri resourceLocator = new Uri("/TickTick;component/views/lock/resetpasswordwindow.xaml", UriKind.Relative);
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
			ResetHint = (TextBlock)target;
			break;
		case 2:
			PasswordText = (PasswordBox)target;
			PasswordText.PasswordChanged += OnPasswordChanged;
			break;
		case 3:
			PasswordInvalidText = (TextBlock)target;
			break;
		case 4:
			((TextBlock)target).MouseLeftButtonUp += OnForgetPasswordClick;
			break;
		case 5:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 6:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
