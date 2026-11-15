using System;
using TickTickModels;

namespace ticktick_WPF.Models;

public class HolidayModel : BaseModel
{
	public int type { get; set; }

	public DateTime date { get; set; }

	public string name { get; set; }

	public string region { get; set; }
}
