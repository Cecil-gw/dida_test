using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Service.SortOrder;

public class ProjectPinSortOrderService
{
	public static async Task<List<SyncSortOrderModel>> GetAsync()
	{
		return (await SyncSortOrderDao.GetAllOrderBySortOrderType("projectPinned")) ?? new List<SyncSortOrderModel>();
	}

	public static async Task<bool> CheckIsProjectPinned(string entityId, int type)
	{
		return await SyncSortOrderDao.GetSortOrderByIdAndType("projectPinned", entityId, type) != null;
	}

	public static async Task<ProjectPinSortOrderModel> Insert(string entityId, int type)
	{
		List<long> source = (await GetAsync()).Select((SyncSortOrderModel m) => m.SortOrder).ToList();
		long sortOrder = (source.Any() ? (source.Max() + 268435456) : 0);
		ProjectPinSortOrderModel pinModel = new ProjectPinSortOrderModel
		{
			EntityId = entityId,
			Type = type,
			SortOrder = sortOrder,
			SyncStatus = 1
		};
		await SyncSortOrderDao.InsertOrUpdate(pinModel);
		return pinModel;
	}

	public static async Task Delete(string id, int type)
	{
		await SyncSortOrderDao.DeleteAsync(new ProjectPinSortOrderModel
		{
			EntityId = id,
			Type = type
		});
	}

	public static async Task UpdateProjectPinModel(string entityId, int type, long sortOrder)
	{
		SyncSortOrderModel syncSortOrderModel = await SyncSortOrderDao.GetSortOrderByIdAndType("projectPinned", entityId, type);
		if (syncSortOrderModel != null)
		{
			syncSortOrderModel.SortOrder = sortOrder;
			syncSortOrderModel.SyncStatus = 1;
			await SyncSortOrderDao.InsertOrUpdate(syncSortOrderModel);
		}
	}
}
