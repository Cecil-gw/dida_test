using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ticktick_WPF.Util;

public class ListViewDropManage
{
	public delegate Point GetPositionDelegate(IInputElement element);

	public static int GetCurrentIndex(ListView listview, GetPositionDelegate getPosition)
	{
		int result = -1;
		for (int i = 0; i < listview.Items.Count; i++)
		{
			ListViewItem listViewItem = Utils.GetListViewItem(listview, i);
			if (listViewItem != null && IsMouseOverTarget(listViewItem, getPosition))
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public static bool IsMouseOverTarget(Visual target, GetPositionDelegate getPosition)
	{
		Rect descendantBounds = VisualTreeHelper.GetDescendantBounds(target);
		Point point = getPosition((IInputElement)target);
		return descendantBounds.Contains(point);
	}
}
