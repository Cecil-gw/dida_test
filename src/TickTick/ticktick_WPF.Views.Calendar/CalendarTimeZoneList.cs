using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Calendar;

public class CalendarTimeZoneList : SortableListView<CalendarTimeZoneConfigItemViewModel>
{
	public CalendarTimeZoneList()
	{
		PopupContent = new ContentControl
		{
			Background = Brushes.Transparent,
			Margin = new Thickness(0.0),
			Content = new CalendarTimeZoneConfigItem
			{
				Container = 
				{
					Margin = new Thickness(-10.0, 0.0, 0.0, 0.0),
					Visibility = Visibility.Visible
				}
			}
		};
		ItemList.SetResourceReference(ItemsControl.ItemTemplateProperty, "CalendarTimeZoneItemTemplate");
		StartDragDiff = 0;
	}
}
