using System.Windows.Media;

namespace ticktick_WPF.Views.Widget;

public class CountDownListItem
{
	public string CountdownId { get; set; }

	public string Title { get; set; }

	public ImageSource Icon { get; set; }

	public string Days { get; set; }

	public int DayFontSize { get; set; } = 22;

	public bool IsShowDayUnit { get; set; }

	public bool IsPassed { get; set; }

	public bool IsNumber { get; set; }
}
