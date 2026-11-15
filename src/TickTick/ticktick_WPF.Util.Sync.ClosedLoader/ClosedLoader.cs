using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Provider;

namespace ticktick_WPF.Util.Sync.ClosedLoader;

public class ClosedLoader
{
	public ProjectIdentity _identity;

	public LoadStatus CompletedTaskLoadStatus = new LoadStatus(drainOff: false, null, DateTime.Now, 5);

	private string _entityId;

	private bool _localDrainOff;

	private bool _loadRemote = true;

	public int GetCompletedLimit()
	{
		return CompletedTaskLoadStatus.Count;
	}

	public bool CheckDrainOff(int num)
	{
		_localDrainOff = CompletedTaskLoadStatus.Count > num;
		if (!_loadRemote || CompletedTaskLoadStatus.DrainOff)
		{
			return _localDrainOff;
		}
		return false;
	}

	private void ResetLoadStatus()
	{
		CompletedTaskLoadStatus.Count = 5;
		CompletedTaskLoadStatus.DrainOff = false;
	}

	public void AddLimit(ProjectIdentity projectIdentity, int limit = 50)
	{
		if (projectIdentity != null && _identity != projectIdentity)
		{
			_identity = projectIdentity;
		}
		CompletedTaskLoadStatus.Count += limit;
	}

	public void SetIdentity(ProjectIdentity projectIdentity)
	{
		if (projectIdentity != null && _identity != projectIdentity)
		{
			if (_identity?.Id != projectIdentity?.Id)
			{
				ResetLoadStatus();
			}
			_identity = projectIdentity;
			_loadRemote = !(_identity is FilterProjectIdentity) && !(_identity is MatrixQuadrantIdentity);
		}
	}

	public async Task<bool> NeedPullCompletedTasks(ProjectIdentity identity = null)
	{
		return await ClosedListSyncService.NeedPullCompletedTasks(identity ?? _identity);
	}

	public async Task<bool> TryLoadCompletedTasks(ProjectIdentity identity, bool loadMore = true, DateTime? localEarliestDate = null)
	{
		if (identity is ColumnProjectIdentity columnProjectIdentity)
		{
			identity = columnProjectIdentity.Project;
		}
		if (!(identity is BindAccountCalendarProjectIdentity) && !(identity is SubscribeCalendarProjectIdentity) && !(identity is SearchProjectIdentity) && !(identity is FilterProjectIdentity) && !(identity is AssignToMeProjectIdentity) && !(identity is CompletedProjectIdentity) && !(identity is AbandonedProjectIdentity) && !(identity is TrashProjectIdentity) && !(identity is MatrixQuadrantIdentity))
		{
			if (identity is TagProjectIdentity)
			{
				return await TryLoadCompletedTasksInTag(identity.Id);
			}
			_entityId = string.Empty;
			string projectIds = string.Empty;
			bool isAll = false;
			DateTime? fromDate = identity.GetCompletedFromTime();
			if (!(identity is GroupProjectIdentity group))
			{
				if (identity is NormalProjectIdentity normalProjectIdentity)
				{
					projectIds = normalProjectIdentity.Id;
					_entityId = normalProjectIdentity.Id;
				}
			}
			else
			{
				projectIds = await GetGroupIds(group);
				if (string.IsNullOrEmpty(projectIds))
				{
					return false;
				}
				_entityId = group.GroupId;
			}
			if (string.IsNullOrEmpty(projectIds))
			{
				_entityId = "all";
				isAll = true;
			}
			ClosedLoadStatus status = await CompletionLoadStatusDao.GetLoadStatusByEntityId(_entityId);
			if (!status.IsAllLoaded && (_localDrainOff || !localEarliestDate.HasValue || localEarliestDate.Value <= status.EarliestPulledDate))
			{
				List<TaskModel> tasks = ((!isAll) ? (await Communicator.GetClosedTasks(projectIds, fromDate, status.EarliestPulledDate, (identity.ViewMode == "kanban") ? 500 : 50)) : (await Communicator.GetClosedTasks("all", fromDate, status.EarliestPulledDate, (identity.ViewMode == "kanban") ? 500 : 50)));
				if (tasks == null)
				{
					return false;
				}
				status.LastPullTime = DateTime.Now;
				if (tasks.Count < 50)
				{
					status.IsAllLoaded = true;
					await CompletionLoadStatusDao.SaveCompletionLoadStatus(status);
					CompletedTaskLoadStatus.DrainOff = true;
				}
				else
				{
					DateTime dateTime = tasks.LastOrDefault((TaskModel t) => t.completedTime.HasValue)?.completedTime ?? DateTime.Now;
					if (!status.IsAllLoaded && status.EarliestPulledDate > dateTime)
					{
						status.EarliestPulledDate = dateTime.AddSeconds(-1.0);
						await CompletionLoadStatusDao.SaveCompletionLoadStatus(status);
					}
				}
				if (await TaskService.MergeTasks(tasks))
				{
					return true;
				}
			}
			else
			{
				CompletedTaskLoadStatus.DrainOff = status.IsAllLoaded;
			}
			return false;
		}
		return false;
	}

	private async Task<bool> TryLoadCompletedTasksInTag(string tag)
	{
		_entityId = tag;
		ClosedLoadStatus status = await CompletionLoadStatusDao.GetLoadStatusByEntityId(_entityId);
		if (!status.IsAllLoaded)
		{
			TagCompletedTasks tagCompletedTasks = await Communicator.GetTagCompletedTasks(new List<string> { tag }, status.TagToken, 200);
			List<TaskModel> tasks = tagCompletedTasks?.tasks;
			UtilLog.Info("LoadTagCompletedTasks : " + tasks?.Count + ", " + Utils.GetSafeDateLogText(tasks?.LastOrDefault()?.completedTime) + ", token :" + tagCompletedTasks?.nextToken);
			if (tasks == null)
			{
				return false;
			}
			status.LastPullTime = DateTime.Now;
			if (tasks.Count < 200 || tagCompletedTasks.nextToken == null || tagCompletedTasks.nextToken == status.TagToken)
			{
				status.IsAllLoaded = true;
				if (!string.IsNullOrEmpty(tagCompletedTasks.nextToken))
				{
					status.TagToken = tagCompletedTasks.nextToken;
				}
				CompletedTaskLoadStatus.DrainOff = true;
			}
			else
			{
				status.TagToken = tagCompletedTasks.nextToken;
			}
			await CompletionLoadStatusDao.SaveCompletionLoadStatus(status);
			if (await TaskService.MergeTasks(tasks))
			{
				UtilLog.Info("LoadTagCompletedTasks Success");
				return true;
			}
		}
		else
		{
			CompletedTaskLoadStatus.DrainOff = status.IsAllLoaded;
			UtilLog.Info("LoadTagCompletedTasks AllLoaded");
		}
		return false;
	}

	private static async Task<string> GetGroupIds(GroupProjectIdentity group)
	{
		string projectIds = string.Empty;
		ObservableCollection<ProjectModel> observableCollection = await ProjectDao.GetProjectsInGroup(group.GroupId);
		if (observableCollection.Count > 0)
		{
			for (int i = 0; i < observableCollection.Count; i++)
			{
				projectIds = ((i >= observableCollection.Count - 1) ? (projectIds + observableCollection[i].id) : (projectIds + observableCollection[i].id + ","));
			}
		}
		return projectIds;
	}
}
