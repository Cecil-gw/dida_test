using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Time;

public class MonthDaySelector : UserControl, IComponentConnector
{
	public List<int> SelectedDays = new List<int>();

	private int _tabIndex;

	private DayCellSelector _tabSelectItem;

	private bool _canBatchSelect = true;

	internal MonthDaySelector RootView;

	internal Grid MonthSwitchGrid;

	internal TextBlock MonthText;

	internal Grid NextOrLastGrid;

	internal Border NextGrid;

	internal Grid CellsGrid;

	private bool _contentLoaded;

	public int CurrentMonth { get; set; }

	public MonthDaySelector()
	{
		InitializeComponent();
		InitCells();
	}

	public void NotifySelectedChanged()
	{
		foreach (object child in CellsGrid.Children)
		{
			if (child is DayCellSelector dayCellSelector)
			{
				dayCellSelector.ItemSelected = SelectedDays.Contains(dayCellSelector.DayOfMonth);
			}
		}
	}

	private void InitCells(int days = 31, bool withLast = true)
	{
		CellsGrid.Children.Clear();
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 7; j++)
			{
				int num = i * 7 + j + 1;
				if (num <= days)
				{
					DayCellSelector dayCellSelector = new DayCellSelector
					{
						DayOfMonth = num,
						MonthMode = true,
						ItemSelected = SelectedDays.Contains(num)
					};
					dayCellSelector.SelectedChanged -= OnCellSelected;
					dayCellSelector.SelectedChanged += OnCellSelected;
					CellsGrid.Children.Add(dayCellSelector);
					Grid.SetRow(dayCellSelector, i);
					Grid.SetColumn(dayCellSelector, j);
				}
			}
		}
		if (withLast)
		{
			DayCellSelector dayCellSelector2 = new DayCellSelector(66.0)
			{
				DayOfMonth = -1,
				MonthMode = true,
				ItemSelected = SelectedDays.Contains(-1)
			};
			dayCellSelector2.SelectedChanged -= OnCellSelected;
			dayCellSelector2.SelectedChanged += OnCellSelected;
			dayCellSelector2.HorizontalAlignment = HorizontalAlignment.Left;
			CellsGrid.Children.Add(dayCellSelector2);
			Grid.SetRow(dayCellSelector2, 4);
			Grid.SetColumn(dayCellSelector2, (days != 28) ? 3 : 0);
			Grid.SetColumnSpan(dayCellSelector2, 4);
		}
	}

	private void OnCellSelected(object sender, bool selected)
	{
		DayCellSelector dayCellSelector = (DayCellSelector)sender;
		if (selected)
		{
			SelectedDays.Add(dayCellSelector.DayOfMonth);
		}
		else
		{
			SelectedDays.Remove(dayCellSelector.DayOfMonth);
		}
		if (_canBatchSelect)
		{
			return;
		}
		foreach (object child in CellsGrid.Children)
		{
			if (child is DayCellSelector dayCellSelector2)
			{
				dayCellSelector2.ItemSelected = false;
			}
		}
		dayCellSelector.ItemSelected = true;
		SelectedDays.Clear();
		SelectedDays.Add(dayCellSelector.DayOfMonth);
	}

	public void SetTabSelected(bool tab, int? step = null)
	{
		if (tab)
		{
			if (step.HasValue)
			{
				int num = _tabIndex + step.Value;
				if (num >= 0 && (num == 27 || num < CellsGrid.Children.Count))
				{
					num = Math.Min(_tabIndex + step.Value, CellsGrid.Children.Count - 1);
					if (CellsGrid.Children[num] is DayCellSelector dayCellSelector)
					{
						_tabSelectItem?.SetTabSelected(tabSelect: false);
						_tabSelectItem = dayCellSelector;
						_tabIndex = num;
						dayCellSelector.SetTabSelected(tabSelect: true);
					}
				}
			}
			else
			{
				_tabIndex = 0;
				_tabSelectItem?.SetTabSelected(tabSelect: false);
				_tabSelectItem = CellsGrid.Children[_tabIndex] as DayCellSelector;
				_tabSelectItem?.SetTabSelected(tabSelect: true);
			}
		}
		else
		{
			_tabIndex = -1;
			_tabSelectItem?.SetTabSelected(tabSelect: false);
			_tabSelectItem = null;
		}
	}

	public void HandleEnter()
	{
		_tabSelectItem?.Select();
	}

	public void SetMonth(int month, bool batch = true)
	{
		MonthSwitchGrid.Visibility = ((month == -1) ? Visibility.Collapsed : Visibility.Visible);
		int days = ((month == -1) ? 31 : new DateTime(2023, month, 1).AddMonths(1).AddDays(-1.0).Day);
		InitCells(days, month == 2 || month == -1);
		_canBatchSelect = batch;
		if (month != -1)
		{
			MonthText.Text = DateUtils.FormatMonth(new DateTime(2023, month, 1));
			CurrentMonth = month;
		}
	}

	private void LastClick(object sender, MouseButtonEventArgs e)
	{
		CurrentMonth--;
		if (CurrentMonth < 1)
		{
			CurrentMonth = 12;
		}
		SetMonth(CurrentMonth, _canBatchSelect);
	}

	private void NextClick(object sender, MouseButtonEventArgs e)
	{
		CurrentMonth++;
		if (CurrentMonth > 12)
		{
			CurrentMonth = 1;
		}
		SetMonth(CurrentMonth, _canBatchSelect);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/monthdayselector.xaml", UriKind.Relative);
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
			RootView = (MonthDaySelector)target;
			break;
		case 2:
			MonthSwitchGrid = (Grid)target;
			break;
		case 3:
			MonthText = (TextBlock)target;
			break;
		case 4:
			NextOrLastGrid = (Grid)target;
			break;
		case 5:
			((Border)target).MouseLeftButtonUp += LastClick;
			break;
		case 6:
			NextGrid = (Border)target;
			NextGrid.MouseLeftButtonUp += NextClick;
			break;
		case 7:
			CellsGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
