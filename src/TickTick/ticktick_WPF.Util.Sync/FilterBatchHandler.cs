using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public class FilterBatchHandler : BatchHandler
{
	public FilterBatchHandler(string userId, SyncResult syncResult)
		: base(userId, syncResult)
	{
	}

	public async Task MergeWithServer(Collection<FilterModel> serverFilters, LogModel logModel)
	{
		logModel.Log += $"  |  MergeFilters num :{serverFilters?.Count}";
		if (serverFilters == null)
		{
			return;
		}
		List<FilterModel> added = new List<FilterModel>();
		List<FilterModel> updated = new List<FilterModel>();
		Dictionary<string, FilterModel> localFilters = await FilterDao.GetLocalSyncedFilterMap();
		foreach (FilterModel delta in serverFilters)
		{
			if (localFilters != null && localFilters.ContainsKey(delta.id))
			{
				FilterModel revised = localFilters[delta.id];
				localFilters.Remove(revised.id);
				if (revised.etag == delta.etag || revised.syncStatus == 0 || revised.deleted == 1)
				{
					continue;
				}
				bool merged = false;
				if (revised.syncStatus == 1 || revised.syncStatus == 3)
				{
					FilterSyncedJsonModel filterSyncedJsonModel = await FilterSyncJsonDao.GetSavedJson(revised.id);
					if (filterSyncedJsonModel != null)
					{
						merged = true;
						FilterModel? original = JsonConvert.DeserializeObject<FilterModel>(filterSyncedJsonModel.JsonString);
						revised.syncStatus = 1;
						MergeUtils.Merge(original, delta, revised);
					}
				}
				delta._Id = revised._Id;
				delta.deleted = 0;
				if (!merged)
				{
					delta.syncStatus = 2;
					delta.Timeline = revised.Timeline ?? new TimelineModel(Constants.SortType.project.ToString());
					delta.Timeline.SortType = delta.SyncTimeline?.SortType ?? delta.Timeline.SortType;
					delta.Timeline.sortOption = delta.SyncTimeline?.SortOption ?? delta.Timeline.sortOption;
					updated.Add(delta);
				}
				else
				{
					updated.Add(revised);
				}
			}
			else
			{
				delta.deleted = 0;
				delta.syncStatus = 2;
				delta.Timeline = ((delta.SyncTimeline == null) ? null : new TimelineModel(delta.SyncTimeline.SortType, delta.SyncTimeline.SortOption));
				added.Add(delta);
			}
		}
		List<string> deleted = new List<string>();
		if (localFilters != null)
		{
			deleted.AddRange(from item in localFilters
				where item.Value.syncStatus != 0 && item.Value.deleted != 1
				select item.Key);
		}
		logModel.Log += $" a:{added.Count} u:{updated.Count} d:{deleted.Count}";
		if (added.Count > 0 || updated.Count > 0 || deleted.Count > 0)
		{
			await FilterDao.SaveServerMergeData(added, updated, deleted);
			if (syncResult == null)
			{
				syncResult = new SyncResult();
			}
			syncResult.AddedFilters = added;
			syncResult.UpdatedFilters = updated;
			syncResult.DeletedFilterIds = deleted;
		}
	}

	public static async Task<SyncFilterBean> CommitToServer()
	{
		List<FilterModel> list = await FilterDao.GetNeedPostFilter();
		return (list == null) ? null : FilterTransfer.DescribeSyncFilterBean(list);
	}

	public static async Task HandleCommitResult(BatchUpdateResult result, Collection<string> deleted, LogModel logModel)
	{
		await FilterDao.SaveCommitResultBackToDb(result.Id2etag, deleted);
		if (result.Id2etag != null && result.Id2etag.Keys.Any())
		{
			logModel.Log += "Id2etag : ";
			foreach (string key in result.Id2etag.Keys)
			{
				logModel.Log = logModel.Log + "  " + key + "  ";
			}
			await FilterSyncJsonDao.BatchDeleteFilters(result.Id2etag.Keys.ToList());
		}
		if (deleted.Any())
		{
			await FilterSyncJsonDao.BatchDeleteFilters(deleted.ToList());
		}
		if (result.Id2error == null || !result.Id2error.Keys.Any())
		{
			return;
		}
		string text = string.Empty;
		foreach (string key2 in result.Id2error.Keys)
		{
			text = text + key2 + " : " + result.Id2error[key2];
		}
		logModel.Log = logModel.Log + "  error:  " + text;
	}
}
