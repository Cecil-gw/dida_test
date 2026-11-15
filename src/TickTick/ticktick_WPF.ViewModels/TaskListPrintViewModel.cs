using System.Collections.Generic;

namespace ticktick_WPF.ViewModels;

public class TaskListPrintViewModel : BaseViewModel
{
	public string ListName { get; set; }

	public bool IsNormal { get; set; }

	public List<TaskPrintViewModel> TaskPrintViewModels { get; set; } = new List<TaskPrintViewModel>();
}
