using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.TimerDetail;

public class TimerTimelineItemViewModel : TimerDetailItemViewModel
{
	public TimerModel TModel;

	private string _interval = "week";

	private string _intervalText = Utils.GetString("Week");

	private TimerDetailPanel _detail;

	public DateTime StartDate { get; set; }

	public DateTime EndDate { get; set; }

	public string Interval
	{
		get
		{
			return _interval;
		}
		set
		{
			if (_interval != value)
			{
				_interval = value;
				OnPropertyChanged("IntervalText");
				OnPropertyChanged("DateText");
			}
		}
	}

	public string DateText
	{
		get
		{
			string interval = _interval;
			if (!(interval == "month"))
			{
				if (interval == "year")
				{
					return DateUtils.FormatYear(StartDate);
				}
				return DateUtils.FormatShortDate(StartDate) + " - " + DateUtils.FormatShortDate(EndDate);
			}
			return DateUtils.FormatYearMonth(StartDate, DateFormatLen.Short);
		}
	}

	public string IntervalText
	{
		get
		{
			string interval = _interval;
			if (!(interval == "month"))
			{
				if (interval == "year")
				{
					return Utils.GetString("TimelineYear");
				}
				return Utils.GetString("TimelineWeek");
			}
			return Utils.GetString("TimelineMonth");
		}
	}

	public TimerTimelineItemViewModel(TimerDetailPanel detail)
	{
		_detail = detail;
	}

	public void SetTimer(TimerModel timer)
	{
		TModel = timer;
		StartDate = Utils.GetWeekStart(DateTime.Today);
		EndDate = StartDate.AddDays(6.0);
		base.TimerTimeline = true;
	}

	public async Task<Dictionary<string, long>> GetStatistics()
	{
		return await TimerService.GetTimerStatistics(TModel, StartDate, EndDate, Interval);
	}

	public void SetDateText()
	{
		OnPropertyChanged("DateText");
	}

	public void ReloadData()
	{
		OnPropertyChanged("Reload");
	}

	public void SetInterval(string interval)
	{
		string interval2 = Interval;
		Interval = interval;
		switch (interval)
		{
		case "week":
		{
			DateTime date = ((DateTime.Today < EndDate) ? DateTime.Today : EndDate);
			StartDate = Utils.GetWeekStart(date);
			EndDate = StartDate.AddDays(6.0);
			break;
		}
		case "month":
		{
			DateTime dateTime = ((interval2 == "year") ? StartDate.AddMonths(11) : StartDate.AddDays(1 - StartDate.Day));
			StartDate = ((dateTime > DateTime.Today) ? DateTime.Today.AddDays(1 - DateTime.Today.Day) : dateTime);
			EndDate = StartDate.AddMonths(1).AddDays(-1.0);
			break;
		}
		case "year":
			StartDate = new DateTime(StartDate.Year, 1, 1);
			EndDate = StartDate.AddYears(1).AddDays(-1.0);
			break;
		}
		_detail.ReloadRecords();
		_detail.PullTimeline(this);
	}

	public void NotifySpanChanged(bool getFront = true)
	{
		_detail.ReloadRecords();
		_detail.PullTimeline(this, getFront);
	}
}
