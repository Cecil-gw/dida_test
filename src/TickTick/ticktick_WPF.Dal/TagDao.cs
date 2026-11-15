using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Drag;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Dal;

public class TagDao : BaseDao<TagModel>
{
	public static async Task<TagModel> TryCreateTag(string tag, string color = "", string parent = null, bool randomColor = true, bool notify = true)
	{
		TagModel model = await CreateTag(tag, color, parent, randomColor);
		if (model != null)
		{
			await SaveNewTag(model, notify);
			await CompletionLoadStatusDao.SetTagTasksLoaded(tag);
		}
		return model;
	}

	public static async Task SaveNewTag(TagModel tag, bool notify = true)
	{
		await App.Connection.InsertAsync(tag);
		CacheManager.UpdateTag(tag, !notify);
		if (notify)
		{
			DataChangedNotifier.NotifyTagChanged(tag);
		}
		SyncManager.TryDelaySync(1000);
	}

	public static async Task<TagModel> CreateTag(string tag, string color = "", string parent = "", bool randomColor = true)
	{
		string tagName = TagSerializer.NormalizeName(tag);
		if (string.IsNullOrEmpty(tagName))
		{
			return null;
		}
		List<TagModel> list = await GetAllTags();
		if (list.FirstOrDefault((TagModel t) => t.name == tagName) == null)
		{
			long sortOrder = (string.IsNullOrEmpty(parent) ? GetNextTagSortOrder(list) : ProjectDragHelper.CalculateInsertTagSortOrder(upDown: true, parent));
			if (string.IsNullOrEmpty(color) && randomColor)
			{
				color = ThemeUtil.GetRandomColor();
			}
			Pinyin pinyin = PinyinUtils.ToPinyin(tagName);
			return new TagModel
			{
				id = Utils.GetGuid(),
				name = tagName,
				label = TagSerializer.NormalizeLabel(tag),
				sortOrder = sortOrder,
				sortType = Constants.SortType.project.ToString(),
				SortOption = SortOptionUtils.GetSortOptionBySortType(Constants.SortType.project, inKanban: false),
				userId = Utils.GetCurrentUserIdInt().ToString(),
				status = 0,
				pinyin = pinyin.Text,
				inits = pinyin.Inits,
				deleted = 0,
				color = color,
				parent = parent,
				type = 1
			};
		}
		return null;
	}

	private static long GetNextTagSortOrder(IReadOnlyCollection<TagModel> tags)
	{
		if (tags != null && tags.Count > 0)
		{
			return tags.OrderBy((TagModel tag) => tag.sortOrder).First().sortOrder - 268435456;
		}
		return 0L;
	}

	public static async Task DeleteTag(string tagName)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<TagModel> list = (from tagModel in CacheManager.GetTags()
			where (tagModel.name == tagName || tagModel.parent == tagName) && tagModel.userId == userId
			select tagModel).ToList();
		foreach (TagModel tag in list)
		{
			if (tag.name == tagName)
			{
				await App.Connection.DeleteAsync(tag);
				CacheManager.DeleteTag(tagName);
				if (tag.IsChild())
				{
					await CheckParent(tag.parent);
				}
			}
			if (tag.name != tagName && tag.parent == tagName)
			{
				tag.parent = "";
				await UpdateTag(tag);
			}
			await CompletionLoadStatusDao.DeleteStatusByEntityId(tag.name);
		}
	}

	public static async Task CheckParent(string name)
	{
		TagModel tagModel = CacheManager.GetTags().FirstOrDefault((TagModel t) => t.name == name);
		if (tagModel != null && !tagModel.IsParent() && (tagModel.collapsed || tagModel.SortOption?.groupBy == Constants.SortType.tag.ToString() || tagModel.Timeline?.sortOption?.groupBy == Constants.SortType.tag.ToString()))
		{
			tagModel.collapsed = false;
			if (tagModel.SortOption?.groupBy == Constants.SortType.tag.ToString())
			{
				tagModel.SortOption.groupBy = Constants.SortType.project.ToString();
				tagModel.sortType = Constants.SortType.project.ToString();
				tagModel.status = ((tagModel.status != 0) ? 1 : 0);
			}
			if (tagModel.Timeline?.sortOption?.groupBy == Constants.SortType.tag.ToString())
			{
				tagModel.Timeline.sortOption.groupBy = Constants.SortType.project.ToString();
				tagModel.Timeline.SortType = Constants.SortType.project.ToString();
				tagModel.SyncTimeline.SortOption = tagModel.Timeline.sortOption;
				tagModel.SyncTimeline.SortType = tagModel.Timeline.SortType;
				tagModel.status = ((tagModel.status != 0) ? 1 : 0);
			}
			await UpdateTag(tagModel);
		}
	}

	public static async Task<Dictionary<string, TagModel>> GetLocalSyncTagDict()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<TagModel> list = await (from tag in App.Connection.Table<TagModel>()
			where tag.userId == userId && tag.status != 0
			select tag).ToListAsync();
		Dictionary<string, TagModel> dictionary = new Dictionary<string, TagModel>();
		if (list != null && list.Count > 0)
		{
			foreach (TagModel item in list)
			{
				if (!dictionary.ContainsKey(item.name))
				{
					dictionary.Add(item.name, item);
				}
			}
		}
		return dictionary;
	}

	public static async Task<List<TagModel>> GetAllTags()
	{
		if (App.Connection != null)
		{
			string userId = Utils.GetCurrentUserIdInt().ToString();
			return await (from tag in App.Connection.Table<TagModel>()
				where tag.userId == userId
				orderby tag.sortOrder
				select tag).ToListAsync();
		}
		return null;
	}

	public static async Task UpdateTag(TagModel tag, string originName = null, bool notify = true)
	{
		CacheManager.UpdateTag(tag, !notify);
		await TagSyncedJsonDao.TrySaveTag(tag, originName);
		await App.Connection.UpdateAsync(tag);
	}

	public static async Task<TagModel> UpdateTag(string originalName, string newLabel, string color, string parent, bool notify = true)
	{
		string tagName = newLabel.ToLower().Trim();
		TagModel tag = null;
		if (!string.IsNullOrEmpty(originalName))
		{
			List<TagModel> tags = await GetTagsByName(originalName);
			if (tags.Count >= 1)
			{
				tag = tags[0];
				for (int i = 1; i < tags.Count; i++)
				{
					await App.Connection.DeleteAsync(tags[i]);
				}
			}
		}
		if (tag == null)
		{
			tag = await TryCreateTag(newLabel, "", null, randomColor: true, notify);
		}
		else
		{
			Pinyin pinyin = PinyinUtils.ToPinyin(tagName);
			tag.name = tagName;
			tag.label = newLabel;
			tag.pinyin = pinyin.Text;
			tag.inits = pinyin.Inits;
			tag.userId = Utils.GetCurrentUserIdInt().ToString();
			if (tag.parent != parent && (!string.IsNullOrEmpty(tag.parent) || !string.IsNullOrEmpty(parent)))
			{
				if (string.IsNullOrEmpty(parent))
				{
					TagModel tagModel = CacheManager.GetTags().LastOrDefault((TagModel t) => t.parent == tag.parent);
					if (tagModel != null && tagModel.id != tag.id)
					{
						tag.sortOrder = ProjectDragHelper.CalculateInsertTagSortOrder(upDown: true, tagModel.name);
					}
				}
				else
				{
					tag.sortOrder = ProjectDragHelper.CalculateInsertTagSortOrder(upDown: true, parent);
				}
				tag.parent = parent;
			}
			if (tag.color != color)
			{
				tag.color = color;
			}
			if (tag.status != 0)
			{
				tag.status = 1;
			}
			CacheManager.DeleteTag(originalName);
			await UpdateTag(tag, originalName, notify);
		}
		return tag;
	}

	public static async Task<TagModel> GetTagByName(string tagId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from tag in App.Connection.Table<TagModel>()
			where tag.name == tagId && tag.userId == userId
			select tag).FirstOrDefaultAsync();
	}

	public static async Task<List<TagModel>> GetTagsByName(string name)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from tag in App.Connection.Table<TagModel>()
			where tag.name == name && tag.userId == userId
			select tag).ToListAsync();
	}

	public static async Task<TagModel> GetTagById(string tagId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from tag in App.Connection.Table<TagModel>()
			where tag.id == tagId && tag.userId == userId
			select tag).FirstOrDefaultAsync();
	}

	public static async Task TryBatchAddTags(List<string> tags, bool isShare = false)
	{
		if (tags == null || tags.Count <= 0)
		{
			return;
		}
		List<TagModel> obj = await GetAllTags();
		HashSet<string> hashSet = new HashSet<string>(obj.Select((TagModel tag) => tag.name));
		List<TagModel> notExistedTags = new List<TagModel>();
		string userId = Utils.GetCurrentUserIdInt().ToString();
		long num = GetNextTagSortOrder(obj);
		foreach (string item in TagSerializer.NormalizeNames(tags))
		{
			if (hashSet.Add(item))
			{
				Pinyin pinyin = PinyinUtils.ToPinyin(item);
				notExistedTags.Add(new TagModel
				{
					id = Utils.GetGuid(),
					name = item,
					sortType = Constants.SortType.project.ToString(),
					SortOption = SortOptionUtils.GetSortOptionBySortType(Constants.SortType.project, inKanban: false),
					pinyin = pinyin.Text,
					inits = pinyin.Inits,
					status = 0,
					userId = userId,
					deleted = 0,
					sortOrder = num,
					color = ThemeUtil.GetRandomColor(),
					type = ((!isShare) ? 1 : 2)
				});
				num += 268435456;
			}
		}
		if (notExistedTags.Count <= 0)
		{
			return;
		}
		await App.Connection.InsertAllAsync(notExistedTags);
		foreach (TagModel item2 in notExistedTags)
		{
			CacheManager.UpdateTag(item2);
		}
	}

	public static async Task SaveServerMergeData(List<TagModel> added, List<TagModel> updated, IEnumerable<TagModel> deleted)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<string> list = (await GetAllTags()).Select((TagModel tagModel) => tagModel.name).ToList();
		List<TagModel> addtags = new List<TagModel>();
		foreach (TagModel item in added)
		{
			if (!list.Contains(item.name))
			{
				if (string.IsNullOrEmpty(item.id))
				{
					item.id = Utils.GetGuid();
					item.userId = userId;
				}
				addtags.Add(item);
			}
		}
		if (added.Count > 0)
		{
			await App.Connection.InsertAllAsync(addtags);
			foreach (TagModel item2 in addtags)
			{
				CacheManager.UpdateTag(item2);
			}
		}
		if (updated != null && updated.Count > 0)
		{
			await App.Connection.UpdateAllAsync(updated);
			await TagSyncedJsonDao.BatchDeleteTags(updated.Select((TagModel tagModel) => tagModel.name).ToList());
			foreach (TagModel tag in updated)
			{
				TagModel cacheTag = CacheManager.GetTags().FirstOrDefault((TagModel t) => t.name == tag.name);
				if (cacheTag != null && string.IsNullOrEmpty(cacheTag.parent) && !string.IsNullOrEmpty(tag.parent))
				{
					(from t in CacheManager.GetTags()
						where t.parent == cacheTag.name
						select t).ToList().ForEach(async delegate(TagModel t)
					{
						t.parent = tag.parent;
						await UpdateTag(t);
					});
				}
				if (cacheTag == null || !tag.IsEquals(cacheTag))
				{
					tag.id = cacheTag?.id ?? tag.id ?? Utils.GetGuid();
					CacheManager.UpdateTag(tag);
				}
			}
		}
		foreach (TagModel tag2 in deleted)
		{
			await App.Connection.DeleteAsync(tag2);
			CacheManager.DeleteTag(tag2.name);
		}
	}

	public static async Task<List<TagModel>> GetNeedPostTags()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from x in App.Connection.Table<TagModel>()
			where x.status != 2 && x.userId == userId
			select x).ToListAsync();
	}

	public static async Task<bool> SaveCommitResultBackToDb(Dictionary<string, string> id2Etag, Dictionary<string, string> id2Error, LogModel logModel)
	{
		bool needRecommit = false;
		if (id2Etag != null)
		{
			logModel.Log += "Id2etag : ";
			foreach (string serverId in id2Etag.Keys)
			{
				TagModel tag = await GetTagByName(serverId);
				if (tag != null)
				{
					tag.status = 2;
					tag.etag = id2Etag[serverId];
					await App.Connection.UpdateAsync(tag);
					CacheManager.UpdateTag(tag, onlyUpdate: true);
				}
				logModel.Log = logModel.Log + "  " + serverId + "  ";
			}
			await TagSyncedJsonDao.BatchDeleteTags(id2Etag.Keys.ToList());
		}
		if (id2Error != null && id2Error.Count > 0)
		{
			string serverId = string.Empty;
			foreach (string id in id2Error.Keys)
			{
				List<TagModel> tags = await GetTagsByName(id);
				if (tags != null && tags.Count > 0 && id2Error[id] == "EXISTED")
				{
					if (tags[0].status == 0)
					{
						needRecommit = true;
						tags[0].status = 1;
						await App.Connection.UpdateAsync(tags[0]);
					}
					if (tags.Count > 1)
					{
						for (int i = 1; i < tags.Count; i++)
						{
							await App.Connection.DeleteAsync(tags[i]);
						}
					}
				}
				serverId = serverId + id + " : " + id2Error[id];
			}
			logModel.Log = logModel.Log + "  error:  " + serverId;
		}
		return needRecommit;
	}

	public static async Task MergeParentTag(string original, string target)
	{
		List<TagModel> tags = TagDataHelper.GetTags();
		List<TagModel> list = tags.Where((TagModel t) => t.name == original || t.parent == original).ToList();
		if (list.Count <= 1)
		{
			return;
		}
		long num = 1L >> 38 / list.Count;
		TagModel tagModel = tags.FirstOrDefault((TagModel t) => t.name == target);
		if (tagModel == null)
		{
			return;
		}
		bool flag = tags.Any((TagModel t) => t.parent == target);
		int num2 = tags.IndexOf(tagModel);
		if (num2 >= 0 && tags.Count >= num2 + 2)
		{
			num = (tags[num2 + 1].sortOrder - tagModel.sortOrder) / list.Count;
		}
		int num3 = 1;
		foreach (TagModel item in list.Where((TagModel tag) => tag.name != original))
		{
			item.parent = (flag ? tagModel.name : "");
			item.status = ((item.status != 0) ? 1 : 0);
			item.sortOrder = tagModel.sortOrder + num * num3++;
			UpdateTag(item);
		}
	}

	public static void UpdateParent(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return;
		}
		TagModel tagModel = CacheManager.GetTags().FirstOrDefault((TagModel t) => t?.name == name);
		try
		{
			if (tagModel == null || tagModel.IsChild() || tagModel.IsParent() || (!(tagModel.SortOption?.groupBy != Constants.SortType.tag.ToString()) && !(tagModel.Timeline?.sortOption?.groupBy != Constants.SortType.tag.ToString())))
			{
				return;
			}
			tagModel.sortType = Constants.SortType.tag.ToString();
			if (tagModel.SortOption != null)
			{
				tagModel.SortOption.groupBy = Constants.SortType.tag.ToString();
			}
			if (tagModel.Timeline?.sortOption != null)
			{
				tagModel.Timeline.sortOption.groupBy = Constants.SortType.tag.ToString();
				tagModel.Timeline.SortType = Constants.SortType.tag.ToString();
				if (tagModel.SyncTimeline != null)
				{
					tagModel.SyncTimeline.SortOption = tagModel.Timeline.sortOption;
					tagModel.SyncTimeline.SortType = tagModel.Timeline.SortType;
				}
				else
				{
					tagModel.SyncTimeline = new TimelineSyncModel
					{
						SortOption = tagModel.Timeline.sortOption,
						SortType = tagModel.Timeline.SortType
					};
				}
			}
			tagModel.status = ((tagModel.status != 0) ? 1 : 0);
			UpdateTag(tagModel);
		}
		catch (Exception)
		{
			UtilLog.Info("TagDao.UpdateParent timeline == null ? " + (tagModel?.Timeline == null) + ", parent.SyncTimeline == null ? " + (tagModel?.SyncTimeline == null));
		}
	}

	public static async Task<bool> HandleDropTaskTags(TaskModel fromTask, TaskModel dropTask)
	{
		List<string> list = TagSerializer.ToTags(fromTask.tag);
		if (list != null && list.Count > 0)
		{
			List<string> first = TagSerializer.ToTags(dropTask.tag) ?? new List<string>();
			first = first.Union(list).ToList();
			await TaskService.SetTags(dropTask.id, first);
			return true;
		}
		return false;
	}

	public static List<string> GetValidAndChildren(List<string> tagNames)
	{
		if (tagNames == null)
		{
			return new List<string>();
		}
		List<TagModel> tags = CacheManager.GetTags();
		ConcurrentDictionary<string, TagModel> tagDict = CacheManager.GetTagDict();
		tagNames = tagNames.Where((string t) => t == "*withtags" || t == "!tag" || (!string.IsNullOrEmpty(t) && tagDict.ContainsKey(t))).ToList();
		List<string> second = (from t in tags
			where t.IsChild() && tagNames.Contains(t.parent)
			select t.name).ToList();
		return tagNames.Union(second).ToList();
	}

	public static bool IsParentTag(string tagName)
	{
		return (from t in CacheManager.GetTags()
			where t.name == tagName || t.parent == tagName
			select t).ToList().Count > 1;
	}

	public static async Task SwitchViewModel(TagModel tag, string viewMode, bool isParallel = false)
	{
		if (tag == null)
		{
			return;
		}
		if (isParallel)
		{
			tag.ParallelViewMode = viewMode;
			UpdateTag(tag);
			return;
		}
		tag.viewMode = viewMode;
		if (!(viewMode == "kanban"))
		{
			if (viewMode == "timeline")
			{
				TimelineModel.CheckTimelineEmpty(tag, Constants.SortType.project);
			}
		}
		else
		{
			SortOption sortOption = tag.SortOption;
			if (sortOption != null && sortOption.groupByType == Constants.SortType.none)
			{
				tag.SortOption.groupBy = "project";
			}
		}
		tag.status = ((tag.status != 0) ? 1 : 0);
		UpdateTag(tag);
	}
}
