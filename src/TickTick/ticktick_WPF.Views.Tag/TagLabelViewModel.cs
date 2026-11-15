using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Tag;

public class TagLabelViewModel : BaseViewModel
{
	public bool IsAdd { get; private set; }

	public bool Adding { get; set; }

	public long SortOrder { get; set; }

	public string Color { get; }

	public bool Editable { get; set; } = true;

	public string Tag { get; }

	public string Title { get; } = "";

	private TagLabelViewModel()
	{
	}

	public TagLabelViewModel(string tag)
	{
		Tag = tag;
		Title = CacheManager.GetTags().FirstOrDefault((TagModel t) => t.name == tag)?.GetDisplayName() ?? tag ?? "";
		Color = ThemeUtil.GetColorValue("ColorPrimary").ToString();
		IsAdd = false;
		SortOrder = 0L;
	}

	public TagLabelViewModel(TagModel tag)
	{
		Tag = tag.name;
		Title = tag.GetDisplayName() ?? "";
		Color = ((!string.IsNullOrEmpty(tag.color)) ? tag.color : ThemeUtil.GetColorValue("ColorPrimary").ToString());
		IsAdd = false;
		SortOrder = tag.sortOrder;
	}

	public static TagLabelViewModel BuildAddModel()
	{
		return new TagLabelViewModel
		{
			IsAdd = true,
			SortOrder = long.MaxValue
		};
	}
}
