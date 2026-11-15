using System;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

[Serializable]
public class ProjectGroupModel : BaseModel, ITimeline
{
	public string id { get; set; }

	public string etag { get; set; }

	public string name { get; set; } = string.Empty;

	public long? sortOrder { get; set; }

	public int deleted { get; set; }

	public int userId { get; set; }

	public string sortType { get; set; }

	public bool open { get; set; }

	public bool showAll { get; set; } = true;

	[JsonIgnore]
	public string sync_status { get; set; }

	public string teamId { get; set; }

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

	public string viewMode { get; set; }

	[JsonProperty("timeline")]
	[Ignore]
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

	public ProjectGroupModel Clone()
	{
		return MemberwiseClone() as ProjectGroupModel;
	}

	public SortOption GetSortOption()
	{
		if (SortOption == null)
		{
			if (string.IsNullOrEmpty(sortType))
			{
				sortType = "project";
			}
			return SortOptionUtils.GetSortOptionBySortType(Utils.LoadSortType(sortType), viewMode == "kanban");
		}
		return SortOption;
	}
}
