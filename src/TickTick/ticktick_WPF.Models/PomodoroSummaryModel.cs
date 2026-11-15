using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

[Serializable]
public class PomodoroSummaryModel : BaseModel
{
	public string id { get; set; }

	[Indexed]
	public string taskId { get; set; }

	[JsonProperty("pomoCount")]
	public int count { get; set; }

	[JsonIgnore]
	public long duration { get; set; }

	[JsonProperty("pomoDuration")]
	public long PomoDuration { get; set; }

	public int estimatedPomo { get; set; }

	public string userId { get; set; }

	[JsonProperty("estimatedDuration")]
	public long EstimatedDuration { get; set; }

	[JsonProperty("stopwatchDuration")]
	public long StopwatchDuration { get; set; }

	[Ignore]
	public List<object[]> focuses { get; set; }

	[JsonIgnore]
	public string focusesString
	{
		get
		{
			return JsonConvert.SerializeObject(focuses);
		}
		set
		{
			try
			{
				focuses = JsonConvert.DeserializeObject<List<object[]>>(value) ?? new List<object[]>();
			}
			catch (Exception)
			{
				focuses = new List<object[]>();
			}
		}
	}

	public PomodoroSummaryModel Copy(string copyTaskSid, bool clear = false)
	{
		PomodoroSummaryModel pomodoroSummaryModel = (PomodoroSummaryModel)MemberwiseClone();
		pomodoroSummaryModel.id = Utils.GetGuid();
		pomodoroSummaryModel.taskId = copyTaskSid;
		if (clear)
		{
			pomodoroSummaryModel.count = 0;
			pomodoroSummaryModel.duration = 0L;
			pomodoroSummaryModel.StopwatchDuration = 0L;
			pomodoroSummaryModel.PomoDuration = 0L;
			pomodoroSummaryModel.focuses = null;
		}
		return pomodoroSummaryModel;
	}

	public bool IsEmpty()
	{
		if (count <= 0 && PomoDuration + StopwatchDuration < 30 && estimatedPomo <= 0 && EstimatedDuration <= 0)
		{
			List<object[]> list = focuses;
			return list == null || list.Count <= 0;
		}
		return false;
	}

	internal void CheckNegative()
	{
		if (count < 0)
		{
			count = 0;
		}
		if (PomoDuration < 0)
		{
			PomoDuration = 0L;
		}
		if (StopwatchDuration < 0)
		{
			StopwatchDuration = 0L;
		}
	}

	public void AddFocuses(object[] focus, bool checkExist = false)
	{
		if (focuses != null)
		{
			if (!checkExist || !focuses.Any((object[] f) => f[0] as string == focus[0] as string))
			{
				focuses.Add(focus);
			}
		}
		else
		{
			focuses = new List<object[]> { focus };
		}
	}

	public (int, long) GetFocusSummary()
	{
		int num = count;
		long num2 = PomoDuration + StopwatchDuration;
		List<object[]> list = focuses;
		if (list != null && list.Count > 0)
		{
			foreach (object[] item in focuses.Where((object[] focus) => focus.Length == 3))
			{
				long valueOrDefault = (item[2] as long?).GetValueOrDefault();
				num2 += valueOrDefault;
				if (item[1] as long? != 1)
				{
					num += ((valueOrDefault >= Constants.MinFocusDuration) ? 1 : 0);
				}
			}
		}
		return (num, num2);
	}

	public (int, long, long) GetPomoFocusSummary()
	{
		int num = count;
		long num2 = PomoDuration;
		long num3 = StopwatchDuration;
		List<object[]> list = focuses;
		if (list != null && list.Count > 0)
		{
			foreach (object[] item in focuses.Where((object[] focus) => focus.Length == 3))
			{
				long valueOrDefault = (item[2] as long?).GetValueOrDefault();
				long? num4 = item[1] as long?;
				if (num4 != 1)
				{
					num += ((valueOrDefault >= Constants.MinFocusDuration) ? 1 : 0);
				}
				switch (num4)
				{
				case 0L:
					num2 += valueOrDefault;
					break;
				case 1L:
					num3 += valueOrDefault;
					break;
				}
			}
		}
		return (num, num2, num3);
	}
}
