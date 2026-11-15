using System.Collections.Generic;
using KotlinModels;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class DeleteRecurrenceUndo : UndoController
{
	private readonly List<TaskDeleteRecurrenceUndoEntity> _undoEntitys;

	public DeleteRecurrenceUndo(List<TaskDeleteRecurrenceUndoEntity> undoEntity)
	{
		_undoEntitys = undoEntity;
	}

	public override string GetTitle()
	{
		return string.Empty;
	}

	public override string GetContent()
	{
		return Utils.GetString("TaskDeleted1");
	}

	public override async void Undo()
	{
		foreach (TaskDeleteRecurrenceUndoEntity undoEntity in _undoEntitys)
		{
			await TaskService.UndoDeleteTaskRecurrence(undoEntity);
		}
		UndoHelper.CanUndoIds.Clear();
	}

	public override async void Finished()
	{
		_undoEntitys.Clear();
		UndoHelper.CanUndoIds.Clear();
	}
}
