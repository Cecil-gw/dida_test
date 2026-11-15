using System.Collections.Generic;
using ticktick_WPF.Models;

namespace ticktick_WPF.ViewModels;

public class HabitSectionListViewModel : HabitItemBaseViewModel
{
	public HabitSectionModel Section;

	public readonly List<HabitItemViewModel> Children = new List<HabitItemViewModel>();

	public new long SortOrder;

	private string _title;

	public override string Id => Section?.Id;

	public bool IsOpen
	{
		get
		{
			return Section?.IsOpen ?? true;
		}
		set
		{
			if (Section != null)
			{
				Section.IsOpen = value;
			}
			OnPropertyChanged("IsOpen");
		}
	}

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			OnPropertyChanged("Title");
		}
	}

	public bool IsOther { get; set; }

	public bool IsAdd { get; set; }

	public int Num { get; set; }

	public HabitSectionListViewModel(HabitSectionModel section)
	{
		Title = section.DisplayName;
		Section = section;
		SortOrder = Section?.SortOrder ?? 0;
		base.IsSection = true;
	}

	public HabitSectionListViewModel()
	{
	}
}
