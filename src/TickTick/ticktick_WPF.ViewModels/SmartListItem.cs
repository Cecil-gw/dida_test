using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace ticktick_WPF.ViewModels;

public class SmartListItem
{
	public int id { get; set; }

	public string smartListTitle { get; set; }

	public int smartListStatus { get; set; }

	public ObservableCollection<ComboBoxItem> comboBoxItemList { get; set; }
}
