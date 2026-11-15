using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Timeline;

public class TimelineHeader : Canvas
{
	public static readonly DependencyProperty OneDayWidthProperty = DependencyProperty.Register("OneDayWidth", typeof(double), typeof(TimelineHeader), new PropertyMetadata(0.0, OnDependencyChanged));

	public static readonly DependencyProperty StartEndTupleProperty = DependencyProperty.Register("StartEndTuple", typeof(Tuple<DateTime, DateTime>), typeof(TimelineHeader), new PropertyMetadata(null, OnDependencyChanged));

	public static readonly DependencyProperty ShowWeekDayProperty = DependencyProperty.Register("ShowWeekDay", typeof(bool), typeof(TimelineHeader), new PropertyMetadata(false, OnDependencyChanged));

	public static readonly DependencyProperty ShowHolidayProperty = DependencyProperty.Register("ShowHoliday", typeof(bool), typeof(TimelineHeader), new PropertyMetadata(false, OnDependencyChanged));

	public static readonly DependencyProperty HoverStartEndTupleTupleProperty = DependencyProperty.Register("HoverStartEndTuple", typeof(List<Tuple<DateTime, DateTime>>), typeof(TimelineHeader), new PropertyMetadata(null, OnDependencyChanged));

	public static readonly DependencyProperty TimelineDayWidthIndexProperty = DependencyProperty.Register("TimelineDayWidthIndex", typeof(int), typeof(TimelineHeader), new PropertyMetadata(4, OnDependencyChanged));

	private DelayActionHandler _delayRender;

	public double OneDayWidth
	{
		get
		{
			return (double)GetValue(OneDayWidthProperty);
		}
		set
		{
			SetCurrentValue(OneDayWidthProperty, value);
		}
	}

	public Tuple<DateTime, DateTime> StartEndTuple
	{
		get
		{
			return (Tuple<DateTime, DateTime>)GetValue(StartEndTupleProperty);
		}
		set
		{
			SetCurrentValue(StartEndTupleProperty, value);
		}
	}

	public bool ShowWeekDay
	{
		get
		{
			return (bool)GetValue(ShowWeekDayProperty);
		}
		set
		{
			SetCurrentValue(ShowWeekDayProperty, value);
		}
	}

	public bool ShowHoliday
	{
		get
		{
			return (bool)GetValue(ShowHolidayProperty);
		}
		set
		{
			SetCurrentValue(ShowHolidayProperty, value);
		}
	}

	public List<Tuple<DateTime, DateTime>> HoverStartEndTuple
	{
		get
		{
			return (List<Tuple<DateTime, DateTime>>)GetValue(HoverStartEndTupleTupleProperty);
		}
		set
		{
			SetCurrentValue(HoverStartEndTupleTupleProperty, value);
		}
	}

	public int TimelineDayWidthIndex
	{
		get
		{
			return (int)GetValue(TimelineDayWidthIndexProperty);
		}
		set
		{
			SetCurrentValue(TimelineDayWidthIndexProperty, value);
		}
	}

	private static void OnDependencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((TimelineHeader)d).InvalidateVisual();
	}

	public TimelineHeader()
	{
		base.ClipToBounds = true;
		base.IsHitTestVisible = false;
		_delayRender = new DelayActionHandler(20);
		_delayRender.DoAction += delegate
		{
			base.Dispatcher.Invoke(base.InvalidateVisual);
		};
		base.Loaded += delegate
		{
			LocalSettings.Settings.PropertyChanged += OnSettingsChanged;
		};
		base.Unloaded += delegate
		{
			LocalSettings.Settings.PropertyChanged -= OnSettingsChanged;
		};
	}

	private void DrawWeekName(DrawingContext dc, string name, double x, double width, bool isToday, string weekDayText = null)
	{
		SolidColorBrush foreground = (isToday ? new SolidColorBrush(TimelineThemes.DayLineColor) : TimelineThemes.NormalDayBrush);
		FormattedText formattedText = new FormattedText(name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12.0, foreground);
		dc.DrawText(formattedText, new Point(x + (width - formattedText.Width) / 2.0, base.Height - 14.0 - formattedText.Height / 2.0));
		DragWeekText(dc, weekDayText, isToday, x, width);
	}

	private void DragWeekText(DrawingContext dc, string weekDayText, bool isToday, double x, double width)
	{
		if (!string.IsNullOrEmpty(weekDayText))
		{
			SolidColorBrush foreground = (isToday ? new SolidColorBrush(TimelineThemes.DayLineColor) : TimelineThemes.WeekendDayBrush);
			FormattedText formattedText = new FormattedText(weekDayText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 10.0, foreground);
			dc.DrawText(formattedText, new Point(x + (width - formattedText.Width) / 2.0, 11.0 - formattedText.Height / 2.0));
		}
	}

	private void DrawDayName(DrawingContext dc, string name, double x, double width, bool isToday, bool isWeekends = false, bool drawText = true, bool isRest = false, bool isWork = false, string weekDayText = null)
	{
		if (drawText || isRest || isWork)
		{
			SolidColorBrush foreground = (isToday ? new SolidColorBrush(TimelineThemes.DayLineColor) : (isWeekends ? TimelineThemes.WeekendDayBrush : TimelineThemes.NormalDayBrush));
			FormattedText formattedText = new FormattedText(name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12.0, foreground);
			if (isRest || isWork)
			{
				Rect rectangle = new Rect(x + (width + formattedText.Width + 2.0) / 2.0, base.Height - 28.0 + 3.0, 10.0, 10.0);
				DrawingImage imageSource = Utils.GetImageSource(isRest ? "RestDrawingImage" : "WorkDrawingImage");
				dc.DrawImage(imageSource, rectangle);
			}
			if (drawText)
			{
				dc.DrawText(formattedText, new Point(x + (width - formattedText.Width) / 2.0, base.Height - 14.0 - formattedText.Height / 2.0));
				DragWeekText(dc, weekDayText, isToday, x, width);
			}
		}
	}

	private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "ThemeId")
		{
			InvalidateVisual();
		}
	}

	protected override void OnRender(DrawingContext dc)
	{
		base.OnRender(dc);
		if (!(base.ActualWidth <= 0.0) && StartEndTuple != null)
		{
			switch (TimelineConstants.GetDayWidthRange(TimelineDayWidthIndex))
			{
			case "month":
				DrawWeek(dc);
				break;
			default:
				DrawDay(dc);
				break;
			}
		}
	}

	private bool DateInHover(DateTime dateTime)
	{
		if (HoverStartEndTuple == null)
		{
			return false;
		}
		foreach (Tuple<DateTime, DateTime> item in HoverStartEndTuple)
		{
			if (item != null && dateTime >= item.Item1 && dateTime <= item.Item2)
			{
				return true;
			}
		}
		return false;
	}

	private void DrawWeek(DrawingContext dc)
	{
		TupleExtensions.Deconstruct(StartEndTuple, out var item, out var item2);
		DateTime dateTime = item;
		DateTime dateTime2 = item2;
		double num = 0.0;
		double num2 = OneDayWidth * 7.0;
		if (!DateInHover(DateTime.Today))
		{
			string weekDayText = (ShowWeekDay ? DateUtils.GetShortWeekTextByWeekDay((int)DateTime.Today.DayOfWeek) : string.Empty);
			double x = (double)(DateTime.Today - dateTime).Days * OneDayWidth;
			DrawWeekName(dc, DateTime.Today.Day.ToString(), x, OneDayWidth, isToday: true, weekDayText);
		}
		while (dateTime <= dateTime2)
		{
			string name = dateTime.Day.ToString();
			if (DateInHover(dateTime) || dateTime == DateTime.Today)
			{
				num += num2;
			}
			else
			{
				string weekDayText2 = (ShowWeekDay ? DateUtils.GetShortWeekTextByWeekDay(1) : string.Empty);
				DrawWeekName(dc, name, num, OneDayWidth, isToday: false, weekDayText2);
				num += num2;
			}
			dateTime = dateTime.AddDays(7.0);
		}
	}

	private void DrawDay(DrawingContext dc)
	{
		TupleExtensions.Deconstruct(StartEndTuple, out var item, out var item2);
		DateTime dateTime = item;
		DateTime dateTime2 = item2;
		double oneDayWidth = OneDayWidth;
		int num = 0;
		while (dateTime <= dateTime2)
		{
			double x = (double)num * oneDayWidth;
			bool isToday = dateTime.Date == DateTime.Today;
			bool isRest = false;
			bool isWork = false;
			if (ShowHoliday && Utils.IsDida())
			{
				HolidayModel holiday = HolidayManager.GetHoliday(dateTime.Date);
				if (holiday != null)
				{
					isRest = holiday.type == 0;
					isWork = holiday.type == 1;
				}
			}
			bool drawText = !DateInHover(dateTime);
			string name = dateTime.Day.ToString();
			string weekDayText = (ShowWeekDay ? DateUtils.GetShortWeekTextByWeekDay((int)dateTime.DayOfWeek) : string.Empty);
			DrawDayName(dc, name, x, oneDayWidth, isToday, DateUtils.IsWeekEnds(dateTime), drawText, isRest, isWork, weekDayText);
			dateTime = dateTime.AddDays(1.0);
			num++;
		}
	}

	private void DrawMonth(DrawingContext dc)
	{
		TupleExtensions.Deconstruct(StartEndTuple, out var item, out var item2);
		DateTime dateTime = item;
		DateTime dateTime2 = item2;
		double oneDayWidth = OneDayWidth;
		double num = 0.0;
		while (dateTime <= dateTime2)
		{
			double num2 = (double)DateTime.DaysInMonth(dateTime.Year, dateTime.Month) * oneDayWidth;
			string name = DateUtils.FormatMonth(dateTime);
			bool isToday = dateTime.Year == DateTime.Today.Year && dateTime.Month == DateTime.Today.Month;
			DrawDayName(dc, name, num, num2, isToday);
			num += num2;
			dateTime = dateTime.AddMonths(1);
		}
	}
}
