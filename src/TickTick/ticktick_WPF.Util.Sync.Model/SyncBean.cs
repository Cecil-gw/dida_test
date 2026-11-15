using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using TickTickModels;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync.Model;

public class SyncBean
{
	public long checkPoint { get; set; }

	public SyncTaskBean syncTaskBean { get; set; }

	public ObservableCollection<ProjectModel> projectProfiles { get; set; }

	public ObservableCollection<ProjectGroupModel> projectGroups { get; set; }

	public ObservableCollection<FilterModel> filters { get; set; }

	public List<MatrixProfileSyncModel> matrixProfiles { get; set; }

	public List<TagModel> tags { get; set; }

	public string inboxId { get; set; }

	public SyncTaskOrderBean syncTaskOrderBean { get; set; }

	public SyncOrderBean syncOrderBean { get; set; }

	public SyncOrderBean syncOrderBeanV3 { get; set; }

	public List<ReminderDelayModel> remindChanges { get; set; }

	[JsonIgnore]
	public string checks { get; set; }

	public bool SyncError { get; set; }

	public bool IsEmpty()
	{
		if (filters == null && projectGroups == null && projectProfiles == null && matrixProfiles == null && syncOrderBean == null && tags == null && (syncTaskBean == null || syncTaskBean.Empty) && (remindChanges == null || remindChanges.Count == 0))
		{
			return string.IsNullOrEmpty(checks);
		}
		return false;
	}
}
