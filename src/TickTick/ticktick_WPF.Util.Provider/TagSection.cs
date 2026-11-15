using System.Collections.Generic;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Util.Provider;

public class TagSection : Section
{
	public TagSection(string tag)
	{
		base.Name = tag;
	}

	public override string GetTag()
	{
		if (base.Name == Utils.GetString("NoTags"))
		{
			return string.Empty;
		}
		return TagSerializer.ToJsonContent(new List<string> { base.Name });
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		return true;
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return true;
	}
}
