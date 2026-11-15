using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace TickTickModels;

public class FocusSyncStatusModel
{
	public string id { get; set; }

	[JsonConverter(typeof(UtcDateTimeWithMillConverter))]
	public DateTime startTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeWithMillConverter))]
	public DateTime? endTime { get; set; }

	public long duration { get; set; }

	public int status { get; set; }

	public int type { get; set; }

	public string etag { get; set; }

	public bool valid { get; set; }

	public bool exited { get; set; }

	public string firstId { get; set; }

	public int autoPomoLeft { get; set; }

	public int pomoCount { get; set; }

	public FocusBreakModel focusBreak { get; set; }

	public FocusTaskModel[] focusTasks { get; set; }

	public FocusLogModel[] focusOnLogs { get; set; }

	public FocusLogModel[] pauseLogs { get; set; }

	public double GetPauseTime(bool getTotal = false)
	{
		FocusLogModel[] array = pauseLogs;
		if (array != null && array.Length != 0)
		{
			List<FocusLogModel> list = pauseLogs.ToList();
			list.Sort((FocusLogModel l, FocusLogModel r) => l.time.CompareTo(r.time));
			DateTime time = list[0].time;
			double num = 0.0;
			foreach (FocusLogModel item in list)
			{
				if (!item.type.HasValue || item.type == 0)
				{
					time = item.time;
					continue;
				}
				num += (item.time - time).TotalSeconds;
				time = item.time;
			}
			FocusLogModel focusLogModel = list[list.Count - 1];
			if (getTotal && focusLogModel.type == 0 && endTime.HasValue && focusLogModel.time < endTime.Value)
			{
				num += (endTime.Value - focusLogModel.time).TotalSeconds;
			}
			return num;
		}
		return 0.0;
	}

	public DateTime GetLastPauseTime()
	{
		FocusLogModel[] array = pauseLogs;
		if (array != null && array.Length != 0)
		{
			List<FocusLogModel> list = pauseLogs.ToList();
			list.Sort((FocusLogModel l, FocusLogModel r) => l.time.CompareTo(r.time));
			return list.LastOrDefault((FocusLogModel p) => p.type == 0)?.time ?? DateTime.Now;
		}
		return DateTime.Now;
	}

	public FocusLogModel GetCurrentFocusOn()
	{
		if (focusOnLogs != null)
		{
			List<FocusLogModel> list = focusOnLogs.ToList();
			list.Sort((FocusLogModel a, FocusLogModel b) => b.time.CompareTo(a.time));
			return list.FirstOrDefault();
		}
		return null;
	}

	public long GetFocusDuration()
	{
		if (!endTime.HasValue)
		{
			return duration * 60;
		}
		return (endTime.Value - startTime).Seconds - (int)GetPauseTime();
	}

	public bool IsDropOrExit()
	{
		if (!exited)
		{
			return status == 3;
		}
		return true;
	}
}
