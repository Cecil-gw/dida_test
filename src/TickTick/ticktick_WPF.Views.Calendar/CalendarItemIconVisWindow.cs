using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Entity.Binding;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public class CalendarItemIconVisWindow : Window, IComponentConnector, IStyleConnector
{
	internal ItemsControl MainItems;

	private bool _contentLoaded;

	private bool needPush { get; set; }

	private bool loaded { get; set; }

	private ObservableCollection<CheckableItem> Items { get; set; } = new ObservableCollection<CheckableItem>();

	public CalendarItemIconVisWindow()
	{
		InitializeComponent();
		MainItems.ItemsSource = Items;
		LoadData();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		loaded = true;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	public void LoadData()
	{
		Items.Clear();
		foreach (DisplayType item in new List<DisplayType>
		{
			DisplayType.Task,
			DisplayType.Event,
			DisplayType.Note,
			DisplayType.Habit,
			DisplayType.Pomo,
			DisplayType.Countdown,
			DisplayType.Course
		})
		{
			CheckableItem checkableItem = new CheckableItem
			{
				Data = item,
				IsChecked = LocalSettings.Settings.IsShowCalendarIcon(item)
			};
			switch (item)
			{
			case DisplayType.Task:
				checkableItem.Name = Utils.GetString("Task");
				break;
			case DisplayType.Event:
				checkableItem.Name = Utils.GetString("ShowItemIconCalendar");
				break;
			case DisplayType.Note:
				checkableItem.Name = Utils.GetString("Notes");
				break;
			case DisplayType.Habit:
				if (!LocalSettings.Settings.ShowHabit)
				{
					continue;
				}
				checkableItem.Name = Utils.GetString("Habit");
				break;
			case DisplayType.Pomo:
				checkableItem.Name = Utils.GetString("FocusRecord");
				break;
			case DisplayType.Countdown:
				if (!LocalSettings.Settings.EnableCountDownDays)
				{
					continue;
				}
				checkableItem.Name = Utils.GetString("CountdownTitle");
				break;
			case DisplayType.Course:
				if (LocalSettings.Settings.UserPreference?.TimeTable?.isEnabled != true)
				{
					continue;
				}
				checkableItem.Name = Utils.GetString("Course");
				break;
			default:
				continue;
			}
			Items.Add(checkableItem);
		}
	}

	private void ShowIconCheckBox_Checked(object sender, RoutedEventArgs e)
	{
		if (loaded && sender is FrameworkElement { DataContext: CheckableItem dataContext })
		{
			LocalSettings.Settings.UpdateCalendarIcon((DisplayType)dataContext.Data, dataContext.IsChecked);
			needPush = true;
			DataChangedNotifier.NotifyCalendarShowIconChanged();
		}
	}

	private void Window_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			Close();
		}
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (needPush)
		{
			CalendarConfig config = LocalSettings.Settings?.UserPreference?.CalendarConfig ?? new CalendarConfig();
			LocalSettings.Settings.SaveCalendarStyleConfig(config, saveAndPush: true);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendaritemiconviswindow.xaml", UriKind.Relative);
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
			((CalendarItemIconVisWindow)target).Loaded += Window_Loaded;
			((CalendarItemIconVisWindow)target).Closing += Window_Closing;
			((CalendarItemIconVisWindow)target).KeyDown += Window_KeyDown;
			break;
		case 2:
			MainItems = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 3)
		{
			((CheckBox)target).Checked += ShowIconCheckBox_Checked;
			((CheckBox)target).Unchecked += ShowIconCheckBox_Checked;
		}
	}
}
