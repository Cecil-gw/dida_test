using System.Windows;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Undo;

public class CloseUndoToast : UndoToast
{
	private CloseUndoModel _undo;

	public CloseUndoToast(CloseUndoModel undo, int closeStatus)
	{
		if (closeStatus == -1)
		{
			DeletedOrCompleted.Text = Utils.GetString("TaskAbandoned");
		}
		else
		{
			DeletedOrCompleted.Text = Utils.GetString("TaskCompleted");
		}
		_undo = undo;
	}

	public override void OnFinished()
	{
		_undo.Finished();
	}

	public override void OnUndo()
	{
		base.Visibility = Visibility.Collapsed;
		_undo.Undo();
	}
}
