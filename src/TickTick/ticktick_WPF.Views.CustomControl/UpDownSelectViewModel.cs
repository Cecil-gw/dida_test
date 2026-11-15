using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.CustomControl;

public class UpDownSelectViewModel : BaseViewModel
{
	private bool _selected;

	private bool _showSelectedIcon = true;

	private bool _hoverSelected;

	public bool Selectable { get; set; } = true;

	public bool Selected
	{
		get
		{
			if (ShowSelected)
			{
				return _selected;
			}
			return false;
		}
		set
		{
			if (_selected != value)
			{
				_selected = value;
				OnPropertyChanged("Selected");
				OnPropertyChanged("ShowSelectedIcon");
				OnSelectedChanged();
			}
		}
	}

	public bool ShowSelectedIcon
	{
		get
		{
			if (Selected)
			{
				return _showSelectedIcon;
			}
			return false;
		}
		set
		{
			if (_showSelectedIcon != value)
			{
				_showSelectedIcon = value;
				OnPropertyChanged("ShowSelectedIcon");
			}
		}
	}

	public bool HoverSelected
	{
		get
		{
			return _hoverSelected;
		}
		set
		{
			if (_hoverSelected != value)
			{
				_hoverSelected = value;
				OnPropertyChanged("HoverSelected");
			}
		}
	}

	public bool SubOpened { get; set; }

	public virtual bool HasChildren { get; }

	public bool IsEnable { get; set; } = true;

	public bool ShowSelected { get; set; } = true;

	public virtual void OnSelectedChanged()
	{
	}
}
