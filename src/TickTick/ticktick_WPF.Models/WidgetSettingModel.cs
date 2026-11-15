using System;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class WidgetSettingModel : BaseModel
{
	public string id { get; set; }

	public string type { get; set; }

	public string identity { get; set; }

	public string userId { get; set; }

	public string themeId { get; set; }

	public float opacity { get; set; }

	public string displayOption { get; set; }

	public bool autoHide { get; set; }

	public string sortType { get; set; }

	public string groupType { get; set; }

	public string order { get; set; }

	public bool hideComplete { get; set; }

	public bool? showCountDown { get; set; }

	public double left { get; set; }

	public double top { get; set; }

	public double width { get; set; }

	public double height { get; set; }

	public bool isLocked { get; set; }
}
