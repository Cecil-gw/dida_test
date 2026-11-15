using System.Collections.ObjectModel;

namespace ticktick_WPF.Entity.Calendar;

internal class YearMonthRowItem : YearViewItem
{
	public ObservableCollection<YearViewMonthData> Items { get; set; } = new ObservableCollection<YearViewMonthData>();
}
