using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Cache;

public class TaskSyncCache : CacheBase<TaskSyncedJsonModel>
{
	public override async Task Load()
	{
		TryLoad();
	}

	private static async Task TryLoad()
	{
		await Task.Delay(15000);
		Task.Run(async delegate
		{
			Stopwatch ws = new Stopwatch();
			ws.Restart();
			List<string> list = (await TaskSyncedJsonDao.GetJsonNotExitIds())?.ToList();
			ws.Stop();
			UtilLog.Info("GetJsonNotExitIds : " + ws.ElapsedMilliseconds + " " + list?.Count());
			if (list != null && list.Count != 0)
			{
				TaskSyncedJsonBean bean = new TaskSyncedJsonBean();
				foreach (string item2 in list)
				{
					TaskModel taskModel = await TaskDao.GetTaskById(item2);
					if (taskModel != null)
					{
						TaskModel item = await TaskDao.AssembleFullTask(taskModel);
						bean.Added.Add(item);
					}
				}
				await TaskSyncedJsonDao.SaveTaskSyncedJsons(bean);
			}
		});
	}
}
