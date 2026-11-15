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

namespace ticktick_WPF.Views.Kanban;

public class KanbanDisplaySettingWindow : Window, IComponentConnector
{
	internal Grid ColorGrid;

	internal CustomSimpleComboBox ShowDateByComboBox;

	internal CustomSimpleComboBox KanbanSizeComboBox;

	internal CheckBox ShowColumnColorCheckBox;

	internal CheckBox ShowInputBoxCheckBox;

	internal Grid ShowCheckItemGrid;

	internal CheckBox ShowCheckItemCheckBox;

	private bool _contentLoaded;

	public KanbanDisplaySettingWindow(bool showSetCheckItem)
	{
		InitializeComponent();
		if (!showSetCheckItem)
		{
			ShowCheckItemGrid.Visibility = Visibility.Collapsed;
		}
		InitSettings();
	}

	protected override void OnClosed(EventArgs e)
	{
		base.Owner.Activate();
		LocalSettings.Settings.Save();
		base.OnClosed(e);
	}

	private void InitSettings()
	{
		KanbanSizeComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("Small"),
			Utils.GetString("medium"),
			Utils.GetString("Large")
		};
		KanbanSizeComboBox.SelectedIndex = LocalSettings.Settings.ExtraSettings.KbSize;
		ShowInputBoxCheckBox.IsChecked = LocalSettings.Settings.ExtraSettings.KbShowAdd;
		ShowCheckItemCheckBox.IsChecked = LocalSettings.Settings.ShowSubtasks;
		ShowColumnColorCheckBox.IsChecked = LocalSettings.Settings.ExtraSettings.KbShowColumnColor;
		ShowDateByComboBox.Init(new List<string>
		{
			Utils.GetString("task_time"),
			Utils.GetString("Countdown")
		});
		ShowDateByComboBox.SelectedIndex = (LocalSettings.Settings.ShowCountDown ? 1 : 0);
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnKanbanSizeChanged(object sender, SimpleComboBoxViewModel e)
	{
		LocalSettings.Settings.ExtraSettings.KbSize = Math.Max(0, KanbanSizeComboBox.SelectedIndex);
		LocalSettings.Settings.SetChanged();
		ResourceUtils.SetKanbanColumnWidth();
		LocalSettings.Settings.NotifyPropertyChanged("KbSize");
	}

	private void ShowInputBoxClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		Mouse.Capture(null);
		LocalSettings.Settings.ExtraSettings.KbShowAdd = !LocalSettings.Settings.ExtraSettings.KbShowAdd;
		ShowInputBoxCheckBox.IsChecked = LocalSettings.Settings.ExtraSettings.KbShowAdd;
		LocalSettings.Settings.NotifyPropertyChanged("KbShowAdd");
	}

	private void ShowColumnColorClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		Mouse.Capture(null);
		LocalSettings.Settings.ExtraSettings.KbShowColumnColor = !LocalSettings.Settings.ExtraSettings.KbShowColumnColor;
		ShowColumnColorCheckBox.IsChecked = LocalSettings.Settings.ExtraSettings.KbShowColumnColor;
		LocalSettings.Settings.NotifyPropertyChanged("KbShowColumnColor");
	}

	private void ShowCheckItemClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		Mouse.Capture(null);
		LocalSettings.Settings.ShowSubtasks = !LocalSettings.Settings.ShowSubtasks;
		ShowCheckItemCheckBox.IsChecked = LocalSettings.Settings.ShowSubtasks;
		SettingsHelper.PushLocalSettings();
		UserActCollectUtils.AddClickEvent("tasklist", "om", "show_hide_checklist");
	}

	private void OnShowDateBySelected(object sender, SimpleComboBoxViewModel e)
	{
		UserActCollectUtils.AddClickEvent("tasklist", "om", "show_hide_countdown");
		LocalSettings.Settings.ShowCountDown = ShowDateByComboBox.SelectedIndex == 1;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/kanban/kanbandisplaysettingwindow.xaml", UriKind.Relative);
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
			ColorGrid = (Grid)target;
			break;
		case 2:
			ShowDateByComboBox = (CustomSimpleComboBox)target;
			break;
		case 3:
			KanbanSizeComboBox = (CustomSimpleComboBox)target;
			break;
		case 4:
			ShowColumnColorCheckBox = (CheckBox)target;
			ShowColumnColorCheckBox.PreviewMouseLeftButtonUp += ShowColumnColorClick;
			break;
		case 5:
			ShowInputBoxCheckBox = (CheckBox)target;
			ShowInputBoxCheckBox.PreviewMouseLeftButtonUp += ShowInputBoxClick;
			break;
		case 6:
			ShowCheckItemGrid = (Grid)target;
			break;
		case 7:
			ShowCheckItemCheckBox = (CheckBox)target;
			ShowCheckItemCheckBox.PreviewMouseLeftButtonUp += ShowCheckItemClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
