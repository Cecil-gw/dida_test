using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Tag;

public class SelectedTagViewModel : BaseViewModel
{
	private string _tag;

	private bool _isAdd;

	public string Tag
	{
		get
		{
			return _tag;
		}
		set
		{
			_tag = value;
			OnPropertyChanged("Tag");
		}
	}

	public bool IsAdd
	{
		get
		{
			return _isAdd;
		}
		set
		{
			_isAdd = value;
			OnPropertyChanged("IsAdd");
		}
	}
}
