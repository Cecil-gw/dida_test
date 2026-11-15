using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class TagViewModel : BaseHidableViewModel
{
	public string Extra { get; } = string.Empty;

	public string Tag { get; } = string.Empty;

	public bool IsMore { get; }

	public string Color { get; } = string.Empty;

	public long SortOrder { get; set; }

	public TagViewModel(TagModel tag)
	{
		if (tag != null)
		{
			Tag = tag.GetDisplayName();
			Extra = tag.GetDisplayName();
			Color = ((!string.IsNullOrEmpty(tag.color)) ? tag.color : ThemeUtil.GetColorValue("ColorPrimary").ToString());
			SortOrder = tag.sortOrder;
		}
	}

	public TagViewModel(string name, string extra)
	{
		Tag = name;
		Extra = extra;
		IsMore = true;
		Color = ThemeUtil.GetColorValue("ColorPrimary").ToString();
		SortOrder = long.MaxValue;
	}

	public TagViewModel Copy()
	{
		return MemberwiseClone() as TagViewModel;
	}
}
