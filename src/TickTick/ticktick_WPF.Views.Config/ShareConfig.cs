using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.Views.Team;

namespace ticktick_WPF.Views.Config;

public class ShareConfig : UserControl, IComponentConnector
{
	internal CheckBox AutoAcceptCheckbox;

	internal TeamNotification NotificationSetting;

	private bool _contentLoaded;

	public ShareConfig()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
		InitCheckBox();
		InitCheckEvent();
		AutoAcceptCheckbox.IsChecked = LocalSettings.Settings.AutoAcceptShare;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
	}

	private void InitCheckBox()
	{
		string[] array = LocalSettings.Settings.NotificationOptions?.Split(';');
		if (array != null && array.Length != 0)
		{
			NotificationSetting.DoOrUndo.IsChecked = array.Contains("DONE") || array.Contains("UNDONE");
			NotificationSetting.Create.IsChecked = array.Contains("CREATE");
			NotificationSetting.Delete.IsChecked = array.Contains("DELETE") || array.Contains("MOVE_OUT");
		}
	}

	private void InitCheckEvent()
	{
		NotificationSetting.DoCheck += OnDoCheck;
		NotificationSetting.CreateCheck += OnCreateCheck;
		NotificationSetting.DelCheck += OnDelCheck;
	}

	private void OnDelCheck(object sender, bool Checked)
	{
		List<string> list = LocalSettings.Settings.NotificationOptions.Split(';').ToList();
		list.Remove("");
		list.Remove("DELETE");
		list.Remove("MOVE_OUT");
		if (Checked)
		{
			list.Add("DELETE");
			list.Add("MOVE_OUT");
		}
		else
		{
			list.Remove("DELETE");
			list.Remove("MOVE_OUT");
		}
		SaveReminderConfig(list);
	}

	private void OnCreateCheck(object sender, bool Checked)
	{
		List<string> list = LocalSettings.Settings.NotificationOptions.Split(';').ToList();
		list.Remove("");
		list.Remove("CREATE");
		if (Checked)
		{
			list.Add("CREATE");
		}
		else
		{
			list.Remove("CREATE");
		}
		SaveReminderConfig(list);
	}

	private void OnDoCheck(object sender, bool Checked)
	{
		List<string> list = LocalSettings.Settings.NotificationOptions.Split(';').ToList();
		list.Remove("");
		list.Remove("DONE");
		list.Remove("UNDONE");
		if (Checked)
		{
			list.Add("DONE");
			list.Add("UNDONE");
		}
		else
		{
			list.Remove("DONE");
			list.Remove("UNDONE");
		}
		SaveReminderConfig(list);
	}

	private void SaveReminderConfig(List<string> notificationOptions)
	{
		LocalSettings.Settings.NotificationOptions = notificationOptions.ToArray().Join(";");
		LocalSettings.Settings.Save();
		Utils.FindParent<SettingDialog>(this)?.SetChanged();
	}

	private void AutoAcceptClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		AutoAcceptCheckbox.IsChecked = AutoAcceptCheckbox.IsChecked != true;
		LocalSettings.Settings.SetAutoAcceptShare(AutoAcceptCheckbox.IsChecked == true);
		SettingsHelper.PushLocalPreference();
		UserActCollectUtils.AddClickEvent("collaborate", "auto_accept_invites", (AutoAcceptCheckbox.IsChecked == true) ? "enable" : "disable");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/shareconfig.xaml", UriKind.Relative);
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
			AutoAcceptCheckbox = (CheckBox)target;
			AutoAcceptCheckbox.PreviewMouseLeftButtonUp += AutoAcceptClick;
			break;
		case 2:
			NotificationSetting = (TeamNotification)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
