using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Dal;

public static class TaskCompletionRateDao
{
	private static ConcurrentDictionary<string, TaskCompletionRate> _cacheRates = new ConcurrentDictionary<string, TaskCompletionRate>();

	public static async Task WarmUp()
	{
		(await App.Connection.Table<TaskCompletionRate>().ToListAsync()).AsParallel().ForAll(delegate(TaskCompletionRate item)
		{
			item.IsFromDb = true;
			_cacheRates.TryAdd(item.TaskId, item);
		});
	}

	public static async Task SaveToDb()
	{
		List<TaskCompletionRate> all = _cacheRates.Values.ToList();
		await App.Connection.UpdateAllAsync(all.Where((TaskCompletionRate item) => item.IsFromDb));
		await App.Connection.InsertAllAsync(all.Where((TaskCompletionRate item) => !item.IsFromDb));
	}

	public static string GetRateStrById(string id)
	{
		TaskCompletionRate rateById = GetRateById(id);
		if (rateById == null || rateById.TotalCount == 0)
		{
			return string.Empty;
		}
		return $"{rateById.CompletedCount}/{rateById.TotalCount}";
	}

	private static TaskCompletionRate GetRateById(string taskId)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return null;
		}
		if (!_cacheRates.TryGetValue(taskId, out var value))
		{
			return null;
		}
		return value;
	}

	public static string GetRateStrByIdInDb(string id, string projectId)
	{
		if (string.IsNullOrEmpty(id))
		{
			return string.Empty;
		}
		List<TaskBaseViewModel> children = TaskCache.GetChildren(id, projectId);
		int completed = children.Count((TaskBaseViewModel item) => item.Status != 0);
		int total = children.Count;
		_cacheRates.AddOrUpdate(id, new TaskCompletionRate
		{
			TaskId = id,
			CompletedCount = completed,
			TotalCount = total
		}, delegate(string key, TaskCompletionRate old)
		{
			old.CompletedCount = completed;
			old.TotalCount = total;
			return old;
		});
		if (total > 0)
		{
			return $"{completed}/{total}";
		}
		return string.Empty;
	}

	public static async Task<List<string>> FetchRateByModels(List<DisplayItemModel> taskModels)
	{
		List<string> changedList = new List<string>();
		int skip = 0;
		List<ChildTaskCompletedModel> fullList = new List<ChildTaskCompletedModel>();
		List<string> ids = taskModels.Select((DisplayItemModel t) => t.Id).ToList();
		while (skip < ids.Count)
		{
			List<string> source = ids.Skip(skip).Take(300).ToList();
			skip += 300;
			if (source.Any())
			{
				fullList.AddRange(await App.Connection.QueryAsync<ChildTaskCompletedModel>("select parentId, status, projectId from TaskModel " + string.Format("where {0} = {1} ", "deleted", 0) + "and parentId in (" + string.Join(",", source.Select((string id) => "'" + id + "'")) + ")", Array.Empty<object>()));
			}
		}
		foreach (string parentId in (from item in fullList
			where !string.IsNullOrEmpty(item.parentId)
			select item.parentId).Distinct())
		{
			ids.Remove(parentId);
			DisplayItemModel parentModel = taskModels.FirstOrDefault((DisplayItemModel t) => t.Id == parentId);
			if (parentModel == null || string.IsNullOrEmpty(parentModel.ProjectId))
			{
				continue;
			}
			int num = (from item in fullList
				where item.projectId == parentModel.ProjectId
				where item.parentId == parentId
				select item).Count((ChildTaskCompletedModel item) => item.status != 0);
			int num2 = fullList.Where((ChildTaskCompletedModel item) => item.projectId == parentModel.ProjectId).Count((ChildTaskCompletedModel item) => item.parentId == parentId);
			if (_cacheRates.TryGetValue(parentId, out var value))
			{
				if (num2 != value.TotalCount || num != value.CompletedCount)
				{
					value.TotalCount = num2;
					value.CompletedCount = num;
					changedList.Add(parentId);
				}
			}
			else
			{
				_cacheRates[parentId] = new TaskCompletionRate
				{
					TaskId = parentId,
					CompletedCount = num,
					TotalCount = num2
				};
				changedList.Add(parentId);
			}
		}
		foreach (string item in ids)
		{
			if (_cacheRates.TryRemove(item, out var _))
			{
				changedList.Add(item);
			}
		}
		return changedList;
	}
}
