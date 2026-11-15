using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync.ClosedLoader;

namespace ticktick_WPF.Util.Sync;

public class ClosedListSyncService
{
	public static void ClearLoadDict()
	{
	}

	public static async Task<bool> NeedPullCompletedTasks(ProjectIdentity projectId)
	{
		if (!(projectId is BindAccountCalendarProjectIdentity) && !(projectId is SubscribeCalendarProjectIdentity) && !(projectId is SearchProjectIdentity) && !(projectId is FilterProjectIdentity) && !(projectId is AssignToMeProjectIdentity) && !(projectId is CompletedProjectIdentity) && !(projectId is AbandonedProjectIdentity) && !(projectId is TrashProjectIdentity))
		{
			if (projectId is TagProjectIdentity)
			{
				ClosedLoadStatus closedLoadStatus = await CompletionLoadStatusDao.GetLoadStatusByEntityId(projectId.Id);
				return closedLoadStatus == null || !closedLoadStatus.IsAllLoaded;
			}
			ClosedLoadStatus closedLoadStatus2 = await CompletionLoadStatusDao.GetLoadStatusByEntityId(projectId.LoadMoreId);
			return !closedLoadStatus2.IsAllLoaded && (DateTime.Now - closedLoadStatus2.LastPullTime).TotalMinutes >= 5.0;
		}
		return false;
	}

	public static async void PullCompletedTaskAtFirstLogin()
	{
		List<TaskModel> list = await Communicator.GetClosedTasks(Utils.GetInboxId(), null, DateTime.Now, 600);
		if (list != null)
		{
			await TaskService.MergeTasks(list);
		}
	}
}
