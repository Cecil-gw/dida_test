using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Setting;

public class MiniCalendarConfig : UserControl, IComponentConnector
{
	internal CheckBox MiniCalendarSwitch;

	private bool _contentLoaded;

	public MiniCalendarConfig()
	{
		InitializeComponent();
		MiniCalendarSwitch.IsChecked = LocalSettings.Settings.MiniCalendarEnabled;
	}

	private void MiniCalendarSwitchChanged(object sender, RoutedEventArgs e)
	{
		if (!UserDao.IsPro())
		{
			ProChecker.CheckPro(ProType.MiniCalendar, Window.GetWindow(this));
			Mouse.Capture(null);
			e.Handled = true;
		}
		else
		{
			NotifyCheckBoxChanged(sender, e);
			LocalSettings.Settings.MiniCalendarEnabled = MiniCalendarSwitch.IsChecked == true;
			LocalSettings.Settings.Save();
		}
	}

	private static void NotifyCheckBoxChanged(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		CheckBox obj = (CheckBox)sender;
		CheckBox obj2 = (CheckBox)sender;
		obj.IsChecked = ((obj2 == null) ? ((bool?)null) : (!obj2.IsChecked));
		e.Handled = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/setting/minicalendarconfig.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			MiniCalendarSwitch = (CheckBox)target;
			MiniCalendarSwitch.PreviewMouseLeftButtonUp += MiniCalendarSwitchChanged;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
