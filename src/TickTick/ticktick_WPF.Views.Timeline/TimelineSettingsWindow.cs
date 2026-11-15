using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Timeline;

public class TimelineSettingsWindow : Window, IComponentConnector
{
	internal CustomSimpleComboBox TaskBlocksColorComboBox;

	internal CheckBox ShowWeekCheckBox;

	internal Grid ShowCheckItemGrid;

	internal CheckBox ShowCheckItemCheckBox;

	private bool _contentLoaded;

	public TimelineSettingsWindow(TimelineViewModel vm)
	{
		InitializeComponent();
		base.DataContext = vm;
		ShowWeekCheckBox.IsChecked = vm.ShowWeek;
		int colorType = (int)vm.ColorType;
		TaskBlocksColorComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("TimelineNoColor"),
			Utils.GetString("lists"),
			Utils.GetString("Tags"),
			Utils.GetString("priority")
		};
		TaskBlocksColorComboBox.SelectedIndex = colorType;
		if (!vm.CanShowCheckItem)
		{
			ShowCheckItemGrid.Visibility = Visibility.Collapsed;
		}
		ShowCheckItemCheckBox.IsChecked = LocalSettings.Settings.ShowSubtasks;
	}

	protected override void OnClosed(EventArgs e)
	{
		base.Owner.Activate();
		base.OnClosed(e);
		if (base.DataContext is TimelineViewModel timelineViewModel)
		{
			timelineViewModel.IsSetting = false;
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnTaskBlocksColorChanged(object sender, SimpleComboBoxViewModel e)
	{
		string data = "none";
		switch (TaskBlocksColorComboBox.SelectedIndex)
		{
		case 1:
			data = "list";
			break;
		case 2:
			data = "tag";
			break;
		case 3:
			data = "priority";
			break;
		}
		UserActCollectUtils.AddClickEvent("timeline", "view_options", data);
		if (base.DataContext is TimelineViewModel timelineViewModel)
		{
			timelineViewModel.ColorType = (TimelineColorType)TaskBlocksColorComboBox.SelectedIndex;
		}
	}

	private void ShowWeekClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		Mouse.Capture(null);
		if (base.DataContext is TimelineViewModel timelineViewModel)
		{
			timelineViewModel.ShowWeek = !timelineViewModel.ShowWeek;
			ShowWeekCheckBox.IsChecked = timelineViewModel.ShowWeek;
		}
	}

	private void ShowCheckItemClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		Mouse.Capture(null);
		UserActCollectUtils.AddClickEvent("timeline", "om", "show_hide_completed");
		LocalSettings.Settings.ShowSubtasks = !LocalSettings.Settings.ShowSubtasks;
		LocalSettings.Settings.Save();
		SettingsHelper.PushLocalSettings();
		ShowCheckItemCheckBox.IsChecked = LocalSettings.Settings.ShowSubtasks;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/timeline/timelinesettingswindow.xaml", UriKind.Relative);
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
			TaskBlocksColorComboBox = (CustomSimpleComboBox)target;
			break;
		case 2:
			ShowWeekCheckBox = (CheckBox)target;
			ShowWeekCheckBox.PreviewMouseLeftButtonUp += ShowWeekClick;
			break;
		case 3:
			ShowCheckItemGrid = (Grid)target;
			break;
		case 4:
			ShowCheckItemCheckBox = (CheckBox)target;
			ShowCheckItemCheckBox.PreviewMouseLeftButtonUp += ShowCheckItemClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
