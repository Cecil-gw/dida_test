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
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class VerifyInputWindow : Window, IComponentConnector
{
	internal StackPanel VerifyPasswordPanel;

	internal PasswordBox PasswordTextBox;

	internal TextBlock PasswordHintText;

	internal StackPanel VerifyTextPanel;

	internal TextBox DeleteTextBox;

	internal TextBlock ErrorTextblock;

	internal Border CloseButton;

	private bool _contentLoaded;

	private bool _hasPassword { get; set; }

	public VerifyInputWindow()
	{
		InitializeComponent();
		Init();
	}

	private async Task Init()
	{
		UserInfoModel userInfoModel = await UserManager.GetUserInfo();
		if (!userInfoModel.fakedEmail)
		{
			_hasPassword = true;
		}
		else
		{
			UserModel userModel = await UserDao.GetUserModelByName(userInfoModel.username);
			if (userModel != null)
			{
				_hasPassword = !string.IsNullOrEmpty(userModel.phone);
			}
		}
		if (_hasPassword)
		{
			VerifyPasswordPanel.Visibility = Visibility.Visible;
			VerifyTextPanel.Visibility = Visibility.Collapsed;
		}
		else
		{
			VerifyPasswordPanel.Visibility = Visibility.Collapsed;
			VerifyTextPanel.Visibility = Visibility.Visible;
		}
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		ErrorTextblock.Visibility = Visibility.Collapsed;
		Button button = sender as Button;
		button.IsEnabled = false;
		button.Content = Utils.GetString("Verifying");
		await Verify();
		button.IsEnabled = true;
		button.Content = Utils.GetString("Confirm");
	}

	private async Task Verify()
	{
		try
		{
			_ = string.Empty;
			if (_hasPassword)
			{
				if (string.IsNullOrEmpty(PasswordTextBox.Password))
				{
					return;
				}
			}
			else if (!(DeleteTextBox.Text == Utils.GetString("EnterDeleteFocusHint")))
			{
				ErrorTextblock.Visibility = Visibility.Visible;
				return;
			}
			if (await PomoService.DeleteAllPomoFocusRec())
			{
				Close();
			}
			else
			{
				ErrorTextblock.Visibility = Visibility.Visible;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message);
		}
	}

	private void PasswordTextBox_PasswordChanged(object sender, RoutedEventArgs e)
	{
		ErrorTextblock.Visibility = Visibility.Collapsed;
		PasswordHintText.Visibility = ((PasswordTextBox.Password.Length > 0) ? Visibility.Hidden : Visibility.Visible);
	}

	private void DeleteTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ErrorTextblock.Visibility = Visibility.Collapsed;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/focusstatistics/verifyinputwindow.xaml", UriKind.Relative);
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
			VerifyPasswordPanel = (StackPanel)target;
			break;
		case 2:
			PasswordTextBox = (PasswordBox)target;
			PasswordTextBox.PasswordChanged += PasswordTextBox_PasswordChanged;
			break;
		case 3:
			PasswordHintText = (TextBlock)target;
			break;
		case 4:
			VerifyTextPanel = (StackPanel)target;
			break;
		case 5:
			DeleteTextBox = (TextBox)target;
			DeleteTextBox.TextChanged += DeleteTextBox_TextChanged;
			break;
		case 6:
			ErrorTextblock = (TextBlock)target;
			break;
		case 7:
			((Button)target).Click += OnSaveClick;
			break;
		case 8:
			((Button)target).Click += OnCancelClick;
			break;
		case 9:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
