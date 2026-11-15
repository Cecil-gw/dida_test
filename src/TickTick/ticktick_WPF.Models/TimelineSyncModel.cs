using Newtonsoft.Json;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Models;

public class TimelineSyncModel
{
	[JsonProperty("sortType")]
	public string SortType { get; set; } = Constants.SortType.sortOrder.ToString();

	[JsonIgnore]
	public SortOption SortOption { get; set; }

	[JsonProperty("sortOption")]
	public object SortOptionForSerialize
	{
		get
		{
			if (SortOption != null)
			{
				return new { SortOption.groupBy, SortOption.orderBy };
			}
			return null;
		}
		set
		{
			SortOption = ((value == null) ? null : JsonConvert.DeserializeObject<SortOption>(value.ToString()));
		}
	}

	public TimelineSyncModel()
	{
	}

	public TimelineSyncModel(string sortType)
	{
		SortType = sortType;
		SortOption = ((sortType == "dueDate") ? new SortOption
		{
			groupBy = "none",
			orderBy = "dueDate"
		} : new SortOption
		{
			groupBy = SortType,
			orderBy = "sortOrder"
		});
	}

	public override bool Equals(object obj)
	{
		if (!(obj is TimelineSyncModel timelineSyncModel))
		{
			return false;
		}
		return object.Equals(SortOption, timelineSyncModel.SortOption);
	}
}
