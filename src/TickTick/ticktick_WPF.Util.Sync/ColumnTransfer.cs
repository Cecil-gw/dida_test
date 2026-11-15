using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public class ColumnTransfer
{
	public static async Task<SyncColumnBean> DescribeSyncColumnBean(Dictionary<string, string> changes)
	{
		SyncColumnBean bean = new SyncColumnBean();
		if (changes != null && changes.Any())
		{
			foreach (KeyValuePair<string, string> change in changes)
			{
				ColumnModel columnModel = await ColumnDao.GetColumnById(change.Key);
				if (columnModel != null)
				{
					switch (columnModel.syncStatus)
					{
					case "init":
					case "new":
						bean.Add.Add(columnModel);
						break;
					case "update":
						bean.Update.Add(columnModel);
						break;
					case "delete":
						bean.Deleted.Add(new ColumnProjectModel
						{
							columnId = columnModel.id,
							projectId = columnModel.projectId
						});
						break;
					}
				}
			}
		}
		return bean;
	}

	public static async Task HandleCommitResult(Dictionary<string, string> id2Etag, Dictionary<string, string> id2Error, LogModel logModel)
	{
		await ColumnDao.SaveCommitResultBackToDb(id2Etag, id2Error, logModel);
	}
}
