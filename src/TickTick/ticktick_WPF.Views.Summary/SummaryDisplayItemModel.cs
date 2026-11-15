using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Summary;

public class SummaryDisplayItemModel : BaseViewModel
{
	public string Key { get; set; }

	public long SortOrder { get; set; }

	public string Style { get; set; }

	public bool Enabled { get; set; }

	public SummaryDisplayItemModel()
	{
	}

	public SummaryDisplayItemModel(string key, long sortOrder, string style, bool enabled)
	{
		Key = key;
		SortOrder = sortOrder;
		Style = style;
		Enabled = enabled;
	}
}
