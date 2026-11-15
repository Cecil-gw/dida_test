using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickModels;
using TickTickUtils;

namespace TickTickDao;

public class TimerDao : BaseDao<TimerModel>
{
	public static async Task<TimerModel> GetTimerById(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		return await (from t in Connection.DbConnection.Table<TimerModel>()
			where t.Id == id
			select t).FirstOrDefaultAsync();
	}

	public static async Task<TimerModel> GetTimerByObjId(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		return await (from t in Connection.DbConnection.Table<TimerModel>()
			where t.ObjId == id
			select t).FirstOrDefaultAsync();
	}

	public static async Task<List<TimerModel>> GetDisplayTimersAsync()
	{
		string userId = GlobalSettings.UserId;
		return await (from t in Connection.DbConnection.Table<TimerModel>()
			where t.UserId == userId && t.Deleted == false
			orderby t.SortOrder
			select t).ToListAsync();
	}

	public static async Task<int> GetActiveTimerCountAsync()
	{
		string sql = "SELECT count(*) as num from TimerModel where UserId ='" + GlobalSettings.UserId + "' and Deleted = false and Status = 0";
		return ((await Connection.DbConnection.QueryAsync<IntModel>(sql, Array.Empty<object>()))?.FirstOrDefault()?.num).GetValueOrDefault();
	}

	public static async Task<long> GetMinSortOrder()
	{
		string sql = "select SortOrder from TimerModel Order By SortOrder limit 1";
		return ((await Connection.DbConnection.QueryAsync<OrderModel>(sql, Array.Empty<object>()))?.FirstOrDefault()?.SortOrder).GetValueOrDefault();
	}

	public static async Task<List<TimerModel>> GetNeedPostTimers()
	{
		string userId = GlobalSettings.UserId;
		return await (from t in Connection.DbConnection.Table<TimerModel>()
			where t.UserId == userId && t.SyncStatus != 2
			select t).ToListAsync();
	}

	public static async Task SaveSortOrder(string id, long sortOrder)
	{
		string query = $"Update TimerModel Set SortOrder = {sortOrder} , SyncStatus = case when SyncStatus = 2 then 1 else SyncStatus end where Id = '{id}'";
		await Connection.DbConnection.ExecuteAsync(query);
	}

	public static async Task SaveEtag(string id, string etag)
	{
		string query = "Update TimerModel Set Etag = '" + etag + "' , SyncStatus = 2 where Id = '" + id + "'";
		await Connection.DbConnection.ExecuteAsync(query);
	}

	public static async Task SaveName(string id, string title)
	{
		string query = "Update TimerModel Set Name = ? , SyncStatus = case when SyncStatus = 2 then 1 else SyncStatus end where Id = '" + id + "'";
		await Connection.DbConnection.ExecuteAsync(query, title);
	}

	public static async Task SetDeleteStatus(string id)
	{
		string query = "Update TimerModel Set Deleted = True , SyncStatus = -1 where Id = '" + id + "'";
		await Connection.DbConnection.ExecuteAsync(query);
	}

	public static async Task ChangeArchiveStatus(string id, long sortOrder)
	{
		TimerModel timerModel = await GetTimerById(id);
		if (timerModel != null)
		{
			timerModel.SortOrder = sortOrder;
			timerModel.Status = ((timerModel.Status == 0) ? 1 : 0);
			timerModel.ArchivedTime = ((timerModel.Status == 1) ? new DateTime?(DateTime.Now) : ((DateTime?)null));
			timerModel.SyncStatus = ((timerModel.SyncStatus != 0) ? 1 : 0);
			await BaseDao<TimerModel>.UpdateAsync(timerModel);
		}
	}

	public static async Task DeleteByIds(List<string> ids)
	{
		string query = "Delete From TimerModel where Id in ('" + string.Join("','", ids) + "')";
		await Connection.DbConnection.ExecuteAsync(query);
	}

	public static async Task SaveOverview(string timerId, string overview)
	{
		string query = "Update TimerModel Set Overview = '" + overview + "' where Id = '" + timerId + "'";
		await Connection.DbConnection.ExecuteAsync(query);
	}

	public static async Task<TimerStatisticsModel> GetStatistics(string key)
	{
		return await (from m in Connection.DbConnection.Table<TimerStatisticsModel>()
			where m.Key == key
			select m).FirstOrDefaultAsync();
	}

	public static async Task SaveStatistics(string key, string statisticsString)
	{
		TimerStatisticsModel model = new TimerStatisticsModel
		{
			Key = key,
			Value = statisticsString
		};
		if (await GetStatistics(key) == null)
		{
			await Connection.DbConnection.InsertAsync(model);
		}
		else
		{
			await Connection.DbConnection.UpdateAsync(model);
		}
	}

	public static async Task<List<string>> GetObjIds()
	{
		string sql = "SELECT ObjId as Text from TimerModel where Deleted = false and UserId = '" + GlobalSettings.UserId + "'";
		return ((await Connection.DbConnection.QueryAsync<StringModel>(sql, Array.Empty<object>()))?.Select((StringModel m) => m.Text).ToList())?.Where((string i) => !string.IsNullOrEmpty(i)).ToList();
	}

	public static async Task<TimerModel> GetTimerByIdOrObjId(string id)
	{
		string sql = "SELECT * from TimerModel where Id ='" + id + "' or ObjId = '" + id + "' ";
		return (await Connection.DbConnection.QueryAsync<TimerModel>(sql, Array.Empty<object>()))?.FirstOrDefault();
	}

	public static async Task SetTimerSyncStatus(string timerId, int status)
	{
		string query = $"Update TimerModel Set SyncStatus = {status} where Id = '{timerId}'";
		await Connection.DbConnection.ExecuteAsync(query);
	}
}
