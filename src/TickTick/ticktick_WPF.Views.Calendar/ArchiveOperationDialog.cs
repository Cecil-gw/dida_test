using ticktick_WPF.Dal;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;

namespace ticktick_WPF.Views.Calendar;

public class ArchiveOperationDialog : OperationDialog
{
	private readonly string _key;

	private ArchiveKind _kind;

	public ArchiveOperationDialog(string key, ArchiveKind kind, bool cancel = false)
		: base(key, cancel ? new OperationItemViewModel(ActionType.CancelArchive) : new OperationItemViewModel(ActionType.Archive))
	{
		_key = key;
		_kind = kind;
	}

	protected override async void OnActionClick(OperationItemViewModel model)
	{
		if (model.Type != ActionType.CancelArchive)
		{
			await ArchivedDao.AddArchivedModel(_key, _kind);
		}
		else
		{
			await ArchivedDao.RemoveArchivedModel(_key);
		}
		if (ABTestManager.IsNewRemindCalculate())
		{
			await CourseReminderCalculator.InitCourseReminders();
		}
		else
		{
			ReminderCalculator.AssembleReminders();
		}
		DataChangedNotifier.OnScheduleChanged();
		if (_kind == ArchiveKind.Course)
		{
			CourseArchiveSyncService.PushLocalArchivedModels();
		}
		if (_kind == ArchiveKind.Event)
		{
			EventArchiveSyncService.PushLocalArchivedModels();
		}
		base.OnActionClick(model);
	}
}
