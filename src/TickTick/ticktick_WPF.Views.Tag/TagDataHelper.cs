using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.Tag;

public static class TagDataHelper
{
	public static List<TagModel> GetTags(bool sort = true)
	{
		List<TagModel> tags = CacheManager.GetTags();
		if (!sort)
		{
			return tags;
		}
		foreach (TagModel item in tags)
		{
			item.SetParentSortOrder();
		}
		return (from t in CacheManager.GetTags()
			orderby t.TypeOrder
			select t).ThenBy((TagModel tag) => tag.parentSortOrder).ThenBy((TagModel tag) => tag.sortOrder).ToList();
	}

	public static Dictionary<string, long> GetTagSortDict()
	{
		List<TagModel> tags = GetTags();
		Dictionary<string, long> dictionary = new Dictionary<string, long>();
		int num = 0;
		foreach (TagModel item in tags)
		{
			if (!dictionary.ContainsKey(item.name))
			{
				dictionary.Add(item.name, num);
			}
			num++;
		}
		return dictionary;
	}

	public static string GetPrimaryTag(Dictionary<string, long> tagSortDict, IList<string> tags, ICollection<string> limits)
	{
		if (tags != null && tags.Count > 0)
		{
			if (tags.Count == 1)
			{
				return tags[0];
			}
			tagSortDict = tagSortDict ?? GetTagSortDict();
			List<string> sortedTags = GetSortedTags(tagSortDict, tags);
			if (sortedTags != null && sortedTags.Count > 0)
			{
				if (limits == null)
				{
					return sortedTags[0];
				}
				return sortedTags.FirstOrDefault(limits.Contains) ?? sortedTags[0];
			}
			return tags[0];
		}
		return string.Empty;
	}

	public static List<string> GetSortedTags(Dictionary<string, long> tagSortDict, ICollection<string> tags)
	{
		return (from kv in tagSortDict
			where tags.Contains(kv.Key)
			orderby kv.Value
			select kv.Key).ToList();
	}

	public static bool CheckIfTagExisted(string id, string tagName)
	{
		List<TagModel> tags = GetTags();
		if (string.IsNullOrEmpty(id))
		{
			return tags.Exists((TagModel tag) => string.Equals(tag.name, tagName, StringComparison.CurrentCultureIgnoreCase));
		}
		return tags.Exists((TagModel tag) => tag.id != id && string.Equals(tag.name, tagName, StringComparison.CurrentCultureIgnoreCase));
	}

	public static TagSelectData GetSelectTagData(List<List<string>> data)
	{
		if (data != null && data.Count > 0)
		{
			List<string> list = data[0];
			List<string> first = new List<string>();
			foreach (List<string> datum in data)
			{
				list = list.Intersect(datum).ToList();
				first = first.Union(datum).ToList();
			}
			first = first.Except(list).ToList();
			return new TagSelectData
			{
				OmniSelectTags = list,
				PartSelectTags = first
			};
		}
		return new TagSelectData();
	}

	public static string GetFirstValidTag()
	{
		List<TagModel> tags = GetTags();
		if (tags.Any())
		{
			if (LocalSettings.Settings.SmartListTag != 2)
			{
				return tags[0].name;
			}
			ConcurrentDictionary<string, int> countData = TaskCountCache.CountData;
			foreach (TagModel item in tags)
			{
				if (countData.ContainsKey(item.name) && countData[item.name] > 0)
				{
					return item.name;
				}
			}
		}
		return string.Empty;
	}

	public static string GetTagDisplayName(string tag)
	{
		return (string.IsNullOrEmpty(tag) ? null : CacheManager.GetTags()?.FirstOrDefault((TagModel t) => t.name == tag.ToLower()))?.GetDisplayName() ?? tag;
	}
}
