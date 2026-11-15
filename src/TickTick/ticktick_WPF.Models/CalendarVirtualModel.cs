namespace ticktick_WPF.Models;

public class CalendarVirtualModel
{
	public string id { get; set; }

	public string title { get; set; }

	public bool isAllDay { get; set; }

	public int start { get; set; }

	public int? dueSpan { get; set; }

	public int priority { get; set; }

	public int status { get; set; }

	public string color { get; set; }

	public long sortOrder { get; set; }

	public int addDay { get; set; }
}
