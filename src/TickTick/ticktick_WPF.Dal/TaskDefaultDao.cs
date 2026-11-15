using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public static class TaskDefaultDao
{
	private static TaskDefaultModel _cacheModel;

	public static event EventHandler<TaskDefaultModel> TaskDefaultChanged;

	public static async Task InitCache()
	{
		_cacheModel = await GetTaskDefaultWithDefault();
	}

	public static TaskDefaultModel GetDefaultSafely()
	{
		return _cacheModel ?? TaskDefaultModel.BuildDefault();
	}

	public static async Task<TaskDefaultModel> GetTaskDefault()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		TaskDefaultModel taskDefaultModel = await (from m in App.Connection.Table<TaskDefaultModel>()
			where m.UserId == userId
			select m).FirstOrDefaultAsync();
		if (taskDefaultModel != null && !string.IsNullOrEmpty(taskDefaultModel.AllDayReminders))
		{
			string[] source = taskDefaultModel.AllDayReminders.Split(',');
			if (source.Any())
			{
				taskDefaultModel.AllDayReminders = string.Join(",", source.Select(TriggerUtils.ConvertLegacyTrigger).ToList());
			}
		}
		_cacheModel = taskDefaultModel;
		return taskDefaultModel;
	}

	public static async Task<TaskDefaultModel> GetTaskDefaultWithDefault()
	{
		return (await GetTaskDefault()) ?? TaskDefaultModel.BuildDefault();
	}

	public static async Task SaveTaskDefault(TaskDefaultModel model)
	{
		if (string.IsNullOrEmpty(model.UserId))
		{
			model.UserId = Utils.GetCurrentUserIdInt().ToString();
		}
		TaskDefaultModel taskDefaultModel = await GetTaskDefault();
		if (taskDefaultModel == null)
		{
			_cacheModel = model;
			await App.Connection.InsertAsync(model);
		}
		else
		{
			model._Id = taskDefaultModel._Id;
			_cacheModel = model;
			await App.Connection.UpdateAsync(model);
		}
	}

	public static async Task UpdateDefaultTagsOnTagChanged(string origin, string newTag)
	{
		TaskDefaultModel taskDefaultModel = await GetTaskDefault();
		List<string> list = taskDefaultModel?.Tags;
		if (list == null || !list.Any((string t) => string.Equals(t, origin, StringComparison.CurrentCultureIgnoreCase)))
		{
			return;
		}
		if (string.IsNullOrEmpty(newTag))
		{
			list.Remove(origin.ToLower());
			taskDefaultModel.Tags = list;
		}
		else
		{
			int num = list.IndexOf(origin.ToLower());
			if (num >= 0)
			{
				list[num] = newTag.ToLower();
				taskDefaultModel.Tags = list;
			}
		}
		await SaveTaskDefault(taskDefaultModel);
		DataChangedNotifier.NotifyTaskDefaultChanged();
		await SettingsHelper.PushLocalSettings();
	}

	public static void ClearCache()
	{
		_cacheModel = null;
	}
}
