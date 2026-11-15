using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Lock;

public class ForgetPasswordWindow : Window, IComponentConnector
{
	private readonly bool _thirdPartyUser;

	internal TextBlock ResetHint;

	private bool _contentLoaded;

	public ForgetPasswordWindow(bool thirdPartyUser)
	{
		InitializeComponent();
		string text = (thirdPartyUser ? string.Format(Utils.GetString("ForgetPasswordHint"), GetLoginType(LocalSettings.Settings.AccountType)) : Utils.GetString("ForgetAndResetPasswordHint"));
		ResetHint.Text = text;
		_thirdPartyUser = thirdPartyUser;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		Hide();
		if (!_thirdPartyUser)
		{
			Utils.ResetPassword(LocalSettings.Settings.LoginUserName);
		}
		LocalSettings.Settings.NeedResetPassword = true;
		LocalSettings.Settings.NeedResetUserId = LocalSettings.Settings.LoginUserId;
		await AppLockDao.ClearPassword();
		await App.Window.ForceLogout();
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private static string GetLoginType(string key)
	{
		return key switch
		{
			"facebook" => "Facebook", 
			"google" => "Google", 
			"twitter" => "Twitter", 
			"wechat" => Utils.GetString("Wechat"), 
			"weibo" => Utils.GetString("Weibo"), 
			"tecent" => "QQ", 
			_ => string.Empty, 
		};
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/lock/forgetpasswordwindow.xaml", UriKind.Relative);
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
			((Button)target).Click += OnSaveClick;
			break;
		case 3:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
