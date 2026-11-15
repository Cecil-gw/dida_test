using System;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.TaskList;

public class SortTypeViewModel : BaseViewModel
{
	private Geometry _icon;

	private string _id;

	private bool _selected;

	private string _title;

	public string Id
	{
		get
		{
			return _id;
		}
		set
		{
			_id = value;
			OnPropertyChanged("Id");
		}
	}

	public string Title
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

	public Geometry Icon
	{
		get
		{
			return _icon;
		}
		set
		{
			_icon = value;
			OnPropertyChanged("Icon");
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
		}
	}

	public SortTypeViewModel(string id, bool inTimeLine = false)
	{
		_id = id;
		_title = GetTitleById(id, inTimeLine);
	}

	public static string GetTitleById(string id, bool inTimeline)
	{
		return id switch
		{
			"sortOrder" => Utils.GetString(inTimeline ? "Default" : "Custom"), 
			"project" => Utils.GetString("List"), 
			"dueDate" => Utils.GetString("TimeToDate"), 
			"title" => Utils.GetString("Title"), 
			"priority" => Utils.GetString("priority"), 
			"tag" => Utils.GetString("tag"), 
			"assignee" => Utils.GetString("assignee"), 
			"createdTime" => Utils.GetString("CreatedTime"), 
			"modifiedTime" => Utils.GetString("ModifiedTime"), 
			"none" => Utils.GetString("none"), 
			_ => Utils.GetString("none"), 
		};
	}

	public static string GetTitleByType(Constants.SortType type, bool inTimeline = false)
	{
		return type switch
		{
			Constants.SortType.sortOrder => Utils.GetString(inTimeline ? "Default" : "Custom"), 
			Constants.SortType.project => Utils.GetString("List"), 
			Constants.SortType.dueDate => Utils.GetString("TimeToDate"), 
			Constants.SortType.title => Utils.GetString("Title"), 
			Constants.SortType.priority => Utils.GetString("priority"), 
			Constants.SortType.tag => Utils.GetString("tag"), 
			Constants.SortType.assignee => Utils.GetString("assignee"), 
			Constants.SortType.createdTime => Utils.GetString("CreatedTime"), 
			Constants.SortType.modifiedTime => Utils.GetString("ModifiedTime"), 
			Constants.SortType.none => Utils.GetString("none"), 
			_ => Utils.GetString("none"), 
		};
	}

	public static string GetDirectionName(string orderBy, Constants.OrderDirection? direction)
	{
		if (!direction.HasValue)
		{
			return string.Empty;
		}
		if (!Enum.TryParse<Constants.SortType>(orderBy, ignoreCase: true, out var result))
		{
			return string.Empty;
		}
		switch (result)
		{
		case Constants.SortType.dueDate:
		case Constants.SortType.createdTime:
		case Constants.SortType.modifiedTime:
			if (direction != Constants.OrderDirection.Asc)
			{
				return Utils.GetString("NewestFirstOrder");
			}
			return Utils.GetString("OldestFirstOrder");
		case Constants.SortType.title:
		case Constants.SortType.assignee:
		case Constants.SortType.tag:
			if (direction != Constants.OrderDirection.Asc)
			{
				return Utils.GetString("Descending");
			}
			return Utils.GetString("Ascending");
		case Constants.SortType.priority:
			if (direction != Constants.OrderDirection.Asc)
			{
				return Utils.GetString("HighestFirstOrder");
			}
			return Utils.GetString("LowestFirstOrder");
		default:
			return string.Empty;
		}
	}

	public static Geometry GetSortIconById(string id, bool inTimeline)
	{
		return Utils.GetGeometry("Ic.SortGroup.Sort");
	}

	public static Geometry GetGroupIconById(string id)
	{
		return Utils.GetGeometry("Ic.SortGroup.Group");
	}

	public bool CanGroup()
	{
		if (Id != "title")
		{
			return Id != "modifiedTime";
		}
		return false;
	}
}
