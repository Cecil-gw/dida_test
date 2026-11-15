using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views.Kanban.Item;

public class ListItemAddView : Border
{
	private QuickAddView _quickAddView;

	private TaskListView _listParent;

	public ListItemAddView()
	{
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_quickAddView?.InputScrollViewer.RemoveHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnQuickAddPreviewMouseWheel));
		_listParent = null;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		_listParent = Utils.FindParent<TaskListView>(this);
		if (_listParent == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (!(dataContext is DisplayItemModel { AddViewModel: not null } m))
		{
			return;
		}
		if (!(_listParent?.ViewModel.ProjectIdentity is ColumnProjectIdentity columnProjectIdentity))
		{
			_listParent?.RemoveAddItem(m);
			return;
		}
		_quickAddView = new QuickAddView(new ColumnProjectIdentity(columnProjectIdentity.Project, columnProjectIdentity.ColumnId), QuickAddView.Scenario.Kanban, "light", focus: false, m.Section)
		{
			Margin = new Thickness(12.0, 4.0, 12.0, 4.0),
			MaxHeight = 0.0
		};
		_quickAddView.TaskAdded += OnTaskAdded;
		_quickAddView.TitleText.TextChanged += OnTextChanged;
		_quickAddView.BatchTaskAdded += OnBatchTaskAdded;
		_quickAddView.InputScrollViewer.AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnQuickAddPreviewMouseWheel), handledEventsToo: true);
		_quickAddView.MaxHeight = 220.0;
		_quickAddView.TitleText.EditBox.LostFocus += OnQuickAddLostFocus;
		if (m.AddViewModel.Text != null)
		{
			_quickAddView.TitleText.EditBox.Text = m.AddViewModel.Text;
		}
		Child = _quickAddView;
		await _quickAddView.FocusEndAsync();
		await Task.Delay(20);
		_listParent.ScrollToItem(m);
	}

	private void OnQuickAddPreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (e.Handled && sender is ScrollViewer scrollViewer)
		{
			scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (double)((float)e.Delta / 2f));
		}
	}

	private void OnTextChanged(object sender, string e)
	{
		if (base.DataContext is DisplayItemModel { AddViewModel: not null } displayItemModel)
		{
			displayItemModel.AddViewModel.Text = e;
		}
	}

	public bool CloseQuickAddActionPopups()
	{
		return _quickAddView?.CloseActionPopups() ?? false;
	}

	private async void OnQuickAddLostFocus(object sender, RoutedEventArgs e)
	{
		QuickAddView quickAddView = _quickAddView;
		if (quickAddView != null && quickAddView.IsLostFocus())
		{
			quickAddView.TitleText.EditBox.LostFocus -= OnQuickAddLostFocus;
			Task task = quickAddView.TryAddTaskOnLostFocus();
			if (base.DataContext is DisplayItemModel model)
			{
				Utils.FindParent<TaskListView>(this)?.RemoveAddItem(model);
			}
			await task;
		}
	}

	private void OnBatchTaskAdded(object sender, List<TaskModel> e)
	{
		if (base.DataContext is DisplayItemModel { AddViewModel: not null } displayItemModel)
		{
			displayItemModel.AddViewModel.Text = null;
		}
	}

	private void OnTaskAdded(object sender, TaskModel e)
	{
		if (base.DataContext is DisplayItemModel { AddViewModel: not null } displayItemModel)
		{
			displayItemModel.AddViewModel.Text = null;
		}
	}
}
