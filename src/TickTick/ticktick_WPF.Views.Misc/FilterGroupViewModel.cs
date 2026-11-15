using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class FilterGroupViewModel : SelectableItemViewModel
{
	public FilterGroupViewModel()
	{
		base.Id = "#allfilters";
		base.Title = Utils.GetString("Filter");
		base.ShowIcon = false;
		base.ShowIndent = true;
		base.ShowSelected = false;
		base.Selectable = false;
		base.IsSectionGroup = true;
		base.Open = true;
		base.IsParent = true;
		base.IsBold = true;
	}
}
