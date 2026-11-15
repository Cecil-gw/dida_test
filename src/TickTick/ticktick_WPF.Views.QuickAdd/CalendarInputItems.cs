using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;

namespace ticktick_WPF.Views.QuickAdd;

public class CalendarInputItems : BaseInputItems<string>
{
	public readonly string AccountId;

	public CalendarInputItems(string accountId)
	{
		AccountId = accountId;
		LoadData();
	}

	protected override ObservableCollection<InputItemViewModel<string>> InitData()
	{
		ObservableCollection<InputItemViewModel<string>> observableCollection = new ObservableCollection<InputItemViewModel<string>>();
		foreach (BindCalendarModel item in GetCalendarsForAccount(CacheManager.GetBindCalendars(), AccountId))
		{
			observableCollection.Add(new InputItemViewModel<string>(item.Name, item.Id));
		}
		return observableCollection;
	}

	public static List<BindCalendarModel> GetCalendarsForAccount(IEnumerable<BindCalendarModel> calendars, string accountId, Func<BindCalendarModel, bool> canUseCalendar = null)
	{
		canUseCalendar = canUseCalendar ?? ((Func<BindCalendarModel, bool>)((BindCalendarModel calendar) => calendar.Accessible));
		return (from calendar in calendars
			where calendar != null && (string.IsNullOrEmpty(accountId) || calendar.AccountId == accountId) && canUseCalendar(calendar)
			orderby calendar.sortOrder
			select calendar).ToList();
	}
}
