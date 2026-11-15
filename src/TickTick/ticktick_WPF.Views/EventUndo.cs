using ticktick_WPF.Dal;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class EventUndo : UndoController
{
	private readonly string _eventId;

	private readonly string _eventTitle;

	public EventUndo(string eventId, string eventTitle)
	{
		_eventId = eventId;
		_eventTitle = eventTitle;
	}

	public override string GetTitle()
	{
		return _eventTitle;
	}

	public override string GetContent()
	{
		return Utils.GetString("Deleted");
	}

	public override async void Undo()
	{
		await CalendarService.UndoDeleteEvent(_eventId);
		CalendarEventChangeNotifier.NotifyEventRestored(_eventId);
	}

	public override async void Finished()
	{
		await SyncStatusDao.AddEventDeletedSyncStatus(_eventId);
		SyncManager.Sync();
	}
}
