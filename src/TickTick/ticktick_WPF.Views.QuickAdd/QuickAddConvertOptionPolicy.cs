namespace ticktick_WPF.Views.QuickAdd;

public static class QuickAddConvertOptionPolicy
{
	public static QuickAddConvertOptions Create(bool isNote, bool isCalendar, bool isNoteProject, bool canConvertToEvent)
	{
		return new QuickAddConvertOptions(isNote || isCalendar, !isNote, canConvertToEvent && !isCalendar);
	}
}
