namespace ticktick_WPF.ViewModels;

public class HabitItemBaseViewModel : BaseViewModel
{
	private bool _selected;

	private long _sortOrder;

	private bool _dragging;

	public virtual string Id { get; set; }

	public virtual bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			_selected = value;
			OnPropertyChanged("Selected");
		}
	}

	public virtual bool Archived { get; set; }

	public virtual long SortOrder
	{
		get
		{
			return _sortOrder;
		}
		set
		{
			_sortOrder = value;
			OnPropertyChanged("SortOrder");
		}
	}

	public bool Dragging
	{
		get
		{
			return _dragging;
		}
		set
		{
			_dragging = value;
			OnPropertyChanged("Dragging");
			OnPropertyChanged("ShowDateCheckIn");
		}
	}

	public bool Enable { get; set; } = true;

	public bool IsSection { get; set; }

	public bool IsFirstOne { get; set; }

	public HabitItemBaseViewModel Clone()
	{
		return (HabitItemBaseViewModel)MemberwiseClone();
	}
}
