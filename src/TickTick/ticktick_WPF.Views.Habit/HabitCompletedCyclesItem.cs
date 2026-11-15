using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;

namespace ticktick_WPF.Views.Habit;

public class HabitCompletedCyclesItem : UserControl, IComponentConnector
{
	internal Image RightImage;

	private bool _contentLoaded;

	public HabitCompletedCyclesItem()
	{
		InitializeComponent();
		base.Loaded += OnControlLoaded;
		base.Unloaded += OnControlUnloaded;
	}

	public HabitCompletedCyclesItem(HabitModel habit, int cycle)
	{
		InitializeComponent();
		base.Loaded += OnControlLoaded;
		base.Unloaded += OnControlUnloaded;
		base.Visibility = ((!(habit.TargetDays > 0)) ? Visibility.Collapsed : Visibility.Visible);
		base.DataContext = new HabitDetailCompletedCyclesViewModel(habit, cycle);
	}

	private void OnControlUnloaded(object sender, RoutedEventArgs e)
	{
		BindEvents();
	}

	private void OnControlLoaded(object sender, RoutedEventArgs e)
	{
		UnbindEvents();
	}

	private void BindEvents()
	{
		DataChangedNotifier.ThemeModeChanged += OnThemeModeChanged;
	}

	private void UnbindEvents()
	{
		DataChangedNotifier.ThemeModeChanged -= OnThemeModeChanged;
	}

	private async void OnThemeModeChanged(object sender, EventArgs e)
	{
		if (base.DataContext is HabitDetailCompletedCyclesViewModel habitDetailCompletedCyclesViewModel)
		{
			habitDetailCompletedCyclesViewModel.UpdateIcon();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitcompletedcyclesitem.xaml", UriKind.Relative);
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
			RightImage = (Image)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
