using System;
using Newtonsoft.Json;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class SortOption
{
	public string groupBy
	{
		get
		{
			return groupByType.ToString();
		}
		set
		{
			if (!string.IsNullOrEmpty(value) && Enum.TryParse<Constants.SortType>(value, ignoreCase: true, out var result))
			{
				groupByType = result;
			}
			else
			{
				groupByType = Constants.SortType.none;
			}
		}
	}

	[JsonIgnore]
	public Constants.SortType groupByType { get; set; } = Constants.SortType.none;

	public string orderBy
	{
		get
		{
			return orderByType.ToString();
		}
		set
		{
			if (!string.IsNullOrEmpty(value) && Enum.TryParse<Constants.SortType>(value, ignoreCase: true, out var result))
			{
				orderByType = result;
			}
			else
			{
				orderByType = Constants.SortType.sortOrder;
			}
			SetDefaultDirection();
		}
	}

	[JsonIgnore]
	public Constants.SortType orderByType { get; set; }

	public string order
	{
		get
		{
			return orderDirection?.ToString().ToLower();
		}
		set
		{
			if (!string.IsNullOrEmpty(value) && Enum.TryParse<Constants.OrderDirection>(value, ignoreCase: true, out var result))
			{
				orderDirection = result;
			}
			else
			{
				orderDirection = null;
			}
		}
	}

	[JsonIgnore]
	public Constants.OrderDirection? orderDirection { get; set; }

	[JsonIgnore]
	public bool isDefaultDirection => GetDefaultDirection() == orderDirection;

	[JsonIgnore]
	public bool SupportsDirection => GetDefaultDirection().HasValue;

	[JsonIgnore]
	public bool isFromKanbanColumn { get; set; }

	public void SetDefaultDirection()
	{
		orderDirection = GetDefaultDirection();
	}

	public Constants.OrderDirection? GetDefaultDirection()
	{
		if (!Enum.TryParse<Constants.SortType>(orderBy, ignoreCase: true, out var result))
		{
			return null;
		}
		switch (result)
		{
		case Constants.SortType.dueDate:
			return Constants.OrderDirection.Asc;
		case Constants.SortType.createdTime:
		case Constants.SortType.modifiedTime:
			return Constants.OrderDirection.Desc;
		case Constants.SortType.title:
		case Constants.SortType.assignee:
			return Constants.OrderDirection.Asc;
		case Constants.SortType.tag:
			return Constants.OrderDirection.Asc;
		case Constants.SortType.priority:
			return Constants.OrderDirection.Desc;
		default:
			return null;
		}
	}

	public override string ToString()
	{
		return "g:" + groupBy + ",o:" + orderBy + ",d:" + order;
	}

	public string ToSmartString()
	{
		string text = groupBy + "," + orderBy;
		if (!string.IsNullOrEmpty(order))
		{
			text = text + "," + order;
		}
		return text;
	}

	public static SortOption GetOptionInSmartString(string optionString)
	{
		if (string.IsNullOrEmpty(optionString))
		{
			return new SortOption
			{
				groupBy = "dueDate",
				orderBy = "dueDate"
			};
		}
		string[] array = optionString.Split(',');
		if (array.Length == 1)
		{
			return SortOptionUtils.GetSortOptionBySortType(Utils.LoadSortType(array[0]), inKanban: false);
		}
		if (array.Length >= 2)
		{
			SortOption sortOption = new SortOption
			{
				groupBy = array[0],
				orderBy = array[1]
			};
			if (array.Length >= 3 && !string.IsNullOrEmpty(array[2]))
			{
				sortOption.order = array[2];
			}
			return sortOption;
		}
		return new SortOption
		{
			groupBy = "dueDate",
			orderBy = "dueDate"
		};
	}

	public bool Equal(SortOption b)
	{
		if (b == null)
		{
			return false;
		}
		if (groupBy == b.groupBy && orderBy == b.orderBy)
		{
			return order == b.order;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is SortOption sortOption))
		{
			return false;
		}
		if (groupBy == sortOption.groupBy && orderBy == sortOption.orderBy)
		{
			return order == sortOption.order;
		}
		return false;
	}

	public bool ContainsSortType(Constants.SortType sortType)
	{
		if (groupByType != sortType)
		{
			return orderByType == sortType;
		}
		return true;
	}

	public string GetOldSortKey()
	{
		return "taskBy#" + groupBy + "_" + orderBy;
	}

	public string GetSortKey()
	{
		switch (orderByType)
		{
		case Constants.SortType.title:
		case Constants.SortType.assignee:
		case Constants.SortType.createdTime:
		case Constants.SortType.modifiedTime:
			return null;
		default:
		{
			string text = "taskBy#" + groupBy + "|{0}_" + orderBy;
			if (orderDirection.HasValue && !isDefaultDirection)
			{
				text += ((orderDirection == Constants.OrderDirection.Asc) ? "_ascending" : "_descending");
			}
			return text;
		}
		}
	}

	public bool IsNone()
	{
		if (string.IsNullOrEmpty(groupBy) || groupBy == "none")
		{
			if (!string.IsNullOrEmpty(orderBy))
			{
				return orderBy == "none";
			}
			return true;
		}
		return false;
	}

	public SortOption Copy()
	{
		SortOption sortOption = new SortOption
		{
			groupBy = groupBy,
			orderBy = orderBy,
			order = order
		};
		if (!sortOption.orderDirection.HasValue)
		{
			sortOption.SetDefaultDirection();
		}
		return sortOption;
	}

	public bool SpecialSort()
	{
		if ((!(groupBy == "sortOrder") || !(orderBy == "sortOrder")) && !(groupBy == "none"))
		{
			return !(groupBy == "assign");
		}
		return false;
	}
}
