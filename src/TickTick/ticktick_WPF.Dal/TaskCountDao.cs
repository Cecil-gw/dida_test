using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Dal;

public static class TaskCountDao
{
	public static async Task<List<SortableTask>> GetAllSortableTasks()
	{
		string sql = "select distinct(id) as TaskId, projectId as ProjectId, tag as Tag from taskmodel where userid = '" + LocalSettings.Settings.LoginUserId + "' and status = 0 and deleted = 0";
		return await App.Connection.QueryAsync<SortableTask>(sql, Array.Empty<object>());
	}
}
