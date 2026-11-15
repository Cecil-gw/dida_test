using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Habit;

public class HabitSettingWindow : Window, IComponentConnector
{
	internal StackPanel HabitConfig;

	internal CheckBox ShowInTodCheckBox;

	private bool _contentLoaded;

	public HabitSettingWindow()
	{
		InitializeComponent();
		base.InputBindings.Add(new KeyBinding(WindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		base.Closing += delegate
		{
			base.Owner?.Activate();
		};
	}

	private void ShowInTodayClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		ShowInTodCheckBox.IsChecked = !ShowInTodCheckBox.IsChecked;
		UserActCollectUtils.AddClickEvent("habit", "settings", (ShowInTodCheckBox.IsChecked == true) ? "enable_showintoday" : "disable_showindtoday");
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		LocalSettings.Settings.HabitInToday = ShowInTodCheckBox.IsChecked == true;
		HabitService.PushHabitSettings();
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitsettingwindow.xaml", UriKind.Relative);
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
			HabitConfig = (StackPanel)target;
			break;
		case 2:
			ShowInTodCheckBox = (CheckBox)target;
			ShowInTodCheckBox.PreviewMouseLeftButtonUp += ShowInTodayClick;
			break;
		case 3:
			((Button)target).Click += OnSaveClick;
			break;
		case 4:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
