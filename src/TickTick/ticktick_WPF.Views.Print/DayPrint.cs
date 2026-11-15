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
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Print;

public class DayPrint : UserControl, IComponentConnector
{
	public WeekDayCellViewModel PointModel;

	public DateTime Date;

	internal FlowDocumentPageViewer PageViewer;

	internal FlowDocument Doc;

	internal Run MonthText;

	internal Run WeekdayText;

	internal BlockUIContainer CenterLine;

	internal BlockUIContainer BottomLine;

	private bool _contentLoaded;

	public List<CalendarDisplayViewModel> AllDayTasks { get; set; }

	public bool IsAll { get; set; }

	public DayPrint()
	{
		InitializeComponent();
	}

	public DayPrint(List<CalendarDisplayViewModel> allDayTasks, WeekDayCellViewModel pointModel, double docWidth, double docHeight, DateTime date, bool isAll)
	{
		InitializeComponent();
		Doc.FontFamily = ((LocalSettings.Settings.ExtraSettings.AppFontFamily == "SourceHansansSC_CN") ? new FontFamily("Microsoft YaHei UI") : LocalSettings.Settings.FontFamily);
		Doc.PageHeight = docHeight;
		Doc.PageWidth = docWidth;
		Doc.ColumnWidth = docWidth;
		PageViewer.Height = docHeight;
		AllDayTasks = allDayTasks;
		PointModel = pointModel;
		IsAll = isAll;
		Date = date;
		int num = 160;
		MonthText.Text = DateUtils.FormatYearMonthDay(date, DateFormatLen.Middle);
		WeekdayText.Text = "   " + date.ToString("ddd", App.Ci);
		if (allDayTasks != null && allDayTasks.Count > 0)
		{
			for (int i = 0; i < allDayTasks.Count; i++)
			{
				Doc.Blocks.InsertBefore(CenterLine, new BlockUIContainer(new DayPrintRowControl((i == 0) ? Utils.GetString("AllDay") : null, allDayTasks[i])));
				num += 50;
			}
		}
		else
		{
			Doc.Blocks.InsertBefore(CenterLine, new BlockUIContainer(new DayPrintRowControl(Utils.GetString("AllDay"), null)));
			num += 50;
		}
		if (isAll)
		{
			DisplayByTimeLine(pointModel, docWidth, docHeight, num);
		}
		else
		{
			DisplayByStartTime();
		}
	}

	private void DisplayByTimeLine(WeekDayCellViewModel pointModel, double pageWidth, double pageHeight, double allDayHeight)
	{
		CalendarGeoHelper.AssemblyCells(pointModel.Cells, pageWidth - 268.0 + 6.0, pointModel.Date);
		int num;
		if (CalendarGeoHelper.TopFolded)
		{
			List<CalendarDisplayModel> topTasks = pointModel.TopTasks;
			if (topTasks != null && topTasks.Count > 1)
			{
				num = pointModel.TopTasks.Count * 30 + 20;
				goto IL_006d;
			}
		}
		num = 60;
		goto IL_006d;
		IL_00a2:
		int num3;
		double num2 = num3;
		double num5;
		double num4 = (CalendarGeoHelper.TopFolded ? ((double)((CalendarGeoHelper.GetEndHour() - CalendarGeoHelper.GetStartHour()) * 120) + num5 + num2) : 2880.0);
		double num6 = pageHeight - 197.0 - allDayHeight;
		if (num6 > 100.0)
		{
			DayTimePointPrintPanel uiElement = new DayTimePointPrintPanel(pointModel)
			{
				Height = num6 - 10.0,
				Container = 
				{
					Height = num4
				}
			};
			Doc.Blocks.InsertBefore(BottomLine, new BlockUIContainer(uiElement));
		}
		else
		{
			num6 = pageHeight - 197.0;
			Doc.Blocks.InsertBefore(BottomLine, new BlockUIContainer(new DayTimePointPrintPanel(pointModel)
			{
				Height = num6 - 10.0,
				Container = 
				{
					Height = num4
				}
			}));
		}
		for (double num7 = num4 - num6 + 20.0; num7 > 0.0; num7 -= num6 - 20.0)
		{
			num6 = pageHeight - 197.0;
			DayTimePointPrintPanel uiElement2 = new DayTimePointPrintPanel(pointModel)
			{
				Height = Math.Min(num6 - 10.0, num7),
				Container = 
				{
					Margin = new Thickness(0.0, num7 - num4, 0.0, 0.0),
					Height = num4
				}
			};
			Doc.Blocks.InsertBefore(BottomLine, new BlockUIContainer(uiElement2));
		}
		return;
		IL_006d:
		num5 = num;
		if (CalendarGeoHelper.TopFolded)
		{
			List<CalendarDisplayModel> botTasks = pointModel.BotTasks;
			if (botTasks != null && botTasks.Count > 1)
			{
				num3 = pointModel.BotTasks.Count * 30 + 20;
				goto IL_00a2;
			}
		}
		num3 = 60;
		goto IL_00a2;
	}

	private void DisplayByStartTime()
	{
		WeekDayCellViewModel pointModel = PointModel;
		if (pointModel != null && pointModel.Cells.Count > 0)
		{
			List<TaskCellViewModel> cells = PointModel.Cells;
			cells.Sort(delegate(TaskCellViewModel a, TaskCellViewModel b)
			{
				if (a.DisplayStartDate.HasValue && b.DisplayStartDate.HasValue)
				{
					if (!(a.DisplayStartDate.Value == b.DisplayStartDate.Value))
					{
						return a.DisplayStartDate.Value.CompareTo(b.DisplayStartDate.Value);
					}
					if (a.Status != b.Status)
					{
						return a.Status.CompareTo(b.Status);
					}
					return b.Priority.CompareTo(a.Priority);
				}
				return 0;
			});
			DateTime dateTime = Date.AddDays(-1.0);
			bool flag = true;
			{
				foreach (TaskCellViewModel item in cells.Where((TaskCellViewModel cell) => cell.DisplayStartDate.HasValue))
				{
					if (item.DisplayStartDate.Value != dateTime)
					{
						if (!flag)
						{
							Doc.Blocks.InsertBefore(BottomLine, new BlockUIContainer(new DayPrintRowControl(null, null)
							{
								Height = 20.0
							}));
						}
						Doc.Blocks.InsertBefore(BottomLine, new BlockUIContainer(new DayPrintRowControl(item.DisplayStartDate.Value.ToString("HH:mm"), item)));
						dateTime = item.DisplayStartDate.Value;
						flag = false;
					}
					else
					{
						Doc.Blocks.InsertBefore(BottomLine, new BlockUIContainer(new DayPrintRowControl(null, item)));
					}
				}
				return;
			}
		}
		Doc.Blocks.InsertBefore(BottomLine, new BlockUIContainer(new DayPrintRowControl(null, null)));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/print/dayprint.xaml", UriKind.Relative);
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
			WeekdayText = (Run)target;
			break;
		case 5:
			CenterLine = (BlockUIContainer)target;
			break;
		case 6:
			BottomLine = (BlockUIContainer)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
