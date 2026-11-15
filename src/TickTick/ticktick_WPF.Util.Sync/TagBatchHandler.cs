using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.Util.Twitter;

namespace ticktick_WPF.Util.Sync;

public class TagBatchHandler : BatchHandler
{
	public TagBatchHandler(string userId, SyncResult syncResult)
		: base(userId, syncResult)
	{
	}

	public async Task MergeWithServer(List<TagModel> serverTags, LogModel logModel)
	{
		logModel.Log += $"  |  MergeTags num :{serverTags?.Count}";
		if (serverTags == null)
		{
			return;
		}
		string localUserId = Utils.GetCurrentUserIdInt().ToString();
		List<TagModel> added = new List<TagModel>();
		List<TagModel> updated = new List<TagModel>();
		Dictionary<string, TagModel> localTags = await TagDao.GetLocalSyncTagDict();
		foreach (TagModel delta in serverTags)
		{
			if (localTags.ContainsKey(delta.name))
			{
				TagModel revised = localTags[delta.name];
				localTags.Remove(revised.name);
				if (revised.etag == delta.etag && Utils.IsEqualString(revised.color, delta.color))
				{
					if (revised.type == 0 && delta.type == 2)
					{
						revised.type = delta.type;
						updated.Add(revised);
					}
				}
				else
				{
					if (revised.deleted == 1)
					{
						continue;
					}
					bool merged = false;
					if (revised.status == 1)
					{
						TagSyncedJsonModel tagSyncedJsonModel = await TagSyncedJsonDao.GetSavedJson(revised.name);
						if (tagSyncedJsonModel != null)
						{
							merged = true;
							MergeUtils.Merge(JsonConvert.DeserializeObject<TagModel>(tagSyncedJsonModel.JsonString), delta, revised);
						}
					}
					Pinyin pinyin = PinyinUtils.ToPinyin(delta.name);
					delta._Id = revised._Id;
					delta.id = revised.id;
					delta.userId = localUserId;
					delta.inits = pinyin.Inits;
					delta.pinyin = pinyin.Text;
					if (!merged)
					{
						delta.status = 2;
						delta.Timeline = revised.Timeline ?? new TimelineModel(Constants.SortType.project.ToString());
						delta.Timeline.SortType = delta.SyncTimeline?.SortType ?? delta.Timeline.SortType;
						delta.Timeline.sortOption = delta.SyncTimeline?.SortOption ?? delta.Timeline.sortOption;
					}
					else
					{
						delta.sortType = revised.sortType;
						delta.sortOrder = revised.sortOrder;
						delta.color = revised.color;
						delta.status = revised.status;
						delta.parent = revised.parent;
						delta.Timeline = ((delta.SyncTimeline == null) ? null : new TimelineModel(delta.SyncTimeline.SortType, delta.SyncTimeline.SortOption));
					}
					delta.deleted = 0;
					delta.collapsed = revised.collapsed;
					updated.Add(delta);
				}
			}
			else
			{
				Pinyin pinyin2 = PinyinUtils.ToPinyin(delta.name);
				delta.userId = localUserId;
				delta.inits = pinyin2.Inits;
				delta.pinyin = pinyin2.Text;
				delta.status = 2;
				delta.deleted = 0;
				added.Add(delta);
			}
		}
		List<TagModel> deleted = localTags.Values.ToList();
		logModel.Log = logModel.Log + "  a:" + added.Select((TagModel a) => a.name).Join(",") + " u:" + updated.Select((TagModel a) => a.name).Join(",") + " d:" + deleted.Select((TagModel a) => a.name).Join(",");
		if (added.Count > 0 || updated.Count > 0 || deleted.Count > 0)
		{
			await TagDao.SaveServerMergeData(added, updated, deleted);
			if (syncResult == null)
			{
				syncResult = new SyncResult();
			}
			syncResult.AddedTags = added;
			syncResult.UpdatedTags = updated;
			syncResult.DeletedTags = deleted;
		}
	}

	public static async Task<SyncTagBean> CommitToServer()
	{
		List<TagModel> list = await TagDao.GetNeedPostTags();
		if (list != null && list.Count > 0)
		{
			SyncTagBean syncTagBean = new SyncTagBean();
			foreach (TagModel tag in list)
			{
				if (string.IsNullOrEmpty(tag.name))
				{
					App.Connection.DeleteAsync(tag);
					continue;
				}
				if (tag.parent == null)
				{
					tag.parent = "";
				}
				switch (tag.status)
				{
				case 0:
					if (syncTagBean.Add.Any((TagModel t) => t.name == tag.name))
					{
						App.Connection.DeleteAsync(tag);
					}
					else
					{
						syncTagBean.Add.Add(tag);
					}
					continue;
				case 1:
					if (tag.deleted == 1)
					{
						continue;
					}
					break;
				case 3:
					if (tag.deleted == 1)
					{
						continue;
					}
					break;
				default:
					continue;
				}
				if (syncTagBean.Update.Any((TagModel t) => t.name == tag.name))
				{
					App.Connection.DeleteAsync(tag);
				}
				else
				{
					syncTagBean.Update.Add(tag);
				}
			}
			return syncTagBean;
		}
		return null;
	}

	public static async Task<bool> HandleCommitResult(Dictionary<string, string> id2Etag, Dictionary<string, string> id2Error, LogModel logModel)
	{
		return await TagDao.SaveCommitResultBackToDb(id2Etag, id2Error, logModel);
	}
}
