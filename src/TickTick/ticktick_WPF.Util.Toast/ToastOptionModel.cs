using System;
using Newtonsoft.Json;
using ticktick_WPF.Models;
using ticktick_WPF.Util.ReminderTime;

namespace ticktick_WPF.Util.Toast;

public class ToastOptionModel
{
	public string Id { get; set; }

	public object Parma { get; set; }

	public DateTime StartTime { get; set; }

	public ToastType ToastType { get; set; }

	public string TargetId { get; set; }

	public string EventArchivedId { get; set; }

	public string CurrentOptionName { get; set; }

	public ToastOptionModel()
	{
		Id = Utils.GetGuid0(16);
	}

	public ToastOptionModel(ReminderModel reminderModel)
	{
		Id = (reminderModel.IsAnnoyingAlert ? AnnoyingReminderManager.GetReminderKey(reminderModel) : Utils.GetGuid0(16));
		if (!string.IsNullOrEmpty(reminderModel.HabitId))
		{
			ToastType = ToastType.RemindHabit;
		}
		else if (!string.IsNullOrEmpty(reminderModel.EventId))
		{
			ToastType = ToastType.RemindEvent;
			EventArchivedId = reminderModel.EventArchivedId;
		}
		else if (!string.IsNullOrEmpty(reminderModel.CheckItemId))
		{
			ToastType = ToastType.RemindCheckItem;
		}
		else if (reminderModel.Type == ReminderModel.RemindType.Course)
		{
			ToastType = ToastType.RemindCourse;
		}
		else if (reminderModel.Type == ReminderModel.RemindType.Countdown)
		{
			ToastType = ToastType.RemindCountdown;
		}
		else
		{
			ToastType = ToastType.RemindTask;
		}
		TargetId = reminderModel.GetObjId();
		Parma = reminderModel.ReminderTime;
		StartTime = reminderModel.StartDate ?? DateTime.Now;
	}

	public string ToJson(string currentName)
	{
		CurrentOptionName = currentName;
		return JsonConvert.SerializeObject(this);
	}

	public static ToastOptionModel FromJson(string jsonStr)
	{
		if (string.IsNullOrEmpty(jsonStr))
		{
			return null;
		}
		try
		{
			return JsonConvert.DeserializeObject<ToastOptionModel>(jsonStr);
		}
		catch
		{
			return null;
		}
	}
}
