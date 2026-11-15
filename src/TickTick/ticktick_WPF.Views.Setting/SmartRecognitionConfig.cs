using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Setting;

public class SmartRecognitionConfig : UserControl, IComponentConnector
{
	internal CheckBox DateToggleCheckbox;

	internal CheckBox RemoveDateTextCheckbox;

	internal CheckBox RemoteTagTextCheckbox;

	internal CheckBox UrlParseCheckbox;

	private bool _contentLoaded;

	public SmartRecognitionConfig()
	{
		InitializeComponent();
		InitKeepTags();
		InitDateParsingSettings();
		InitParseUrl();
	}

	private void RemoveDateTextChecked(object sender, RoutedEventArgs e)
	{
		LocalSettings.Settings.RemoveTimeText = RemoveDateTextCheckbox.IsChecked ?? true;
		LocalSettings.Settings.Save();
		Utils.FindParent<SettingDialog>(this)?.SetChanged();
	}

	private void DateParsingClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			checkBox.IsChecked = !checkBox.IsChecked;
			LocalSettings.Settings.DateParsing = checkBox.IsChecked ?? true;
			LocalSettings.Settings.Save();
		}
		RemoveDateTextCheckbox.Visibility = ((!LocalSettings.Settings.DateParsing) ? Visibility.Collapsed : Visibility.Visible);
		Utils.FindParent<SettingDialog>(this)?.SetChanged();
	}

	private void RemoveDateTextClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			checkBox.IsChecked = !checkBox.IsChecked;
			LocalSettings.Settings.RemoveTimeText = checkBox.IsChecked ?? true;
			LocalSettings.Settings.Save();
		}
		Utils.FindParent<SettingDialog>(this)?.SetChanged();
	}

	private void InitDateParsingSettings()
	{
		RemoveDateTextCheckbox.IsChecked = LocalSettings.Settings.RemoveTimeText;
		DateToggleCheckbox.IsChecked = LocalSettings.Settings.DateParsing;
		RemoveDateTextCheckbox.Visibility = ((!LocalSettings.Settings.DateParsing) ? Visibility.Collapsed : Visibility.Visible);
		RemoveDateTextCheckbox.Checked += RemoveDateTextChecked;
		RemoveDateTextCheckbox.Unchecked += RemoveDateTextChecked;
	}

	private void InitKeepTags()
	{
		RemoteTagTextCheckbox.IsChecked = !LocalSettings.Settings.KeepTagsInText;
	}

	private void RemoveTagTextClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			checkBox.IsChecked = !checkBox.IsChecked;
			LocalSettings.Settings.KeepTagsInText = checkBox.IsChecked != true;
			LocalSettings.Settings.Save();
		}
		Utils.FindParent<SettingDialog>(this)?.SetChanged();
	}

	private void InitParseUrl()
	{
		UrlParseCheckbox.IsChecked = LocalSettings.Settings.UserPreference?.GeneralConfig?.urlParseEnabled ?? true;
	}

	private void UrlParseClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			checkBox.IsChecked = !checkBox.IsChecked;
			LocalSettings.Settings.SetUrlParseEnable(checkBox.IsChecked == true);
			LocalSettings.Settings.Save();
		}
		SettingsHelper.PushLocalPreference();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/setting/smartrecognitionconfig.xaml", UriKind.Relative);
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
			DateToggleCheckbox = (CheckBox)target;
			DateToggleCheckbox.PreviewMouseLeftButtonUp += DateParsingClick;
			break;
		case 2:
			RemoveDateTextCheckbox = (CheckBox)target;
			RemoveDateTextCheckbox.PreviewMouseLeftButtonUp += RemoveDateTextClick;
			break;
		case 3:
			RemoteTagTextCheckbox = (CheckBox)target;
			RemoteTagTextCheckbox.PreviewMouseLeftButtonUp += RemoveTagTextClick;
			break;
		case 4:
			UrlParseCheckbox = (CheckBox)target;
			UrlParseCheckbox.PreviewMouseLeftButtonUp += UrlParseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
