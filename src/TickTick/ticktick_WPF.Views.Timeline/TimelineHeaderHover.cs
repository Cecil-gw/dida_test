using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Timeline;

public class TimelineHeaderHover : Canvas
{
	public static readonly DependencyProperty OneDayWidthProperty = DependencyProperty.Register("OneDayWidth", typeof(double), typeof(TimelineHeaderHover), new PropertyMetadata(0.0, OnDependencyChanged));

	public static readonly DependencyProperty StartEndTupleProperty = DependencyProperty.Register("StartEndTuple", typeof(Tuple<DateTime, DateTime>), typeof(TimelineHeaderHover), new PropertyMetadata(null, OnDependencyChanged));

	public static readonly DependencyProperty ShowWeekDayProperty = DependencyProperty.Register("ShowWeekDay", typeof(bool), typeof(TimelineHeaderHover), new PropertyMetadata(false, OnDependencyChanged));

	public static readonly DependencyProperty HoverStartEndTupleTuplesProperty = DependencyProperty.Register("HoverStartEndTuples", typeof(List<Tuple<DateTime, DateTime>>), typeof(TimelineHeaderHover), new PropertyMetadata(null, OnDependencyChanged));

	public static readonly DependencyProperty TimelineDayWidthIndexProperty = DependencyProperty.Register("TimelineDayWidthIndex", typeof(int), typeof(TimelineHeaderHover), new PropertyMetadata(4, OnDependencyChanged));

	public static readonly DependencyProperty HighLightHoverProperty = DependencyProperty.Register("HighLightHover", typeof(bool), typeof(TimelineHeaderHover), new PropertyMetadata(false, OnDependencyChanged));

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

	public List<Tuple<DateTime, DateTime>> HoverStartEndTuples
	{
		get
		{
			return (List<Tuple<DateTime, DateTime>>)GetValue(HoverStartEndTupleTuplesProperty);
		}
		set
		{
			SetCurrentValue(HoverStartEndTupleTuplesProperty, value);
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

	public bool HighLightHover
	{
		get
		{
			return (bool)GetValue(HighLightHoverProperty);
		}
		set
		{
			SetCurrentValue(HighLightHoverProperty, value);
		}
	}

	private static void OnDependencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((TimelineHeaderHover)d).InvalidateVisual();
	}

	public TimelineHeaderHover()
	{
		base.ClipToBounds = true;
		base.IsHitTestVisible = false;
		base.Loaded += delegate
		{
			LocalSettings.Settings.PropertyChanged += OnSettingsChanged;
		};
		base.Unloaded += delegate
		{
			LocalSettings.Settings.PropertyChanged -= OnSettingsChanged;
		};
	}

	private void DrawDayName(DrawingContext dc, string name, double x, double width)
	{
		FormattedText formattedText = new FormattedText(name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12.0, TimelineThemes.PrimaryColorBrush);
		dc.DrawText(formattedText, new Point(x + (width - formattedText.Width) / 2.0, base.Height - 14.0 - formattedText.Height / 2.0));
	}

	private void DrawWeekText(DrawingContext dc, string weekText, double x, double width)
	{
		FormattedText formattedText = new FormattedText(weekText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 10.0, TimelineThemes.PrimaryColorBrush);
		dc.DrawText(formattedText, new Point(x + (width - formattedText.Width) / 2.0, 11.0 - formattedText.Height / 2.0));
	}

	private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "ThemeId")
		{
			InvalidateVisual();
		}
	}

	private static void DrawRoundedRectangle(DrawingContext dc, Brush brush, Pen pen, Rect rect, CornerRadius cornerRadius)
	{
		StreamGeometry streamGeometry = new StreamGeometry();
		using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
		{
			bool isStroked = pen != null;
			streamGeometryContext.BeginFigure(rect.TopLeft + new Vector(0.0, cornerRadius.TopLeft), brush != null, isClosed: true);
			streamGeometryContext.ArcTo(new Point(rect.TopLeft.X + cornerRadius.TopLeft, rect.TopLeft.Y), new Size(cornerRadius.TopLeft, cornerRadius.TopLeft), 90.0, isLargeArc: false, SweepDirection.Clockwise, isStroked, isSmoothJoin: true);
			streamGeometryContext.LineTo(rect.TopRight - new Vector(cornerRadius.TopRight, 0.0), isStroked, isSmoothJoin: true);
			streamGeometryContext.ArcTo(new Point(rect.TopRight.X, rect.TopRight.Y + cornerRadius.TopRight), new Size(cornerRadius.TopRight, cornerRadius.TopRight), 90.0, isLargeArc: false, SweepDirection.Clockwise, isStroked, isSmoothJoin: true);
			streamGeometryContext.LineTo(rect.BottomRight - new Vector(0.0, cornerRadius.BottomRight), isStroked, isSmoothJoin: true);
			streamGeometryContext.ArcTo(new Point(rect.BottomRight.X - cornerRadius.BottomRight, rect.BottomRight.Y), new Size(cornerRadius.BottomRight, cornerRadius.BottomRight), 90.0, isLargeArc: false, SweepDirection.Clockwise, isStroked, isSmoothJoin: true);
			streamGeometryContext.LineTo(rect.BottomLeft + new Vector(cornerRadius.BottomLeft, 0.0), isStroked, isSmoothJoin: true);
			streamGeometryContext.ArcTo(new Point(rect.BottomLeft.X, rect.BottomLeft.Y - cornerRadius.BottomLeft), new Size(cornerRadius.BottomLeft, cornerRadius.BottomLeft), 90.0, isLargeArc: false, SweepDirection.Clockwise, isStroked, isSmoothJoin: true);
			streamGeometryContext.Close();
		}
		dc.DrawGeometry(brush, pen, streamGeometry);
	}

	protected override void OnRender(DrawingContext dc)
	{
		base.OnRender(dc);
		if (base.ActualWidth <= 0.0 || base.ActualHeight <= 0.0 || StartEndTuple == null || HoverStartEndTuples == null)
		{
			return;
		}
		TupleExtensions.Deconstruct(StartEndTuple, out var item, out var item2);
		DateTime dateTime = item;
		foreach (Tuple<DateTime, DateTime> item3 in HoverStartEndTuples.Where((Tuple<DateTime, DateTime> tuple) => tuple != null))
		{
			TupleExtensions.Deconstruct(item3, out item2, out item);
			DateTime dateTime2 = item2;
			DateTime dateTime3 = item;
			dateTime2 = dateTime2.Date;
			dateTime3 = dateTime3.Date;
			int num = Math.Max((dateTime3 - dateTime2).Days + 1, 1);
			double oneDayWidth = OneDayWidth;
			double num2 = (double)(dateTime2 - dateTime).Days * oneDayWidth;
			DrawRoundedRectangle(dc, TimelineThemes.PrimaryColorBrush, null, new Rect(num2, base.ActualHeight - 2.0, (double)num * oneDayWidth, 2.0), new CornerRadius(2.0));
			if (HighLightHover)
			{
				DrawRoundedRectangle(dc, TimelineThemes.PrimaryColor10Brush, null, new Rect(num2, 0.0, (double)num * oneDayWidth, base.ActualHeight - 2.0), new CornerRadius(2.0));
			}
			switch (TimelineConstants.GetDayWidthRange(TimelineDayWidthIndex))
			{
			case "month":
			{
				if (num > 5)
				{
					num2 += oneDayWidth / 2.0;
					string name = DateUtils.FormatMonthDay(dateTime2, DateFormatLen.Middle);
					DrawDayName(dc, name, num2 + 10.0, 0.0);
					string text = (ShowWeekDay ? DateUtils.GetShortWeekTextByWeekDay((int)dateTime2.DayOfWeek) : string.Empty);
					if (!string.IsNullOrEmpty(text))
					{
						DrawWeekText(dc, text, num2, 0.0);
					}
					string text2 = DateUtils.FormatMonthDay(dateTime2.AddDays(num - 1), DateFormatLen.Middle);
					double width = Utils.MeasureString(text2, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12.0).Width;
					DrawDayName(dc, text2, num2 + (double)num * oneDayWidth - width - oneDayWidth / 2.0, width);
					string text3;
					if (!ShowWeekDay)
					{
						text3 = string.Empty;
					}
					else
					{
						item = dateTime2.AddDays(num - 1);
						text3 = DateUtils.GetShortWeekTextByWeekDay((int)item.DayOfWeek);
					}
					string text4 = text3;
					if (!string.IsNullOrEmpty(text4))
					{
						double width2 = Utils.MeasureString(text4, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 10.0).Width;
						DrawWeekText(dc, text4, num2 + (double)num * oneDayWidth - width2 - oneDayWidth / 2.0 - 5.0, width2);
					}
					continue;
				}
				for (int num3 = 0; num3 < num; num3++)
				{
					DrawDayName(dc, dateTime2.Day.ToString(), num2, oneDayWidth);
					string text5 = (ShowWeekDay ? DateUtils.GetShortWeekTextByWeekDay((int)dateTime2.DayOfWeek) : string.Empty);
					if (!string.IsNullOrEmpty(text5))
					{
						DrawWeekText(dc, text5, num2, oneDayWidth);
					}
					dateTime2 = dateTime2.AddDays(1.0);
					num2 += oneDayWidth;
				}
				continue;
			}
			}
			for (int num4 = 0; num4 < num; num4++)
			{
				DrawDayName(dc, dateTime2.Day.ToString(), num2, oneDayWidth);
				string text6 = (ShowWeekDay ? DateUtils.GetShortWeekTextByWeekDay((int)dateTime2.DayOfWeek) : string.Empty);
				if (!string.IsNullOrEmpty(text6))
				{
					DrawWeekText(dc, text6, num2, oneDayWidth);
				}
				dateTime2 = dateTime2.AddDays(1.0);
				num2 += oneDayWidth;
			}
		}
	}
}
