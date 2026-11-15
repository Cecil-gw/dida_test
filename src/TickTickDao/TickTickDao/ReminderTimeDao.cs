using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickModels;
using TickTickUtils;

namespace TickTickDao;

public class ReminderTimeDao
{
	public static async Task AddReminderTimes(List<ReminderTimeModel> models)
	{
		if (models == null)
		{
			return;
		}
		List<ReminderTimeModel> list = models.ToList();
		foreach (ReminderTimeModel item in list)
		{
			if (item != null && string.IsNullOrEmpty(item.UserId))
			{
				item.UserId = GlobalSettings.UserId;
			}
		}
		await Connection.DbConnection.InsertAllAsync(list);
	}

	public static async Task DeleteReminderTimeByIdAndType(string id, int type)
	{
		await Connection.DbConnection.ExecuteAsync("DELETE FROM ReminderTimeModel WHERE EntityId=? and Type=? and UserId=?", id, type, GlobalSettings.UserId);
	}

	public static async Task DeleteReminderTimeByType(int type)
	{
		await Connection.DbConnection.ExecuteAsync("DELETE FROM ReminderTimeModel WHERE Type=? and UserId=?", type, GlobalSettings.UserId);
	}

	public static async Task DeleteAllReminderTime()
	{
		await Connection.DbConnection.ExecuteAsync("DELETE FROM ReminderTimeModel WHERE UserId=?", GlobalSettings.UserId);
	}

	public static async Task<List<ReminderTimeModel>> GetReminderTimeModelBetweenTime(DateTime start, DateTime end)
	{
		long ticks = start.Ticks;
		long ticks2 = end.Ticks;
		string sql = $"Select * from ReminderTimeModel Where ReminderTime >= {ticks} and ReminderTime <= {ticks2} and UserId='{GlobalSettings.UserId}' ";
		return await Connection.DbConnection.QueryAsync<ReminderTimeModel>(sql, Array.Empty<object>());
	}
}
