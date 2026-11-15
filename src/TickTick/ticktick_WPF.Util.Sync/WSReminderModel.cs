using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public class WSReminderModel
{
	public string type { get; set; }

	public RemindMessage data { get; set; }
}
