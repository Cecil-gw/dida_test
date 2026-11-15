using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Time;

public class RepeatViewModel : UpDownSelectViewModel
{
	public string Title { get; set; }

	public string Value { get; set; }

	public string Desc { get; set; }

	public bool IsSplit { get; set; }

	public RepeatViewModel()
	{
		IsSplit = true;
		base.IsEnable = false;
	}

	public RepeatViewModel(string title, string desc, string value, bool selected)
	{
		Title = title;
		Desc = desc;
		Value = value;
		base.Selected = selected;
	}
}
