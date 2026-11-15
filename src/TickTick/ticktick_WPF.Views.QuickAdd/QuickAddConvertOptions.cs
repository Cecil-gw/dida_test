namespace ticktick_WPF.Views.QuickAdd;

public class QuickAddConvertOptions
{
	public bool ShowConvertToTask { get; }

	public bool ShowConvertToNote { get; }

	public bool ShowConvertToEvent { get; }

	public QuickAddConvertOptions(bool showConvertToTask, bool showConvertToNote, bool showConvertToEvent)
	{
		ShowConvertToTask = showConvertToTask;
		ShowConvertToNote = showConvertToNote;
		ShowConvertToEvent = showConvertToEvent;
	}
}
