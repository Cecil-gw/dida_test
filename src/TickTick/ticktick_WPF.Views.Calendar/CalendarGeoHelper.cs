using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public class CalendarGeoHelper
{
	private class CellGeo
	{
		public int Column;

		public string Id;

		public List<CellGeo> LeftSibling = new List<CellGeo>();

		public List<CellGeo> RightSibling = new List<CellGeo>();

		public CellGeo LeftParent;

		public CellGeo RightParent;

		public double Top;

		public double Height;

		public double? ChildTop;

		public string Tips;

		public double Width;

		public double Left;

		public double Bottom => Top + Height;

		public int Index { get; set; }

		public static CellGeo Build(TaskCellViewModel model, DateTime date)
		{
			DateTime? displayStartDate = model.DisplayStartDate;
			if (displayStartDate.HasValue)
			{
				DateTime startDate = displayStartDate.GetValueOrDefault();
				if (startDate.Date != date.Date)
				{
					startDate = date.Date;
				}
				DateTime? dueDate = (model.BarMode ? ((DateTime?)null) : model.DisplayDueDate);
				CellGeo cellGeo = new CellGeo
				{
					Id = model.Identity,
					Top = TransVerticalOffset(startDate),
					Height = CalculateCellHeight(startDate, dueDate)
				};
				if (dueDate.HasValue && dueDate.Value.Date != date.Date)
				{
					dueDate = date.Date.AddDays(1.0);
				}
				if (TopFolded)
				{
					int hour = startDate.Hour;
					int startHourForTask = GetStartHourForTask();
					if (hour >= GetEndHour())
					{
						cellGeo.Top = -1.0 * HourHeight;
						cellGeo.Height = 0.0;
					}
					else if (hour <= startHourForTask)
					{
						if (dueDate.HasValue && (dueDate.Value - date.Date).TotalHours > (double)(startHourForTask + 1))
						{
							DateTime startDate2 = startDate.Date.AddHours(startHourForTask + 1);
							cellGeo.Top = TransVerticalOffset(startDate2);
							cellGeo.Height = CalculateCellHeight(startDate2, dueDate);
						}
						else
						{
							cellGeo.Top = -1.0 * HourHeight;
							cellGeo.Height = 0.0;
						}
					}
				}
				return cellGeo;
			}
			return null;
		}

		public static CellGeo Build(TaskCellViewModel model, double hourHeight, DateTime spanStart)
		{
			DateTime? displayStartDate = model.DisplayStartDate;
			if (displayStartDate.HasValue)
			{
				DateTime dateTime = displayStartDate.GetValueOrDefault();
				if (dateTime < spanStart)
				{
					dateTime = spanStart;
				}
				DateTime? dateTime2 = model.DisplayDueDate;
				if (dateTime2.HasValue && dateTime2.Value > spanStart.AddHours(4.0))
				{
					dateTime2 = spanStart.AddHours(4.0);
				}
				return new CellGeo
				{
					Id = model.Identity,
					Top = (dateTime - spanStart).TotalHours * hourHeight,
					Height = ((!dateTime2.HasValue) ? 16.0 : Math.Max(16.0, (dateTime2.Value - dateTime).TotalHours * hourHeight))
				};
			}
			return null;
		}

		public bool IsCross(CellGeo geo)
		{
			if (!(Top <= geo.Top) || !(Bottom > geo.Top))
			{
				if (geo.Top <= Top)
				{
					return geo.Bottom > Top;
				}
				return false;
			}
			return true;
		}
	}

	private static bool _topFolded = true;

	public const double CellMargin = 1.0;

	public const double TimelineWidth = 56.0;

	public const double CellDragMargin = 16.0;

	public const double SameLineCellDiff = 32.0;

	public const double CellRightMargin = 9.0;

	public const double CellLeftMargin = 2.0;

	public static List<List<double>> HourZoomHeights = new List<List<double>>
	{
		new List<double> { 44.0, 48.0, 52.0, 60.0, 68.0, 76.0, 88.0, 100.0, 116.0, 132.0 },
		new List<double> { 44.0, 48.0, 52.0, 60.0, 68.0, 76.0, 88.0, 100.0, 116.0, 132.0 },
		new List<double> { 48.0, 52.0, 56.0, 64.0, 72.0, 84.0, 96.0, 112.0, 128.0, 144.0 },
		new List<double> { 52.0, 56.0, 60.0, 68.0, 80.0, 92.0, 108.0, 124.0, 140.0, 156.0 },
		new List<double> { 56.0, 60.0, 68.0, 76.0, 88.0, 104.0, 120.0, 136.0, 152.0, 168.0 },
		new List<double> { 60.0, 68.0, 76.0, 88.0, 100.0, 116.0, 132.0, 148.0, 164.0, 180.0 },
		new List<double> { 64.0, 76.0, 88.0, 100.0, 116.0, 132.0, 148.0, 164.0, 180.0, 196.0 },
		new List<double> { 68.0, 84.0, 100.0, 116.0, 132.0, 148.0, 164.0, 180.0, 196.0, 212.0 }
	};

	public static readonly List<int> TaskBarHeights = new List<int> { 18, 20, 22, 24, 26, 28, 30, 32 };

	public static readonly List<double> TaskCellPointHeights = new List<double> { 18.0, 20.0, 22.0, 24.0, 26.0, 28.0, 30.0, 32.0 };

	public static readonly List<int> TaskBarFontSizes = new List<int> { 11, 12, 13, 14, 15, 16, 17, 18 };

	public static readonly List<int> TaskBarIconSizes = new List<int> { 13, 14, 15, 16, 17, 18, 19, 20 };

	public static readonly List<int> TaskCellTimeSizes = new List<int> { 10, 10, 11, 11, 12, 12, 13, 13 };

	public static readonly List<double> TaskCellIconTops = new List<double> { 3.5, 3.5, 4.0, 5.0, 5.0, 6.0, 7.0, 7.0 };

	public static readonly List<double> TaskCellTextTops = new List<double> { 3.0, 3.0, 3.0, 4.0, 4.0, 4.0, 5.0, 5.0 };

	public static readonly List<double> TaskCellTimeTextTops = new List<double> { 2.0, 3.0, 3.0, 4.0, 4.0, 4.0, 5.0, 6.0 };

	public static readonly List<int> TaskTitleLefts = new List<int> { 0, 0, 0, 1, 1, 2, 2, 3 };

	private static int _startHour = LocalSettings.Settings.CollapsedStart;

	private static int _endHour = LocalSettings.Settings.CollapsedEnd;

	public static readonly List<int> TaskCellMinHeights = new List<int> { 10, 10, 11, 12, 13, 14, 15, 16 };

	public static List<double> HourZoomHeight => HourZoomHeights[CalTitleFontSizeIndex];

	public static int CalTitleFontSizeIndex => LocalSettings.Settings.ExtraSettings.CalFontSize;

	public static double CalTitleFontSize => TaskBarFontSizes[CalTitleFontSizeIndex];

	public static double CalTitleIconSize => TaskBarIconSizes[CalTitleFontSizeIndex];

	public static double CellTimeFontSize => TaskCellTimeSizes[CalTitleFontSizeIndex];

	public static int TaskBarHeight => TaskBarHeights[CalTitleFontSizeIndex];

	public static double TaskCellIconTop => TaskCellIconTops[CalTitleFontSizeIndex];

	public static double TaskCellTextTop => TaskCellTextTops[CalTitleFontSizeIndex];

	public static double TaskCellTimeTextTop => TaskCellTimeTextTops[CalTitleFontSizeIndex];

	public static int TaskTitleLeft => TaskTitleLefts[CalTitleFontSizeIndex];

	public static bool TopFolded
	{
		get
		{
			return _topFolded;
		}
		set
		{
			_topFolded = value;
			CalendarGeoHelper.TopFoldedChanged?.Invoke(null, null);
		}
	}

	public static double HourHeight => LocalSettings.Settings.CalendarHourHeight;

	public static double QuarterHourHeight => LocalSettings.Settings.CalendarHourHeight / 4.0;

	public static double HalfHourHeight => LocalSettings.Settings.CalendarHourHeight / 2.0;

	public static double MinMinute => LocalSettings.Settings.CalendarMinMinute;

	public static int CalColumns
	{
		get
		{
			if (!LocalSettings.Settings.ShowCalWeekend)
			{
				return 5;
			}
			return 7;
		}
	}

	public static double MinHeight => TaskCellMinHeights[CalTitleFontSizeIndex];

	public static double MinPointHeight => TaskCellPointHeights[CalTitleFontSizeIndex];

	public static event EventHandler TopFoldedChanged;

	public static event EventHandler<bool> ExpandTop;

	public static event EventHandler<double> CalendarHourHeightChanged;

	public static void SetFontSize(int index)
	{
		int currentHourHeightIndex = GetCurrentHourHeightIndex();
		if (index < 0)
		{
			index = 0;
		}
		if (index > 7)
		{
			index = 7;
		}
		LocalSettings.Settings.ExtraSettings.CalFontSize = index;
		SetCalendarHourHeight(HourZoomHeight[currentHourHeightIndex]);
		CalendarEventChangeNotifier.NotifyFontSizeChanged();
		LocalSettings.Settings.Save(force: true);
	}

	public static void ChangeFontSize(bool add)
	{
		int calTitleFontSizeIndex = CalTitleFontSizeIndex;
		int currentHourHeightIndex = GetCurrentHourHeightIndex();
		calTitleFontSizeIndex = ((!add) ? (calTitleFontSizeIndex - 1) : (calTitleFontSizeIndex + 1));
		if (calTitleFontSizeIndex < 0)
		{
			calTitleFontSizeIndex = 0;
		}
		if (calTitleFontSizeIndex > 7)
		{
			calTitleFontSizeIndex = 7;
		}
		LocalSettings.Settings.ExtraSettings.CalFontSize = calTitleFontSizeIndex;
		SetCalendarHourHeight(HourZoomHeight[currentHourHeightIndex]);
		CalendarEventChangeNotifier.NotifyFontSizeChanged();
		LocalSettings.Settings.Save(force: true);
	}

	public static int GetCurrentHourHeightIndex()
	{
		double current = HourHeight;
		int num = HourZoomHeight.FindIndex((double h) => h >= current);
		if (num < 0)
		{
			num = 9;
		}
		return num;
	}

	public static void SetCalendarHourHeight(double height)
	{
		double validHeight = LocalSettings.GetValidHeight(height);
		LocalSettings.Settings.CalendarHourHeight = validHeight;
		LocalSettings.Settings.CalendarMinMinute = 60.0 / ((double)(int)validHeight / MinHeight);
		CalendarGeoHelper.CalendarHourHeightChanged?.Invoke(null, validHeight);
	}

	private static double TransVerticalOffset(DateTime startDate)
	{
		return ((startDate - startDate.Date).TotalHours - (double)GetStartHourForTask()) * HourHeight - GetTopFoldDiff();
	}

	public static int GetCollapsedHours()
	{
		return _startHour + (24 - _endHour);
	}

	public static double CalculateCellHeight(DateTime startDate, DateTime? dueDate)
	{
		double hourHeight = HourHeight;
		_ = MinMinute;
		if (!dueDate.HasValue || dueDate == startDate)
		{
			return MinPointHeight;
		}
		DateTime dateTime = startDate.Date.AddHours(GetEndHour() + ((!TopFolded) ? 1 : 0));
		if (dueDate.Value < dateTime)
		{
			dateTime = dueDate.Value;
		}
		int num = (int)((dateTime - startDate).TotalHours * hourHeight);
		return Math.Max(MinHeight, num);
	}

	public static int TranslateMinute(double height)
	{
		double num = HourHeight / 60.0;
		int num2 = (int)(height * 1.0 / num);
		int num3 = num2 % 15;
		if (num3 >= 7)
		{
			return num2 + (15 - num3);
		}
		return num2 - num3;
	}

	public static DateTime TranslateVerticalOffset(DateTime date, double offset)
	{
		int num = (int)(offset / QuarterHourHeight);
		return date.Date.AddMinutes(num * 15).AddHours(GetStartHourForTask());
	}

	public static DateTime TranslateVerticalOffsetToStart(DateTime date, double offset)
	{
		int num = (int)(offset / QuarterHourHeight);
		return date.Date.AddMinutes(num * 15).AddHours(GetStartHourForTask());
	}

	public static TaskCellViewModel BuildAddTaskModel(DateTime date, double verticalOffset)
	{
		verticalOffset += GetTopFoldDiff();
		int num = (int)(verticalOffset / QuarterHourHeight);
		double num2 = (double)num * QuarterHourHeight;
		DateTime value = date.Date.AddMinutes(num * 15).AddHours(GetStartHourForTask());
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		TaskBaseViewModel taskBaseViewModel = new TaskBaseViewModel
		{
			Id = Utils.GetGuid(),
			Type = DisplayType.Task,
			Kind = "TEXT",
			Priority = defaultSafely.Priority,
			Tag = defaultSafely.TagString,
			Tags = defaultSafely.Tags?.ToArray(),
			StartDate = value,
			Status = 0,
			Title = string.Empty,
			IsAllDay = false
		};
		if (defaultSafely.DateMode == 1)
		{
			taskBaseViewModel.DueDate = value.AddMinutes(defaultSafely.Duration);
		}
		TaskCellViewModel taskCellViewModel = new TaskCellViewModel();
		taskCellViewModel.SourceViewModel = taskBaseViewModel;
		taskCellViewModel.Height = MinPointHeight;
		taskCellViewModel.Color = "transparent";
		taskCellViewModel.Selected = true;
		taskCellViewModel.HorizontalOffset = 0.0;
		taskCellViewModel.VerticalOffset = num2 - GetTopFoldDiff();
		taskCellViewModel.NewAdd = true;
		taskCellViewModel.SetIcon();
		return taskCellViewModel;
	}

	public static void AssemblyCells(List<TaskCellViewModel> cells, double width, DateTime date, DateTime? spanStart = null, double? hourHeight = null)
	{
		if (cells == null || cells.Count == 0)
		{
			return;
		}
		IEnumerable<CellGeo> enumerable = AssemblyCellGeoInfo(cells, width, date, spanStart, hourHeight);
		Dictionary<string, CellGeo> dictionary = new Dictionary<string, CellGeo>();
		foreach (CellGeo item in enumerable)
		{
			if (item.Id != null && !dictionary.ContainsKey(item.Id))
			{
				dictionary.Add(item.Id, item);
			}
		}
		if (dictionary.Count <= 0)
		{
			return;
		}
		foreach (TaskCellViewModel cell in cells)
		{
			if (string.IsNullOrEmpty(cell.Identity))
			{
				continue;
			}
			if (dictionary.ContainsKey(cell.Identity))
			{
				CellGeo cellGeo = dictionary[cell.Identity];
				if (cellGeo != null)
				{
					cell.Width = cellGeo.Width;
					cell.Height = cellGeo.Height;
					cell.HorizontalOffset = cellGeo.Left;
					cell.VerticalOffset = cellGeo.Top;
					cell.Tips = cellGeo.Tips;
					cell.ZIndex = cellGeo.Index;
				}
			}
			else
			{
				UtilLog.Info("CalendarGeoHelper.AssemblyCells, " + cell.Identity + " not assembly");
			}
		}
	}

	private static IEnumerable<CellGeo> AssemblyCellGeoInfoTemp(IReadOnlyCollection<TaskCellViewModel> cells, double width, DateTime date, DateTime? spanStart, double? hourHeight = null)
	{
		if (cells != null && cells.Count > 0)
		{
			List<TaskCellViewModel> list = cells.Where((TaskCellViewModel c) => c != null).ToList();
			list.Sort((TaskCellViewModel a, TaskCellViewModel b) => CalendarSortHelper.CompareForTaskCell(a, b, DateTime.Today));
			List<CellGeo> list2 = (from m in list
				select spanStart.HasValue ? CellGeo.Build(m, hourHeight ?? 60.0, spanStart.Value) : CellGeo.Build(m, date) into x
				where x?.Id != null
				orderby x.Top, x.Height descending
				select x).ToList();
			List<List<List<CellGeo>>> list3 = AssemblyGroupsTemp(list2.ToList());
			double num = width * 0.05;
			{
				foreach (List<List<CellGeo>> item in list3)
				{
					for (int num2 = 0; num2 < item.Count; num2++)
					{
						List<CellGeo> list4 = item[num2];
						if (num2 == 0 || item[num2 - 1].Count == 0)
						{
							double num3 = (width - 9.0) / (double)list4.Count;
							for (int num4 = 0; num4 < list4.Count; num4++)
							{
								CellGeo cellGeo = list4[num4];
								cellGeo.Left = num3 * (double)num4;
								cellGeo.Width = num3 - 2.0;
								cellGeo.Index = 0;
							}
							continue;
						}
						int num5 = num2 - 1;
						List<CellGeo> list5 = null;
						do
						{
							list5 = item[num5];
							if (list5.Count == 0)
							{
								break;
							}
							double minTop = list5.Min((CellGeo m) => m.Top);
							double maxBottom = list5.Max((CellGeo m) => m.Bottom);
							if (list4.Any((CellGeo c) => c.Top >= minTop && c.Top < maxBottom))
							{
								break;
							}
							num5--;
						}
						while (num5 >= 0);
						if (list5.Count == 0)
						{
							double num6 = (width - 9.0) / (double)list4.Count;
							for (int num7 = 0; num7 < list4.Count; num7++)
							{
								CellGeo cellGeo2 = list4[num7];
								cellGeo2.Left = num6 * (double)num7;
								cellGeo2.Width = num6 - 2.0;
								cellGeo2.Index = 0;
							}
							continue;
						}
						int count = list4.Count;
						bool flag = count <= list5.Count;
						int num8 = count / list5.Count;
						int num9 = count % list5.Count;
						int num10 = 0;
						for (int num11 = 0; num11 < list5.Count; num11++)
						{
							CellGeo cellGeo3 = list5[num11];
							double num12 = cellGeo3.Width - num;
							if (flag)
							{
								if (list4.Count > num11)
								{
									CellGeo cellGeo4 = list4[num11];
									cellGeo4.Left = cellGeo3.Left + num;
									cellGeo4.Width = num12;
									cellGeo4.Index = num2;
									if (!cellGeo3.ChildTop.HasValue)
									{
										cellGeo3.ChildTop = cellGeo4.Top;
									}
								}
								continue;
							}
							int num13 = num8 + ((num11 + num9 >= list5.Count) ? 1 : 0);
							double num14 = num12 / (double)num13;
							for (int num15 = num10; num15 < num10 + num13; num15++)
							{
								CellGeo cellGeo5 = list4[num15];
								cellGeo5.Left = cellGeo3.Left + num + num14 * (double)(num15 - num10);
								cellGeo5.Width = num14;
								cellGeo5.Index = num2;
								if (!cellGeo3.ChildTop.HasValue)
								{
									cellGeo3.ChildTop = cellGeo5.Top;
								}
							}
							num10 += num13;
						}
					}
				}
				return list2;
			}
		}
		return null;
	}

	private static IEnumerable<CellGeo> AssemblyCellGeoInfo(IReadOnlyCollection<TaskCellViewModel> cells, double width, DateTime date, DateTime? spanStart, double? hourHeight = null)
	{
		if (cells != null && cells.Count > 0)
		{
			List<TaskCellViewModel> list = cells.Where((TaskCellViewModel c) => c != null).ToList();
			list.Sort((TaskCellViewModel a, TaskCellViewModel b) => CalendarSortHelper.CompareForTaskCell(a, b, DateTime.Today));
			List<(CellGeo, int)> list2 = AssemblyGroups((from m in list
				select spanStart.HasValue ? CellGeo.Build(m, hourHeight ?? 60.0, spanStart.Value) : CellGeo.Build(m, date) into x
				where x?.Id != null
				orderby x.Top, x.Height descending
				select x).ToList());
			ConcurrentBag<CellGeo> concurrentBag = new ConcurrentBag<CellGeo>();
			foreach (var item3 in list2)
			{
				CellGeo item = item3.Item1;
				int item2 = item3.Item2;
				double oneWidth = (width - 9.0 - 2.0) / (double)item2;
				TraversalCellGeo(item, oneWidth, item2, concurrentBag);
			}
			return concurrentBag.ToList();
		}
		return null;
	}

	private static void TraversalCellGeo(CellGeo node, double oneWidth, double fullDeep, ConcurrentBag<CellGeo> solvedBag)
	{
		if (solvedBag.Contains(node))
		{
			return;
		}
		solvedBag.Add(node);
		if (node.LeftParent == null && !node.LeftSibling.Any())
		{
			node.Left = 0.0;
			if (node.RightParent != null && Math.Abs(node.Top - node.RightParent.Top) > 0.01)
			{
				node.Width = node.RightParent.Left - 1.0;
			}
			else
			{
				node.Width = (double)(node.Column + 1) * oneWidth - 1.0;
			}
		}
		else if (node.RightParent == null && !node.RightSibling.Any())
		{
			node.Left = oneWidth * (double)node.Column;
			node.Width = (fullDeep - (double)node.Column) * oneWidth - 1.0;
		}
		else if (node.RightParent != null && node.LeftParent != null)
		{
			node.Left = oneWidth * (double)(node.LeftParent.Column + 1);
			node.Width = (double)(node.RightParent.LeftParent.Column - node.LeftParent.Column) * oneWidth - 1.0;
		}
		else
		{
			node.Left = oneWidth * (double)node.Column;
			node.Width = oneWidth - 1.0;
		}
		if (node.RightSibling.Any())
		{
			foreach (CellGeo item in node.RightSibling)
			{
				TraversalCellGeo(item, oneWidth, fullDeep, solvedBag);
			}
		}
		if (!node.LeftSibling.Any())
		{
			return;
		}
		foreach (CellGeo item2 in node.LeftSibling)
		{
			TraversalCellGeo(item2, oneWidth, fullDeep, solvedBag);
		}
	}

	private static void AssembleChildren(CellGeo node, List<CellGeo> restNodes, int column, HashSet<CellGeo> reloved)
	{
		if (reloved.Contains(node))
		{
			return;
		}
		reloved.Add(node);
		node.Column = column;
		List<CellGeo> list = restNodes.Where(delegate(CellGeo n)
		{
			if (n.Id == node.Id)
			{
				return false;
			}
			if (n.Id == node.LeftParent?.Id)
			{
				return false;
			}
			if (n.Id == node.RightParent?.Id)
			{
				return false;
			}
			if (n.RightParent != null && node.LeftParent != null)
			{
				for (CellGeo leftParent = node.LeftParent; leftParent != null; leftParent = leftParent.LeftParent)
				{
					if (leftParent == n.RightParent)
					{
						return false;
					}
				}
			}
			return (node.RightParent == null || !(n.Top < node.RightParent.Bottom)) && StartTimeContains(node, n);
		}).ToList();
		if (!list.Any())
		{
			return;
		}
		List<CellGeo> list2 = list.Where(delegate(CellGeo n)
		{
			if (column == 0)
			{
				return false;
			}
			if (n.RightParent != null && n.RightParent.Column > node.Column)
			{
				return false;
			}
			if (n.LeftParent == null && n.RightParent == null && node.Bottom <= n.Top)
			{
				return false;
			}
			if (node.Column <= n.Column && (n.LeftParent == null || n.LeftParent.Column >= node.Column))
			{
				return false;
			}
			CellGeo cellGeo3 = node;
			while (cellGeo3.LeftParent != null)
			{
				cellGeo3 = cellGeo3.LeftParent;
				if (cellGeo3.LeftParent == null || cellGeo3.LeftParent == n.LeftParent)
				{
					return cellGeo3.Bottom <= n.Top;
				}
				if (cellGeo3.LeftParent.RightSibling.Max((CellGeo child) => child.Bottom) > n.Top)
				{
					return false;
				}
			}
			return cellGeo3.Bottom <= n.Top;
		}).ToList();
		if (list2.Any())
		{
			list2 = list2.OrderBy((CellGeo time) => time.Top).ToList();
			CellGeo cellGeo = list2.First();
			foreach (CellGeo item in list2)
			{
				if (!StartTimeContains(cellGeo, item))
				{
					cellGeo = item;
				}
				cellGeo.RightParent = node;
			}
		}
		node.LeftSibling = list2.Where((CellGeo child) => child.RightParent == node).ToList();
		list.RemoveAll((CellGeo child) => child.RightParent == node);
		List<CellGeo> list3 = (from time in list
			where time.LeftParent == null || (time.LeftParent != null && time.LeftParent.Column < node.Column)
			orderby time.Top
			select time).ToList();
		if (list3.Any())
		{
			CellGeo cellGeo2 = list3.First();
			if (Math.Abs(cellGeo2.Top - node.Top) < 0.01)
			{
				node.RightParent = cellGeo2;
			}
			foreach (CellGeo item2 in list3)
			{
				if (!StartTimeContains(cellGeo2, item2))
				{
					cellGeo2 = item2;
				}
				cellGeo2.LeftParent = node;
			}
		}
		node.RightSibling = list.Where((CellGeo child) => child.LeftParent == node).ToList();
		foreach (CellGeo item3 in node.RightSibling)
		{
			AssembleChildren(item3, restNodes, column + 1, reloved);
		}
		foreach (CellGeo item4 in node.LeftSibling)
		{
			AssembleChildren(item4, restNodes, column - 1, reloved);
		}
	}

	private static bool StartTimeContains(CellGeo node1, CellGeo node2)
	{
		if (MathUtil.DoubleLe(node1.Top, node2.Top, 0.1))
		{
			return MathUtil.DoubleLt(node2.Top, node1.Bottom, 0.1);
		}
		return false;
	}

	private static List<List<List<CellGeo>>> AssemblyGroupsTemp(List<CellGeo> geos)
	{
		List<List<List<CellGeo>>> list = new List<List<List<CellGeo>>>();
		List<List<CellGeo>> list2 = new List<List<CellGeo>>();
		List<CellGeo> list3 = new List<CellGeo>();
		list.Add(list2);
		list2.Add(list3);
		List<CellGeo> list4 = new List<CellGeo>();
		while (geos.Any())
		{
			CellGeo current = geos.First();
			geos.Remove(current);
			if (list4.Count == 0 || list4.Any((CellGeo o) => o.IsCross(current)))
			{
				if (list3.Count != 0 && list3.All((CellGeo c) => Math.Abs(current.Top - c.Top) >= 32.0))
				{
					list3 = new List<CellGeo>();
					list2.Add(list3);
				}
				list4.Add(current);
			}
			else
			{
				list4.Clear();
				list4.Add(current);
				list2 = new List<List<CellGeo>>();
				list3 = new List<CellGeo>();
				list.Add(list2);
				list2.Add(list3);
			}
			list3.Add(current);
		}
		return list;
	}

	private static List<(CellGeo, int)> AssemblyGroups(List<CellGeo> geos)
	{
		List<(CellGeo, int)> list = new List<(CellGeo, int)>();
		while (geos.Any())
		{
			CellGeo cellGeo = geos.First();
			geos.Remove(cellGeo);
			HashSet<CellGeo> hashSet = new HashSet<CellGeo>();
			AssembleChildren(cellGeo, geos, 0, hashSet);
			int item = hashSet.Max((CellGeo cellGeo2) => cellGeo2.Column) + 1;
			list.Add((cellGeo, item));
			foreach (CellGeo item2 in hashSet)
			{
				geos.Remove(item2);
			}
		}
		return list;
	}

	public static double GetWeekColumnWidth(double sectionWidth, int count)
	{
		return (sectionWidth - 56.0) / (double)count;
	}

	public static double CalHorizontalOffset(double x, double width)
	{
		int num = (int)((x - 56.0) / width);
		if (num >= 0)
		{
			return 56.0 + (double)num * width + 1.0;
		}
		return x - 12.0;
	}

	public static double GetBarWidth(double dayWidth, int column, int range)
	{
		double num = 0.0;
		for (int i = column; i < column + range; i++)
		{
			if (!IsColumnHide(i))
			{
				num += dayWidth;
			}
		}
		return num;
	}

	public static double GetBarLeft(double dayWidth, int column)
	{
		double num = 0.0;
		for (int i = 0; i < column; i++)
		{
			if (!IsColumnHide(i))
			{
				num += dayWidth;
			}
		}
		return num;
	}

	public static int GetRealColumn(int column)
	{
		if (!LocalSettings.Settings.ShowCalWeekend)
		{
			switch (LocalSettings.Settings.WeekStartFromType)
			{
			case DayOfWeek.Sunday:
				column--;
				break;
			case DayOfWeek.Saturday:
				column -= 2;
				break;
			}
		}
		return column;
	}

	public static bool IsColumnHide(int column)
	{
		if (!LocalSettings.Settings.ShowCalWeekend)
		{
			switch (LocalSettings.Settings.WeekStartFromType)
			{
			case DayOfWeek.Sunday:
				if (column != 0)
				{
					return column == 6;
				}
				return true;
			case DayOfWeek.Saturday:
				if (column != 0)
				{
					return column == 1;
				}
				return true;
			case DayOfWeek.Monday:
				if (column != 5)
				{
					return column == 6;
				}
				return true;
			}
		}
		return false;
	}

	public static double GetMonthDragBarWidth(double width, int count)
	{
		return width / (double)count + 1.0;
	}

	public static void SetStartHour(int hour)
	{
		if (hour < _endHour)
		{
			_startHour = hour;
			LocalSettings.Settings.CollapsedStart = hour;
		}
	}

	public static int GetStartHourForTask()
	{
		if (!TopFolded)
		{
			return 0;
		}
		return _startHour - 1;
	}

	public static int GetStartHour(bool display = true)
	{
		if (!display || TopFolded)
		{
			return _startHour;
		}
		return 0;
	}

	public static int GetEndHour(bool display = true)
	{
		if (!display || TopFolded)
		{
			return _endHour;
		}
		return 24;
	}

	public static void SetEndHour(int hour)
	{
		if (hour > _startHour)
		{
			_endHour = hour;
			LocalSettings.Settings.CollapsedEnd = hour;
		}
	}

	public static int GetTimeBlockCount()
	{
		if (!TopFolded)
		{
			return 24;
		}
		return _endHour - _startHour + 1;
	}

	public static int GetPointOffset()
	{
		int hour = DateTime.Now.Hour;
		int startHourForTask = GetStartHourForTask();
		int endHour = GetEndHour();
		if (hour >= startHourForTask && hour < endHour)
		{
			return (int)((DateTime.Now - DateTime.Today.AddHours(startHourForTask)).TotalMinutes * (HourHeight * 1.0 / 60.0)) - (int)GetTopFoldDiff();
		}
		return -1;
	}

	public static double GetRoundOffset(double currentY)
	{
		double quarterHourHeight = QuarterHourHeight;
		int num = (int)(currentY / quarterHourHeight);
		double num2 = quarterHourHeight * (double)(num - 1);
		double num3 = quarterHourHeight * (double)num;
		double num4 = quarterHourHeight * (double)(num + 1);
		double val = Math.Abs(currentY - num3);
		double num5 = Math.Abs(currentY - num2);
		double num6 = Math.Abs(currentY - num4);
		double result = num3;
		double num7 = Math.Min(Math.Min(val, num5), num6);
		if (Math.Abs(num7 - num6) <= 0.001)
		{
			result = num4;
		}
		else if (Math.Abs(num7 - num5) <= 0.001)
		{
			result = num2;
		}
		return result;
	}

	public static void NotifyTopUnFolded(bool isTop)
	{
		CalendarGeoHelper.ExpandTop?.Invoke(null, isTop);
	}

	public static void GetCellPosition(TaskCellViewModel cellModel)
	{
		if (!cellModel.BaseDate.HasValue)
		{
			return;
		}
		CellGeo cellGeo = CellGeo.Build(cellModel, cellModel.BaseDate.Value.Date);
		if (!(cellModel.IsAllDay ?? true) && cellModel.DueDate.HasValue)
		{
			TimeSpan? timeSpan = cellModel.DueDate - cellModel.StartDate;
			if (!timeSpan.HasValue || !(timeSpan.GetValueOrDefault().TotalHours > 24.0))
			{
				cellModel.Height = cellGeo.Height;
				goto IL_00e7;
			}
		}
		cellModel.Height = MinPointHeight;
		cellModel.SourceViewModel.DueDate = null;
		goto IL_00e7;
		IL_00e7:
		cellModel.HorizontalOffset = cellGeo.Left;
		cellModel.VerticalOffset = cellGeo.Top;
		cellModel.SourceViewModel.IsAllDay = false;
	}

	public static void ResetCellHeightAndVertical(TaskCellViewModel cellModel, DateTime? baseDate)
	{
		if (baseDate.HasValue)
		{
			CellGeo cellGeo = CellGeo.Build(cellModel, baseDate.Value.Date);
			cellModel.Height = cellGeo.Height;
			cellModel.VerticalOffset = cellGeo.Top;
		}
	}

	public static double GetTopFoldDiff()
	{
		if (!TopFolded)
		{
			return 0.0;
		}
		return HourHeight - 32.0;
	}

	public static List<string> GetDisplayTimelineTimeZoneNames()
	{
		if (!LocalSettings.Settings.EnableCalendarTimeZone)
		{
			return new List<string> { TimeZoneData.LocalTimeZoneModel.TimeZoneName };
		}
		CalTimelineTimeZones calendarDisplayTimeZones = LocalSettings.Settings.CalendarDisplayTimeZones;
		List<string> list = new List<string>();
		if (calendarDisplayTimeZones != null && calendarDisplayTimeZones.additional?.Count > 0)
		{
			foreach (CalTimeZone item in calendarDisplayTimeZones.additional)
			{
				TimeZoneInfo timeZoneInfo = TimeZoneUtils.GetTimeZoneInfo(item.timeZone);
				if (timeZoneInfo != null)
				{
					List<string> hiddenCalTimeZones = LocalSettings.Settings.ExtraSettings.HiddenCalTimeZones;
					if (hiddenCalTimeZones == null || !hiddenCalTimeZones.Contains(item.timeZone))
					{
						list.Add(timeZoneInfo.DisplayName);
					}
				}
			}
		}
		list.Add(TimeZoneData.LocalTimeZoneModel.TimeZoneName);
		return list;
	}

	public static List<string> GetDisplayTimelineTimeZones(bool withCityName = true)
	{
		if (!LocalSettings.Settings.EnableCalendarTimeZone)
		{
			return new List<string> { TimeZoneData.LocalTimeZoneModel.TimeZoneName };
		}
		CalTimelineTimeZones calendarDisplayTimeZones = LocalSettings.Settings.CalendarDisplayTimeZones;
		List<string> list = new List<string>();
		if (calendarDisplayTimeZones != null && calendarDisplayTimeZones.additional?.Count > 0)
		{
			for (int num = calendarDisplayTimeZones.additional.Count - 1; num >= 0; num--)
			{
				CalTimeZone calTimeZone = calendarDisplayTimeZones.additional[num];
				TimeZoneInfo timeZoneInfo = TimeZoneUtils.GetTimeZoneInfo(calTimeZone.timeZone);
				if (timeZoneInfo != null)
				{
					List<string> hiddenCalTimeZones = LocalSettings.Settings.ExtraSettings.HiddenCalTimeZones;
					if (hiddenCalTimeZones == null || !hiddenCalTimeZones.Contains(calTimeZone.timeZone))
					{
						list.Add((!string.IsNullOrWhiteSpace(calTimeZone.label)) ? calTimeZone.label : (withCityName ? TimeZoneUtils.GetTimeZoneDisplayName(timeZoneInfo) : TimeZoneUtils.GetTimeZoneUtcName(timeZoneInfo)));
					}
				}
			}
		}
		string text = calendarDisplayTimeZones?.current?.FirstOrDefault((CalTimeZone t) => t.timeZone == TimeZoneData.LocalTimeZoneModel.TimeZoneName)?.label;
		list.Add((!string.IsNullOrWhiteSpace(text)) ? text : (withCityName ? TimeZoneUtils.GetTimeZoneDisplayName(TimeZoneInfo.Local) : TimeZoneUtils.GetTimeZoneUtcName(TimeZoneInfo.Local)));
		return list;
	}

	public static int GetTimeZoneCount()
	{
		return GetDisplayTimelineTimeZones().Count;
	}

	public static double GetCellTextFontSize(int height)
	{
		double calTitleFontSize = CalTitleFontSize;
		double num;
		if (height >= 10)
		{
			num = Math.Max(9.0, (double)height * 0.8);
			if (num > calTitleFontSize)
			{
				return calTitleFontSize;
			}
		}
		else
		{
			num = calTitleFontSize;
		}
		return num;
	}
}
