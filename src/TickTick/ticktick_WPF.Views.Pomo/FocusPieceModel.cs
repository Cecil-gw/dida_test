using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Dal;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo;

public class FocusPieceModel
{
	public string Id { get; set; }

	public int Type { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime BeginTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? EndTime { get; set; }

	public long GetDuration()
	{
		if (!Utils.IsEmptyDate(EndTime) && EndTime.HasValue)
		{
			return Utils.GetTotalSecond(BeginTime, EndTime.Value);
		}
		return 0L;
	}

	public async Task<string> GetTaskId()
	{
		if (Type == 0)
		{
			return Id;
		}
		if (Type == 2)
		{
			TimerModel timerModel = await TimerDao.GetTimerById(Id);
			if (timerModel != null && timerModel.ObjType == "task")
			{
				return (await TaskDao.GetThinTaskById(timerModel.ObjId))?.id;
			}
		}
		return string.Empty;
	}
}
