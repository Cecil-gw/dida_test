using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Views.Calendar.Month;

namespace ticktick_WPF.Views.Print;

public class MonthPrintRowControl : UserControl, IComponentConnector
{
	internal MonthPrintRowControl Root;

	internal Grid Container;

	internal Grid BottomLine;

	internal Grid DayTextGrid;

	internal TextBlock FirstDay;

	internal TextBlock SecondDay;

	internal TextBlock ThirdDay;

	internal TextBlock FourthDay;

	internal TextBlock FifthDay;

	internal TextBlock SixthDay;

	internal TextBlock SeventhDay;

	private bool _contentLoaded;

	public MonthPrintRowControl()
	{
		InitializeComponent();
	}

	public MonthPrintRowControl(DateTime begin, int month)
	{
		InitializeComponent();
		base.Height = 44.0;
		DayTextGrid.Visibility = Visibility.Visible;
		FirstDay.Text = begin.Day.ToString();
		SecondDay.Text = begin.AddDays(1.0).Day.ToString();
		ThirdDay.Text = begin.AddDays(2.0).Day.ToString();
		FourthDay.Text = begin.AddDays(3.0).Day.ToString();
		FifthDay.Text = begin.AddDays(4.0).Day.ToString();
		SixthDay.Text = begin.AddDays(5.0).Day.ToString();
		SeventhDay.Text = begin.AddDays(6.0).Day.ToString();
		FirstDay.Opacity = ((month == 0 || begin.Month == month) ? 1.0 : 0.56);
		SecondDay.Opacity = ((month == 0 || begin.AddDays(1.0).Month == month) ? 1.0 : 0.56);
		ThirdDay.Opacity = ((month == 0 || begin.AddDays(2.0).Month == month) ? 1.0 : 0.56);
		FourthDay.Opacity = ((month == 0 || begin.AddDays(3.0).Month == month) ? 1.0 : 0.56);
		FifthDay.Opacity = ((month == 0 || begin.AddDays(4.0).Month == month) ? 1.0 : 0.56);
		SixthDay.Opacity = ((month == 0 || begin.AddDays(5.0).Month == month) ? 1.0 : 0.56);
		SeventhDay.Opacity = ((month == 0 || begin.AddDays(6.0).Month == month) ? 1.0 : 0.56);
	}

	public MonthPrintRowControl(IEnumerable<WeekEventModel> rowData, bool showBottom)
	{
		InitializeComponent();
		if (showBottom)
		{
			BottomLine.Visibility = Visibility.Visible;
		}
		SetData(rowData, showBottom);
	}

	public void SetData(IEnumerable<WeekEventModel> rowData, bool showBottom, double topMargin = 0.0)
	{
		foreach (WeekEventModel rowDatum in rowData)
		{
			if (rowDatum.Data.LoadMoreWidth == 0.0 || rowDatum.Data.IsLoadMore)
			{
				CalendarPrintCell calendarPrintCell = new CalendarPrintCell
				{
					Margin = new Thickness(6.0, 3.0 + topMargin, 6.0, 3.0)
				};
				calendarPrintCell.SetValue(Grid.ColumnProperty, rowDatum.Column);
				calendarPrintCell.SetValue(Grid.ColumnSpanProperty, rowDatum.ColumnSpan);
				calendarPrintCell.Height = 25.0;
				base.Height = (rowDatum.Data.IsLoadMore ? 32.0 : (32.0 + topMargin));
				calendarPrintCell.DataContext = rowDatum.Data;
				Container.Children.Add(calendarPrintCell);
			}
		}
		if (showBottom)
		{
			base.Height += 3.0;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/print/monthprintrowcontrol.xaml", UriKind.Relative);
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
			Root = (MonthPrintRowControl)target;
			break;
		case 2:
			Container = (Grid)target;
			break;
		case 3:
			BottomLine = (Grid)target;
			break;
		case 4:
			DayTextGrid = (Grid)target;
			break;
		case 5:
			FirstDay = (TextBlock)target;
			break;
		case 6:
			SecondDay = (TextBlock)target;
			break;
		case 7:
			ThirdDay = (TextBlock)target;
			break;
		case 8:
			FourthDay = (TextBlock)target;
			break;
		case 9:
			FifthDay = (TextBlock)target;
			break;
		case 10:
			SixthDay = (TextBlock)target;
			break;
		case 11:
			SeventhDay = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
