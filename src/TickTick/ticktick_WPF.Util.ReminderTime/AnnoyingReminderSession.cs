using System;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.ReminderTime;

internal class AnnoyingReminderSession
{
	public string Key { get; set; }

	public ReminderModel Reminder { get; set; }

	public AnnoyingReminderChannel Channel { get; set; }

	public DateTime StartedAt { get; set; }

	public DateTime NextNotifyAt { get; set; }

	public DateTime? ExpireAt { get; set; }

	public AnnoyingReminderSession Clone()
	{
		return new AnnoyingReminderSession
		{
			Key = Key,
			Reminder = Reminder?.Copy(),
			Channel = Channel,
			StartedAt = StartedAt,
			NextNotifyAt = NextNotifyAt,
			ExpireAt = ExpireAt
		};
	}
}
