using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ticktick_WPF.Dal;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Timeline;

public class TimelineGroupViewModel : TimelineDisplayBase
{
	private double _height;

	private string _title;

	private bool _isOpen;

	private bool _editing;

	private bool _isArrangeOpen = true;

	private Visibility _showArrow;

	private string _avatarUrl;

	private TimelineViewModel _parent;

	public string CatId { get; set; }

	public string ProjectId { get; set; }

	public string Id { get; set; }

	public int Line { get; set; }

	public long SortOrder { get; set; }

	public string AvatarUrl
	{
		get
		{
			return _avatarUrl ?? string.Empty;
		}
		set
		{
			_avatarUrl = value;
			OnPropertyChanged("AvatarUrl");
			SetAvatar();
		}
	}

	public BitmapImage Avatar { get; set; }

	public double Height
	{
		get
		{
			return _height;
		}
		set
		{
			_height = value;
			OnPropertyChanged("Height");
		}
	}

	public override string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			OnPropertyChanged("Title");
		}
	}

	public bool IsOpen
	{
		get
		{
			return _isOpen;
		}
		set
		{
			_isOpen = value;
			Angle = ((!value) ? (-90) : 0);
			OnPropertyChanged("Angle");
			OnPropertyChanged("IsOpen");
			OnPropertyChanged("Data");
		}
	}

	public bool Editing
	{
		get
		{
			return _editing;
		}
		set
		{
			if (_editing != value)
			{
				_parent?.SetGroupEditing(value);
			}
			_editing = value;
			OnPropertyChanged("Editing");
		}
	}

	public bool IsArrangeOpen
	{
		get
		{
			return _isArrangeOpen;
		}
		set
		{
			_isArrangeOpen = value;
			ArrangeAngle = ((!value) ? (-90) : 0);
			OnPropertyChanged("ArrangeAngle");
			OnPropertyChanged("IsArrangeOpen");
			OnPropertyChanged("Data");
		}
	}

	public Visibility ShowArrow
	{
		get
		{
			return _showArrow;
		}
		set
		{
			_showArrow = value;
			OnPropertyChanged("ShowArrow");
		}
	}

	public double Angle { get; set; }

	public double ArrangeAngle { get; set; }

	public TimelineViewModel Parent { get; set; }

	public bool Editable { get; set; }

	public bool IsAvatar { get; set; }

	public TimelineGroupViewModel(TimelineViewModel parent)
	{
		_parent = parent ?? TimelineViewModel.Instance;
	}

	private async void SetAvatar()
	{
		Avatar = await AvatarHelper.GetAvatarByUrlAsync(AvatarUrl);
		OnPropertyChanged("Avatar");
	}

	public async Task UpdateIsOpen(bool isOpen)
	{
		await UpdateOpen(isOpen, isArrange: false);
	}

	public async Task UpdateIsArrangeOpen(bool isOpen)
	{
		await UpdateOpen(isOpen, isArrange: true);
	}

	private async Task UpdateOpen(bool isOpen, bool isArrange)
	{
		if (isArrange)
		{
			IsArrangeOpen = isOpen;
		}
		else
		{
			IsOpen = isOpen;
		}
		if (!isOpen)
		{
			await SectionStatusDao.CloseProjectSection((isArrange ? "Arrange_" : string.Empty) + CatId, Id);
		}
		else
		{
			await SectionStatusDao.OpenProjectSection((isArrange ? "Arrange_" : string.Empty) + CatId, Id);
		}
		if (isArrange)
		{
			await Parent.UpdateSortArrangeAsync(updateGroup: false);
		}
		else
		{
			await Parent.UpdateCellLineAsync();
		}
	}

	public async Task ToggleOpen(bool isOpen)
	{
		if (isOpen != IsOpen)
		{
			IsOpen = isOpen;
			if (isOpen)
			{
				await SectionStatusDao.OpenProjectSection(CatId, Id);
			}
			else
			{
				await SectionStatusDao.CloseProjectSection(CatId, Id);
			}
		}
	}
}
