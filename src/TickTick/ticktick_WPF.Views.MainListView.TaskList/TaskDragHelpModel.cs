using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MainListView.TaskList;

public class TaskDragHelpModel : BaseViewModel
{
	private bool _isDragging;

	public static TaskDragHelpModel DragHelp { get; set; } = new TaskDragHelpModel();

	public bool IsDragging
	{
		get
		{
			return _isDragging;
		}
		set
		{
			_isDragging = value;
			OnPropertyChanged("IsDragging");
		}
	}

	private TaskDragHelpModel()
	{
	}
}
