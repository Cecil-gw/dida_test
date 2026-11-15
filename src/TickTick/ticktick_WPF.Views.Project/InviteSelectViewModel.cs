using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Project;

public class InviteSelectViewModel : BaseViewModel
{
	private bool _preDelete;

	public ShareInviteTarget Target { get; set; }

	public string DisplayName { get; set; }

	public bool PreDelete
	{
		get
		{
			return _preDelete;
		}
		set
		{
			_preDelete = value;
			OnPropertyChanged("PreDelete");
		}
	}

	public bool IsInput { get; set; }
}
