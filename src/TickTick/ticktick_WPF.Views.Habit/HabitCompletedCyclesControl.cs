using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Habit;

public class HabitCompletedCyclesControl : UserControl, IComponentConnector
{
	private HabitModel _habit;

	internal Grid CompletedCyclesCurrentGrid;

	internal EscPopup CompletedCyclesDetailPopup;

	internal ListView CompletedCyclesDetailList;

	private bool _contentLoaded;

	public HabitModel Habit
	{
		get
		{
			return _habit;
		}
		set
		{
			_habit = value;
			Reload();
		}
	}

	public HabitCompletedCyclesControl()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
	}

	private void BindEvent()
	{
		DataChangedNotifier.HabitCyclesChanged -= OnHabitCyclesChanged;
		DataChangedNotifier.HabitCyclesChanged += OnHabitCyclesChanged;
	}

	private void UnbindEvents()
	{
		DataChangedNotifier.HabitCyclesChanged -= OnHabitCyclesChanged;
	}

	private async void OnHabitCyclesChanged(object sender, string habitId)
	{
		if (habitId == _habit?.Id)
		{
			Habit = (await HabitDao.GetHabitById(habitId)) ?? Habit;
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		Reload();
		BindEvent();
	}

	private async void Reload()
	{
		if (Habit != null && base.IsLoaded && !(Habit.TargetDays <= 0))
		{
			CompletedCyclesCurrentGrid.Children.Clear();
			int valueOrDefault = Habit.CompletedCycles.GetValueOrDefault();
			HabitCompletedCyclesItem element = new HabitCompletedCyclesItem(Habit, valueOrDefault);
			if (Habit != null && Habit.CompletedCyclesList != null && Habit.CompletedCyclesList.Count > 1)
			{
				_ = Habit.CompletedCyclesList[0].isComplete;
			}
			CompletedCyclesCurrentGrid.Children.Add(element);
		}
	}

	private void OnClicked(object sender, MouseButtonEventArgs e)
	{
		if (CompletedCyclesDetailPopup.IsOpen)
		{
			return;
		}
		List<HabitDetailCompletedCyclesViewModel> list = new List<HabitDetailCompletedCyclesViewModel>();
		for (int num = Habit.CompletedCyclesList.Count - 1; num >= 0; num--)
		{
			if (Habit.CompletedCyclesList[num].isComplete)
			{
				list.Add(new HabitDetailCompletedCyclesViewModel(Habit, num));
			}
		}
		if (list.Count > 0)
		{
			ItemsSourceHelper.SetItemsSource(CompletedCyclesDetailList, list);
			CompletedCyclesDetailPopup.IsOpen = true;
		}
	}

	private void OnCloseClicked(object sender, MouseButtonEventArgs e)
	{
		CompletedCyclesDetailPopup.IsOpen = false;
		e.Handled = true;
	}

	public void Dispose()
	{
		DataChangedNotifier.HabitCyclesChanged -= OnHabitCyclesChanged;
	}

	public void ShowCompletedCircle()
	{
		OnClicked(null, null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitcompletedcyclescontrol.xaml", UriKind.Relative);
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
			CompletedCyclesCurrentGrid = (Grid)target;
			break;
		case 2:
			CompletedCyclesDetailPopup = (EscPopup)target;
			break;
		case 3:
			((Border)target).MouseLeftButtonUp += OnCloseClicked;
			break;
		case 4:
			CompletedCyclesDetailList = (ListView)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
