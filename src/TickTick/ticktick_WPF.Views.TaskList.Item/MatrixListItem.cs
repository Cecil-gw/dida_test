using System.Windows;

namespace ticktick_WPF.Views.TaskList.Item;

public class MatrixListItem : TaskListItem
{
	public MatrixListItem()
		: base(inMatrix: true, inPreview: false)
	{
		base.RequestBringIntoView += OnBringIntoView;
	}

	private void OnBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		e.Handled = true;
	}
}
