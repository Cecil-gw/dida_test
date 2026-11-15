using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Dal;

public class ScheduleDao
{
	private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

	public static async Task<List<CourseScheduleModel>> GetAllSchedulesAsync()
	{
		string userId = LocalSettings.Settings.LoginUserId;
		return await (from m in App.Connection.Table<CourseScheduleModel>()
			where m.UserId == userId
			select m).ToListAsync();
	}

	public static async Task InsertScheduleAsync(CourseScheduleModel model)
	{
		if (model != null)
		{
			if (string.IsNullOrEmpty(model.UserId))
			{
				model.UserId = LocalSettings.Settings.LoginUserId;
			}
			CourseScheduleModel courseScheduleModel = await GetScheduleByIdAsync(model.Id);
			if (courseScheduleModel != null)
			{
				model._Id = courseScheduleModel._Id;
				await App.Connection.UpdateAsync(model);
			}
			else
			{
				await App.Connection.InsertAsync(model);
			}
		}
	}

	public static async Task<CourseScheduleModel> GetScheduleByIdAsync(string id)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		return await (from m in App.Connection.Table<CourseScheduleModel>()
			where m.UserId == userId && m.Id == id
			select m).FirstOrDefaultAsync();
	}

	public static async Task UpdateRemoteScheduleAsync(CourseScheduleModel model)
	{
		await _semaphore.WaitAsync();
		try
		{
			if (model.LessonTimes != null)
			{
				model.LessonTimesStr = JsonConvert.SerializeObject(model.LessonTimes);
			}
			if (model.Reminders != null)
			{
				model.RemindersStr = JsonConvert.SerializeObject(model.Reminders);
			}
			await App.Connection.UpdateAsync(model);
			await SaveCoursesAsync(model);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public static async Task AddRemoteScheduleAsync(CourseScheduleModel remote)
	{
		if (remote == null)
		{
			return;
		}
		await _semaphore.WaitAsync();
		try
		{
			if (remote.LessonTimes != null)
			{
				remote.LessonTimesStr = JsonConvert.SerializeObject(remote.LessonTimes);
			}
			if (remote.Reminders != null)
			{
				remote.RemindersStr = JsonConvert.SerializeObject(remote.Reminders);
			}
			await InsertScheduleAsync(remote);
			await SaveCoursesAsync(remote);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public static async Task DeleteScheduleAsync(CourseScheduleModel model)
	{
		if (model != null)
		{
			await App.Connection.DeleteAsync(model);
			await DeleteCoursedByScheduleIdAsync(model.Id);
		}
	}

	private static async Task SaveCoursesAsync(CourseScheduleModel schedule)
	{
		if (schedule == null)
		{
			return;
		}
		await DeleteCoursedByScheduleIdAsync(schedule.Id);
		if (schedule.Courses == null || !schedule.Courses.Any())
		{
			return;
		}
		foreach (CourseModel course in schedule.Courses)
		{
			course.ScheduleId = schedule.Id;
			if (course.Items != null)
			{
				course.ItemsStr = JsonConvert.SerializeObject(course.Items);
			}
		}
		await App.Connection.InsertAllAsync(schedule.Courses);
	}

	public static async Task<List<CourseModel>> GetCoursedByScheduleIdAsync(string scheduleId)
	{
		return await (from m in App.Connection.Table<CourseModel>()
			where m.ScheduleId == scheduleId
			select m).ToListAsync();
	}

	public static async Task<CourseModel> GetCoursedByIdAsync(string courseId)
	{
		return await (from m in App.Connection.Table<CourseModel>()
			where m.Id == courseId
			select m).FirstOrDefaultAsync();
	}

	private static async Task DeleteCoursedByScheduleIdAsync(string scheduleId)
	{
		List<CourseModel> list = await GetCoursedByScheduleIdAsync(scheduleId);
		if (list == null)
		{
			return;
		}
		foreach (CourseModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}
}
