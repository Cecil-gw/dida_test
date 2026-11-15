using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Habit;

public class HabitIconViewModel : BaseViewModel
{
	private bool _selected;

	public string IconName { get; set; }

	public string IconUrl { get; set; }

	public string Color { get; set; }

	public string DescText { get; set; }

	public bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			_selected = value;
			OnPropertyChanged("Selected");
		}
	}

	public HabitIconViewModel()
	{
	}

	public HabitIconViewModel(string iconRes)
	{
		IconName = iconRes;
		if (!iconRes.StartsWith("txt"))
		{
			IconUrl = "../../Assets/Habits/" + iconRes.ToLower() + ".png";
		}
		DescText = HabitUtils.GetHabitHintText(iconRes);
		Color = HabitUtils.IconInfos[iconRes].IconOutlineColor;
	}

	public static List<HabitIconViewModel> BuildItems()
	{
		List<HabitIconViewModel> list = new List<HabitIconViewModel>();
		foreach (string key in HabitUtils.IconInfos.Keys)
		{
			if (!(key == "habit_control_temper") && !(key == "habit_roll_eyes"))
			{
				list.Add(new HabitIconViewModel(key));
			}
		}
		return list;
	}
}
