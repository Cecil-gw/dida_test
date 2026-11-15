using System.Windows;

namespace ticktick_WPF.Views.CountDown.ViewModels;

public class StyleSelectionResult
{
	public string Style { get; set; } = "normal";

	public string[] StyleColors { get; set; }

	public string PresetImage { get; set; }

	public string LocalImagePath { get; set; }

	public string AttachmentId { get; set; }

	public Rect ClipRect { get; set; }
}
