namespace ticktick_WPF.Models;

public interface ITimeline
{
	TimelineModel Timeline { get; set; }

	TimelineSyncModel SyncTimeline { get; set; }
}
