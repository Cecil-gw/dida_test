using ticktick_WPF.Models;

namespace ticktick_WPF.Views;

public interface IRemindPop
{
	string ReminderKey { get; }

	bool IsSameReminder(ReminderModel reminder);

	void ShowWindow();

	void UpdateReminder(ReminderModel reminder);

	void TryClose();

	void SetDisplayStyle(int remindersCount);

	void TryHide();
}
