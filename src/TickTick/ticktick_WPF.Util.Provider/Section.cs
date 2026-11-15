using System;
using System.Collections.Generic;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class Section
{
	public const string OutdatedId = "outdated";

	public static Section NoAssignee = new Section(Utils.GetString("NotAssigned"), -2147483647, null, "noassignee", "-1");

	private bool _canSort;

	private bool _canSwitch;

	public string Name { get; set; }

	public long Ordinal { get; set; }

	public long Ordinal2 { get; set; }

	public int Count { get; set; }

	public DateTime? SectionDate { get; set; }

	public string SectionId { get; set; }

	public string SectionUserId { get; set; }

	public bool Customized { get; set; }

	public string ProjectId { get; set; }

	public List<DisplayItemModel> Children { get; set; } = new List<DisplayItemModel>();

	public string SectionEntityId { get; set; }

	public Section(bool canSort = false, bool canSwitch = false)
	{
		_canSort = canSort;
		_canSwitch = canSwitch;
	}

	public Section(string name, int oridinal, DateTime? sectionDate, string sectionId, string sectionUserId = "", bool canSort = false, bool canSwitch = false)
	{
		Name = name;
		Ordinal = oridinal;
		SectionDate = ((!sectionDate.HasValue || Utils.IsEmptyDate(sectionDate)) ? default(DateTime) : sectionDate.Value);
		SectionId = sectionId;
		SectionUserId = sectionUserId;
		_canSort = canSort;
		_canSwitch = canSwitch;
	}

	public virtual bool CanSwitch(DisplayType displayType)
	{
		return _canSwitch;
	}

	public virtual bool CanSort(Constants.SortType orderBy)
	{
		return _canSort;
	}

	public virtual int GetPriority()
	{
		return TaskDefaultDao.GetDefaultSafely().Priority;
	}

	public virtual string GetProjectId()
	{
		return string.Empty;
	}

	public virtual DateTime? GetStartDate()
	{
		return SectionDate;
	}

	public virtual int GetTaskStatus()
	{
		return 0;
	}

	public virtual string GetAssignee()
	{
		return string.Empty;
	}

	public virtual string GetTag()
	{
		return string.Empty;
	}
}
