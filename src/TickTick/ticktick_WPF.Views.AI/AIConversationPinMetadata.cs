using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ticktick_WPF.Views.AI;

internal sealed class AIConversationPinMetadata
{
	[JsonProperty("pinned_time")]
	public JToken PinnedTime { get; set; }

	[JsonProperty("sort_order")]
	public long? SortOrder { get; set; }

	[JsonIgnore]
	public bool IsPinned
	{
		get
		{
			if (PinnedTime != null && PinnedTime.Type != JTokenType.Null)
			{
				return PinnedTime.Type != JTokenType.Undefined;
			}
			return false;
		}
	}

	[JsonIgnore]
	public decimal TimeSortKey
	{
		get
		{
			if (!IsPinned)
			{
				return 0m;
			}
			if (PinnedTime.Type == JTokenType.Date)
			{
				return PinnedTime.ToObject<DateTimeOffset>().ToUnixTimeMilliseconds();
			}
			if (PinnedTime.Type == JTokenType.Integer || PinnedTime.Type == JTokenType.Float)
			{
				return PinnedTime.Value<decimal>();
			}
			if (DateTimeOffset.TryParse(PinnedTime.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
			{
				return result.ToUnixTimeMilliseconds();
			}
			return 0m;
		}
	}
}
