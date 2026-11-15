using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Entity.Dto;
using ticktick_WPF.Entity.Enums;
using ticktick_WPF.Framework;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Service;

internal class AIRecommendService
{
	public static Action<string> OnToast { get; set; }

	public static Action<List<AIRecommendPropertyDto>> OnResult { get; set; }

	private static HashSet<string> actIds { get; set; } = new HashSet<string>();

	private static Task queryTask { get; set; }

	public static DateTime lastMTime { get; set; }

	public static DateTime lastCTime { get; set; }

	private static DateTime failedBackTime { get; set; }

	public static IEnumerable<TaskBaseViewModel> GetTasks()
	{
		return from x in TaskViewModelHelper.GetTasks(TaskCache.GetAllTask(), null, null, null, null, showComplete: true, inTodayOrWeek: false, inAll: true, null, null, LogicType.Or, withChild: true, inCal: false, orTag: false, TaskType.TaskAndNote, null, checkProjectPermission: true)
			where x.Deleted == 0 && x.Kind != "NOTE" && x.Status == 0 && string.IsNullOrEmpty(x.RepeatFlag) && x.TaskType != 1
			select x;
	}

	public static async Task<bool> StartRecommend()
	{
		DateTime? lastRecTime = lastCTime;
		if (!lastRecTime.HasValue)
		{
			lastRecTime = null;
		}
		failedBackTime = lastCTime;
		lastCTime = DateTime.Now;
		IEnumerable<TaskBaseViewModel> uncompleteTasks = GetTasks();
		Dictionary<string, (string name, string groupId)> projectDict = (await ProjectDao.GetAllProjects()).ToDictionary((ProjectModel x) => x.id, (ProjectModel x) => (name: x.name, groupId: x.groupId));
		Dictionary<string, string> projectGroupDict = (await ProjectGroupDao.GetProjectGroups()).ToDictionary((ProjectGroupModel x) => x.id, (ProjectGroupModel x) => x.name);
		Dictionary<string, string> dictionary = (await ColumnDao.GetAllColumnsAsync()).ToDictionary((ColumnModel x) => x.id, (ColumnModel x) => x.name);
		List<AIRecommendInputDto> uploads = new List<AIRecommendInputDto>();
		foreach (TaskBaseViewModel item in uncompleteTasks)
		{
			if (!string.IsNullOrEmpty(item.RepeatFlag) || (lastRecTime.HasValue && item.CreatedTime.HasValue && item.CreatedTime < lastRecTime && (!item.ModifiedTime.HasValue || item.ModifiedTime < lastRecTime)))
			{
				continue;
			}
			List<string> list = new List<string>();
			if (!string.IsNullOrEmpty(item.ProjectName))
			{
				list.Add(item.ProjectName);
			}
			if (!string.IsNullOrEmpty(item.ColumnId))
			{
				string value;
				if (!string.IsNullOrEmpty(item.ColumnName))
				{
					list.Add(item.ColumnName);
				}
				else if (dictionary.TryGetValue(item.ColumnId, out value))
				{
					list.Add(value);
				}
			}
			if (!string.IsNullOrEmpty(item.ProjectId) && projectDict.TryGetValue(item.ProjectId, out (string, string) value2) && !string.IsNullOrEmpty(value2.Item2) && projectGroupDict.TryGetValue(value2.Item2, out var value3))
			{
				list.Insert(0, value3);
			}
			uploads.Add(new AIRecommendInputDto
			{
				id = item.Id,
				title = item.Title,
				startDate = item.StartDate,
				dueDate = item.DueDate,
				category = (list.Any((string x) => !string.IsNullOrEmpty(x)) ? string.Join("/", list) : null),
				priority = item.Priority,
				tags = (item.Tags ?? new string[0]),
				Time = (item.ModifiedTime ?? item.CreatedTime)
			});
		}
		if (!uploads.Any())
		{
			ToastNotify(Utils.GetString("AINoNewTaskSuggest"));
			return false;
		}
		List<string> addActIds = new List<string>();
		IEnumerable<IEnumerable<AIRecommendInputDto>> enumerable = uploads.OrderByDescending((AIRecommendInputDto x) => x.Time).Take(200).Chunk(50);
		foreach (IEnumerable<AIRecommendInputDto> item2 in enumerable)
		{
			_ = item2;
			(AITaskIdDto, string) tuple = await Communicator.AIRecommendUploadToActQueue(uploads);
			if (tuple.Item1 == null)
			{
				if (tuple.Item2 == "token_usage_exceed_limit")
				{
					ToastNotify(Utils.GetString("AIUsageExceedLimitToast"));
					break;
				}
				ToastNotify(Utils.GetString("AIRequestFailedToast"));
			}
			else
			{
				addActIds.Add(tuple.Item1.taskId);
			}
		}
		if (addActIds.Any())
		{
			foreach (string item3 in addActIds)
			{
				actIds.Add(item3);
			}
			StartQuery();
			return true;
		}
		lastCTime = failedBackTime;
		return false;
	}

	private static void StartQuery()
	{
		if (queryTask != null)
		{
			return;
		}
		queryTask = Task.Run(async delegate
		{
			List<AIRecommendPropertyDto> list = new List<AIRecommendPropertyDto>();
			bool isSuccess = false;
			while (actIds.Any())
			{
				List<string> toRemove = new List<string>();
				foreach (string id in actIds)
				{
					(CommonDto<List<AIRecommendPropertyDto>>, string) tuple = await Communicator.AIRecommendQueryActResult(id);
					if (tuple.Item1 == null)
					{
						if (tuple.Item2 == "token_usage_exceed_limit")
						{
							ToastNotify(Utils.GetString("AIUsageExceedLimitToast"));
						}
						else
						{
							ToastNotify(Utils.GetString("AIRequestFailedToast"));
						}
						toRemove.Add(id);
					}
					else
					{
						AIUploadResultType code = (AIUploadResultType)tuple.Item1.code;
						switch (code)
						{
						case AIUploadResultType.Success:
							list.AddRange(tuple.Item1.data);
							isSuccess = true;
							break;
						case AIUploadResultType.InvalidId:
							ToastNotify(Utils.GetString("AIRequestFailedToast"));
							UtilLog.Error("AIRecommendQueryActResult invalid id: " + id);
							break;
						case AIUploadResultType.HandlingFailed:
							ToastNotify(Utils.GetString("AIRequestFailedToast"));
							UtilLog.Error("AIRecommendQueryActResult handling failed: " + id);
							break;
						}
						if (code != AIUploadResultType.Handling)
						{
							toRemove.Add(id);
						}
					}
				}
				if (toRemove.Any())
				{
					foreach (string item in toRemove)
					{
						actIds.Remove(item);
					}
				}
				if (!actIds.Any())
				{
					break;
				}
				await Task.Delay(5000);
			}
			OnResult?.Invoke(list);
			if (isSuccess)
			{
				if (!list.Any())
				{
					ToastNotify(Utils.GetString("AINoNewTaskSuggest"));
				}
				else
				{
					ToastNotify(Utils.GetString("AISuggestionCompleted"));
				}
			}
			else
			{
				lastCTime = failedBackTime;
			}
			queryTask = null;
		});
	}

	private static void ToastNotify(string text)
	{
		if (OnToast != null)
		{
			OnToast(text);
		}
		else
		{
			Utils.Toast(text);
		}
	}
}
