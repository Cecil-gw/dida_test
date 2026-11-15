using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public static class CalendarEventDragHelper
{
	public static void CheckIfEventCanDrag(CalendarDisplayViewModel model, out bool canDrag, out bool needToast, out string toastStr)
	{
		canDrag = true;
		needToast = false;
		toastStr = null;
		if (model.IsCalendarEvent)
		{
			BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == model.CalendarId);
			if (bindCalendarModel == null || !bindCalendarModel.Accessible)
			{
				canDrag = false;
				needToast = false;
			}
		}
		if (model.IsCheckItem)
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(model.GetTaskId());
			if (taskById == null || (!string.IsNullOrEmpty(taskById.AttendId) && !AgendaHelper.CanAccessAgenda(taskById)))
			{
				canDrag = false;
				needToast = true;
				toastStr = Utils.GetString("AttendeeSetDate");
			}
		}
		if (!string.IsNullOrEmpty(model.AttendId) && !AgendaHelper.CanAccessAgenda(model))
		{
			canDrag = false;
			needToast = true;
			toastStr = Utils.GetString("AttendeeSetDate");
		}
		if (model.IsHabit)
		{
			canDrag = false;
			needToast = true;
			toastStr = Utils.GetString("CannotDragHabit");
		}
		if (model.IsPomo)
		{
			canDrag = false;
			needToast = false;
		}
		if (model.Course != null)
		{
			canDrag = false;
			needToast = true;
			toastStr = Utils.GetString("OperationNotSupport");
		}
		if (model.IsCountdown)
		{
			canDrag = false;
			needToast = true;
			toastStr = Utils.GetString("OperationNotSupport");
		}
	}
}
