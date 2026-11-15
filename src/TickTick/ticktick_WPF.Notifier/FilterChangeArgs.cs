using ticktick_WPF.Models;

namespace ticktick_WPF.Notifier;

public class FilterChangeArgs
{
	public FilterModel Filter;

	public bool RuleChanged;

	public string deleteId;
}
