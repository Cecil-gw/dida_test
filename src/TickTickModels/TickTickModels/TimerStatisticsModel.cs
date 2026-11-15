using SQLite;

namespace TickTickModels;

public class TimerStatisticsModel
{
	[PrimaryKey]
	public string Key { get; set; }

	public string Value { get; set; }
}
