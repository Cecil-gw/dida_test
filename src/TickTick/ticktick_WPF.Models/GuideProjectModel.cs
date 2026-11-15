using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class GuideProjectModel
{
	public string name { get; set; }

	public GuideProjectData metaData { get; set; }

	public string id { get; set; }

	public ProjectModel ToProject()
	{
		if (string.IsNullOrEmpty(id))
		{
			id = Utils.GetGuid();
		}
		if (metaData == null)
		{
			return null;
		}
		return new ProjectModel
		{
			id = id,
			name = name,
			sortType = metaData.sortType,
			viewMode = metaData.viewMode,
			userid = LocalSettings.Settings.LoginUserId,
			sync_status = Constants.SyncStatus.SYNC_NEW.ToString(),
			kind = Constants.ProjectKind.TASK.ToString(),
			inAll = true
		};
	}

	public List<ColumnModel> ToColumns()
	{
		if (string.IsNullOrEmpty(id))
		{
			id = Utils.GetGuid();
		}
		return metaData?.columns?.Select((GuideProjectColumn c) => new ColumnModel
		{
			id = c.id,
			projectId = id,
			name = c.name,
			sortOrder = c.sortOrder,
			syncStatus = "new",
			userId = LocalSettings.Settings.LoginUserId
		}).ToList();
	}

	public List<TaskModel> ToTasks()
	{
		if (string.IsNullOrEmpty(id))
		{
			id = Utils.GetGuid();
		}
		if (metaData?.tasks == null)
		{
			return null;
		}
		List<TaskModel> list = new List<TaskModel>();
		long num = 0L;
		foreach (GuideProjectTask task in metaData.tasks)
		{
			string tId = task.id ?? Utils.GetGuid();
			TaskModel item = new TaskModel
			{
				id = tId,
				title = task.title,
				content = task.content,
				desc = task.desc,
				columnId = task.columnId,
				parentId = task.parentId,
				projectId = id,
				repeatFlag = task.repeatFlag,
				repeatFrom = task.repeatFrom,
				timeZone = task.timeZone,
				startDate = task.startDate,
				dueDate = task.dueDate,
				isFloating = task.floating,
				isAllDay = task.allDay,
				priority = task.priority,
				tags = task.tags.ToArray(),
				userId = LocalSettings.Settings.LoginUserId,
				creator = LocalSettings.Settings.LoginUserId,
				reminders = task.reminders?.Select((string r) => new TaskReminderModel
				{
					id = Utils.GetGuid(),
					taskserverid = tId,
					trigger = r
				}).ToArray(),
				items = task.items?.Select((GuideProjectTaskItem i) => new TaskDetailItemModel
				{
					id = Utils.GetGuid(),
					title = i.title,
					startDate = i.startDate,
					isAllDay = i.allDay,
					TaskServerId = tId
				}).ToArray(),
				Actions = ((task.actions == null) ? null : JsonConvert.SerializeObject(task.actions)),
				Resources = ((task.resources == null) ? null : JsonConvert.SerializeObject(task.resources)),
				label = task.label,
				sortOrder = num,
				taskType = 1
			};
			list.Add(item);
			num += 268435456;
		}
		return list;
	}
}
