using System.Collections.Generic;

namespace TickTickModels;

public class ReminderDelaySyncModel
{
	public List<ReminderDelayModel> add { get; set; } = new List<ReminderDelayModel>();

	public List<string> delete { get; set; } = new List<string>();
}
