using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickModels;
using TickTickUtils;

namespace TickTickDao;

public class ReminderDelayDao : BaseDao<ReminderDelayModel>
{
	public static async Task<List<ReminderDelayModel>> GetDelayModelById(string id, string type)
	{
		return await (from m in Connection.DbConnection.Table<ReminderDelayModel>()
			where m.ObjId == id && m.UserId == GlobalSettings.UserId && m.Type == type
			select m).ToListAsync();
	}

	public static async Task<List<ReminderDelayModel>> GetDelayModelByType(string type)
	{
		return await (from m in Connection.DbConnection.Table<ReminderDelayModel>()
			where m.UserId == GlobalSettings.UserId && m.Type == type
			select m).ToListAsync();
	}

	public static async Task<List<ReminderDelayModel>> GetAllModelsAsync()
	{
		return await (from m in Connection.DbConnection.Table<ReminderDelayModel>()
			where m.UserId == GlobalSettings.UserId
			select m).ToListAsync();
	}

	public static async Task<List<ReminderDelayModel>> GetNeedSyncModelsAsync()
	{
		return await (from m in Connection.DbConnection.Table<ReminderDelayModel>()
			where m.UserId == GlobalSettings.UserId && m.SyncStatus != 2
			select m).ToListAsync();
	}

	public static async Task AddModelAsync(ReminderDelayModel model)
	{
		foreach (ReminderDelayModel item in await GetDelayModelById(model.ObjId, model.Type))
		{
			await BaseDao<ReminderDelayModel>.DeleteAsync(item);
		}
		await BaseDao<ReminderDelayModel>.InsertAsync(model);
	}

	public static async Task DeleteByIdAsync(string modelId, string type)
	{
		List<ReminderDelayModel> list = await GetDelayModelById(modelId, type);
		if (!list.Any())
		{
			return;
		}
		foreach (ReminderDelayModel item in list)
		{
			if (item.SyncStatus != 0)
			{
				item.SyncStatus = -1;
				await BaseDao<ReminderDelayModel>.UpdateAsync(item);
			}
			else
			{
				await BaseDao<ReminderDelayModel>.DeleteAsync(item);
			}
		}
	}
}
