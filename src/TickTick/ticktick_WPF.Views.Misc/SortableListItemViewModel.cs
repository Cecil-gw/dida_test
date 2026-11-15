using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Misc;

public class SortableListItemViewModel : BaseViewModel, IDraggable
{
	private bool _open;

	private bool _dragging;

	private DragVisualState _visualState;

	public bool ParentDragging;

	public bool IsSection { get; set; }

	public string Id { get; set; }

	public string SectionId { get; set; }

	public bool CanSort { get; set; } = true;

	public long SortOrder { get; set; }

	public bool Dragging
	{
		get
		{
			return _dragging;
		}
		set
		{
			if (_dragging != value)
			{
				_dragging = value;
				OnPropertyChanged("Dragging");
			}
		}
	}

	public DragVisualState VisualState
	{
		get
		{
			return _visualState;
		}
		set
		{
			if (_visualState != value)
			{
				_visualState = value;
				OnPropertyChanged("VisualState");
			}
		}
	}

	public bool IsOpen
	{
		get
		{
			return _open;
		}
		set
		{
			_open = value;
			OnPropertyChanged("IsOpen");
		}
	}

	public virtual void OnParentDraggingChanged()
	{
	}

	public virtual void SaveSortOrder()
	{
	}
}
