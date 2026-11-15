using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Config;

public class BindAccountViewModel : BaseViewModel
{
	public string Id { get; set; }

	public string Account { get; set; }

	public string DisplayName { get; set; }

	public BindCalendarAccountModel AccountModel { get; set; }

	public List<BindCalendarViewModel> Calendars { get; set; }

	public string Kind { get; set; }

	public bool IsExpired { get; set; }

	public static BindAccountViewModel Build(BindCalendarAccountModel model)
	{
		BindAccountViewModel bindAccountViewModel = new BindAccountViewModel
		{
			Id = model.Id,
			Account = model.Account,
			AccountModel = model,
			Kind = model.Kind,
			DisplayName = model.DisplayName
		};
		if (model.Site == "feishu")
		{
			bindAccountViewModel.Account = Utils.GetString("FeishuCalendar");
		}
		else if (!string.IsNullOrEmpty(model.Description))
		{
			bindAccountViewModel.Account = model.Description;
		}
		bindAccountViewModel.IsExpired = model.Expired;
		List<BindCalendarViewModel> list = new List<BindCalendarViewModel>();
		if (model.Calendars != null && model.Calendars.Any())
		{
			model.Calendars.Sort((BindCalendarModel a, BindCalendarModel b) => a.sortOrder.CompareTo(b.sortOrder));
			foreach (BindCalendarModel calendar in model.Calendars)
			{
				list.Add(new BindCalendarViewModel
				{
					Id = calendar.Id,
					Name = calendar.Name,
					Color = calendar.Color,
					StatusItems = new ObservableCollection<ComboBoxViewModel>
					{
						new ComboBoxViewModel("show", Utils.GetString("Show"), 32.0)
						{
							Selected = (calendar.Show == "show" || string.IsNullOrEmpty(calendar.Show))
						},
						new ComboBoxViewModel("calendar", Utils.GetString("ShowOnlyInCalendar"), 32.0)
						{
							Selected = (calendar.Show == "calendar")
						},
						new ComboBoxViewModel("hidden", Utils.GetString("Hide"), 32.0)
						{
							Selected = (calendar.Show == "hidden")
						}
					}
				});
			}
		}
		bindAccountViewModel.Calendars = list;
		return bindAccountViewModel;
	}

	public bool IsBindAccount()
	{
		if (!(Kind == "caldav") && !(Kind == "api") && !(Kind == "exchange"))
		{
			return Kind == "icloud";
		}
		return true;
	}
}
