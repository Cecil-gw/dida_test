using System;
using System.Windows.Input;

namespace ticktick_WPF.Views.Calendar;

public interface IDragBarEvent
{
	bool OnSelection();

	DateTime? DragSideOver(MouseEventArgs e, CalendarDisplayViewModel model, bool dragLeft);

	void SetBarDragging(bool dragging);

	bool GetBarDragging();
}
