using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.TaskList;

public interface ITaskList
{
	int QuadrantLevel { get; set; }

	bool Editable();

	bool Copyable();

	void SelectTask(string taskId, TaskSelectType type, bool ignoreBatch = false);

	void TrySetTitleReadonly(string taskId);

	void SelectSubtask(IdExtra id, TaskSelectType type);

	void SelectItem(string eventId, DisplayType itemType);

	void CopyTask(string taskId);

	void DeleteTask(string taskId, TaskDeleteType type);

	void DeleteSelectedTasks();

	void CompleteCheckitem(string itemId, bool playSound = true);

	Task TaskTitleChanged(string taskId, string text);

	void EventTitleChanged(string eventId, string text);

	void SubtaskTitleChanged(string taskId, string checkItemId, string text);

	void MoveUp(string taskId, string itemId, bool fromDetail = false);

	void MoveDown(string taskId, string itemId, bool fromDetail = false);

	Task SplitDisplayItem(string id);

	void SetTitleCaretIndex(int caretIndex);

	Task StartDrag(DisplayItemModel model, Point p, string fromColumnId = null);

	void MultipleTextPaste(string text);

	void BatchShiftSelected(string taskId);

	void BatchCtrlSelected(string taskId);

	void AfterTaskChanged(DisplayItemModel model, bool tryFocused = false);

	void OnItemArchived();

	void OnHabitSkipped(string habitId);

	void ReLoad(string id);

	bool IsCompletedList();

	bool CanAddSubTask();

	void BatchSelectOnMove(bool isUp);

	Task CreateSubTask(DisplayItemModel model, bool addToLastOne = false);

	Task OnCheckBoxRightMouseUp(UIElement element, DisplayItemModel model);

	void OnNavigateTask(DisplayItemModel model);

	void OnLineVisibleChanged(DisplayItemModel model, bool visible);

	void ResetDrag();

	void SetDetailInOperation(bool inOperate, bool active = true);

	void RemoveItemById(string id);

	IToastShowWindow GetToastParent();

	Task OnLostFocus(bool reload);

	void AfterTaskProjectChanged(DisplayItemModel model);

	TimeData GetDefaultTimeData();

	void FocusItem(string modelId);

	void ResetModel(DisplayItemModel model);
}
