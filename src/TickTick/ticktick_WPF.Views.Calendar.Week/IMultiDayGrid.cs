using System.Windows.Input;

namespace ticktick_WPF.Views.Calendar.Week;

public interface IMultiDayGrid
{
	void OnDragEnter();

	void OnDragLeave();

	void OnDragOver(MouseEventArgs e);

	void OnDragDrop();
}
