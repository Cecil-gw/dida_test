using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.ViewModels;

public class ListItemData : UpDownSelectViewModel
{
	public object Key { get; set; }

	public string Value { get; set; }

	public ListItemData(object key, string value)
	{
		Key = key;
		Value = value;
	}
}
