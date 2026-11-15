using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Lock;

public class SetPasswordWindow : Window, IComponentConnector
{
	private readonly SetPasswordMode _mode;

	private Func<Task> _saveHandler;

	internal TextBlock LockHint;

	internal Grid OldPasswordGrid;

	internal PasswordBox OldPasswordText;

	internal TextBlock NewPasswordTitle;

	internal PasswordBox NewPasswordText;

	internal TextBlock PasswordInvalidText;

	internal TextBlock ForgetPasswordText;

	internal Button SaveButton;

	private bool _contentLoaded;

	public SetPasswordWindow(SetPasswordMode mode)
	{
		InitializeComponent();
		_mode = mode;
		InitData();
	}

	private void InitData()
	{
		switch (_mode)
		{
		case SetPasswordMode.Init:
			base.Title = Utils.GetString("SetPassword");
			LockHint.Visibility = Visibility.Collapsed;
			HideOldPassword();
			_saveHandler = SavePassword;
			NewPasswordText.PasswordChanged += OnPasswordChanged;
			break;
		case SetPasswordMode.FirstTry:
			base.Title = Utils.GetString("SetPassword");
			LockHint.Text = Utils.GetString("TrySetPasswordHint");
			HideOldPassword();
			_saveHandler = SavePassword;
			NewPasswordText.PasswordChanged += OnPasswordChanged;
			break;
		case SetPasswordMode.Modify:
			base.Title = Utils.GetString("ChangePassword");
			ForgetPasswordText.Visibility = Visibility.Visible;
			LockHint.Visibility = Visibility.Collapsed;
			_saveHandler = ChangePassword;
			OldPasswordText.PasswordChanged += OnPasswordChanged;
			break;
		case SetPasswordMode.Reset:
			base.Title = Utils.GetString("SetPassword");
			LockHint.Text = Utils.GetString("ClearResetPasswordHint");
			HideOldPassword();
			_saveHandler = SavePassword;
			NewPasswordText.PasswordChanged += OnPasswordChanged;
			break;
		case SetPasswordMode.Clear:
			base.Title = Utils.GetString("DisablePassword");
			ForgetPasswordText.Visibility = Visibility.Visible;
			LockHint.Text = Utils.GetString("ClearPasswordHint");
			PasswordInvalidText.Text = Utils.GetString("IncorrectUnlockPassword");
			HideOldPassword();
			NewPasswordTitle.Visibility = Visibility.Collapsed;
			_saveHandler = ClearPassword;
			NewPasswordText.PasswordChanged += OnPasswordChanged;
			break;
		}
	}

	private void HideOldPassword()
	{
		OldPasswordGrid.Visibility = Visibility.Collapsed;
		NewPasswordText.Focus();
	}

	private async void OnPasswordKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Return:
			await SaveAction();
			break;
		case Key.Escape:
			Close();
			break;
		}
	}

	private async Task SaveAction()
	{
		if (_saveHandler != null)
		{
			await (_saveHandler?.Invoke());
		}
	}

	private async Task ClearPassword()
	{
		string obj = await AppLockCache.GetLockPassword();
		string password = NewPasswordText.Password;
		if (obj != password)
		{
			PasswordInvalidText.Visibility = Visibility.Visible;
			OldPasswordText.Focus();
			OldPasswordText.SelectAll();
		}
		else
		{
			await AppLockDao.ClearPassword();
			Close();
		}
	}

	private async Task ChangePassword()
	{
		string obj = await AppLockCache.GetLockPassword();
		string password = OldPasswordText.Password;
		if (obj != password)
		{
			PasswordInvalidText.Visibility = Visibility.Visible;
			OldPasswordText.Focus();
			OldPasswordText.SelectAll();
		}
		else if (string.IsNullOrEmpty(NewPasswordText.Password))
		{
			await AppLockDao.ClearPassword();
			Close();
		}
		else
		{
			await SavePassword();
		}
	}

	private async Task SavePassword()
	{
		await AppLockDao.SaveLockPassword(NewPasswordText.Password);
		Close();
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

	private void OnPasswordChanged(object sender, RoutedEventArgs e)
	{
		PasswordInvalidText.Visibility = Visibility.Collapsed;
		string value = (sender as PasswordBox)?.Password;
		SaveButton.IsEnabled = !string.IsNullOrEmpty(value);
	}

	private void OnForgetPasswordClick(object sender, MouseButtonEventArgs e)
	{
		Hide();
		if (!(LocalSettings.Settings.AccountType != "mail_account"))
		{
			new ResetPasswordWindow((_mode != SetPasswordMode.Clear) ? ResetMode.Modify : ResetMode.Clear).ShowDialog();
		}
		else
		{
			new ForgetPasswordWindow(thirdPartyUser: true).ShowDialog();
		}
		Close();
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		await SaveAction();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/lock/setpasswordwindow.xaml", UriKind.Relative);
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
			LockHint = (TextBlock)target;
			break;
		case 2:
			OldPasswordGrid = (Grid)target;
			break;
		case 3:
			OldPasswordText = (PasswordBox)target;
			break;
		case 4:
			NewPasswordTitle = (TextBlock)target;
			break;
		case 5:
			NewPasswordText = (PasswordBox)target;
			NewPasswordText.KeyDown += OnPasswordKeyDown;
			NewPasswordText.PasswordChanged += OnPasswordChanged;
			break;
		case 6:
			PasswordInvalidText = (TextBlock)target;
			break;
		case 7:
			ForgetPasswordText = (TextBlock)target;
			ForgetPasswordText.MouseLeftButtonUp += OnForgetPasswordClick;
			break;
		case 8:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 9:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
