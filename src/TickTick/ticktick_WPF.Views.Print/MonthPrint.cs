using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Calendar.Month;

namespace ticktick_WPF.Views.Print;

public class MonthPrint : UserControl, IComponentConnector
{
	private int _month;

	private DateTime _startDate;

	internal MonthPrint Root;

	internal FlowDocument Doc;

	internal Run MonthText;

	internal Grid WeekPanel;

	internal TextBlock Sun;

	internal TextBlock Mon;

	internal TextBlock Tues;

	internal TextBlock Wed;

	internal TextBlock Thur;

	internal TextBlock Fri;

	internal TextBlock Sat;

	private bool _contentLoaded;

	public MonthPrint(List<List<WeekEventModel>> monthRowData, DateTime startDate)
	{
		InitializeComponent();
		Doc.FontFamily = ((LocalSettings.Settings.ExtraSettings.AppFontFamily == "SourceHansansSC_CN") ? new FontFamily("Microsoft YaHei UI") : LocalSettings.Settings.FontFamily);
		_month = startDate.AddDays(7.0).Month;
		_startDate = startDate;
		MonthText.Text = startDate.AddDays(7.0).ToString("y", App.Ci);
		InitWeekText();
		AddWeekViews(monthRowData);
	}

	private void InitWeekText()
	{
		int num = (int)(LocalSettings.Settings.WeekStartFromType + 7) % 7;
		Dictionary<DayOfWeek, int> dictionary = new Dictionary<DayOfWeek, int>();
		foreach (DayOfWeek value2 in Enum.GetValues(typeof(DayOfWeek)))
		{
			int value = (int)(value2 - num + 7) % 7;
			dictionary.Add(value2, value);
		}
		Mon.SetValue(Grid.ColumnProperty, dictionary[DayOfWeek.Monday]);
		Tues.SetValue(Grid.ColumnProperty, dictionary[DayOfWeek.Tuesday]);
		Wed.SetValue(Grid.ColumnProperty, dictionary[DayOfWeek.Wednesday]);
		Thur.SetValue(Grid.ColumnProperty, dictionary[DayOfWeek.Thursday]);
		Fri.SetValue(Grid.ColumnProperty, dictionary[DayOfWeek.Friday]);
		Sat.SetValue(Grid.ColumnProperty, dictionary[DayOfWeek.Saturday]);
		Sun.SetValue(Grid.ColumnProperty, dictionary[DayOfWeek.Sunday]);
	}

	private void AddWeekViews(List<List<WeekEventModel>> monthRowData)
	{
		int num = 0;
		foreach (List<WeekEventModel> monthRowDatum in monthRowData)
		{
			AddWeekView(monthRowDatum, num);
			num++;
		}
	}

	private void AddWeekView(List<WeekEventModel> weekRowData, int num)
	{
		Doc.Blocks.Add(new BlockUIContainer(new MonthPrintRowControl(_startDate.AddDays(num * 7), _month)));
		if (weekRowData != null && weekRowData.Count > 0)
		{
			int num2 = weekRowData.Select((WeekEventModel r) => r.Row).Max();
			for (int num3 = 0; num3 <= num2; num3++)
			{
				int row = num3;
				List<WeekEventModel> list = weekRowData.Where((WeekEventModel data) => data.Row == row).ToList();
				if (list.Any())
				{
					Doc.Blocks.Add(new BlockUIContainer(new MonthPrintRowControl(list, row == num2)));
				}
			}
		}
		else
		{
			Doc.Blocks.Add(new BlockUIContainer(new MonthPrintRowControl(new List<WeekEventModel>(), showBottom: true)));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/print/monthprint.xaml", UriKind.Relative);
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
			Root = (MonthPrint)target;
			break;
		case 2:
			Doc = (FlowDocument)target;
			break;
		case 3:
			MonthText = (Run)target;
			break;
		case 4:
			WeekPanel = (Grid)target;
			break;
		case 5:
			Sun = (TextBlock)target;
			break;
		case 6:
			Mon = (TextBlock)target;
			break;
		case 7:
			Tues = (TextBlock)target;
			break;
		case 8:
			Wed = (TextBlock)target;
			break;
		case 9:
			Thur = (TextBlock)target;
			break;
		case 10:
			Fri = (TextBlock)target;
			break;
		case 11:
			Sat = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
