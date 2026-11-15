using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Misc;

public class SelectableItemViewModel : UpDownSelectViewModel
{
	private bool _highlighted;

	private bool _open;

	private bool _partSelected;

	private bool _inCalFilter;

	private Geometry _icon;

	public bool CanMultiSelect { get; protected set; } = true;

	public string Title { get; set; } = "";

	public string Desc { get; set; } = "";

	public Visibility DescVisibility
	{
		get
		{
			if (!string.IsNullOrEmpty(Desc))
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}
	}

	public string ToolTip { get; set; }

	public string IconText { get; set; }

	public Geometry Icon { get; set; }

	public bool ShowIndent { get; set; }

	public bool IsSubItem { get; set; }

	public bool IsParent { get; set; }

	public string Id { get; protected set; }

	public string ParentId { get; protected set; }

	public long SortOrder { get; set; }

	public string Type { get; set; }

	public bool BatchMode { get; set; }

	public bool IsSplit { get; set; }

	public bool IsSectionGroup { get; set; }

	public bool CanExpand { get; set; } = true;

	public bool ShowIcon { get; set; } = true;

	public string TeamId { get; set; }

	public bool IsTeam { get; set; }

	public bool IsNote { get; set; }

	public bool IsBold { get; set; }

	public bool IsShare { get; set; }

	public string Emoji { get; set; } = string.Empty;

	public bool ShowSubOnSide { get; set; }

	public bool UseCircleCheck { get; set; }

	public bool Linked { get; set; }

	public bool IsGray { get; set; }

	public SelectableItemViewModel GroupParent { get; set; }

	public List<SelectableItemViewModel> Children { get; set; } = new List<SelectableItemViewModel>();

	public override bool HasChildren
	{
		get
		{
			List<SelectableItemViewModel> children = Children;
			if (children == null)
			{
				return false;
			}
			return children.Count > 0;
		}
	}

	public bool Open
	{
		get
		{
			return _open;
		}
		set
		{
			_open = value;
			OnPropertyChanged("Open");
			OnPropertyChanged("Highlighted");
			SetOpenIcon();
			OnPropertyChanged("CheckBox");
			OnPropertyChanged("ShowCheckBox");
		}
	}

	public bool Highlighted
	{
		get
		{
			if (!_partSelected)
			{
				return base.Selected;
			}
			return true;
		}
	}

	public bool InCalFilter
	{
		get
		{
			return _inCalFilter;
		}
		set
		{
			_inCalFilter = value;
			OnPropertyChanged("InCalFilter");
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
			OnPropertyChanged("PartSelected");
			OnPropertyChanged("Highlighted");
			OnPropertyChanged("CheckBox");
			OnPropertyChanged("ShowCheckBox");
		}
	}

	public Geometry CheckBox
	{
		get
		{
			if (!base.ShowSelected)
			{
				return null;
			}
			if (InCalFilter)
			{
				if (UseCircleCheck)
				{
					return Utils.GetGeometry("Ic.Common.CheckedInside");
				}
				if (CanMultiSelect)
				{
					if (base.Selected)
					{
						return Utils.GetGeometry("Ic.TaskType.Completed");
					}
					if (PartSelected)
					{
						return Utils.GetGeometry("Ic.TaskType.PartiallySelect");
					}
					return Utils.GetGeometry("Ic.TaskType.Uncompleted");
				}
			}
			else
			{
				if (UseCircleCheck)
				{
					return Utils.GetGeometry("Ic.Common.CheckedInside");
				}
				if (base.Selected)
				{
					return Utils.GetGeometry("Ic.Common.Completed");
				}
				if (PartSelected && !Open)
				{
					return Utils.GetGeometry("Ic.TaskType.PartiallySelectLinear");
				}
			}
			return null;
		}
	}

	public bool ShowCheckBox
	{
		get
		{
			if (!base.ShowSelected)
			{
				return false;
			}
			if (InCalFilter)
			{
				if (UseCircleCheck)
				{
					return base.Selected;
				}
				return CanMultiSelect;
			}
			if (UseCircleCheck)
			{
				return base.Selected;
			}
			if (!base.Selected)
			{
				if (PartSelected)
				{
					return !Open;
				}
				return false;
			}
			return true;
		}
	}

	protected virtual void SetOpenIcon()
	{
	}

	public override void OnSelectedChanged()
	{
		OnPropertyChanged("Highlighted");
		OnPropertyChanged("CheckBox");
		OnPropertyChanged("ShowCheckBox");
		if (base.Selected && !IsSectionGroup)
		{
			PartSelected = false;
		}
	}

	public (string, string, string) GetProjectAndColumnId()
	{
		string item = string.Empty;
		string item2 = string.Empty;
		string item3;
		if (!(this is ProjectViewModel projectViewModel))
		{
			if (this is ProjectColumnViewModel projectColumnViewModel)
			{
				item = projectColumnViewModel.Id;
				item2 = projectColumnViewModel.Title;
				item3 = projectColumnViewModel.ParentId;
			}
			else
			{
				item3 = Id;
			}
		}
		else
		{
			item3 = projectViewModel.Id;
			List<SelectableItemViewModel> children = projectViewModel.Children;
			if (children != null && children.Count > 0 && projectViewModel.Children[0] is ProjectColumnViewModel projectColumnViewModel2)
			{
				item = projectColumnViewModel2.Id;
				if (projectViewModel.Children.Count > 1)
				{
					item2 = projectColumnViewModel2.Title;
				}
			}
		}
		return (item3, item, item2);
	}

	public virtual ProjectIdentity ToIdentity()
	{
		return null;
	}
}
