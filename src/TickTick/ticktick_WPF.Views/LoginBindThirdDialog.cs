using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using Newtonsoft.Json.Linq;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class LoginBindThirdDialog : Window, IOkCancelWindow, IComponentConnector
{
	private UserModel _user;

	internal Grid ContinueGrid;

	internal Grid InputGrid;

	private bool _contentLoaded;

	public LoginBindThirdDialog(UserModel user)
	{
		InitializeComponent();
		base.DataContext = new LoginBindThirdViewModel
		{
			Email = user.username,
			NotUploading = true
		};
		_user = user;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		if (GetTemplateChild("TitleGrid") is Grid grid)
		{
			grid.Visibility = Visibility.Collapsed;
		}
		base.OnApplyTemplate();
	}

	public void Ok()
	{
		if (InputGrid.IsVisible)
		{
			Save();
		}
		else if (ContinueGrid.IsVisible)
		{
			InputGrid.Visibility = Visibility.Visible;
			ContinueGrid.Visibility = Visibility.Collapsed;
		}
	}

	public async void Save()
	{
		object dataContext = base.DataContext;
		if (!(dataContext is LoginBindThirdViewModel model))
		{
			return;
		}
		model.NetworkBroken = false;
		model.PasswordInvaild = false;
		model.PasswordNotSame = false;
		model.NotUploading = false;
		if (string.IsNullOrEmpty(model.Password) || model.Password.Length < 6)
		{
			model.PasswordInvaild = true;
			model.NotUploading = true;
			return;
		}
		if (model.PasswordComfirm != model.Password)
		{
			model.PasswordNotSame = true;
			model.NotUploading = true;
			return;
		}
		string text = await Communicator.ThirdChangePassword(_user.code, model.Password, model.PasswordComfirm, _user.token);
		if (!string.IsNullOrEmpty(text))
		{
			JObject jObject = JObject.Parse(text);
			if (jObject != null && jObject["token"] != null)
			{
				_user.token = jObject["token"].ToString();
				Close();
				return;
			}
		}
		model.NetworkBroken = true;
		model.NotUploading = true;
	}

	public void OnCancel()
	{
		Close();
	}

	private void OnNewPasswordOkClick(object sender, RoutedEventArgs e)
	{
		Save();
	}

	private void OnNewPasswordCancelClick(object sender, RoutedEventArgs e)
	{
		OnCancel();
	}

	private void OnPasswordChanged(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is LoginBindThirdViewModel loginBindThirdViewModel && sender is PasswordBox passwordBox)
		{
			loginBindThirdViewModel.Password = passwordBox.Password;
			loginBindThirdViewModel.PasswordInvaild = false;
		}
	}

	private void OnPasswordComfirmChanged(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is LoginBindThirdViewModel loginBindThirdViewModel && sender is PasswordBox passwordBox)
		{
			loginBindThirdViewModel.PasswordComfirm = passwordBox.Password;
			loginBindThirdViewModel.PasswordNotSame = false;
		}
	}

	private void OnContinueOkClick(object sender, RoutedEventArgs e)
	{
		Ok();
	}

	private void OnContinueCancelClick(object sender, RoutedEventArgs e)
	{
		OnCancel();
	}

	private void CancelPathMouseLeftUp(object sender, MouseButtonEventArgs e)
	{
		OnCancel();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/loginbindthirddialog.xaml", UriKind.Relative);
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
			ContinueGrid = (Grid)target;
			break;
		case 2:
			((Border)target).MouseLeftButtonUp += CancelPathMouseLeftUp;
			break;
		case 3:
			((Button)target).Click += OnContinueCancelClick;
			break;
		case 4:
			((Button)target).Click += OnContinueOkClick;
			break;
		case 5:
			InputGrid = (Grid)target;
			break;
		case 6:
			((Path)target).MouseLeftButtonUp += CancelPathMouseLeftUp;
			break;
		case 7:
			((PasswordBox)target).PasswordChanged += OnPasswordChanged;
			break;
		case 8:
			((PasswordBox)target).PasswordChanged += OnPasswordComfirmChanged;
			break;
		case 9:
			((Button)target).Click += OnNewPasswordCancelClick;
			break;
		case 10:
			((Button)target).Click += OnNewPasswordOkClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
