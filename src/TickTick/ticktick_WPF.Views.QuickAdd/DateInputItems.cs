using System;
using System.Collections.ObjectModel;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.QuickAdd;

public class DateInputItems : BaseInputItems<DateTime>
{
	public DateInputItems()
	{
		LoadData();
	}

	protected override ObservableCollection<InputItemViewModel<DateTime>> InitData()
	{
		ObservableCollection<InputItemViewModel<DateTime>> observableCollection = new ObservableCollection<InputItemViewModel<DateTime>>();
		observableCollection.Add(new InputItemViewModel<DateTime>(DateUtils.FormatMonthDay(DateTime.Today, DateFormatLen.Long) + "  " + Utils.GetString("Today"), Utils.GetString("Today"), DateTime.Today));
		observableCollection.Add(new InputItemViewModel<DateTime>(DateUtils.FormatMonthDay(DateTime.Today.AddDays(1.0), DateFormatLen.Long) + "  " + Utils.GetString("Tomorrow"), Utils.GetString("Tomorrow"), DateTime.Today.AddDays(1.0)));
		observableCollection.Add(new InputItemViewModel<DateTime>(DateUtils.FormatMonthDay(DateTime.Today.AddDays(7.0), DateFormatLen.Long) + "  " + Utils.GetString("Next") + (Utils.IsDida() ? string.Empty : " ") + DateUtils.FormatWeekDayName(DateTime.Today.AddDays(7.0)), Utils.GetString("Next") + (Utils.IsDida() ? string.Empty : " ") + DateUtils.FormatWeekDayName(DateTime.Today.AddDays(7.0)), DateTime.Today.AddDays(7.0)));
		observableCollection.Add(new InputItemViewModel<DateTime>(DateUtils.FormatMonthDay(DateTime.Today.AddMonths(1), DateFormatLen.Long) + "  " + Utils.GetString("NextMonth"), Utils.GetString("NextMonth"), DateTime.Today.AddMonths(1)));
		return observableCollection;
	}
}
