using System;
using Newtonsoft.Json;
using SQLite;

namespace TickTickModels;

public class FocusOptionModel : BaseModel
{
	public string id { get; set; }

	public string oId { get; set; }

	public int oType { get; set; }

	public string op { get; set; }

	public long duration { get; set; }

	public string firstFocusId { get; set; }

	public string focusOnTitle { get; set; }

	public string focusOnId { get; set; }

	public int focusOnType { get; set; }

	public int autoPomoLeft { get; set; }

	public int pomoCount { get; set; }

	[JsonConverter(typeof(UtcDateTimeWithMillConverter))]
	[Ignore]
	public DateTime time { get; set; }

	[JsonIgnore]
	public long timeStamp
	{
		get
		{
			return time.Ticks;
		}
		set
		{
			time = new DateTime(value);
		}
	}

	public bool manual { get; set; }

	public string note { get; set; }

	public FocusOptionModel Copy()
	{
		return (FocusOptionModel)MemberwiseClone();
	}
}
