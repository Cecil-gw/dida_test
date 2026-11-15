using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class ShareTagAllViewModel : PtfAllViewModel
{
	public ShareTagAllViewModel()
		: base(PtfType.Tag)
	{
		base.Title = Utils.GetString("SharedTags");
		base.Open = LocalSettings.Settings.AllShareTagOpened;
		base.InSubSection = true;
		base.Id = "ShareTag";
		base.CanDrag = false;
	}
}
