using System.Windows;
using System.Windows.Controls.Primitives;

namespace ticktick_WPF.Views.Time;

public struct SetDateDialogArgs
{
	public bool CalendarMode;

	public bool IsNote;

	public bool ItemMode;

	public UIElement Target;

	public double HOffset;

	public double VOffset;

	public PlacementMode Placement;

	public bool ShowQuickDate;

	public bool CanSkip;

	public bool ShowRemind;

	public bool ShowAnnoyingAlert;

	public SetDateDialogArgs(bool calendarMode = false, bool isNote = false, bool itemMode = false, UIElement target = null, double hOffset = 0.0, double vOffset = 0.0, PlacementMode placement = PlacementMode.Relative, bool showQuickDate = true, bool canSkip = true, bool showRemind = true, bool showAnnoyingAlert = true)
	{
		CalendarMode = calendarMode;
		IsNote = isNote;
		ItemMode = itemMode;
		Target = target;
		HOffset = hOffset;
		VOffset = vOffset;
		Placement = placement;
		ShowQuickDate = showQuickDate;
		CanSkip = canSkip;
		ShowRemind = showRemind;
		ShowAnnoyingAlert = showAnnoyingAlert;
	}
}
