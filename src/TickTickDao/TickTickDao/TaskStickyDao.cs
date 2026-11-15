using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickModels;
using TickTickUtils;

namespace TickTickDao;

public class TaskStickyDao : BaseDao<TaskStickyModel>
{
	public new static async Task<List<TaskStickyModel>> GetAllAsync()
	{
		string userId = GlobalSettings.UserId;
		return await (from m in Connection.DbConnection.Table<TaskStickyModel>()
			where m.UserId == userId
			select m).ToListAsync();
	}

	public static async Task<List<TaskStickyModel>> GetOpenedAsync()
	{
		string userId = GlobalSettings.UserId;
		return await (from m in Connection.DbConnection.Table<TaskStickyModel>()
			where m.UserId == userId && m.Closed == false
			select m).ToListAsync();
	}

	public static async Task<TaskStickyModel> GetModelByTaskIdAsync(string taskId, string userId)
	{
		List<TaskStickyModel> stickies = await (from m in Connection.DbConnection.Table<TaskStickyModel>()
			where m.TaskId == taskId && m.UserId == userId
			select m).ToListAsync();
		if (stickies.Count > 1)
		{
			for (int i = 1; i < stickies.Count; i++)
			{
				await BaseDao<TaskStickyModel>.DeleteAsync(stickies[i]);
			}
		}
		return stickies.FirstOrDefault();
	}

	public static async Task<TaskStickyModel> SaveStickyLocation(string taskId, double left, double top, double width, double height, bool expand)
	{
		string userId = GlobalSettings.UserId;
		TaskStickyModel sticky = await GetModelByTaskIdAsync(taskId, userId);
		if (sticky != null)
		{
			sticky.Left = left;
			sticky.Top = top;
			sticky.Width = width;
			sticky.Height = height;
			sticky.Expand = expand;
		}
		await BaseDao<TaskStickyModel>.UpdateAsync(sticky);
		return sticky;
	}

	public static async Task SaveStickyColor(string taskId, string color)
	{
		string userId = GlobalSettings.UserId;
		TaskStickyModel taskStickyModel = await GetModelByTaskIdAsync(taskId, userId);
		if (taskStickyModel != null)
		{
			taskStickyModel.Color = color;
		}
		await BaseDao<TaskStickyModel>.UpdateAsync(taskStickyModel);
	}

	public static async Task SaveStickyTopMost(string taskId, bool topMost)
	{
		string userId = GlobalSettings.UserId;
		TaskStickyModel taskStickyModel = await GetModelByTaskIdAsync(taskId, userId);
		if (taskStickyModel != null)
		{
			taskStickyModel.TopMost = topMost;
		}
		await BaseDao<TaskStickyModel>.UpdateAsync(taskStickyModel);
	}

	public static async Task AddStickyAsync(TaskStickyModel stickyModel)
	{
		string userId = GlobalSettings.UserId;
		TaskStickyModel taskStickyModel = await GetModelByTaskIdAsync(stickyModel.TaskId, userId);
		if (taskStickyModel != null)
		{
			taskStickyModel.Closed = false;
			await BaseDao<TaskStickyModel>.UpdateAsync(taskStickyModel);
		}
		else
		{
			await BaseDao<TaskStickyModel>.InsertAsync(stickyModel);
		}
	}

	public static async Task CloseAsync(string taskId)
	{
		string userId = GlobalSettings.UserId;
		TaskStickyModel taskStickyModel = await GetModelByTaskIdAsync(taskId, userId);
		if (taskStickyModel != null)
		{
			taskStickyModel.LastCloseDate = DateUtils.GetDateNum(DateTime.Now);
			taskStickyModel.Closed = true;
		}
		await BaseDao<TaskStickyModel>.UpdateAsync(taskStickyModel);
	}
}
