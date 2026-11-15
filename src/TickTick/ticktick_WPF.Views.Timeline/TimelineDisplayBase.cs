using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Timeline;

public class TimelineDisplayBase : BaseViewModel
{
	public virtual string Title { get; set; }

	public virtual object Data { get; set; }

	public virtual DisplayType Type { get; set; }
}
