using System;
using System.Linq;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

[Serializable]
public class TagModel : BaseModel, ITimeline
{
	private string _color;

	public string id { get; set; }

	[Indexed]
	public string name { get; set; }

	public string label { get; set; }

	public long sortOrder { get; set; }

	public string sortType { get; set; }

	[JsonIgnore]
	public int status { get; set; }

	public string userId { get; set; }

	public string viewMode { get; set; }

	public int type { get; set; }

	[JsonProperty("sortOption")]
	[Ignore]
	public SortOption SortOption { get; set; }

	[JsonIgnore]
	public string SortOptionString
	{
		get
		{
			if (SortOption != null)
			{
				return JsonConvert.SerializeObject(SortOption);
			}
			return string.Empty;
		}
		set
		{
			SortOption = (string.IsNullOrEmpty(value) ? null : JsonConvert.DeserializeObject<SortOption>(value));
		}
	}

	[Ignore]
	[JsonProperty("timeline")]
	public TimelineSyncModel SyncTimeline { get; set; }

	[JsonIgnore]
	public string SyncTimelineModel
	{
		get
		{
			if (SyncTimeline != null)
			{
				return JsonConvert.SerializeObject(SyncTimeline);
			}
			return string.Empty;
		}
		set
		{
			SyncTimeline = (string.IsNullOrEmpty(value) ? new TimelineSyncModel(Constants.SortType.project.ToString()) : JsonConvert.DeserializeObject<TimelineSyncModel>(value));
		}
	}

	[Ignore]
	[JsonIgnore]
	public TimelineModel Timeline { get; set; }

	[JsonIgnore]
	public string TimelineModel
	{
		get
		{
			if (Timeline != null)
			{
				return JsonConvert.SerializeObject(Timeline);
			}
			return string.Empty;
		}
		set
		{
			Timeline = (string.IsNullOrEmpty(value) ? new TimelineModel(Constants.SortType.project.ToString()) : JsonConvert.DeserializeObject<TimelineModel>(value));
		}
	}

	[JsonIgnore]
	public string ParallelViewMode { get; set; }

	[JsonIgnore]
	public string pinyin { get; set; }

	[JsonIgnore]
	public string inits { get; set; }

	public string etag { get; set; }

	public int deleted { get; set; }

	public string color
	{
		get
		{
			return _color;
		}
		set
		{
			_color = value;
		}
	}

	public string parent { get; set; }

	[JsonIgnore]
	public bool collapsed { get; set; }

	public int TypeOrder
	{
		get
		{
			if (type != 2)
			{
				return 1;
			}
			return 2;
		}
	}

	[JsonIgnore]
	[Ignore]
	public long parentSortOrder { get; set; }

	public bool IsParent()
	{
		return CacheManager.GetTags()?.Any((TagModel t) => t.parent == name) ?? false;
	}

	public bool IsChild()
	{
		if (!string.IsNullOrEmpty(parent))
		{
			return parent != name;
		}
		return false;
	}

	public bool IsEquals(TagModel tag)
	{
		if (tag.name == name && tag.label == label && tag.sortOrder == sortOrder && tag.color == color && parent == tag.parent && tag.deleted == deleted && tag.viewMode == viewMode && object.Equals(tag.SortOption, SortOption))
		{
			return object.Equals(tag.SyncTimeline, SyncTimeline);
		}
		return false;
	}

	public string GetDisplayName()
	{
		if (string.IsNullOrEmpty(label))
		{
			return name;
		}
		return label;
	}

	public string GetDisplayEmoji()
	{
		return EmojiHelper.GetFirstEmoji(GetDisplayName()) ?? string.Empty;
	}

	public string GetDisplayNameWithoutEmoji()
	{
		string displayName = GetDisplayName();
		string displayEmoji = GetDisplayEmoji();
		if (string.IsNullOrEmpty(displayEmoji) || !displayName.StartsWith(displayEmoji))
		{
			return displayName;
		}
		return displayName.Substring(displayEmoji.Length).Trim();
	}

	public SortOption GetSortOption()
	{
		if (SortOption == null)
		{
			if (string.IsNullOrEmpty(sortType))
			{
				sortType = "project";
			}
			return SortOptionUtils.GetSortOptionBySortType(Utils.LoadSortType(sortType), inKanban: false);
		}
		return SortOption;
	}

	public void SetUpdateStatus()
	{
		status = ((status != 0) ? 1 : 0);
	}

	public int GetTagType()
	{
		if (type != 0)
		{
			return type;
		}
		return 1;
	}

	public void SetParentSortOrder()
	{
		if (!string.IsNullOrEmpty(parent))
		{
			TagModel tagByName = CacheManager.GetTagByName(parent);
			if (tagByName != null)
			{
				parentSortOrder = tagByName.sortOrder + 1;
			}
			else
			{
				parentSortOrder = sortOrder;
			}
		}
		else
		{
			parentSortOrder = sortOrder;
		}
	}
}
