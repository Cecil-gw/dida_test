using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class PomoDisplayViewModel : BaseViewModel
{
	public string Id { get; set; }

	public string TimerId { get; set; }

	public bool Enable { get; set; }

	public string Note { get; set; }

	public List<PomoTaskDisplayViewModel> PomoTaskModels { get; set; }

	public PomoDisplayViewModel(PomodoroModel pomo, PomoTask[] pomoTasks)
	{
		Id = pomo.Id;
		Enable = true;
		PomoTaskModels = new List<PomoTaskDisplayViewModel>();
		Note = pomo.Note;
		if (pomoTasks != null && pomoTasks.Any())
		{
			foreach (PomoTask pomoTask in pomoTasks)
			{
				PomoTaskModels.Add(new PomoTaskDisplayViewModel(pomoTask, pomo)
				{
					Enable = Enable
				});
			}
			return;
		}
		long num = Utils.GetTotalSecond(pomo.StartTime, pomo.EndTime) - pomo.PauseDuration;
		num = ((num < 0) ? 0 : num);
		DateTime startTime = pomo.StartTime;
		DateTime dateTime = pomo.StartTime.AddSeconds(num);
		PomoTaskModels.Add(new PomoTaskDisplayViewModel
		{
			PomoId = pomo.Id,
			StartTime = pomo.StartTime,
			Duration = num,
			Enable = Enable,
			DurationString = Utils.GetDurationString(num, isShort: true, showZero: true, showSpan: false),
			TimeString = DateUtils.FormatShortDate(startTime) + " " + DateUtils.GetTimeText(startTime) + " - " + DateUtils.FormatShortDate(dateTime) + " " + DateUtils.GetTimeText(dateTime),
			Icon = Utils.GetIcon((pomo.Type == 0) ? "IcPomo" : "IcPomoTimer")
		});
	}
}
