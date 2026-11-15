using TickTickModels;

namespace ticktick_WPF.Util;

public class ShowCountDownModel : BaseModel
{
	public string UserId { get; set; }

	public string ProjectId { get; set; }

	public bool ShowCountDown { get; set; }
}
