using ticktick_WPF.Views.MainListView.TaskList;

namespace ticktick_WPF.Views.TaskList;

public interface ITaskOperation
{
	void SetPriority(int priority);

	void SetDate(string key);

	void ClearDate();

	void SelectDate(bool relative);

	void ToggleTaskCompleted();

	void Delete();

	TaskListView GetParentList();

	bool ParsingDate();

	bool IsNewAdd();

	void PinOrUnpinTask();

	void OpenSticky();
}
