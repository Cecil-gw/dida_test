using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Models;

namespace ticktick_WPF.Dal;

public class LocationDao : BaseDao<LocationModel>
{
	public static async Task<LocationModel> GetLocationByTaskId(string taskId)
	{
		return await (from loc in App.Connection.Table<LocationModel>()
			where loc.taskId == taskId
			select loc).FirstOrDefaultAsync();
	}

	private static async Task DeleteLocationByTaskId(string taskId)
	{
		List<LocationModel> list = await (from loc in App.Connection.Table<LocationModel>()
			where loc.taskId == taskId
			select loc).ToListAsync();
		if (list == null || !list.Any())
		{
			return;
		}
		foreach (LocationModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task InsertLocation(LocationModel model)
	{
		if (!string.IsNullOrEmpty(model.taskId))
		{
			await DeleteLocationByTaskId(model.taskId);
			await App.Connection.InsertAsync(model);
		}
	}
}
