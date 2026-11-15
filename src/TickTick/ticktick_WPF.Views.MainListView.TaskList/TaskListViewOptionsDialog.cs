using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.MainListView.TaskList;

public class TaskListViewOptionsDialog : Window, IComponentConnector
{
	internal Grid ColorGrid;

	internal CustomSimpleComboBox ShowDateByComboBox;

	internal Image SideImage;

	internal Image PopupImage;

	internal Grid SideSelect;

	internal Grid PopupSelect;

	private bool _contentLoaded;

	public TaskListViewOptionsDialog()
	{
		InitializeComponent();
		string text = (ThemeUtil.IsDark() ? "dark" : "light");
		SideImage.Source = ImageUtils.GetResourceImage("pack://application:,,,/Assets/ImageSource/" + text + "/TaskList_Side.png");
		PopupImage.Source = ImageUtils.GetResourceImage("pack://application:,,,/Assets/ImageSource/" + text + "/TaskList_Popup.png");
		ShowDateByComboBox.Init(new List<string>
		{
			Utils.GetString("task_time"),
			Utils.GetString("Countdown")
		});
		ShowDateByComboBox.SelectedIndex = (LocalSettings.Settings.ShowCountDown ? 1 : 0);
		SideSelect.Visibility = ((!(LocalSettings.Settings.ExtraSettings.TaskDetailLayout != "Popup")) ? Visibility.Collapsed : Visibility.Visible);
		PopupSelect.Visibility = ((!(LocalSettings.Settings.ExtraSettings.TaskDetailLayout == "Popup")) ? Visibility.Collapsed : Visibility.Visible);
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnShowDateBySelected(object sender, SimpleComboBoxViewModel e)
	{
		UserActCollectUtils.AddClickEvent("tasklist", "om", "show_hide_countdown");
		LocalSettings.Settings.ShowCountDown = ShowDateByComboBox.SelectedIndex == 1;
	}

	private void OnPopupClick(object sender, MouseButtonEventArgs e)
	{
		LocalSettings.Settings.ExtraSettings.TaskDetailLayout = "Popup";
		SideSelect.Visibility = Visibility.Collapsed;
		PopupSelect.Visibility = Visibility.Visible;
		LocalSettings.Settings.Save(force: true);
	}

	private void OnSideClick(object sender, MouseButtonEventArgs e)
	{
		LocalSettings.Settings.ExtraSettings.TaskDetailLayout = "Side";
		SideSelect.Visibility = Visibility.Visible;
		PopupSelect.Visibility = Visibility.Collapsed;
		LocalSettings.Settings.Save(force: true);
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		base.Owner?.Activate();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/mainlistview/tasklist/tasklistviewoptionsdialog.xaml", UriKind.Relative);
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
			((TaskListViewOptionsDialog)target).Closing += Window_Closing;
			break;
		case 2:
			ColorGrid = (Grid)target;
			break;
		case 3:
			ShowDateByComboBox = (CustomSimpleComboBox)target;
			break;
		case 4:
			((Border)target).MouseLeftButtonUp += OnSideClick;
			break;
		case 5:
			SideImage = (Image)target;
			break;
		case 6:
			((Border)target).MouseLeftButtonUp += OnPopupClick;
			break;
		case 7:
			PopupImage = (Image)target;
			break;
		case 8:
			SideSelect = (Grid)target;
			break;
		case 9:
			PopupSelect = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
