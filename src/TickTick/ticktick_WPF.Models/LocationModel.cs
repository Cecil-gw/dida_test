using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

public class LocationModel : BaseModel
{
	public class Loc
	{
		public float longitude { get; set; }

		public float latitude { get; set; }
	}

	public string alias { get; set; }

	[Ignore]
	public Loc loc { get; set; }

	public float? radius { get; set; }

	public int? transitionType { get; set; }

	public string shortAddress { get; set; }

	public string address { get; set; }

	public string userId { get; set; }

	public string taskId { get; set; }

	public float longitude { get; set; }

	public float latitude { get; set; }
}
