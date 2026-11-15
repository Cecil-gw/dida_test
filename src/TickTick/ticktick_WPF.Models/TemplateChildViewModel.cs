using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Models;

public class TemplateChildViewModel : BaseViewModel
{
	public string Id { get; set; }

	public string Title { get; set; }

	public int Level { get; set; }

	public bool IsTextMode { get; set; }
}
