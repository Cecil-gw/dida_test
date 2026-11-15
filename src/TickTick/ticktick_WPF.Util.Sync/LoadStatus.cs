using System;

namespace ticktick_WPF.Util.Sync;

public class LoadStatus
{
	public bool DrainOff { get; set; }

	public DateTime? FromTime { get; set; }

	public DateTime ToTime { get; set; }

	public int Count { get; set; }

	public string ProjectIds { get; set; }

	public LoadStatus(bool drainOff, DateTime? fromTime, DateTime toTime, int count)
	{
		DrainOff = drainOff;
		FromTime = fromTime;
		ToTime = toTime;
		Count = count;
	}
}
