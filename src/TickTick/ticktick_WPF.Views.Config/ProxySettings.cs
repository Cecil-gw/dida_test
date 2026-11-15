using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Network;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Config;

public class ProxySettings : UserControl, IComponentConnector
{
	private ProxyModel testFailProxyModel;

	public static readonly DependencyProperty WindowModeProperty = DependencyProperty.Register("WindowMode", typeof(bool), typeof(ProxySettings), new PropertyMetadata(true, null));

	internal ProxySettings RootView;

	internal CustomSimpleComboBox ProxyTypeCombox;

	internal TextBox AddressText;

	internal TextBox PortText;

	internal TextBox UserText;

	internal TextBlock PasswordHint;

	internal PasswordBox PasswordText;

	internal TextBox DomainText;

	internal Button SaveButton;

	internal Button TestButton;

	private bool _contentLoaded;

	public bool WindowMode
	{
		get
		{
			return (bool)GetValue(WindowModeProperty);
		}
		set
		{
			SetValue(WindowModeProperty, value);
		}
	}

	public event EventHandler Cancel;

	public event EventHandler Save;

	public ProxySettings()
	{
		InitializeComponent();
		InitData();
	}

	private void InitData()
	{
		LocalSettings settings = LocalSettings.Settings;
		ProxyTypeCombox.SelectedIndex = settings.ProxyType;
		ProxyTypeCombox.ItemsSource = new List<string>
		{
			Utils.GetString("NoProxy"),
			Utils.GetString("HttpProxy"),
			Utils.GetString("BrowserSettings")
		};
		AddressText.Text = settings.ProxyAddress;
		PortText.Text = settings.ProxyPort;
		UserText.Text = settings.ProxyUsername;
		if (!string.IsNullOrEmpty(settings.ProxyPassword))
		{
			PasswordText.Password = Utils.Base64Decode(settings.ProxyPassword);
			PasswordHint.Visibility = Visibility.Collapsed;
		}
		else
		{
			PasswordHint.Visibility = Visibility.Visible;
		}
		if (WindowMode)
		{
			SaveButton.Visibility = Visibility.Visible;
		}
		SetEnabled();
	}

	private void OnProxyTypeChanged(object sender, SimpleComboBoxViewModel e)
	{
		SetEnabled();
	}

	private void SetEnabled()
	{
		SetTextEnabled(ProxyTypeCombox.SelectedIndex == 1);
		SetTestEnabled(ProxyTypeCombox.SelectedIndex == 1 || ProxyTypeCombox.SelectedIndex == 2);
	}

	private void SetTestEnabled(bool enabled)
	{
		TestButton.IsEnabled = enabled;
	}

	private async void OnTestClick(object sender, RoutedEventArgs e)
	{
		UIElement ui = sender as UIElement;
		if (ui == null)
		{
			return;
		}
		ui.IsEnabled = false;
		try
		{
			testFailProxyModel = GetProxyModel();
			if (testFailProxyModel.proxy == null && (ProxyTypeCombox.SelectedIndex == 1 || ProxyTypeCombox.SelectedIndex == 2))
			{
				MessageBox.Show(Utils.GetString("NetworkFailed"));
				return;
			}
			await ApiClient.TestProxy(testFailProxyModel);
			testFailProxyModel = null;
			MessageBox.Show(Utils.GetString("NetworkOk"));
			SaveProxy();
		}
		catch (Exception)
		{
			MessageBox.Show(Utils.GetString("NetworkFailed"));
		}
		finally
		{
			ui.Dispatcher.Invoke(() => ui.IsEnabled = true);
		}
	}

	private ProxyModel GetProxyModel()
	{
		return ProxyModel.Create(ProxyTypeCombox.SelectedIndex, AddressText.Text.Trim(), PortText.Text.Trim(), UserText.Text.Trim(), PasswordText.Password.Trim(), DomainText.Text.Trim());
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		SaveProxy();
		this.Save?.Invoke(this, null);
	}

	public void SaveProxy()
	{
		if (!GetProxyModel().Equals(testFailProxyModel) || SaveButton.IsEnabled)
		{
			LocalSettings settings = LocalSettings.Settings;
			settings.ProxyType = ProxyTypeCombox.SelectedIndex;
			settings.ProxyAddress = AddressText.Text.Trim();
			settings.ProxyPort = PortText.Text.Trim();
			settings.ProxyUsername = UserText.Text.Trim();
			if (!string.IsNullOrEmpty(PasswordText.Password))
			{
				settings.ProxyPassword = Utils.Base64Encode(PasswordText.Password.Trim());
			}
		}
	}

	private void SetTextEnabled(bool enabled)
	{
		AddressText.IsEnabled = enabled;
		PortText.IsEnabled = enabled;
		UserText.IsEnabled = enabled;
		PasswordText.IsEnabled = enabled;
		DomainText.IsEnabled = enabled;
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		this.Cancel?.Invoke(this, null);
	}

	private void OnPasswordChanged(object sender, RoutedEventArgs e)
	{
		PasswordHint.Visibility = ((!string.IsNullOrEmpty(PasswordText.Password)) ? Visibility.Collapsed : Visibility.Visible);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/proxysettings.xaml", UriKind.Relative);
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
			RootView = (ProxySettings)target;
			break;
		case 2:
			ProxyTypeCombox = (CustomSimpleComboBox)target;
			break;
		case 3:
			AddressText = (TextBox)target;
			break;
		case 4:
			PortText = (TextBox)target;
			break;
		case 5:
			UserText = (TextBox)target;
			break;
		case 6:
			PasswordHint = (TextBlock)target;
			break;
		case 7:
			PasswordText = (PasswordBox)target;
			PasswordText.PasswordChanged += OnPasswordChanged;
			break;
		case 8:
			DomainText = (TextBox)target;
			break;
		case 9:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 10:
			TestButton = (Button)target;
			TestButton.Click += OnTestClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
