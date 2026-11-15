using System;
using System.ComponentModel;
using System.Windows;
using SQLite;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Models;

[Serializable]
public class CalendarWidgetModel : BaseModel
{
	public string id { get; set; }

	public string userId { get; set; }

	public string themeId { get; set; }

	public float opacity { get; set; }

	public string displayOption { get; set; }

	public bool autoHide { get; set; }

	public bool hideComplete { get; set; }

	public double left { get; set; }

	public double top { get; set; }

	public double width { get; set; }

	public double height { get; set; }

	public bool isLocked { get; set; }

	[NotNull]
	[DefaultValue("0")]
	public UniqueWidgetMode mode { get; set; }

	public bool isShowUnit { get; set; } = true;

	public string activeViewId { get; set; } = "";

	public bool isExpanded { get; set; }

	internal void NormalizeAIWidget()
	{
		Size value = WidgetConstants.GetDefaultSize(WidgetType.AI).Value;
		themeId = ((themeId == "dark") ? "dark" : "light");
		opacity = ((float.IsNaN(opacity) || float.IsInfinity(opacity)) ? 1f : Math.Max(0.2f, Math.Min(1f, opacity)));
		width = ((double.IsNaN(width) || double.IsInfinity(width)) ? value.Width : Math.Max(value.Width, Math.Min(1200.0, width)));
		height = ((double.IsNaN(height) || double.IsInfinity(height)) ? value.Height : Math.Max(value.Height, Math.Min(1200.0, height)));
	}
}
