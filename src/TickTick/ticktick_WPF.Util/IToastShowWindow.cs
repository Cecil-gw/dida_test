using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using KotlinModels;
using ticktick_WPF.Models;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Undo;

namespace ticktick_WPF.Util;

public interface IToastShowWindow
{
	void TaskDeleted(string taskId);

	void TryToastString(object sender, string e);

	Task<bool> BatchDeleteTask(List<TaskModel> tasks);

	void TaskComplete(CloseUndoToast undo);

	void TryHideToast();

	void ToastDeleteRecUndo(List<TaskDeleteRecurrenceUndoEntity> undoModels);

	void ToastMoveProjectControl(string projectId, string taskName = null, MoveToastType moveType = MoveToastType.Move);

	void Toast(FrameworkElement uiElement);
}
