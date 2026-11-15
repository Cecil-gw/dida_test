using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.Time;

public class WeekdaySelector : UserControl, IComponentConnector
{
	public List<DayOfWeek> SelectedDays = new List<DayOfWeek>();

	public static readonly DependencyProperty AtLeastOneProperty = DependencyProperty.Register("AtLeastOne", typeof(bool), typeof(DayCellSelector), new PropertyMetadata(false, null));

	internal Grid CellsGrid;

	private bool _contentLoaded;

	public bool AtLeastOne
	{
		get
		{
			return (bool)GetValue(AtLeastOneProperty);
		}
		set
		{
			SetValue(AtLeastOneProperty, value);
		}
	}

	public event EventHandler SelectedDaysChanged;

	public WeekdaySelector()
	{
		InitializeComponent();
		InitCells();
	}

	private void InitCells()
	{
		int num = 0;
		num = (int)LocalSettings.Settings.WeekStartFromType;
		for (int i = 0; i < 7; i++)
		{
			DayOfWeek dayOfWeek = (DayOfWeek)Enum.ToObject(typeof(DayOfWeek), (i + num) % 7);
			DayCellSelector dayCellSelector = new DayCellSelector
			{
				Day = dayOfWeek,
				ItemSelected = SelectedDays.Contains(dayOfWeek)
			};
			dayCellSelector.SelectedChanged -= OnCellSelectedChanged;
			dayCellSelector.SelectedChanged += OnCellSelectedChanged;
			CellsGrid.Children.Add(dayCellSelector);
			Grid.SetColumn(dayCellSelector, i);
		}
	}

	public void NotifySelectedChanged()
	{
		if (SelectedDays == null)
		{
			SelectedDays = new List<DayOfWeek>();
		}
		foreach (DayCellSelector child in CellsGrid.Children)
		{
			child.ItemSelected = SelectedDays.Contains(child.Day);
		}
	}

	private void OnCellSelectedChanged(object sender, bool selected)
	{
		DayCellSelector dayCellSelector = (DayCellSelector)sender;
		if (dayCellSelector != null)
		{
			if (selected)
			{
				SelectedDays.Add(dayCellSelector.Day);
				this.SelectedDaysChanged?.Invoke(this, null);
			}
			else if (AtLeastOne && SelectedDays.Count <= 1)
			{
				dayCellSelector.ItemSelected = true;
			}
			else
			{
				SelectedDays.Remove(dayCellSelector.Day);
				this.SelectedDaysChanged?.Invoke(this, null);
			}
		}
	}

	public void SetTabIndex(int tabIndex)
	{
		for (int i = 0; i < CellsGrid.Children.Count; i++)
		{
			((DayCellSelector)CellsGrid.Children[i]).SetTabSelected(tabIndex == i);
		}
	}

	public void HandleEnter()
	{
		for (int i = 0; i < CellsGrid.Children.Count; i++)
		{
			DayCellSelector dayCellSelector = (DayCellSelector)CellsGrid.Children[i];
			if (dayCellSelector.TabSelected)
			{
				dayCellSelector.Select();
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/weekdayselector.xaml", UriKind.Relative);
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
			CellsGrid = (Grid)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
