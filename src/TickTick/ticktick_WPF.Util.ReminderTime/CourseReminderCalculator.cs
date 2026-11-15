using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;

namespace ticktick_WPF.Util.ReminderTime;

public class CourseReminderCalculator
{
	private static List<ReminderModel> _courseReminders;

	public static async Task InitCourseReminders()
	{
		await ReminderTimeDao.DeleteReminderTimeByType(8);
		if (UserDao.IsPro() && LocalSettings.Settings.UserPreference?.TimeTable?.isEnabled == true)
		{
			_courseReminders = await ScheduleService.LoadCourseReminders();
		}
	}

	public static List<ReminderModel> GetCourseReminders(DateTime start, DateTime end)
	{
		List<ReminderModel> courseReminders = _courseReminders;
		if (courseReminders != null && courseReminders.Any())
		{
			return _courseReminders.Where((ReminderModel c) => c.ReminderTime.HasValue && c.ReminderTime >= start && c.ReminderTime <= end).ToList();
		}
		return null;
	}
}
