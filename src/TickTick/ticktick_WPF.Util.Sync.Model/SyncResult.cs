using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync.Model;

public class SyncResult
{
	public int SyncType { get; set; }

	public bool ColumnChanged { get; set; }

	public bool TeamChanged { get; set; }

	public bool MatrixProfilesChanged { get; set; }

	public NotificationUnreadCount NotificationCount { get; set; }

	public bool RemoteDataChanged
	{
		get
		{
			if (!RemoteProjectGroupsChanged && !RemoteProjectsChanged && !MatrixProfilesChanged && !RemoteFiltersChanged && !RemoteTasksChanged)
			{
				return RemoteTagsChanged;
			}
			return true;
		}
	}

	public List<ProjectModel> AddedProjects { get; set; } = new List<ProjectModel>();

	public List<ProjectModel> UpdatedProjects { get; set; } = new List<ProjectModel>();

	public List<ProjectModel> DeletedProjects { get; set; } = new List<ProjectModel>();

	public bool RemoteProjectsChanged
	{
		get
		{
			if (AddedProjects.Count <= 0 && UpdatedProjects.Count <= 0)
			{
				return DeletedProjects.Count > 0;
			}
			return true;
		}
	}

	public bool RemoteGroupChanged
	{
		get
		{
			if (!AddedProjectGroups.Any() && !UpdatedProjectGroups.Any())
			{
				return DeletedProjectGroups.Any();
			}
			return true;
		}
	}

	public bool RemoteTagChanged
	{
		get
		{
			if (!AddedTags.Any() && !UpdatedTags.Any())
			{
				return DeletedTags.Any();
			}
			return true;
		}
	}

	public List<ProjectGroupModel> AddedProjectGroups { get; set; } = new List<ProjectGroupModel>();

	public List<ProjectGroupModel> UpdatedProjectGroups { get; set; } = new List<ProjectGroupModel>();

	public List<ProjectGroupModel> DeletedProjectGroups { get; set; } = new List<ProjectGroupModel>();

	private bool RemoteProjectGroupsChanged
	{
		get
		{
			if (AddedProjectGroups.Count <= 0 && UpdatedProjectGroups.Count <= 0)
			{
				return DeletedProjectGroups.Count > 0;
			}
			return true;
		}
	}

	public List<FilterModel> AddedFilters { get; set; } = new List<FilterModel>();

	public List<FilterModel> UpdatedFilters { get; set; } = new List<FilterModel>();

	public List<string> DeletedFilterIds { get; set; } = new List<string>();

	public bool RemoteFiltersChanged
	{
		get
		{
			if (AddedFilters.Count <= 0 && UpdatedFilters.Count <= 0)
			{
				return DeletedFilterIds.Count > 0;
			}
			return true;
		}
	}

	public List<TaskModel> AddedTasks { get; set; } = new List<TaskModel>();

	public List<TaskModel> UpdatedTasks { get; set; } = new List<TaskModel>();

	public List<TaskProject> DeletedTasks { get; set; } = new List<TaskProject>();

	public List<TaskProject> DeletedInTrashTasks { get; set; } = new List<TaskProject>();

	public List<TaskProject> DeletedForeverTasks { get; set; } = new List<TaskProject>();

	public List<TagModel> AddedTags { get; set; } = new List<TagModel>();

	public List<TagModel> UpdatedTags { get; set; } = new List<TagModel>();

	public List<TagModel> DeletedTags { get; set; } = new List<TagModel>();

	public bool RemoteTasksChanged
	{
		get
		{
			if (AddedTasks.Count <= 0 && UpdatedTasks.Count <= 0 && DeletedTasks.Count <= 0 && DeletedInTrashTasks.Count <= 0 && DeletedForeverTasks.Count <= 0)
			{
				return TaskSortChanged;
			}
			return true;
		}
	}

	private bool RemoteTagsChanged
	{
		get
		{
			if (AddedTags.Count <= 0 && UpdatedTags.Count <= 0)
			{
				return DeletedTags.Count > 0;
			}
			return true;
		}
	}

	public bool TaskSortChanged { get; set; }

	public void Clear()
	{
		AddedProjects.Clear();
		UpdatedProjects.Clear();
		DeletedProjects.Clear();
		AddedProjectGroups.Clear();
		UpdatedProjectGroups.Clear();
		AddedFilters.Clear();
		UpdatedFilters.Clear();
		DeletedFilterIds.Clear();
		AddedTags.Clear();
		UpdatedTags.Clear();
		DeletedTags.Clear();
		TaskSortChanged = false;
		MatrixProfilesChanged = false;
	}

	public void InitNotification()
	{
		NotificationCount = new NotificationUnreadCount(0, 0);
	}

	public SyncResult Copy()
	{
		return new SyncResult
		{
			AddedProjects = AddedProjects.ToList(),
			UpdatedProjects = UpdatedProjects.ToList(),
			DeletedProjects = DeletedProjects.ToList(),
			AddedProjectGroups = AddedProjectGroups.ToList(),
			UpdatedProjectGroups = UpdatedProjectGroups.ToList(),
			AddedFilters = AddedFilters.ToList(),
			UpdatedFilters = UpdatedFilters.ToList(),
			DeletedFilterIds = DeletedFilterIds.ToList(),
			AddedTags = AddedTags.ToList(),
			UpdatedTags = UpdatedTags.ToList(),
			DeletedTags = DeletedTags.ToList(),
			UpdatedTasks = UpdatedTasks.ToList(),
			AddedTasks = AddedTasks.ToList(),
			DeletedTasks = DeletedTasks.ToList(),
			DeletedForeverTasks = DeletedForeverTasks.ToList(),
			DeletedInTrashTasks = DeletedInTrashTasks.ToList(),
			TaskSortChanged = TaskSortChanged,
			MatrixProfilesChanged = MatrixProfilesChanged,
			NotificationCount = NotificationCount,
			TeamChanged = TeamChanged
		};
	}
}
