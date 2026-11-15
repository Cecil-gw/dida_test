using System;
using Newtonsoft.Json;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Models;

[Serializable]
public class TimelineModel
{
	[JsonProperty("range")]
	public string Range { get; set; }

	public int DayWidthIndex { get; set; }

	[JsonProperty("sortType")]
	public string SortType { get; set; }

	[JsonProperty("sortOption")]
	public SortOption sortOption { get; set; }

	public TimelineModel()
	{
		Range = TimelineConstants.RangeDefault;
		SortType = TimelineConstants.SortDefault;
		sortOption = new SortOption
		{
			groupBy = TimelineConstants.SortDefault,
			orderBy = "sortOrder"
		};
	}

	public TimelineModel(string sortType)
	{
		Range = TimelineConstants.RangeDefault;
		SortType = sortType;
		sortOption = new SortOption
		{
			groupBy = sortType,
			orderBy = "sortOrder"
		};
	}

	public TimelineModel(string sortType, SortOption option)
	{
		Range = TimelineConstants.RangeDefault;
		SortType = sortType;
		sortOption = option ?? new SortOption
		{
			groupBy = sortType,
			orderBy = "sortOrder"
		};
	}

	public bool IsEquals(TimelineModel model)
	{
		if (model == null)
		{
			return false;
		}
		if (Range == model.Range && DayWidthIndex == model.DayWidthIndex)
		{
			return model.sortOption?.Equal(sortOption) ?? false;
		}
		return false;
	}

	public static void CheckTimelineEmpty(ITimeline timeline, Constants.SortType defaultSort)
	{
		if (timeline.Timeline == null)
		{
			timeline.Timeline = new TimelineModel();
		}
		if (timeline.Timeline.Range == null)
		{
			timeline.Timeline.Range = TimelineConstants.RangeDefault;
		}
		if (timeline.Timeline.SortType == null)
		{
			timeline.Timeline.SortType = defaultSort.ToString();
		}
		if (timeline.Timeline.sortOption == null)
		{
			timeline.Timeline.sortOption = new SortOption
			{
				groupBy = ((timeline.Timeline.SortType == "dueDate") ? "none" : timeline.Timeline.SortType),
				orderBy = ((timeline.Timeline.SortType == "dueDate") ? "dueDate" : "sortOrder")
			};
		}
	}

	public TimelineModel Copy()
	{
		return (TimelineModel)MemberwiseClone();
	}

	public bool SyncPropertyChanged(TimelineModel model)
	{
		if (model == null)
		{
			return true;
		}
		return !sortOption.Equal(model.sortOption);
	}

	public SortOption GetSortOption()
	{
		if (sortOption == null)
		{
			sortOption = new SortOption
			{
				groupBy = ((SortType == "dueDate") ? "none" : SortType),
				orderBy = ((SortType == "dueDate") ? "dueDate" : "sortOrder")
			};
		}
		return new SortOption
		{
			groupBy = sortOption.groupBy,
			orderBy = sortOption.orderBy,
			order = sortOption.order
		};
	}
}
