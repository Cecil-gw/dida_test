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
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Calendar.Month;
using ticktick_WPF.Views.Calendar.Week;

namespace ticktick_WPF.Views.Print;

public class WeekPrint : UserControl, IComponentConnector
{
	public double Left;

	public double Right;

	public double Top;

	public double Bottom;

	internal FlowDocumentPageViewer PageViewer;

	internal FlowDocument Doc;

	internal Run MonthText;

	internal BlockUIContainer Head;

	private bool _contentLoaded;

	public DateTime StartDate { get; }

	public List<CalendarTimelineDayViewModel> PointModels { get; }

	public List<WeekEventModel> AllDayModels { get; }

	public int Days { get; set; }

	public bool ShowWeekends { get; set; }

	public WeekPrint()
	{
		InitializeComponent();
	}

	public WeekPrint(List<WeekEventModel> allDayModels, List<CalendarTimelineDayViewModel> pointModels, double pageWidth, double pageHeight, DateTime startDate, int days, bool showWeekends)
	{
		InitializeComponent();
		Doc.FontFamily = ((LocalSettings.Settings.ExtraSettings.AppFontFamily == "SourceHansansSC_CN") ? new FontFamily("Microsoft YaHei UI") : LocalSettings.Settings.FontFamily);
		AllDayModels = allDayModels;
		PointModels = pointModels;
		StartDate = startDate;
		Days = days;
		ShowWeekends = showWeekends;
		Doc.PageHeight = pageHeight;
		Doc.PageWidth = pageWidth;
		PageViewer.Height = pageHeight;
		double num = 210.0;
		MonthText.Text = GetDateText(startDate);
		DateTime end = startDate.AddDays(days + ((!showWeekends) ? 2 : 0));
		int month = DateUtils.GetCurrentMonthDate(startDate, end).Month;
		Head.Child = new MultiDayPrintHeadView(startDate, days, month, showWeekends);
		allDayModels = allDayModels?.Where((WeekEventModel r) => r.Column + r.ColumnSpan > 0 && r.Column < days).ToList() ?? new List<WeekEventModel>();
		List<int> source = allDayModels.Select((WeekEventModel r) => r.Row).ToList();
		int num2 = (source.Any() ? source.Max() : 0);
		for (int num3 = 0; num3 <= num2; num3++)
		{
			int row = num3;
			List<WeekEventModel> list = allDayModels.Where((WeekEventModel data) => data.Row == row).ToList();
			if (list.Any())
			{
				if (row == 0)
				{
					MultiDayAllDayPrintRowView multiDayAllDayPrintRowView = new MultiDayAllDayPrintRowView(days, showAllDayText: true);
					multiDayAllDayPrintRowView.SetData(list, row == num2);
					Doc.Blocks.Add(new BlockUIContainer(multiDayAllDayPrintRowView));
				}
				else
				{
					MultiDayAllDayPrintRowView multiDayAllDayPrintRowView2 = new MultiDayAllDayPrintRowView(days, showAllDayText: false);
					multiDayAllDayPrintRowView2.SetData(list, row == num2);
					Doc.Blocks.Add(new BlockUIContainer(multiDayAllDayPrintRowView2));
					double height = multiDayAllDayPrintRowView2.Height;
					num = ((!(num + height > pageHeight - 197.0)) ? (num + height) : height);
				}
			}
		}
		AddPointModels(pointModels, pageWidth, pageHeight, num);
	}

	private string GetDateText(DateTime startDate)
	{
		DateTime dateTime = startDate.AddDays(6.0);
		if (startDate.Month != dateTime.Month)
		{
			return startDate.ToString("Y", App.Ci) + " - " + dateTime.ToString("Y", App.Ci);
		}
		return startDate.ToString("Y", App.Ci);
	}

	private void AddPointModels(List<CalendarTimelineDayViewModel> pointModels, double pageWidth, double pageHeight, double allDayHeight)
	{
		int val = 0;
		int val2 = 0;
		foreach (CalendarTimelineDayViewModel pointModel in pointModels)
		{
			if (pointModel.TopTasks != null)
			{
				val = Math.Max(pointModel.TopTasks.Count, val);
			}
			if (pointModel.BotTasks != null)
			{
				val2 = Math.Max(pointModel.BotTasks.Count, val2);
			}
			CalendarGeoHelper.AssemblyCells(pointModel.Cells, (pageWidth - 268.0) / (double)pointModels.Count + 6.0, pointModel.Date);
		}
		int num = (CalendarGeoHelper.TopFolded ? ((CalendarGeoHelper.GetEndHour() - CalendarGeoHelper.GetStartHour() + ((CalendarGeoHelper.GetEndHour() == 24) ? 1 : 2)) * 120) : 2880);
		double num2 = pageHeight - 197.0 - allDayHeight;
		if (num2 > 30.0)
		{
			if (num2 < 100.0)
			{
				num2 = 25.0;
				Doc.Blocks.Add(new BlockUIContainer(new WeekTimelinePrintView(null, 15.0)));
			}
			else
			{
				Border uiElement = new Border
				{
					Height = num2 - 10.0,
					ClipToBounds = true,
					Child = new WeekTimelinePrintView(pointModels, num)
				};
				Doc.Blocks.Add(new BlockUIContainer(uiElement));
			}
		}
		else
		{
			num2 = pageHeight - 197.0;
			Doc.Blocks.Add(new BlockUIContainer(new Border
			{
				Height = num2 - 10.0,
				ClipToBounds = true,
				Child = new WeekTimelinePrintView(pointModels, num)
			}));
		}
		for (double num3 = (double)num - num2 + 20.0; num3 > 0.0; num3 -= num2 - 20.0)
		{
			num2 = pageHeight - 197.0;
			Border uiElement2 = new Border
			{
				Height = num2 - 10.0,
				ClipToBounds = true,
				Child = new WeekTimelinePrintView(pointModels, num)
				{
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(0.0, num3 - (double)num, 0.0, 0.0)
				}
			};
			Doc.Blocks.Add(new BlockUIContainer(uiElement2));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/print/weekprint.xaml", UriKind.Relative);
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
			PageViewer = (FlowDocumentPageViewer)target;
			break;
		case 2:
			Doc = (FlowDocument)target;
			break;
		case 3:
			MonthText = (Run)target;
			break;
		case 4:
			Head = (BlockUIContainer)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
