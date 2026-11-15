using System;
using System.Collections.Generic;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class DateListData : SortProjectData
{
	public string DateStamp { get; set; }

	public DateListData(string stamp)
	{
		DateTime date = DateTime.ParseExact(stamp, "yyyyMMdd", null);
		base.AddTaskHint = string.Format(Utils.GetString("CenterAddTaskDateTextBoxPreviewText"), DateUtils.FormatTimeDesc(date, isAllDay: true, showDesc: false), Utils.GetString("Inbox"), Utils.GetString("Task").ToLower());
		TitleInProjectGroup = Utils.GetString("Inbox");
		base.ShowProjectSort = false;
		base.ShowLoadMore = true;
		DateStamp = stamp;
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		if (!allCompleted)
		{
			return new EmptyIconsSourceModel("NormalEmpty", new List<int> { 1, 2, 3, 4, 6 });
		}
		return new EmptyIconsSourceModel("TaskCompletedEmpty");
	}

	public override string GetEmptyTitle(bool allCompleted)
	{
		return Utils.GetString(allCompleted ? "TaskAllCompleted" : "NoTaskInDate");
	}

	public override string GetEmptyContent(bool allCompleted)
	{
		return Utils.GetString("TaskItEasy");
	}

	public override string GetTitle()
	{
		return new DateProjectIdentity(DateStamp).GetDisplayTitle();
	}
}
