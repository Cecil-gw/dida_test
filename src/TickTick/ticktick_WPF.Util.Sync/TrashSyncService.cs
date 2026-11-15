using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views;

namespace ticktick_WPF.Util.Sync;

public class TrashSyncService
{
	private const int LoadRemoteStep = 50;

	private static readonly object TrashLoadLock = new object();

	public static bool PersonalDrainOff;

	public static bool TeamDrainOff;

	private static int _personalPage;

	private static int _teamPage;

	private static long? _personalNext;

	private static long? _teamNext;

	private static HashSet<string> _personalLoadedTaskIds = new HashSet<string>();

	private static HashSet<string> _teamLoadedTaskIds = new HashSet<string>();

	private static HashSet<string> _personalPendingTaskIds = new HashSet<string>();

	private static HashSet<string> _teamPendingTaskIds = new HashSet<string>();

	private static bool _personalHasRemoteSnapshot;

	private static bool _teamHasRemoteSnapshot;

	private static Task<bool> _personalLoadTask;

	private static Task<bool> _teamLoadTask;

	private static int _personalRefreshVersion;

	private static int _teamRefreshVersion;

	public static bool IsDrainOff(bool isPerson)
	{
		if (!(!UserManager.IsTeamUser() || isPerson))
		{
			return TeamDrainOff;
		}
		return PersonalDrainOff;
	}

	public static long? Next(bool isPerson)
	{
		if (!(!UserManager.IsTeamUser() || isPerson))
		{
			return _teamNext;
		}
		return _personalNext;
	}

	public static int TrashPage(bool isPerson)
	{
		return Math.Max(isPerson ? _personalPage : _teamPage, 1);
	}

	public Task<bool> TryLoadTrashTasks(bool isPerson)
	{
		isPerson = !UserManager.IsTeamUser() || isPerson;
		lock (TrashLoadLock)
		{
			Task<bool> task = (isPerson ? _personalLoadTask : _teamLoadTask);
			if (task != null && !task.IsCompleted)
			{
				return task;
			}
			int refreshVersion = (isPerson ? _personalRefreshVersion : _teamRefreshVersion);
			Task<bool> task2 = TryLoadTrashTasksCore(isPerson, refreshVersion);
			if (isPerson)
			{
				_personalLoadTask = task2;
			}
			else
			{
				_teamLoadTask = task2;
			}
			return task2;
		}
	}

	public async Task<bool> TryLoadMoreTrashTasks(bool isPerson)
	{
		isPerson = !UserManager.IsTeamUser() || isPerson;
		Task<bool> task;
		lock (TrashLoadLock)
		{
			task = (isPerson ? _personalLoadTask : _teamLoadTask);
		}
		if (task != null && !task.IsCompleted)
		{
			await task;
		}
		return await TryLoadTrashTasks(isPerson);
	}

	private async Task<bool> TryLoadTrashTasksCore(bool isPerson, int refreshVersion)
	{
		if (IsDrainOff(isPerson))
		{
			return false;
		}
		long? requestNext = Next(isPerson);
		var (nextText, list) = await Communicator.BatchCheckTrash(requestNext, 50, isPerson ? 1 : 2);
		if (!IsCurrentRefresh(isPerson, refreshVersion))
		{
			return false;
		}
		if (list == null)
		{
			return false;
		}
		List<string> loadedTaskIds = (from task in list ?? new List<TaskModel>()
			where !string.IsNullOrEmpty(task?.id)
			select task.id).ToList();
		await TaskService.MergeTasks(list);
		if (!IsCurrentRefresh(isPerson, refreshVersion))
		{
			return false;
		}
		if (isPerson)
		{
			_personalHasRemoteSnapshot = true;
			_personalLoadedTaskIds.UnionWith(loadedTaskIds);
		}
		else
		{
			_teamHasRemoteSnapshot = true;
			_teamLoadedTaskIds.UnionWith(loadedTaskIds);
		}
		long result = requestNext ?? (-1);
		bool flag = string.IsNullOrEmpty(nextText) || !long.TryParse(nextText, out result) || requestNext == result;
		if (isPerson)
		{
			_personalNext = ((flag || result == -1) ? ((long?)null) : new long?(result));
			PersonalDrainOff = flag;
			_personalPage++;
		}
		else
		{
			_teamNext = ((flag || result == -1) ? ((long?)null) : new long?(result));
			TeamDrainOff = flag;
			_teamPage++;
		}
		return true;
	}

	private static bool IsCurrentRefresh(bool isPerson, int refreshVersion)
	{
		return ((!UserManager.IsTeamUser() || isPerson) ? _personalRefreshVersion : _teamRefreshVersion) == refreshVersion;
	}

	public static void Reset()
	{
		lock (TrashLoadLock)
		{
			_personalRefreshVersion++;
			_teamRefreshVersion++;
			_personalLoadTask = null;
			_teamLoadTask = null;
		}
		PersonalDrainOff = false;
		TeamDrainOff = false;
		_personalPage = 0;
		_teamPage = 0;
		_personalNext = null;
		_teamNext = null;
		_personalLoadedTaskIds.Clear();
		_teamLoadedTaskIds.Clear();
		_personalPendingTaskIds.Clear();
		_teamPendingTaskIds.Clear();
		_personalHasRemoteSnapshot = false;
		_teamHasRemoteSnapshot = false;
	}

	public static void Reset(bool isPerson)
	{
		isPerson = !UserManager.IsTeamUser() || isPerson;
		lock (TrashLoadLock)
		{
			if (isPerson)
			{
				_personalRefreshVersion++;
				_personalLoadTask = null;
			}
			else
			{
				_teamRefreshVersion++;
				_teamLoadTask = null;
			}
		}
		if (isPerson)
		{
			PersonalDrainOff = false;
			_personalPage = 0;
			_personalNext = null;
			_personalLoadedTaskIds.Clear();
			_personalPendingTaskIds.Clear();
			_personalHasRemoteSnapshot = false;
		}
		else
		{
			TeamDrainOff = false;
			_teamPage = 0;
			_teamNext = null;
			_teamLoadedTaskIds.Clear();
			_teamPendingTaskIds.Clear();
			_teamHasRemoteSnapshot = false;
		}
	}

	public static async Task ResetForRefresh(bool isPerson)
	{
		Reset(isPerson);
		HashSet<string> obj = (await SyncStatusDao.GetEntityIdSetByType(5)) ?? new HashSet<string>();
		obj.UnionWith(UndoHelper.DeletingIds.Where((string id) => !string.IsNullOrEmpty(id)).ToList());
		List<string> collection = (from task in TaskCache.GetTasksByIds(obj.ToList()) ?? new List<TaskBaseViewModel>()
			where task != null && task.Deleted == 1 && TaskViewModelHelper.CheckTeamTrash(task.TeamId, isPerson)
			select task.Id into id
			where !string.IsNullOrEmpty(id)
			select id).ToList();
		if (!UserManager.IsTeamUser() || isPerson)
		{
			_personalPendingTaskIds = new HashSet<string>(collection);
		}
		else
		{
			_teamPendingTaskIds = new HashSet<string>(collection);
		}
	}

	public static HashSet<string> GetLoadedTaskIds(bool isPerson)
	{
		if (!(!UserManager.IsTeamUser() || isPerson))
		{
			return _teamLoadedTaskIds;
		}
		return _personalLoadedTaskIds;
	}

	public static HashSet<string> GetPendingTaskIds(bool isPerson)
	{
		if (!(!UserManager.IsTeamUser() || isPerson))
		{
			return _teamPendingTaskIds;
		}
		return _personalPendingTaskIds;
	}

	public static bool HasRemoteSnapshot(bool isPerson)
	{
		if (!(!UserManager.IsTeamUser() || isPerson))
		{
			return _teamHasRemoteSnapshot;
		}
		return _personalHasRemoteSnapshot;
	}
}
