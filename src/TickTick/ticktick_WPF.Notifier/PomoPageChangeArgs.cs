namespace ticktick_WPF.Notifier;

public class PomoPageChangeArgs
{
	public PomoPageEventType EventType { get; set; }

	public object Data { get; set; }

	public PomoDurationChangeSource Source { get; set; }
}
