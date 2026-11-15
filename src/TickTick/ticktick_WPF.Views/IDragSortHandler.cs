using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views;

public interface IDragSortHandler
{
	bool CanStart(object item, DependencyObject source);

	bool Start(ItemsControl control, object item);

	void Move(ItemsControl control, object item, object target, bool after);

	void Complete(ItemsControl control, object item, bool cancelled);
}
