using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public sealed class BindAccountCalendarProjectIdentity : ProjectIdentity
{
	private readonly BindCalendarModel _defaultCalendar;

	public readonly BindCalendarAccountModel Account;

	public override string SortProjectId => null;

	public BindAccountCalendarProjectIdentity(BindCalendarAccountModel account)
	{
		Account = account;
		Id = account.Id;
		base.SortOption = SortOptionUtils.GetSortOptionBySortType(Constants.SortType.dueDate, inKanban: false);
		_defaultCalendar = SubscribeCalendarHelper.GetDefaultCalendar(account.Id);
		base.CanDrag = false;
	}

	public override bool IsCalendar()
	{
		return true;
	}

	public override string GetAccountId()
	{
		return Account.Id;
	}

	public override string GetProjectId()
	{
		return _defaultCalendar?.Id;
	}

	public override string GetProjectName()
	{
		return _defaultCalendar?.Name;
	}

	public override List<string> GetTags()
	{
		return new List<string>();
	}

	public override TimeData GetTimeData()
	{
		return TimeData.InitDefaultDuration();
	}

	public override string GetDisplayTitle()
	{
		return Account.DisplayName;
	}
}
