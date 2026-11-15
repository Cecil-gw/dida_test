using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class DateEditDialog : ConditionEditDialogBase<string>
{
	private const string DaysLater = "dayslater";

	private const string Days = "days";

	private const string DaysFromToday = "daysfromtoday";

	private const string Span = "span";

	private List<string> _originDates;

	private int _version = 1;

	public event EventHandler<List<string>> OnSelectedDateChanged;

	public DateEditDialog(int version = 1)
		: this(showAll: false, version)
	{
	}

	private DateEditDialog(bool showAll, int version = 1)
		: this(showAll, new List<string>(), LogicType.Or, version)
	{
	}

	public DateEditDialog(bool showAll, List<string> selectedDates, LogicType logicType, int version, bool useDefaultMinWidth = true)
	{
		InitializeComponent();
		_originDates = selectedDates.ToList();
		_version = version;
		if (useDefaultMinWidth)
		{
			base.MinWidth = 452.0;
		}
		base.ViewModel = new FilterConditionViewModel
		{
			Type = CondType.Date,
			ShowAll = showAll,
			ItemsSource = InitDateItems(selectedDates),
			IsAllSelected = (selectedDates.Count == 0),
			Logic = logicType,
			SupportedLogic = new List<LogicType>
			{
				LogicType.Not,
				LogicType.Or
			}
		};
		base.OnSelectedValueChanged += delegate
		{
			this.OnSelectedDateChanged?.Invoke(this, base.ViewModel.IsAllSelected ? new List<string>() : GetSelectedValues());
		};
	}

	private ObservableCollection<FilterListItemViewModel> InitDateItems(List<string> selectedDates)
	{
		ObservableCollection<FilterListItemViewModel> observableCollection = new ObservableCollection<FilterListItemViewModel>
		{
			FilterListItemViewModel.BuildDateItem(Utils.GetString("NoDate"), "nodue", "Ic.CalendarOM.NoDate"),
			FilterListItemViewModel.BuildDateItem(Utils.GetString("overdue"), "overdue", "Ic.CalendarOM.Expired"),
			FilterListItemViewModel.BuildDateItem(Utils.GetString("RecurringDate"), "recurring", "Ic.TaskList.Repeat"),
			FilterListItemViewModel.BuildDateItem(Utils.GetString("Today"), "today", "Ic.ProjectList.Date" + DateTime.Today.Day),
			FilterListItemViewModel.BuildDateItem(Utils.GetString("Tomorrow"), "tomorrow", "Ic.ProjectList.Tomorrow"),
			FilterListItemViewModel.BuildDateItem(Utils.GetString("ThisWeek"), "thisweek", "Ic.ProjectList." + DateTime.Today.DayOfWeek),
			FilterListItemViewModel.BuildDateItem(Utils.GetString("NextWeek"), "nextweek", "Ic.TaskOM.NextWeek"),
			FilterListItemViewModel.BuildDateItem(Utils.GetString("ThisMonth"), "thismonth", "Ic.TaskOM.TodayEvening"),
			FilterListItemViewModel.BuildDateItem(Utils.GetString("NextMonth"), string.Format("offset({0})", "1M"), "Ic.TaskOM.NextMonth")
		};
		FilterListItemViewModel filterListItemViewModel = FilterListItemViewModel.BuildDateItem(Utils.GetString("Duration"), $"span({0}~{7})", null, "Ic.TaskDetail.Time");
		filterListItemViewModel.DisplayType = FilterItemDisplayType.Span;
		GetDaysSpanValue(filterListItemViewModel, selectedDates);
		observableCollection.Add(filterListItemViewModel);
		foreach (FilterListItemViewModel item in observableCollection)
		{
			if (!item.Selected && selectedDates.Contains(item.Value))
			{
				item.Selected = true;
			}
		}
		return observableCollection;
	}

	private static void GetDaysSpanValue(FilterListItemViewModel model, List<string> selectedDates)
	{
		model.DaysFrom = 0;
		model.DaysTo = 7;
		string text = selectedDates.FirstOrDefault((string date) => date.StartsWith("span"));
		if (text != null)
		{
			model.Selected = true;
			model.Value = text;
			(int?, int?) spanPairInRule = FilterUtils.GetSpanPairInRule(text);
			int? item = spanPairInRule.Item1;
			int? item2 = spanPairInRule.Item2;
			model.DaysFrom = item;
			model.DaysTo = item2;
			return;
		}
		int? daysFrom = null;
		int? daysTo = null;
		string text2 = selectedDates.FirstOrDefault((string date) => date.EndsWith("daysfromtoday") && date.StartsWith("-"));
		if (text2 != null)
		{
			int value = GetDaysValue(text2, FilterItemDisplayType.NDaysAgo) * -1;
			daysFrom = value;
			daysTo = value;
		}
		string text3 = selectedDates.FirstOrDefault((string date) => date.EndsWith("daysfromtoday") && !date.StartsWith("-"));
		if (text3 != null)
		{
			int daysValue = GetDaysValue(text3, FilterItemDisplayType.AfterNDays);
			daysFrom = daysValue;
			daysTo = daysValue;
		}
		string text4 = selectedDates.FirstOrDefault((string date) => date.EndsWith("dayslater"));
		if (text4 != null)
		{
			int daysValue2 = GetDaysValue(text4, FilterItemDisplayType.NDaysLater);
			daysFrom = daysValue2;
			daysTo = null;
		}
		string text5 = selectedDates.FirstOrDefault((string date) => date.EndsWith("days"));
		if (text5 != null)
		{
			int daysValue3 = GetDaysValue(text5, FilterItemDisplayType.NextNDays);
			daysFrom = 0;
			daysTo = daysValue3 - 1;
		}
		List<string> list = new List<string>();
		list.Add(text5);
		list.Add(text4);
		list.Add(text3);
		list.Add(text2);
		list.RemoveAll((string s) => s == null);
		string value2 = list.Join(";");
		if (!string.IsNullOrEmpty(value2))
		{
			model.Selected = true;
			model.Value = value2;
			model.DaysFrom = daysFrom;
			model.DaysTo = daysTo;
		}
	}

	private static int GetDaysValue(string rule, FilterItemDisplayType type)
	{
		string oldValue = string.Empty;
		switch (type)
		{
		case FilterItemDisplayType.NextNDays:
			oldValue = "days";
			break;
		case FilterItemDisplayType.NDaysLater:
			oldValue = "dayslater";
			break;
		case FilterItemDisplayType.AfterNDays:
			oldValue = "daysfromtoday";
			break;
		case FilterItemDisplayType.NDaysAgo:
			oldValue = "daysfromtoday";
			rule = rule.Replace("-", string.Empty);
			break;
		}
		if (rule != null)
		{
			int.TryParse(rule.Replace(oldValue, string.Empty), out var result);
			return result;
		}
		return 7;
	}

	protected override void SyncOriginal()
	{
		_originDates = GetSelectedValues().ToList();
	}

	protected override bool CanSave()
	{
		if (GetSelectedValues().Count <= 0)
		{
			return base.CanSave();
		}
		return true;
	}

	public override void Restore()
	{
		RestoreSetSelected();
		this.OnSelectedDateChanged?.Invoke(this, _originDates);
	}

	private void RestoreSetSelected()
	{
		foreach (FilterListItemViewModel item in base.ViewModel.ItemsSource)
		{
			item.Selected = _originDates.Contains(item.Value.ToString());
		}
	}

	protected override List<string> GetSelectedValues()
	{
		if (base.ViewModel != null)
		{
			if (base.ViewModel.IsAllSelected)
			{
				return new List<string>();
			}
			List<string> list = new List<string>();
			{
				foreach (FilterListItemViewModel item in base.ViewModel.ItemsSource)
				{
					if (!item.Selected)
					{
						continue;
					}
					string text = (string)item.Value;
					if (text.Contains(";"))
					{
						IEnumerable<string> collection = from p in text.Split(';')
							where !string.IsNullOrEmpty(p)
							select p;
						list.AddRange(collection);
					}
					else
					{
						list.Add(text);
					}
				}
				return list;
			}
		}
		return new List<string>();
	}
}
