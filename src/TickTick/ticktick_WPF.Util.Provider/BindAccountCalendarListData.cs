using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class BindAccountCalendarListData : SortProjectData
{
	public BindCalendarAccountModel Account { get; set; }

	public BindAccountCalendarListData(BindCalendarAccountModel account)
	{
		base.ShowProjectSort = false;
		base.ShowCustomSort = false;
		Account = account;
		base.ShowLoadMore = false;
		base.EmptyTitle = Utils.GetString(account.Expired ? "CalendarExpiredTitle" : "CalendarA");
		base.EmptyContent = Utils.GetString(account.Expired ? "CalendarExpiredSummary" : "CalendarB");
		base.AddTaskHint = string.Empty;
		BindCalendarModel defaultCalendar = SubscribeCalendarHelper.GetDefaultCalendar(account.Id);
		base.AddTaskHint = ((defaultCalendar != null && !account.Expired) ? string.Format(Utils.GetString("AddAgendaTo"), defaultCalendar.Name) : string.Empty);
	}

	public override string GetTitle()
	{
		if (Account.Site == "feishu")
		{
			return Utils.GetString("FeishuCalendar");
		}
		return Account.DisplayName;
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		return new EmptyIconsSourceModel("NormalEmpty", new List<int> { 1, 2, 3, 4, 6 });
	}
}
