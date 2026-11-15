using System;
using System.Linq;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Models;

[Serializable]
public class FilterModel : BaseModel, ITimeline
{
	[Indexed]
	public string id { get; set; }

	public string name { get; set; }

	public string rule { get; set; }

	public long sortOrder { get; set; }

	public string sortType { get; set; }

	public string etag { get; set; }

	public DateTime createdTime { get; set; }

	public DateTime modifiedTime { get; set; }

	public int syncStatus { get; set; }

	public int deleted { get; set; }

	public string userId { get; set; }

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

	public bool IsAvailable()
	{
		return deleted != 1;
	}

	public bool IsLocalAdded()
	{
		return syncStatus == 0;
	}

	public bool IsLocalUpdated()
	{
		if (syncStatus == 1 || syncStatus == 3)
		{
			return deleted != 1;
		}
		return false;
	}

	public bool IsLocalDeleted()
	{
		if (syncStatus == 1 || syncStatus == 3)
		{
			return deleted == 1;
		}
		return false;
	}

	public bool ContainsDate()
	{
		if (Parser.GetFilterRuleType(rule) == 0)
		{
			NormalFilterViewModel normalFilterViewModel = Parser.ToNormalModel(rule);
			if (normalFilterViewModel.DueDates != null)
			{
				return normalFilterViewModel.DueDates.Any((string d) => d != "nodue" && d != "recurring");
			}
			return false;
		}
		return Parser.ToAdvanceModel(rule).CardList.Where((CardViewModel card) => card.Type == CardType.Filter).ToList().Any((CardViewModel c) => c is DateCardViewModel { Values: not null } dateCardViewModel && dateCardViewModel.Values.Any((string v) => v != "nodue" && v != "recurring"));
	}

	public FilterModel Copy()
	{
		return (FilterModel)MemberwiseClone();
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
