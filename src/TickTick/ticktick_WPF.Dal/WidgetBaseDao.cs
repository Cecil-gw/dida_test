using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

internal abstract class WidgetBaseDao<T> where T : WidgetBaseModel, new()
{
	public static async Task<List<T>> GetWidgets()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from widget in App.Connection.Table<T>()
			where widget.UserId == userId
			select widget).ToListAsync();
	}

	public static async Task<T> GetWidgetById(string widgetId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return (await App.Connection.Table<T>().ToListAsync()).Where((T widget) => widget.Id == widgetId && widget.UserId == userId).FirstOrDefault();
	}

	public static async Task DeleteWidgetById(string id)
	{
		List<T> list = await (from v in App.Connection.Table<T>()
			where v.Id == id
			select v).ToListAsync();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (T item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task UpdateWidget(T settingModel)
	{
		await App.Connection.UpdateAsync(settingModel);
	}
}
