using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Subscribe;

public class TelegramAccountWindow : MyWindow, IComponentConnector
{
	private const string UserHasBound = "user_has_bound";

	private bool _isConnecting;

	internal Border DisplayImage;

	internal Button ConnectButton;

	internal Button CloseButton;

	private bool _contentLoaded;

	public TelegramAccountWindow()
	{
		InitializeComponent();
		DisplayImage.Background = new ImageBrush
		{
			ImageSource = ImageUtils.GetResourceImage("pack://application:,,,/Assets/ImageSource/TelegramAccount.png"),
			Stretch = Stretch.UniformToFill
		};
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!CloseButton.IsMouseOver && !ConnectButton.IsMouseOver && e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
			e.Handled = false;
		}
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void OnConnectClick(object sender, RoutedEventArgs e)
	{
		if (_isConnecting)
		{
			return;
		}
		_isConnecting = true;
		ConnectButton.IsEnabled = false;
		ConnectButton.Content = Utils.GetString("Connecting");
		try
		{
			var (telegramBindCodeDto, text) = await Communicator.GetTelegramBindCode();
			if (!string.IsNullOrWhiteSpace(telegramBindCodeDto?.url))
			{
				Utils.TryProcessStartUrl(telegramBindCodeDto.url);
				Close();
			}
			else if (text == "user_has_bound")
			{
				Toast(Utils.GetString("AccountHasIntegrated"));
			}
			else
			{
				Toast(Utils.GetString("NetworkException"));
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			Toast(Utils.GetString("NetworkException"));
		}
		finally
		{
			_isConnecting = false;
			ConnectButton.IsEnabled = true;
			ConnectButton.Content = Utils.GetString("Connect");
		}
	}

	private void Toast(string message)
	{
		if (!SettingDialog.TryToast(SettingsType.Calendar, message))
		{
			Utils.Toast(message);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/subscribe/telegramaccountwindow.xaml", UriKind.Relative);
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
			DisplayImage = (Border)target;
			break;
		case 2:
			ConnectButton = (Button)target;
			ConnectButton.Click += OnConnectClick;
			break;
		case 3:
			CloseButton = (Button)target;
			CloseButton.Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
