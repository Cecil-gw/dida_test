using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Dal;

public class TempDataDao : BaseDao<TempDataModel>
{
	public static async Task<TempDataModel> QueryTempDataModelListDbByTypeAndUserIdAsync(string type, string userId)
	{
		return await Task.Run(async delegate
		{
			List<TempDataModel> list = await (from v in App.Connection.Table<TempDataModel>()
				where v.User_Id.Equals(userId)
				where v.DataType.Equals(type)
				select v).ToListAsync();
			return (list.Count != 0) ? list[0] : null;
		});
	}

	public static async Task<TempDataModel> QueryTempDataModelListDbByTypeAndEntityIdAsync(string type, string entityId)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		return await Task.Run(async delegate
		{
			List<TempDataModel> list = await (from v in App.Connection.Table<TempDataModel>()
				where v.EntityId.Equals(entityId) && v.User_Id == userId
				where v.DataType.Equals(type)
				select v).ToListAsync();
			return (list.Count != 0) ? list[0] : null;
		});
	}

	public static async Task<int> UpdateOrInsertTempDataModelListDbByTypeAndEntityIdAsync(TempDataModel tempDataModel)
	{
		return await Task.Run(async delegate
		{
			_ = 2;
			try
			{
				TempDataModel tempDataModel2 = await QueryTempDataModelListDbByTypeAndEntityIdAsync(tempDataModel.DataType, tempDataModel.EntityId);
				if (tempDataModel2 != null)
				{
					tempDataModel._Id = tempDataModel2._Id;
					await App.Connection.UpdateAsync(tempDataModel);
					return 0;
				}
				await App.Connection.InsertAsync(tempDataModel);
				return tempDataModel._Id;
			}
			catch (Exception)
			{
				return -1;
			}
		});
	}

	public static async Task<int> UpdateOrInsertTempDataModelListDbByTypeAndUserIdAsync(TempDataModel tempDataModel)
	{
		return await Task.Run(async delegate
		{
			_ = 2;
			try
			{
				TempDataModel tempDataModel2 = await QueryTempDataModelListDbByTypeAndUserIdAsync(tempDataModel.DataType, tempDataModel.User_Id);
				if (tempDataModel2 != null)
				{
					tempDataModel._Id = tempDataModel2._Id;
					await App.Connection.UpdateAsync(tempDataModel);
					return 0;
				}
				await App.Connection.InsertAsync(tempDataModel);
				return tempDataModel._Id;
			}
			catch (Exception)
			{
				return -1;
			}
		});
	}
}
