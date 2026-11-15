using System.Collections.Generic;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Tag;

public class TagDisplayViewModel : BaseViewModel
{
	private bool _childSelected;

	private bool _collapsed;

	private bool _isOpen;

	private bool _partSelected;

	private bool _selected;

	private bool _isSubTag;

	private bool _isParent;

	private bool _focused;

	public string Tag;

	public bool IsAddTag { get; private set; }

	public bool IsAllTag { get; set; }

	public Geometry Icon { get; set; }

	public bool ShowIcon { get; set; } = true;

	public string Emoji { get; set; } = string.Empty;

	public bool ShowEmoji => !string.IsNullOrEmpty(Emoji);

	public double IconWidth { get; set; } = 15.0;

	public bool Highlighted
	{
		get
		{
			if (!Selected)
			{
				return ShowPartSelected;
			}
			return true;
		}
	}

	public bool ShowPartSelected
	{
		get
		{
			if (!Selected)
			{
				if (!PartSelected)
				{
					if (!_isOpen && IsParent)
					{
						return _childSelected;
					}
					return false;
				}
				return true;
			}
			return false;
		}
	}

	public string Parent { get; set; }

	public bool IsSplit { get; set; }

	public string Title { get; set; }

	public string FullTitle { get; set; }

	public List<TagDisplayViewModel> Children { get; set; }

	public bool Focused
	{
		get
		{
			return _focused;
		}
		set
		{
			_focused = value;
			OnPropertyChanged("Focused");
		}
	}

	public bool IsSubTag
	{
		get
		{
			return _isSubTag;
		}
		set
		{
			_isSubTag = value;
			OnPropertyChanged("IsSubTag");
		}
	}

	public bool IsParent
	{
		get
		{
			return _isParent;
		}
		set
		{
			_isParent = value;
			OnPropertyChanged("IsParent");
			OnPropertyChanged("ShowPartSelected");
			OnPropertyChanged("Highlighted");
		}
	}

	public bool PartSelected
	{
		get
		{
			return _partSelected;
		}
		set
		{
			_partSelected = value;
			OnPropertyChanged("ShowPartSelected");
			OnPropertyChanged("Highlighted");
		}
	}

	public bool ChildSelected
	{
		get
		{
			return _childSelected;
		}
		set
		{
			_childSelected = value;
			OnPropertyChanged("ShowPartSelected");
			OnPropertyChanged("Highlighted");
		}
	}

	public bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			_selected = value;
			OnPropertyChanged("Selected");
			OnPropertyChanged("Highlighted");
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
			OnPropertyChanged("IsOpen");
			OnPropertyChanged("ShowPartSelected");
			OnPropertyChanged("Highlighted");
		}
	}

	public bool Collapsed
	{
		get
		{
			return _collapsed;
		}
		set
		{
			_collapsed = value;
			OnPropertyChanged("Collapsed");
		}
	}

	private TagDisplayViewModel()
	{
	}

	public TagDisplayViewModel(bool split)
	{
		IsSplit = split;
	}

	public TagDisplayViewModel(string title, bool selected, bool partSelected = false)
	{
		Title = title;
		FullTitle = title;
		_selected = selected;
		_partSelected = partSelected;
		Tag = title.ToLower();
		Icon = Utils.GetGeometry("Ic.ProjectList.Tag");
	}

	public TagDisplayViewModel(TagModel tag, bool selected, bool partSelected = false, bool showFullTitle = false)
	{
		string displayEmoji = tag.GetDisplayEmoji();
		if (!string.IsNullOrEmpty(displayEmoji))
		{
			Emoji = displayEmoji;
			Title = (showFullTitle ? tag.GetDisplayName() : tag.GetDisplayNameWithoutEmoji());
		}
		else
		{
			Title = tag.GetDisplayName();
			Icon = Utils.GetGeometry("Ic.ProjectList.Tag");
		}
		FullTitle = tag.GetDisplayName();
		_selected = selected;
		_partSelected = partSelected;
		Tag = tag.name;
	}

	public static TagDisplayViewModel BuildAddTag(string tag, bool selected = false)
	{
		return new TagDisplayViewModel
		{
			Title = string.Format(Utils.GetString("CreateTag"), tag),
			FullTitle = string.Format(Utils.GetString("CreateTag"), tag),
			Selected = false,
			IsAddTag = true,
			Tag = tag,
			_selected = selected,
			Icon = Utils.GetGeometry("Ic.Tag.AddTag"),
			IconWidth = 18.0
		};
	}

	public static TagDisplayViewModel BuildAllTag(bool selected = true)
	{
		return new TagDisplayViewModel
		{
			Title = Utils.GetString("AllTags"),
			FullTitle = Utils.GetString("AllTags"),
			IsAllTag = true,
			Tag = "*tag",
			Selected = selected,
			Icon = Utils.GetGeometry("Ic.Tag.AllTag"),
			IconWidth = 18.0
		};
	}

	public static TagDisplayViewModel BuildNoTag(bool selected = false)
	{
		return new TagDisplayViewModel
		{
			Title = Utils.GetString("NoTags"),
			FullTitle = Utils.GetString("NoTags"),
			Tag = "!tag",
			Selected = selected,
			Icon = Utils.GetGeometry("Ic.Tag.NoTag")
		};
	}

	public static TagDisplayViewModel BuildWithTag(bool selected = false)
	{
		return new TagDisplayViewModel
		{
			Title = Utils.GetString("WithTags"),
			Tag = "*withtags",
			Selected = selected,
			Icon = Utils.GetGeometry("Ic.Tag.HasTag")
		};
	}

	public TagDisplayViewModel Clone()
	{
		return (TagDisplayViewModel)MemberwiseClone();
	}
}
