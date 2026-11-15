using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class TagGroupViewModel : SelectableItemViewModel
{
	public TagGroupViewModel()
	{
		base.Title = Utils.GetString("tag");
		base.Id = "#alltags";
		base.ShowIcon = false;
		base.ShowIndent = true;
		base.Selectable = true;
		base.IsSectionGroup = true;
		base.Open = true;
		base.IsParent = true;
		base.IsBold = true;
		base.UseCircleCheck = true;
	}
}
