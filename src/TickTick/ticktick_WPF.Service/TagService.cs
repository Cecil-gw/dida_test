using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Service;

public static class TagService
{
	public static async Task<TagModel> TryCreateTag(string tag, string color = "", string parent = "", bool randomColor = true)
	{
		return await TagDao.TryCreateTag(tag, color, parent, randomColor);
	}

	public static async Task SaveTag(string id, string original, string newLabel, string color, string parent, bool notify = true)
	{
		LocalSettings.Settings.OnTagNameChanged(original, newLabel);
		TagModel tag = await TagDao.UpdateTag(original, newLabel, color, parent, notify);
		if (original != newLabel.ToLower())
		{
			await TaskDao.BatchUpdateAffectedTaskOnTagChanged(original, newLabel);
			await TaskSortOrderInPriorityDao.BatchUpdateAffectedTaskOnTagChanged(original, newLabel);
			await FilterDao.BatchUpdateAffectedFilterOnTagChanged(original, newLabel);
			await MatrixManager.UpdateAffectedQuadrantsOnTagChanged(original, newLabel);
			await TaskDefaultDao.UpdateDefaultTagsOnTagChanged(original, newLabel);
		}
		if (notify && tag != null)
		{
			DataChangedNotifier.NotifyTagChanged(tag, original);
		}
	}

	public static async Task MergeTag(string original, string merged)
	{
		await TagDao.MergeParentTag(original, merged.ToLower());
		await TaskDao.BatchUpdateAffectedTaskOnTagChanged(original, merged);
		await TaskSortOrderInPriorityDao.BatchUpdateAffectedTaskOnTagChanged(original, merged);
		await TagDao.DeleteTag(original);
		await TaskDefaultDao.UpdateDefaultTagsOnTagChanged(original, null);
		await Communicator.MergeTag(original, merged.ToLower());
	}

	public static async Task DeleteTag(string tag)
	{
		await TagDao.DeleteTag(tag);
		await TaskDefaultDao.UpdateDefaultTagsOnTagChanged(tag, null);
		await TaskDao.BatchUpdateTaskOnTagDeleted(tag);
		await FilterDao.BatchUpdateFilterOnTagDeleted(tag);
		DataChangedNotifier.NotifyTagDeleted();
	}

	public static async Task CheckTaskTags(List<TaskModel> tasks, bool checkShare = false)
	{
		if (tasks == null || tasks.Count <= 0)
		{
			return;
		}
		HashSet<string> shareProjectIds = new HashSet<string>(from p in CacheManager.GetProjects()
			where p.IsShareList()
			select p.id);
		foreach (TaskModel task in tasks)
		{
			if (task.tags != null && task.tags.Length != 0)
			{
				bool isShare = false;
				if (checkShare)
				{
					isShare = shareProjectIds.Contains(task.projectId);
				}
				await CheckTagsExist(task.tags.ToList(), isShare);
			}
		}
	}

	public static async Task CheckTagsExist(List<string> tags, bool isShare = false)
	{
		if (tags == null || tags.Count == 0)
		{
			return;
		}
		List<string> missingTagLabels = GetMissingTagLabels(tags, (string name) => CacheManager.GetTagByName(name) != null);
		if (missingTagLabels.Count <= 0)
		{
			return;
		}
		foreach (string item in missingTagLabels)
		{
			TagModel tagModel = await TagDao.CreateTag(item);
			if (tagModel != null)
			{
				tagModel.type = ((!isShare) ? 1 : 2);
				await TagDao.SaveNewTag(tagModel);
			}
			await Task.Delay(10);
		}
	}

	public static bool ExistShareTag()
	{
		return CacheManager.GetTags().Any((TagModel t) => t.type == 2);
	}

	internal static List<string> GetMissingTagLabels(IEnumerable<string> labels, Func<string, bool> exists)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		List<string> list = new List<string>();
		foreach (string label in labels)
		{
			string text = TagSerializer.NormalizeLabel(label);
			string text2 = TagSerializer.NormalizeName(text);
			if (!string.IsNullOrEmpty(text2) && hashSet.Add(text2) && !exists(text2))
			{
				list.Add(text);
			}
		}
		return list;
	}

	public static async void SwitchTagType(string tagName)
	{
		TagModel tag = CacheManager.GetTagByName(tagName);
		if (tag == null)
		{
			return;
		}
		int type = ((tag.type == 2) ? 1 : 2);
		if (!string.IsNullOrEmpty(tag.parent))
		{
			TagModel tagByName = CacheManager.GetTagByName(tag.parent);
			if (tagByName != null)
			{
				tag = tagByName;
			}
		}
		tag.type = type;
		tag.SetUpdateStatus();
		await TagDao.UpdateTag(tag, null, notify: false);
		List<TagModel> list = (from t in CacheManager.GetTags()
			where t.parent == tag.name
			select t).ToList();
		foreach (TagModel item in list)
		{
			item.type = type;
			item.SetUpdateStatus();
			await TagDao.UpdateTag(item, null, notify: false);
		}
		DataChangedNotifier.NotifyTagTypeChanged();
		SyncManager.Sync();
	}
}
