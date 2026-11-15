using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Theme;

public class InterfaceStyleWindow : Window, IComponentConnector
{
	internal InterfaceStyleWindow Root;

	internal RadioButton FlatRadioButton;

	internal RadioButton CardRadioButton;

	private bool _contentLoaded;

	public bool UseLeftbarColor { get; set; }

	public bool IsDefaultTheme { get; set; }

	public InterfaceStyleWindow()
	{
		string themeId = LocalSettings.Settings.ThemeId;
		UseLeftbarColor = !ThemeKey.IsColorTheme(themeId) && themeId != "Dark" && themeId != "DarkBlue";
		IsDefaultTheme = themeId == "White";
		InitializeComponent();
		if (LocalSettings.Settings.ExtraSettings.InterfaceStyle == InterfaceStyle.Flat)
		{
			FlatRadioButton.IsChecked = true;
		}
		else
		{
			CardRadioButton.IsChecked = true;
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void ConfirmButton_Click(object sender, RoutedEventArgs e)
	{
		ApplyChange();
		Close();
	}

	private void ApplyChange()
	{
		LocalSettings.Settings.ExtraSettings.InterfaceStyle = ((CardRadioButton.IsChecked == true) ? InterfaceStyle.Card : InterfaceStyle.Flat);
		LocalSettings.Settings.Save();
		CardThemeDictService.ReloadInterfaceStyle();
		DataChangedNotifier.NotifyInterfaceStyleChanged();
	}

	private void FlatMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		FlatRadioButton.IsChecked = true;
		ApplyChange();
	}

	private void CardMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		CardRadioButton.IsChecked = true;
		ApplyChange();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/interfacestylewindow.xaml", UriKind.Relative);
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
			Root = (InterfaceStyleWindow)target;
			break;
		case 2:
			((StackPanel)target).MouseLeftButtonUp += FlatMouseLeftButtonUp;
			break;
		case 3:
			FlatRadioButton = (RadioButton)target;
			break;
		case 4:
			((StackPanel)target).MouseLeftButtonUp += CardMouseLeftButtonUp;
			break;
		case 5:
			CardRadioButton = (RadioButton)target;
			break;
		case 6:
			((Button)target).Click += ConfirmButton_Click;
			break;
		case 7:
			((Button)target).Click += OnCloseClick;
			break;
		case 8:
			((Button)target).Click += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
