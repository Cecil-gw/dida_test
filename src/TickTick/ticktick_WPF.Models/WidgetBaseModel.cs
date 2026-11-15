using TickTickModels;

namespace ticktick_WPF.Models;

public class WidgetBaseModel : BaseModel
{
	public string Id { get; set; }

	public string UserId { get; set; }

	public string ThemeId { get; set; }

	public float Opacity { get; set; } = 0.7f;

	public string DisplayOption { get; set; }

	public bool AutoHide { get; set; }

	public double Left { get; set; }

	public double Top { get; set; }

	public double Width { get; set; }

	public double Height { get; set; }

	public bool IsLocked { get; set; }
}
