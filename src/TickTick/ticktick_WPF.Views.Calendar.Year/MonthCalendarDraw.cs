using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils.Lang;
using ticktick_WPF.Entity.Calendar;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Calendar.Year;

public class MonthCalendarDraw : UserControl, IComponentConnector
{
	private DateTime _lastClickTime;

	private Point _firstClickPoint;

	private const int DoubleClickInterval = 400;

	private bool _contentLoaded;

	private Debouncer redrawDebouncer { get; set; }

	private Typeface typeface { get; set; }

	private YearViewMonthData viewModel { get; set; }

	public MonthCalendarDraw()
	{
		SetFont();
		InitializeComponent();
	}

	private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		viewModel = base.DataContext as YearViewMonthData;
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		if (viewModel != null)
		{
			redrawDebouncer = new Debouncer(10, OnRedrawTrigger);
			viewModel.ParentVM.PropertyChanged += ParentVM_PropertyChanged;
			YearCalendarViewModel parentVM = viewModel.ParentVM;
			parentVM.OnDateCountChange = (Action<DateTime>)Delegate.Combine(parentVM.OnDateCountChange, new Action<DateTime>(OnDateCountChange));
			YearCalendarViewModel parentVM2 = viewModel.ParentVM;
			parentVM2.OnForceRedraw = (Action)Delegate.Combine(parentVM2.OnForceRedraw, new Action(OnForceRedraw));
			DataChangedNotifier.FontFamilyChanged += DataChangedNotifier_FontFamilyChanged;
			InvalidateVisual();
		}
	}

	private void UserControl_Unloaded(object sender, RoutedEventArgs e)
	{
		if (viewModel != null)
		{
			redrawDebouncer?.Dispose();
			viewModel.ParentVM.PropertyChanged -= ParentVM_PropertyChanged;
			YearCalendarViewModel parentVM = viewModel.ParentVM;
			parentVM.OnDateCountChange = (Action<DateTime>)Delegate.Remove(parentVM.OnDateCountChange, new Action<DateTime>(OnDateCountChange));
			YearCalendarViewModel parentVM2 = viewModel.ParentVM;
			parentVM2.OnForceRedraw = (Action)Delegate.Remove(parentVM2.OnForceRedraw, new Action(OnForceRedraw));
			DataChangedNotifier.FontFamilyChanged -= DataChangedNotifier_FontFamilyChanged;
		}
	}

	private void DataChangedNotifier_FontFamilyChanged(object sender, EventArgs e)
	{
		SetFont();
		redrawDebouncer?.Trigger();
	}

	private void SetFont()
	{
		try
		{
			if (string.IsNullOrEmpty(LocalSettings.Settings.ExtraSettings?.AppFontFamily))
			{
				typeface = new Typeface(SystemFonts.MessageFontFamily.Source);
			}
			else
			{
				typeface = new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
			}
		}
		catch (Exception)
		{
			typeface = new Typeface(SystemFonts.MessageFontFamily.Source);
		}
	}

	private void OnRedrawTrigger()
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			InvalidateVisual();
		});
	}

	private void ParentVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "DayFontSize" || e.PropertyName == "ShowHeatMap" || e.PropertyName == "ThemeId")
		{
			redrawDebouncer?.Trigger();
		}
	}

	private void OnDateCountChange(DateTime date)
	{
		if (viewModel != null && date.Year == viewModel.Year && date.Month == viewModel.Month)
		{
			redrawDebouncer?.Trigger();
		}
	}

	private void OnForceRedraw()
	{
		redrawDebouncer?.Trigger();
	}

	protected override void OnRender(DrawingContext dc)
	{
		if (viewModel == null)
		{
			return;
		}
		base.OnRender(dc);
		double cellWidth = viewModel.ParentVM.CellWidth;
		double num = viewModel.ParentVM.CellHeight - 0.3;
		for (int i = 0; i < viewModel.Days.Count; i++)
		{
			YearViewCalendarDay yearViewCalendarDay = viewModel.Days[i];
			int num2 = i / 7;
			int num3 = i % 7;
			Point pos = new Point((double)num3 * cellWidth, (double)num2 * num);
			SolidColorBrush solidColorBrush = null;
			FormattedText formattedText = new FormattedText(foreground: yearViewCalendarDay.IsToday ? Brushes.White : ((!yearViewCalendarDay.IsCurrentMonth) ? ThemeUtil.GetColor("BaseColorOpacity40", this) : ThemeUtil.GetColor("BaseColorOpacity80", this)), textToFormat: yearViewCalendarDay.Day.ToString(), culture: CultureInfo.CurrentCulture, flowDirection: FlowDirection.LeftToRight, typeface: typeface, emSize: viewModel.ParentVM.DayFontSize, pixelsPerDip: VisualTreeHelper.GetDpi(this).PixelsPerDip);
			double num4 = Math.Max(formattedText.Width, formattedText.Height);
			DrawBackground(dc, yearViewCalendarDay, pos, cellWidth, num, num4 / 2.0 + 3.0);
			double dayLineHeight = viewModel.ParentVM.DayLineHeight;
			if (!double.IsNaN(dayLineHeight))
			{
				_ = 0.0;
			}
			double a = pos.Y + num / 2.0 - formattedText.Height / 2.0;
			dc.DrawText(formattedText, new Point(Math.Round(pos.X + cellWidth / 2.0 - formattedText.Width / 2.0), Math.Round(a)));
		}
	}

	private void DrawBackground(DrawingContext dc, YearViewCalendarDay day, Point pos, double w, double h, double r)
	{
		SolidColorBrush yearViewDayCellColor = CalendarUtils.GetYearViewDayCellColor(viewModel.ParentVM.ShowHeatMap, day.Count, day.IsToday, day.IsCurrentMonth);
		if (yearViewDayCellColor != null)
		{
			if (viewModel.ParentVM.ShowHeatMap)
			{
				dc.DrawRectangle(yearViewDayCellColor, null, new Rect(new Point(pos.X + 1.0, pos.Y + 1.0), new Size(w - 1.0, h - 1.0)));
			}
			else
			{
				dc.DrawEllipse(yearViewDayCellColor, null, new Point(pos.X + w / 2.0, pos.Y + h / 2.0), r, r);
			}
		}
		if (day.IsMouseOver)
		{
			SolidColorBrush solidColorBrush = null;
			solidColorBrush = ((!viewModel.ParentVM.ShowHeatMap) ? ThemeUtil.GetColor("BaseColorOpacity6", this) : ((day.Count <= 0) ? ThemeUtil.GetColor("BaseColorOpacity5", this) : ThemeUtil.GetColor("BaseColorOpacity3", this)));
			if (viewModel.ParentVM.ShowHeatMap)
			{
				dc.DrawRectangle(solidColorBrush, null, new Rect(new Point(pos.X + 1.0, pos.Y + 1.0), new Size(w - 1.0, h - 1.0)));
			}
			else
			{
				dc.DrawEllipse(solidColorBrush, null, new Point(pos.X + w / 2.0, pos.Y + h / 2.0), r, r);
			}
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (viewModel != null)
		{
			Point position = e.GetPosition(this);
			HighLightMouseOver(position);
		}
	}

	protected override void OnMouseLeave(MouseEventArgs e)
	{
		base.OnMouseLeave(e);
		if (viewModel != null)
		{
			YearViewCalendarDay yearViewCalendarDay = viewModel.Days.FirstOrDefault((YearViewCalendarDay d) => d.IsMouseOver);
			if (yearViewCalendarDay != null)
			{
				yearViewCalendarDay.IsMouseOver = false;
				InvalidateVisual();
			}
			base.Cursor = Cursors.Arrow;
		}
	}

	protected override void OnMouseUp(MouseButtonEventArgs e)
	{
		if (viewModel != null)
		{
			Point position = e.GetPosition(this);
			int indexFromPoint = GetIndexFromPoint(position);
			if (indexFromPoint >= 0)
			{
				HandleClick(viewModel.Days[indexFromPoint], position);
			}
		}
	}

	private async void HandleClick(YearViewCalendarDay day, Point clickPoint)
	{
		bool num = (DateTime.Now - _lastClickTime).TotalMilliseconds <= 400.0 && Math.Abs(clickPoint.X - _firstClickPoint.X) < 5.0 && Math.Abs(clickPoint.Y - _firstClickPoint.Y) < 5.0;
		_lastClickTime = DateTime.Now;
		_firstClickPoint = clickPoint;
		if (num)
		{
			YearViewPopup.TryClose();
			Utils.FindParent<CalendarControl>(this)?.NavigateDate(day.Date, CalendarViewType.Week);
		}
		else if (YearViewPopup.IsOpenForDate(day.Date) || YearViewPopup.WasRecentlyClosedForDate(day.Date))
		{
			YearViewPopup.TryClose();
		}
		else
		{
			YearViewPopup.TryClose();
			await ShowYearViewPopup(day);
		}
	}

	private async Task ShowYearViewPopup(YearViewCalendarDay day)
	{
		int num = viewModel.Days.IndexOf(day);
		if (num >= 0)
		{
			int num2 = num % 7;
			int num3 = num / 7;
			double cellWidth = viewModel.ParentVM.CellWidth;
			double num4 = viewModel.ParentVM.CellHeight - 0.3;
			Point cellCenter = new Point((double)num2 * cellWidth + cellWidth / 2.0, (double)num3 * num4 + num4 / 2.0);
			List<CalendarDisplayViewModel> children = await GetTasksForDate(day.Date);
			await new YearViewPopup(day.Date, this, cellCenter, cellWidth, GetWidgetThemeId(), Utils.FindParent<IToastShowWindow>(this))
			{
				Owner = Window.GetWindow(this),
				Width = 220.0
			}.ShowPopup(children);
		}
	}

	private string GetWidgetThemeId()
	{
		CalendarWidget calendarWidget = Utils.FindParent<CalendarWidget>(this);
		if (calendarWidget != null)
		{
			return calendarWidget.ThemeId;
		}
		return string.Empty;
	}

	internal async Task<List<CalendarDisplayViewModel>> GetTasksForDate(DateTime date)
	{
		try
		{
			CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
			ProjectExtra projectFilter = ((calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
			List<CalendarDisplayModel> obj = (await CalendarDisplayService.GetDisplayModels(date.Date, date.Date.AddDays(1.0), withCourses: true, withFocus: false, projectFilter, pullRemote: false))?.Where(delegate(CalendarDisplayModel t)
			{
				if (t.DisplayStartDate.HasValue)
				{
					if (!t.DisplayDueDate.HasValue)
					{
						return date.Date == t.DisplayStartDate.Value.Date;
					}
					if (date.Date.AddDays(1.0) > t.DisplayStartDate.Value)
					{
						return date.Date < t.DisplayDueDate.Value;
					}
					return false;
				}
				return false;
			}).ToList() ?? new List<CalendarDisplayModel>();
			obj.Sort(delegate(CalendarDisplayModel a, CalendarDisplayModel b)
			{
				bool flag = CalendarSortHelper.IsSpanItem(a.DisplayStartDate, a.DisplayDueDate, a.IsAllDay);
				bool flag2 = CalendarSortHelper.IsSpanItem(b.DisplayStartDate, b.DisplayDueDate, b.IsAllDay);
				if (flag != flag2)
				{
					if (!flag)
					{
						return 1;
					}
					return -1;
				}
				return flag ? CalendarSortHelper.CompareForGridSpanItems(a, b, DateTime.Today) : CalendarSortHelper.CompareForGridPointItems(a, b, DateTime.Today);
			});
			return obj.Select(delegate(CalendarDisplayModel task, int index)
			{
				CalendarDisplayViewModel calendarDisplayViewModel = CalendarDisplayViewModel.Build(task);
				calendarDisplayViewModel.Row = index;
				return calendarDisplayViewModel;
			}).ToList();
		}
		catch (Exception ex)
		{
			UtilLog.Error("GetTasksForDate error: " + ex.Message);
			return new List<CalendarDisplayViewModel>();
		}
	}

	private int GetIndexFromPoint(Point point)
	{
		double cellWidth = viewModel.ParentVM.CellWidth;
		double num = viewModel.ParentVM.CellHeight - 0.3;
		int num2 = (int)(point.X / cellWidth);
		int num3 = (int)(point.Y / num);
		if (num2 < 0 || num2 >= 7 || num3 < 0)
		{
			return -1;
		}
		int num4 = num3 * 7 + num2;
		if (num4 < 0 || num4 >= viewModel.Days.Count)
		{
			return -1;
		}
		return num4;
	}

	public void DragHighLight(bool highlight = true)
	{
		if (highlight)
		{
			Point position = Mouse.GetPosition(this);
			HighLightMouseOver(position);
			return;
		}
		YearViewCalendarDay yearViewCalendarDay = viewModel.Days.FirstOrDefault((YearViewCalendarDay d) => d.IsMouseOver);
		if (yearViewCalendarDay != null)
		{
			yearViewCalendarDay.IsMouseOver = false;
			InvalidateVisual();
		}
	}

	public DateTime? GetMouseoverDay()
	{
		Point position = Mouse.GetPosition(this);
		int indexFromPoint = GetIndexFromPoint(position);
		if (indexFromPoint >= 0)
		{
			return viewModel.Days[indexFromPoint].Date;
		}
		return null;
	}

	private void HighLightMouseOver(Point point)
	{
		int indexFromPoint = GetIndexFromPoint(point);
		bool flag = indexFromPoint >= 0;
		if (flag && !viewModel.Days[indexFromPoint].IsMouseOver)
		{
			YearViewCalendarDay yearViewCalendarDay = viewModel.Days.FirstOrDefault((YearViewCalendarDay d) => d.IsMouseOver);
			if (yearViewCalendarDay != null)
			{
				yearViewCalendarDay.IsMouseOver = false;
			}
			viewModel.Days[indexFromPoint].IsMouseOver = true;
			InvalidateVisual();
			base.Cursor = Cursors.Hand;
		}
		else if (!flag)
		{
			YearViewCalendarDay yearViewCalendarDay2 = viewModel.Days.FirstOrDefault((YearViewCalendarDay d) => d.IsMouseOver);
			if (yearViewCalendarDay2 != null)
			{
				yearViewCalendarDay2.IsMouseOver = false;
				InvalidateVisual();
			}
			base.Cursor = Cursors.Arrow;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/year/monthcalendardraw.xaml", UriKind.Relative);
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
			((MonthCalendarDraw)target).DataContextChanged += UserControl_DataContextChanged;
			((MonthCalendarDraw)target).Loaded += UserControl_Loaded;
			((MonthCalendarDraw)target).Unloaded += UserControl_Unloaded;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
