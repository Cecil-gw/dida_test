using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class MatrixProfileQuadrantSyncModel
{
	[JsonProperty("id")]
	public string id { get; set; }

	[JsonProperty("quadrant", NullValueHandling = NullValueHandling.Ignore)]
	public int? quadrant { get; set; }

	[JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
	public string name { get; set; }

	[JsonProperty("rule", NullValueHandling = NullValueHandling.Ignore)]
	public string rule { get; set; }

	[JsonProperty("sortType", NullValueHandling = NullValueHandling.Ignore)]
	public string sortType { get; set; }

	[JsonProperty("sortOption", NullValueHandling = NullValueHandling.Ignore)]
	public SortOption SortOption { get; set; }

	[JsonProperty("groupBy", NullValueHandling = NullValueHandling.Ignore)]
	public string groupBy { get; set; }

	[JsonProperty("orderBy", NullValueHandling = NullValueHandling.Ignore)]
	public string orderBy { get; set; }

	[JsonProperty("order", NullValueHandling = NullValueHandling.Ignore)]
	public string order { get; set; }

	[JsonProperty("sortOrder")]
	public long? sortOrder { get; set; }

	[JsonProperty("createdTime", NullValueHandling = NullValueHandling.Ignore)]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? createdTime { get; set; }

	[JsonProperty("modifiedTime", NullValueHandling = NullValueHandling.Ignore)]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? modifiedTime { get; set; }

	[JsonProperty("etag", NullValueHandling = NullValueHandling.Ignore)]
	public string etag { get; set; }

	[JsonProperty("deleted", NullValueHandling = NullValueHandling.Ignore)]
	public int? deleted { get; set; }

	public bool ShouldSerializequadrant()
	{
		return false;
	}

	public bool ShouldSerializegroupBy()
	{
		return false;
	}

	public bool ShouldSerializeorderBy()
	{
		return false;
	}

	public bool ShouldSerializeorder()
	{
		return false;
	}

	public SortOption GetSortOption()
	{
		if (SortOption != null)
		{
			return SortOption;
		}
		if (string.IsNullOrEmpty(orderBy) && string.IsNullOrEmpty(groupBy))
		{
			return null;
		}
		return new SortOption
		{
			groupBy = groupBy,
			orderBy = orderBy,
			order = order
		};
	}
}
