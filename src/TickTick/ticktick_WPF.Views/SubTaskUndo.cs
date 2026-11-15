using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class SubTaskUndo : UndoController
{
	private readonly TaskDetailItemModel _subTaskModel;

	public SubTaskUndo(TaskDetailItemModel subTaskModel)
	{
		_subTaskModel = subTaskModel;
	}

	public override string GetTitle()
	{
		return Utils.GetString("OneCheckitem");
	}

	public override string GetContent()
	{
		return Utils.GetString("Deleted");
	}

	public override async void Undo()
	{
		await TaskService.UndoDeletedCheckItem(_subTaskModel);
	}

	public override void Finished()
	{
	}
}
