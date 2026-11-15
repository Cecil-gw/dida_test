using System.Collections.ObjectModel;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Config;

public class BindCalendarViewModel : BaseViewModel
{
	public string Id { get; set; }

	public string Name { get; set; }

	public string Color { get; set; }

	public ObservableCollection<ComboBoxViewModel> StatusItems { get; set; }
}
