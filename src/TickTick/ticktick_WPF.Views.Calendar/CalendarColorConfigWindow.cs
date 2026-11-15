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
using ticktick_WPF.Entity.Calendar;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.Calendar;

public class CalendarColorConfigWindow : Window, IComponentConnector
{
	internal CustomSimpleComboBox ModeSwitcher;

	internal ColorSelector FixedColorSelector;

	internal GroupTitle GroupTitleControl;

	private bool _contentLoaded;

	private CalendarColorConfigViewModel viewModel { get; set; }

	public CalendarColorConfigWindow()
	{
		InitializeComponent();
		base.DataContext = (viewModel = new CalendarColorConfigViewModel());
		CalendarColorConfigViewModel calendarColorConfigViewModel2 = viewModel;
		calendarColorConfigViewModel2.OnGroupChange = (Action<CalendarColorConfigViewModel.CalColorGroupType>)Delegate.Combine(calendarColorConfigViewModel2.OnGroupChange, new Action<CalendarColorConfigViewModel.CalColorGroupType>(OnGroupChange));
		CalendarColorConfigViewModel calendarColorConfigViewModel3 = viewModel;
		calendarColorConfigViewModel3.OnModeChange = (Action<int>)Delegate.Combine(calendarColorConfigViewModel3.OnModeChange, new Action<int>(OnModeChange));
		bool showHabit = LocalSettings.Settings.ShowHabit;
		bool enableCountDownDays = LocalSettings.Settings.EnableCountDownDays;
		List<string> list = new List<string> { "Task" };
		if (showHabit)
		{
			list.Add("Habit");
		}
		if (enableCountDownDays)
		{
			list.Add("CountdownTitle");
		}
		GroupTitleControl.Titles = string.Join("|", list);
		viewModel.IsShowGroupTitle = list.Count() > 1;
		FixedColorSelector.SetCustomColorList(new List<string> { "#FFAE58", "#7BD866", "#54CBBB", "#67A2FA", "#C096FF", "#767DFD", "#FF7ABF", "#FF7070" });
	}

	private async void Window_Loaded(object sender, RoutedEventArgs e)
	{
		await viewModel.Init();
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		CalendarColorConfigViewModel calendarColorConfigViewModel = viewModel;
		calendarColorConfigViewModel.OnGroupChange = (Action<CalendarColorConfigViewModel.CalColorGroupType>)Delegate.Remove(calendarColorConfigViewModel.OnGroupChange, new Action<CalendarColorConfigViewModel.CalColorGroupType>(OnGroupChange));
		CalendarColorConfigViewModel calendarColorConfigViewModel2 = viewModel;
		calendarColorConfigViewModel2.OnModeChange = (Action<int>)Delegate.Remove(calendarColorConfigViewModel2.OnModeChange, new Action<int>(OnModeChange));
	}

	private void OnGroupChange(CalendarColorConfigViewModel.CalColorGroupType type)
	{
		switch (type)
		{
		case CalendarColorConfigViewModel.CalColorGroupType.Task:
		{
			List<string> items3 = new List<string>
			{
				Utils.GetString("ColorByList"),
				Utils.GetString("ColorByTag"),
				Utils.GetString("ColorByPriority")
			};
			ModeSwitcher.Init(items3);
			break;
		}
		case CalendarColorConfigViewModel.CalColorGroupType.Habit:
		{
			List<string> items2 = new List<string>
			{
				Utils.GetString("ColorByTheme"),
				Utils.GetString("ColorByFixed"),
				Utils.GetString("ColorByHabit")
			};
			ModeSwitcher.Init(items2);
			break;
		}
		case CalendarColorConfigViewModel.CalColorGroupType.Countdown:
		{
			List<string> items = new List<string>
			{
				Utils.GetString("ColorByFixed"),
				Utils.GetString("ColorByCountdown")
			};
			ModeSwitcher.Init(items);
			break;
		}
		}
	}

	private void OnModeChange(int value)
	{
		ModeSwitcher.SelectedIndex = value;
	}

	private async void GroupTitle_SelectedTitleChanged(object sender, GroupTitleViewModel e)
	{
		await viewModel.ChangeSelectGroup((CalendarColorConfigViewModel.CalColorGroupType)e.Index);
	}

	private async void OnModeSwitcherSelected(object sender, SimpleComboBoxViewModel model)
	{
		await viewModel.ChangeMode(ModeSwitcher.SelectedIndex);
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		viewModel.OnClosing();
		Close();
	}

	private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.Source == sender)
		{
			base.OnMouseLeftButtonDown(e);
			DragMove();
		}
	}

	private async void OnItemColorSelect(object sender, string e)
	{
		if (sender is FrameworkElement { DataContext: CalColorConfigItem dataContext })
		{
			await viewModel.ChangeItemColor(dataContext);
		}
	}

	private void OnFixedColorSelect(object sender, string e)
	{
		viewModel.ChangeFixedColor();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendarcolorconfigwindow.xaml", UriKind.Relative);
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
			((CalendarColorConfigWindow)target).Closing += Window_Closing;
			((CalendarColorConfigWindow)target).Loaded += Window_Loaded;
			break;
		case 2:
			ModeSwitcher = (CustomSimpleComboBox)target;
			break;
		case 3:
			FixedColorSelector = (ColorSelector)target;
			break;
		case 4:
			((Grid)target).PreviewMouseLeftButtonDown += Grid_PreviewMouseLeftButtonDown;
			break;
		case 5:
			GroupTitleControl = (GroupTitle)target;
			break;
		case 6:
			((Button)target).Click += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
