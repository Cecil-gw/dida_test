using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Models;

namespace ticktick_WPF.Views.Habit;

public class HabitCheckinChartControl : UserControl, INotifyPropertyChanged, IComponentConnector
{
	private bool _hitEnd;

	private string _unit;

	internal ItemsControl AxisY;

	internal Grid IndicatorLine;

	internal TextBlock GoalText;

	internal ScrollViewer MonthScrollViewer;

	internal ItemsControl AxisX;

	internal Border LeftButton;

	internal Border RightButton;

	private bool _contentLoaded;

	public bool HitEnd
	{
		get
		{
			return _hitEnd;
		}
		set
		{
			_hitEnd = value;
			OnPropertyChanged("HitEnd");
		}
	}

	public string Unit
	{
		get
		{
			return _unit;
		}
		set
		{
			_unit = value;
			OnPropertyChanged("Unit");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public HabitCheckinChartControl()
	{
		InitializeComponent();
	}

	public void Load(HabitModel habit, DateTime startDate, DateTime endDate, List<HabitCheckInModel> checkIns)
	{
		int num = 2;
		int num2 = 0;
		HabitCheckInModel habitCheckInModel = checkIns.OrderByDescending((HabitCheckInModel checkIn) => checkIn.Value).FirstOrDefault();
		if (habitCheckInModel != null)
		{
			num2 = (int)Math.Round(habitCheckInModel.Value, 1);
		}
		num = (int)Math.Round(Math.Max(num2, habit.Goal), 0);
		List<OffsetInfo> list = new List<OffsetInfo>();
		int num3 = Math.Max((int)Math.Round((float)num / 5f, 0), 1);
		num = num3 * 5;
		if (num < num2)
		{
			num3++;
			num = num3 * 5;
		}
		for (int num4 = 0; num4 <= 5; num4++)
		{
			list.Add(new OffsetInfo(num - num3 * num4, 24.0));
		}
		AxisY.ItemsSource = list;
		double bottom = habit.Goal / (double)num3 * 24.0 + 28.0;
		GoalText.Text = habit.Goal.ToString() ?? "";
		IndicatorLine.Margin = new Thickness(0.0, 0.0, 0.0, bottom);
		List<DayCheckInColumnInfo> list2 = new List<DayCheckInColumnInfo>();
		DateTime currentDate = startDate;
		while (currentDate <= endDate)
		{
			double height = 0.0;
			HabitCheckInModel habitCheckInModel2 = checkIns.FirstOrDefault((HabitCheckInModel check) => check.CheckinStamp == currentDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
			if (habitCheckInModel2 != null)
			{
				height = habitCheckInModel2.Value / (double)num3 * 24.0;
			}
			list2.Add(new DayCheckInColumnInfo(currentDate, height, habitCheckInModel2?.Value ?? 0.0, habitCheckInModel2?.Goal ?? habit.Goal, habit.Unit));
			currentDate = currentDate.AddDays(1.0);
		}
		AxisX.ItemsSource = list2;
		MoveToToday();
		MonthScrollViewer.ScrollChanged -= OnScrollChanged;
		MonthScrollViewer.ScrollChanged += OnScrollChanged;
		Unit = HabitUtils.GetUnitText(habit.Unit);
		base.DataContext = this;
	}

	private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		ScrollViewer scrollViewer = (ScrollViewer)sender;
		HitEnd = Math.Abs(scrollViewer.HorizontalOffset - scrollViewer.ScrollableWidth) <= 0.0;
	}

	private void MoveToToday()
	{
		int num = DateTime.Today.Day * 32;
		if ((double)num > base.Width)
		{
			MonthScrollViewer.ScrollToHorizontalOffset((double)num - base.Width + base.Width * 0.6);
		}
	}

	private void MoveRightClick(object sender, MouseButtonEventArgs e)
	{
		MonthScrollViewer.ScrollToHorizontalOffset(MonthScrollViewer.HorizontalOffset + base.Width * 0.6);
	}

	private void MoveLeftClick(object sender, MouseButtonEventArgs e)
	{
		MonthScrollViewer.ScrollToHorizontalOffset(MonthScrollViewer.HorizontalOffset - base.Width * 0.6);
	}

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void OnMouseWheel(object sender, MouseWheelEventArgs e)
	{
		MonthScrollViewer.ScrollToHorizontalOffset(MonthScrollViewer.HorizontalOffset - (double)e.Delta / 3.0);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitcheckinchartcontrol.xaml", UriKind.Relative);
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
			AxisY = (ItemsControl)target;
			break;
		case 2:
			IndicatorLine = (Grid)target;
			break;
		case 3:
			GoalText = (TextBlock)target;
			break;
		case 4:
			MonthScrollViewer = (ScrollViewer)target;
			MonthScrollViewer.PreviewMouseWheel += OnMouseWheel;
			break;
		case 5:
			AxisX = (ItemsControl)target;
			break;
		case 6:
			LeftButton = (Border)target;
			LeftButton.MouseLeftButtonUp += MoveLeftClick;
			break;
		case 7:
			RightButton = (Border)target;
			RightButton.MouseLeftButtonUp += MoveRightClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
