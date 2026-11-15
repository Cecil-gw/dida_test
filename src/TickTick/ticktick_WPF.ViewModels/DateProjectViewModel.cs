using System;
using System.Collections.Generic;
using System.Globalization;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;

namespace ticktick_WPF.ViewModels;

public class DateProjectViewModel : ProjectItemViewModel, IDroppable
{
	public string ProjectId => string.Empty;

	public DateTime? DefaultDate => null;

	public bool IsCompleted => false;

	public bool IsAbandoned => false;

	public List<string> Tags => new List<string>();

	public int Priority => 0;

	public bool Multiple => false;

	public bool IsDeleted => false;

	public DateProjectViewModel(string dateStamp)
	{
		base.Id = dateStamp;
		base.Title = GetTitle(dateStamp);
		base.CanDrag = true;
		base.CanDrop = true;
		base.IsPtfItem = true;
		ListType = ProjectListType.Project;
		base.ViewMode = "list";
	}

	private static string GetTitle(string stamp)
	{
		return DateUtils.FormatYearMonthDay(DateTime.ParseExact(stamp, "yyyyMMdd", CultureInfo.InvariantCulture), DateFormatLen.Short);
	}

	public override ProjectIdentity GetIdentity()
	{
		return new DateProjectIdentity(base.Id);
	}
}
