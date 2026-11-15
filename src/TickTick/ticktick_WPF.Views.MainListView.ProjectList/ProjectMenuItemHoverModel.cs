using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MainListView.ProjectList;

public class ProjectMenuItemHoverModel : BaseViewModel
{
	public static ProjectMenuItemHoverModel Model = new ProjectMenuItemHoverModel();

	private PtfType _hoverType = PtfType.Null;

	private string _teamId = string.Empty;

	public string TeamId
	{
		get
		{
			return _teamId;
		}
		set
		{
			_teamId = value;
			OnPropertyChanged("TeamId");
		}
	}

	public PtfType HoverType
	{
		get
		{
			return _hoverType;
		}
		set
		{
			if (_hoverType != value)
			{
				_hoverType = value;
				OnPropertyChanged("HoverType");
			}
		}
	}
}
