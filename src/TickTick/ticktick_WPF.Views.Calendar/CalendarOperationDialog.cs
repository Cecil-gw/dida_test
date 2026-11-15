using System;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Views.Calendar;

public class CalendarOperationDialog : OperationDialog
{
	private readonly DateTime? _dueDate;

	private readonly string _eventId;

	private readonly DateTime? _startDate;

	private bool _isAllDay;

	private EventArchiveArgs _args;

	public CalendarOperationDialog(EventArchiveArgs args, bool cancel = false)
		: base(args.Id, cancel ? new OperationItemViewModel(ActionType.CancelArchive) : new OperationItemViewModel(ActionType.Archive))
	{
		_eventId = args.Id;
		args.Id = ArchivedDao.GetOriginalId(args.Id);
		_args = args;
	}

	protected override async void OnActionClick(OperationItemViewModel model)
	{
		if (model.Type == ActionType.CancelArchive)
		{
			await ArchivedEventDao.RemoveArchivedEvent(ArchivedEventDao.GenerateEventKey(_args));
			UtilLog.Info("CancelArchiveEvent " + _args.Id + "," + _args.StartTime.ToString());
		}
		else
		{
			await ArchivedEventDao.AddArchivedModel(_args);
			UtilLog.Info("ArchiveEvent " + _args.Id + "," + _args.StartTime.ToString());
		}
		if (ABTestManager.IsNewRemindCalculate())
		{
			await EventReminderCalculator.RecalEventReminders(await CalendarEventDao.GetEventByEventIdOrId(_eventId));
		}
		else
		{
			ReminderCalculator.AssembleReminders();
		}
		await SearchHelper.OnEventArchiveChanged(_eventId);
		EventArchiveSyncService.PushLocalArchivedModels();
		base.OnActionClick(model);
	}
}
