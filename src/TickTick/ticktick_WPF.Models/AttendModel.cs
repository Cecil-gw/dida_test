using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

public class AttendModel : BaseModel
{
	[Unique]
	public string attendId { get; set; }

	public string json { get; set; }
}
