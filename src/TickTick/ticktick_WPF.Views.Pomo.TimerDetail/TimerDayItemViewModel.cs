using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Pomo.TimerDetail;

public class TimerDayItemViewModel : BaseViewModel
{
	private double _itemWidth;

	private int _itemMinWidth;

	private int _columnWidth;

	private int _columnHeight;

	private string _dayText;

	public bool HideText;

	public double ItemWidth
	{
		get
		{
			return _itemWidth;
		}
		set
		{
			if (Math.Abs(_itemWidth - value) > 0.001)
			{
				_itemWidth = value;
				OnPropertyChanged("ItemWidth");
				OnPropertyChanged("DayText");
			}
		}
	}

	public int ItemMinWidth
	{
		get
		{
			return _itemMinWidth;
		}
		set
		{
			if (_itemMinWidth != value)
			{
				_itemMinWidth = value;
				OnPropertyChanged("ItemMinWidth");
			}
		}
	}

	public int ColumnWidth
	{
		get
		{
			return _columnWidth;
		}
		set
		{
			if (_columnWidth != value)
			{
				_columnWidth = value;
				OnPropertyChanged("ColumnWidth");
			}
		}
	}

	public int ColumnHeight
	{
		get
		{
			return _columnHeight;
		}
		set
		{
			if (_columnHeight != value)
			{
				_columnHeight = value;
				OnPropertyChanged("ColumnHeight");
			}
		}
	}

	public string DayText
	{
		get
		{
			return _dayText;
		}
		set
		{
			_dayText = value;
			OnPropertyChanged("DayText");
		}
	}

	public bool LineVisible { get; set; } = true;

	public DateTime Date { get; set; }

	public long Minutes { get; set; }

	public string Interval { get; set; }

	public FormattedText TextFormat { get; set; }

	public TimerDayItemViewModel(DateTime date, long minutes, string interval, long maxMinutes)
	{
		Date = date;
		Minutes = minutes;
		Interval = interval;
		switch (interval)
		{
		case "year":
			_dayText = date.Month.ToString();
			_columnWidth = 12;
			break;
		case "month":
			_dayText = date.Day.ToString();
			_columnWidth = 5;
			HideText = date.Day % 2 == 0;
			LineVisible = date.Day % 7 == 1;
			break;
		case "week":
			_dayText = DateUtils.GetShortWeekTextByWeekDay((int)date.DayOfWeek);
			_columnWidth = 20;
			break;
		}
		TextFormat = new FormattedText(DayText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12.0, ThemeUtil.GetColor("BaseColorOpacity40"));
		_columnHeight = (int)(minutes * 120 / maxMinutes);
	}

	public string GetDateText()
	{
		if (!(Interval == "year"))
		{
			return DateUtils.FormatShortDate(Date);
		}
		return DateUtils.FormatYearMonth(Date, DateFormatLen.Short);
	}
}
