using System.Collections.Generic;
using System.Threading.Tasks;
using TickTickModels;
using TickTickUtils;

namespace TickTickDao;

public class WindowModelDao : BaseDao<WindowModel>
{
	public new static async Task<List<WindowModel>> GetAllAsync()
	{
		string userId = GlobalSettings.UserId;
		return await (from m in Connection.DbConnection.Table<WindowModel>()
			where m.UserId == userId
			select m).ToListAsync();
	}

	public static async Task<WindowModel> GetModelByIdAsync(string id)
	{
		return await (from m in Connection.DbConnection.Table<WindowModel>()
			where m.Id == id && m.UserId == GlobalSettings.UserId
			select m).FirstOrDefaultAsync();
	}

	public static async Task<WindowModel> SaveWindowLocation(string id, double left, double top, double width, double height)
	{
		WindowModel window = await GetModelByIdAsync(id);
		if (window != null)
		{
			window.Left = left;
			window.Top = top;
			window.Width = width;
			window.Height = height;
		}
		await BaseDao<WindowModel>.UpdateAsync(window);
		return window;
	}

	public static async Task SaveWindow(WindowModel window)
	{
		WindowModel windowModel = await GetModelByIdAsync(window.Id);
		if (windowModel != null)
		{
			windowModel.Left = window.Left;
			windowModel.Top = window.Top;
			windowModel.Width = window.Width;
			windowModel.Height = window.Height;
			windowModel.TopMost = window.TopMost;
			windowModel.MaxSize = window.MaxSize;
			windowModel.Data = window.Data;
			windowModel.Closed = window.Closed;
			await BaseDao<WindowModel>.UpdateAsync(windowModel);
		}
	}

	public static async Task SaveWindowMaxSize(string id, bool maxSize)
	{
		WindowModel windowModel = await GetModelByIdAsync(id);
		if (windowModel != null)
		{
			windowModel.MaxSize = maxSize;
		}
		await BaseDao<WindowModel>.UpdateAsync(windowModel);
	}

	public static async Task CloseAsync(string id)
	{
		WindowModel windowModel = await GetModelByIdAsync(id);
		if (windowModel != null)
		{
			windowModel.Closed = true;
			await BaseDao<WindowModel>.UpdateAsync(windowModel);
		}
	}

	public static async Task OpenWindow(WindowModel windowModel)
	{
		WindowModel windowModel2 = await GetModelByIdAsync(windowModel.Id);
		if (windowModel2 != null)
		{
			windowModel2.Closed = false;
			await BaseDao<WindowModel>.UpdateAsync(windowModel2);
		}
	}
}
