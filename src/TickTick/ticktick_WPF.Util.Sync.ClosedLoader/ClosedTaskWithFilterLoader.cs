using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Completed;

namespace ticktick_WPF.Util.Sync.ClosedLoader;

public class ClosedTaskWithFilterLoader
{
	public static ClosedTaskWithFilterLoader CompletionLoader = new ClosedTaskWithFilterLoader
	{
		InCompletedProject = true
	};

	public bool InCompletedProject;

	public static ClosedTaskWithFilterLoader AbandonedLoader = new ClosedTaskWithFilterLoader
	{
		InAbandonedProject = true
	};

	public bool InAbandonedProject;

	public static ClosedTaskWithFilterLoader CalClosedLoader = new ClosedTaskWithFilterLoader
	{
		InCalProject = true
	};

	public bool InCalProject;

	public LoadStatus CompletedProjectStatus = new LoadStatus(drainOff: false, null, DateTime.Now, 50);

	private List<string> _selectedProjectIds;

	private DateTime _pullToTime;

	private bool _localDrainOff;

	private readonly SemaphoreSlim _loadSemaphore = new SemaphoreSlim(1, 1);

	private List<KeyValuePair<DateTime, DateTime>> _dateKvList = new List<KeyValuePair<DateTime, DateTime>>();

	public event EventHandler<bool> Loaded;

	private async Task<bool> GetProjectLoadStatus(DateTime? earliestInList)
	{
		await SetTimeSpan();
		ClosedLoadStatus closedLoadStatus = await CompletionLoadStatusDao.GetStatusInAllList();
		if (!closedLoadStatus.IsAllLoaded)
		{
			if (CompletedProjectStatus.FromTime.HasValue)
			{
				DateTime earliestPulledDate = closedLoadStatus.EarliestPulledDate;
				DateTime? fromTime = CompletedProjectStatus.FromTime;
				if (!(earliestPulledDate > fromTime))
				{
					goto IL_0178;
				}
			}
			_pullToTime = ResolvePullToTime(closedLoadStatus.EarliestPulledDate, CompletedProjectStatus.ToTime, earliestInList);
			if (earliestInList > _pullToTime)
			{
				return false;
			}
			return true;
		}
		goto IL_0178;
		IL_0178:
		CompletedProjectStatus.DrainOff = true;
		return false;
	}

	internal static DateTime ResolvePullToTime(DateTime statusEarliestPulledDate, DateTime filterToTime, DateTime? earliestInList)
	{
		DateTime dateTime = ((Utils.IsEmptyDate(filterToTime) || statusEarliestPulledDate < filterToTime) ? statusEarliestPulledDate : filterToTime);
		if (earliestInList.HasValue && earliestInList.Value < dateTime)
		{
			dateTime = earliestInList.Value;
		}
		return dateTime;
	}

	private async Task SetTimeSpan()
	{
		ClosedFilterViewModel closedFilterViewModel = null;
		if (InCompletedProject)
		{
			closedFilterViewModel = CompletedProjectIdentity.Filter;
		}
		if (InAbandonedProject)
		{
			closedFilterViewModel = AbandonedProjectIdentity.Filter;
		}
		if (closedFilterViewModel != null)
		{
			CompletedFilterData completedFilterData = await closedFilterViewModel.GetCompletedFilterData();
			_selectedProjectIds = completedFilterData.ProjectIds;
			CompletedProjectStatus.FromTime = completedFilterData.FromTime;
			CompletedProjectStatus.ToTime = completedFilterData.ToTime;
		}
	}

	private async Task<bool?> LoadTasks(DateTime? earliestInList = null, int loadTimes = 0, bool loadMore = false, bool checkNum = true)
	{
		if (loadMore)
		{
			CompletedProjectStatus.Count += 50;
		}
		if (loadTimes >= 4)
		{
			return true;
		}
		if (!(await GetProjectLoadStatus(earliestInList)))
		{
			return !_localDrainOff && loadMore;
		}
		DateTime requestToTime = _pullToTime;
		List<TaskModel> tasks = await Communicator.GetClosedTasks("all", CompletedProjectStatus.FromTime, requestToTime, 100);
		if (tasks == null)
		{
			return null;
		}
		bool responseHasTasks = tasks.Count > 0;
		if (tasks.Count > 0)
		{
			await TaskService.MergeTasks(tasks);
		}
		if (tasks.Count < 100)
		{
			CompletedProjectStatus.DrainOff = true;
		}
		ClosedLoadStatus closedLoadStatus = await CompletionLoadStatusDao.GetLoadStatusByEntityId("all");
		closedLoadStatus.LastPullTime = DateTime.Now;
		bool cursorAdvanced = false;
		if (!CompletedProjectStatus.DrainOff)
		{
			DateTime? earliestDate = tasks.LastOrDefault((TaskModel t) => t.completedTime.HasValue)?.completedTime;
			if (!earliestDate.HasValue)
			{
				return ShouldReloadAfterRequest(loadMore, responseHasTasks, cursorAdvanced: false);
			}
			earliestDate = earliestDate.Value.AddSeconds(-1.0);
			if (!closedLoadStatus.IsAllLoaded && closedLoadStatus.EarliestPulledDate <= requestToTime)
			{
				DateTime earliestPulledDate = closedLoadStatus.EarliestPulledDate;
				DateTime? dateTime = earliestDate;
				if (earliestPulledDate > dateTime)
				{
					closedLoadStatus.EarliestPulledDate = earliestDate.Value;
					await CompletionLoadStatusDao.SaveCompletionLoadStatus(closedLoadStatus);
					cursorAdvanced = true;
				}
			}
			if (checkNum)
			{
				List<string> selectedProjectIds = _selectedProjectIds;
				if (selectedProjectIds != null && selectedProjectIds.Count > 0 && !tasks.Any((TaskModel t) => _selectedProjectIds.Contains(t.projectId)))
				{
					if (earliestDate.Value >= requestToTime)
					{
						return ShouldReloadAfterRequest(loadMore, responseHasTasks, cursorAdvanced);
					}
					loadTimes++;
					bool? flag = await LoadTasks(earliestDate, loadTimes);
					if (!flag.HasValue)
					{
						return null;
					}
					return ShouldReloadAfterRequest(loadMore, responseHasTasks || flag.Value, cursorAdvanced);
				}
			}
		}
		else if (!closedLoadStatus.IsAllLoaded && closedLoadStatus.EarliestPulledDate <= requestToTime)
		{
			DateTime dateTime2 = tasks.LastOrDefault((TaskModel t) => t.completedTime.HasValue)?.completedTime ?? closedLoadStatus.EarliestPulledDate;
			closedLoadStatus.EarliestPulledDate = CompletedProjectStatus.FromTime ?? dateTime2;
			closedLoadStatus.IsAllLoaded = !CompletedProjectStatus.FromTime.HasValue;
			await CompletionLoadStatusDao.SaveCompletionLoadStatus(closedLoadStatus);
			cursorAdvanced = true;
		}
		return ShouldReloadAfterRequest(loadMore, responseHasTasks, cursorAdvanced);
	}

	public async void TryLoadTasks(DateTime? earliestInList = null, bool loadMore = false, bool checkNum = true, Action onLoadFailed = null)
	{
		await ExecuteLoadAsync(async delegate
		{
			bool? flag;
			try
			{
				flag = await LoadTasks(earliestInList, 0, loadMore, checkNum);
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex);
				flag = null;
			}
			if (flag.HasValue)
			{
				NotifyLoaded(flag.Value);
			}
			else
			{
				HandleLoadFailure(loadMore, onLoadFailed);
			}
		});
	}

	internal async Task ExecuteLoadAsync(Func<Task> loadAction)
	{
		await _loadSemaphore.WaitAsync();
		try
		{
			await loadAction();
		}
		finally
		{
			_loadSemaphore.Release();
		}
	}

	internal void HandleLoadFailure(bool loadMore, Action onLoadFailed)
	{
		if (loadMore)
		{
			NotifyLoaded(shouldReload: true);
			return;
		}
		try
		{
			onLoadFailed?.Invoke();
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}

	private void NotifyLoaded(bool shouldReload)
	{
		try
		{
			this.Loaded?.Invoke(this, shouldReload);
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}

	internal static bool ShouldReloadAfterRequest(bool loadMore, bool responseHasTasks, bool cursorAdvanced)
	{
		return loadMore || responseHasTasks || cursorAdvanced;
	}

	public async Task<bool> LoadTasksInSpan(DateTime start, DateTime end, string prjId = "all", bool onlyCheckStart = false)
	{
		if (onlyCheckStart)
		{
			if (!(await GetProjectLoadStatus(start)))
			{
				return false;
			}
		}
		else if (_dateKvList.Any((KeyValuePair<DateTime, DateTime> kv) => kv.Key >= start && kv.Value <= end))
		{
			return false;
		}
		KeyValuePair<DateTime, DateTime> dateKv = new KeyValuePair<DateTime, DateTime>(start, end);
		_dateKvList.Add(dateKv);
		ClosedLoadStatus closedLoadStatus = await CompletionLoadStatusDao.GetLoadStatusByEntityId(prjId);
		if (closedLoadStatus.IsAllLoaded || closedLoadStatus.EarliestPulledDate < start)
		{
			return false;
		}
		bool savePullDate = false;
		DateTime toTime;
		if (closedLoadStatus.EarliestPulledDate > start.AddDays(100.0))
		{
			toTime = end;
		}
		else
		{
			toTime = closedLoadStatus.EarliestPulledDate;
			savePullDate = true;
		}
		bool tasksPulled = false;
		List<TaskModel> tasks = await Communicator.GetClosedTasks(prjId, start, toTime, 1200);
		if (tasks == null)
		{
			_dateKvList.Remove(dateKv);
			return false;
		}
		if (tasks.Count > 0 && await TaskService.MergeTasks(tasks))
		{
			tasksPulled = true;
		}
		while (tasks.Count >= 1200)
		{
			TaskModel taskModel = tasks.LastOrDefault((TaskModel t) => t.completedTime.HasValue);
			if (taskModel == null || !taskModel.completedTime.HasValue)
			{
				break;
			}
			toTime = ((toTime == taskModel.completedTime.Value) ? taskModel.completedTime.Value.AddSeconds(-1.0) : taskModel.completedTime.Value);
			if (toTime < start)
			{
				break;
			}
			List<TaskModel> closedTasks = await Communicator.GetClosedTasks(prjId, start, toTime, 1200);
			if (await TaskService.MergeTasks(closedTasks))
			{
				tasksPulled = true;
			}
			if (closedTasks == null || closedTasks.Count <= 0)
			{
				break;
			}
			tasks = closedTasks;
		}
		ClosedLoadStatus closedLoadStatus2 = await CompletionLoadStatusDao.GetLoadStatusByEntityId(prjId);
		if (savePullDate || onlyCheckStart)
		{
			closedLoadStatus2.LastPullTime = DateTime.Now;
			closedLoadStatus2.EarliestPulledDate = start.AddSeconds(-1.0);
			await CompletionLoadStatusDao.SaveCompletionLoadStatus(closedLoadStatus2);
		}
		return tasksPulled;
	}

	public bool NeedShowLoadMore(int completedCount)
	{
		_localDrainOff = completedCount < CompletedProjectStatus.Count;
		if (CompletedProjectStatus.DrainOff)
		{
			return completedCount >= CompletedProjectStatus.Count;
		}
		return true;
	}

	public void Reset()
	{
		CompletedProjectStatus.Count = 50;
		CompletedProjectStatus.DrainOff = false;
	}
}
