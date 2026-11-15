using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Lock;

public class AppUnlockWindow : Window, IComponentConnector
{
	private static AppUnlockWindow _unlockWindow;

	internal PasswordBox PasswordText;

	internal TextBlock PasswordInvalidText;

	internal Button SaveButton;

	private bool _contentLoaded;

	public event EventHandler OnUnlock;

	public AppUnlockWindow()
	{
		InitializeComponent();
		base.Activated += OnActivated;
		base.Deactivated += OnDeactivated;
	}

	private void OnDeactivated(object sender, EventArgs e)
	{
		Keyboard.Focus(null);
	}

	private async void OnActivated(object sender, EventArgs e)
	{
		await Task.Delay(100);
		PasswordText.Focus();
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		TryUnlockApp();
	}

	private async void TryUnlockApp()
	{
		if (!string.IsNullOrEmpty(PasswordText.Password))
		{
			string text = await AppLockCache.GetLockPassword();
			if (PasswordText.Password == text)
			{
				UtilLog.Info("TryUnlockApp success");
				await App.Unlock();
				this.OnUnlock?.Invoke(this, null);
				Close();
			}
			else
			{
				PasswordInvalidText.Visibility = Visibility.Visible;
				PasswordText.SelectAll();
			}
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnPasswordKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Return:
			TryUnlockApp();
			break;
		case Key.Escape:
			Close();
			break;
		}
	}

	private void OnPasswordChanged(object sender, RoutedEventArgs e)
	{
		PasswordInvalidText.Visibility = Visibility.Collapsed;
		SaveButton.IsEnabled = PasswordText.Password.Length > 0;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async void OnForgetPasswordClick(object sender, MouseButtonEventArgs e)
	{
		Hide();
		if (!(LocalSettings.Settings.AccountType != "mail_account"))
		{
			new ResetPasswordWindow(ResetMode.Unlock).ShowDialog();
		}
		else
		{
			new ForgetPasswordWindow(thirdPartyUser: true).ShowDialog();
		}
		Close();
	}

	public static async void TryUnlockApp(Action handler)
	{
		if (_unlockWindow == null)
		{
			_unlockWindow = new AppUnlockWindow();
			_unlockWindow.Closed += OnUnlockClosed;
			_unlockWindow.OnUnlock += delegate
			{
				handler?.Invoke();
			};
		}
		try
		{
			await Task.Delay(200);
			_unlockWindow.Show();
			_unlockWindow.Activate();
			await Task.Delay(1000);
			_unlockWindow?.PasswordText?.Focus();
		}
		catch (Exception)
		{
		}
	}

	private static void OnUnlockClosed(object sender, EventArgs e)
	{
		_unlockWindow = null;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/lock/appunlockwindow.xaml", UriKind.Relative);
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
			PasswordText = (PasswordBox)target;
			PasswordText.KeyDown += OnPasswordKeyDown;
			PasswordText.PasswordChanged += OnPasswordChanged;
			break;
		case 2:
			PasswordInvalidText = (TextBlock)target;
			break;
		case 3:
			((TextBlock)target).MouseLeftButtonUp += OnForgetPasswordClick;
			break;
		case 4:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 5:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
