using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CheckList;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.MarkDown;

public class ImmersiveContent : UserControl, IComponentConnector
{
	private readonly Stack<ProjectTask> _navigateStack = new Stack<ProjectTask>();

	private readonly ScaleTransform _scaleTransform = new ScaleTransform(1.0, 1.0);

	private bool _isPreparing;

	internal ImmersiveContent Root;

	internal ScrollViewer TaskDetailScrollViewer;

	internal Grid ImmersiveGrid;

	internal TaskDetailImmerseView TaskDetail;

	internal Grid EditorMenuControl;

	internal EditorMenu EditorMenu;

	private bool _contentLoaded;

	public ImmersiveContent()
	{
		InitializeComponent();
		InitEvent();
		base.Unloaded += OnUnloaded;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		EditorMenu.EditorAction -= OnEditorAction;
		EditorMenu.HighlightColorRequested -= OnHighlightColorRequested;
		EditorMenu.ExitImmersive -= OnExitClick;
		EditorMenu.NavigateBack -= OnNavigateBack;
		TaskDetail.PreviewKeyDown -= OnDetailKeyDown;
		UnbindContentEditorEvents();
		TaskDetail.ScrollViewer.PreviewMouseWheel -= OnDetailScroll;
		TaskDetail.Dispose();
	}

	private void InitEvent()
	{
		EditorMenu.EditorAction -= OnEditorAction;
		EditorMenu.EditorAction += OnEditorAction;
		EditorMenu.HighlightColorRequested -= OnHighlightColorRequested;
		EditorMenu.HighlightColorRequested += OnHighlightColorRequested;
		EditorMenu.ExitImmersive -= OnExitClick;
		EditorMenu.ExitImmersive += OnExitClick;
		EditorMenu.NavigateBack -= OnNavigateBack;
		EditorMenu.NavigateBack += OnNavigateBack;
		EditorMenu.MoreOptionGrid.Visibility = Visibility.Collapsed;
		TaskDetail.PreviewKeyDown -= OnDetailKeyDown;
		TaskDetail.PreviewKeyDown += OnDetailKeyDown;
		RebindContentEditorEvents();
		TaskDetail.ScrollViewer.PreviewMouseWheel -= OnDetailScroll;
		AddScrollHandler();
		TaskDetail.NavigateTask -= OnNavigateTask;
		TaskDetail.NavigateTask += OnNavigateTask;
		TaskDetail.EscKeyUp -= OnEsc;
		TaskDetail.EscKeyUp += OnEsc;
		TaskDetail.CheckItemsDeleted -= OnCheckItemDeleted;
		TaskDetail.CheckItemsDeleted += OnCheckItemDeleted;
		RefreshEditorMenuState();
	}

	private void OnContentSelectionChanged(object sender, EventArgs e)
	{
		RefreshEditorMenuState();
	}

	private void OnContentCaretPositionChanged(object sender, EventArgs e)
	{
		RefreshEditorMenuState();
	}

	private void RefreshEditorMenuState()
	{
		MarkDownEditor contentText = TaskDetail.GetContentText();
		if (contentText == null)
		{
			EditorMenu.ApplyEditorState(string.Empty, null);
			return;
		}
		string currentParagraphStyle = contentText.GetCurrentParagraphStyle();
		EditorMenu.ApplyEditorState(currentParagraphStyle, contentText.GetCurrentEditorActions(), contentText.GetCurrentHighlightContextColor());
	}

	private void RebindContentEditorEvents()
	{
		UnbindContentEditorEvents();
		MarkDownEditor contentText = TaskDetail.GetContentText();
		if (contentText != null)
		{
			contentText.CaretVerticalOffsetChanged -= OnContentVerticalOffsetChanged;
			contentText.CaretVerticalOffsetChanged += OnContentVerticalOffsetChanged;
			contentText.EditorStateChanged -= OnContentEditorStateChanged;
			contentText.EditorStateChanged += OnContentEditorStateChanged;
			contentText.EditBox.TextArea.SelectionChanged -= OnContentSelectionChanged;
			contentText.EditBox.TextArea.SelectionChanged += OnContentSelectionChanged;
			contentText.EditBox.TextArea.Caret.PositionChanged -= OnContentCaretPositionChanged;
			contentText.EditBox.TextArea.Caret.PositionChanged += OnContentCaretPositionChanged;
		}
	}

	private void UnbindContentEditorEvents()
	{
		MarkDownEditor contentText = TaskDetail.GetContentText();
		if (contentText != null)
		{
			contentText.CaretVerticalOffsetChanged -= OnContentVerticalOffsetChanged;
			contentText.EditorStateChanged -= OnContentEditorStateChanged;
			contentText.EditBox.TextArea.SelectionChanged -= OnContentSelectionChanged;
			contentText.EditBox.TextArea.Caret.PositionChanged -= OnContentCaretPositionChanged;
		}
	}

	private void OnContentEditorStateChanged(object sender, EventArgs e)
	{
		RefreshEditorMenuState();
	}

	private void OnCheckItemDeleted(object sender, TaskDetailItemModel model)
	{
		UndoToast undoToast = new UndoToast();
		undoToast.InitSubtaskUndo(model);
		Utils.FindParent<MainWindow>(this)?.Toast(undoToast);
	}

	private void OnEsc(object sender, EventArgs e)
	{
		Utils.FindParent<IListViewParent>(this)?.ExitImmersiveMode();
	}

	private void OnNavigateBack(object sender, EventArgs e)
	{
		if (_navigateStack.Count > 0)
		{
			ShowContent(_navigateStack.Pop().TaskId);
		}
		if (_navigateStack.Count <= 0)
		{
			SetNavigateBackDisabled();
		}
	}

	private void SetNavigateBackDisabled()
	{
		EditorMenu.NavigateBackBtn.IsEnabled = false;
		EditorMenu.NavigateBackBtn.Visibility = Visibility.Collapsed;
	}

	private async void OnNavigateTask(object sender, ProjectTask projectTask)
	{
		_navigateStack.Push(new ProjectTask
		{
			TaskId = TaskDetail.TaskId,
			ProjectId = TaskDetail.ProjectId
		});
		TaskModel task = await TaskUtils.TryLoadTask(projectTask.TaskId, projectTask.ProjectId);
		if (task != null)
		{
			SetNavigateEnabled();
			ShowContent(projectTask.TaskId);
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == task.projectId);
			if (projectModel == null || (projectModel.closed.HasValue && projectModel.closed.Value) || task.deleted != 0 || task.kind == "CHECKLIST")
			{
				EditorMenu.SetReadOnly(readOnly: true);
			}
			else
			{
				EditorMenu.SetReadOnly(readOnly: false);
			}
		}
	}

	private void SetNavigateEnabled()
	{
		EditorMenu.NavigateBackBtn.IsEnabled = true;
		EditorMenu.NavigateBackBtn.Visibility = Visibility.Visible;
	}

	private async void ShowContent(string taskId)
	{
		Storyboard storyboard = new Storyboard();
		DoubleAnimation doubleAnimation = new DoubleAnimation
		{
			AutoReverse = false,
			FillBehavior = FillBehavior.HoldEnd,
			From = 1.0,
			To = 0.0,
			Duration = TimeSpan.FromMilliseconds(200.0)
		};
		DoubleAnimation doubleAnimation2 = new DoubleAnimation
		{
			BeginTime = TimeSpan.FromMilliseconds(200.0),
			AutoReverse = false,
			FillBehavior = FillBehavior.HoldEnd,
			From = 0.0,
			To = 1.0,
			Duration = TimeSpan.FromMilliseconds(200.0)
		};
		storyboard.Children.Add(doubleAnimation);
		storyboard.Children.Add(doubleAnimation2);
		Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(UIElement.OpacityProperty));
		Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath(UIElement.OpacityProperty));
		storyboard.Begin(Root);
		await Task.Delay(200);
		await TaskDetail.Navigate(taskId);
		RebindContentEditorEvents();
		RefreshEditorMenuState();
	}

	private void OnContentVerticalOffsetChanged(object sender, double offset)
	{
		double actualHeight = TaskDetail.GetTitleText().ActualHeight;
		double num = TaskDetailScrollViewer.VerticalOffset + TaskDetailScrollViewer.ActualHeight - actualHeight;
		double num2 = offset + 24.0 + 21.0 + 56.0;
		if (num2 > num)
		{
			TaskDetailScrollViewer.ScrollToVerticalOffset(num2 - TaskDetailScrollViewer.ActualHeight + actualHeight);
		}
		double num3 = TaskDetailScrollViewer.VerticalOffset - actualHeight;
		if (offset + 24.0 - 21.0 < num3)
		{
			offset = offset + 24.0 - 21.0;
			TaskDetailScrollViewer.ScrollToVerticalOffset(offset + actualHeight - 21.0);
		}
		if (offset <= 24.0)
		{
			TaskDetailScrollViewer.ScrollToVerticalOffset(actualHeight - 24.0);
		}
	}

	private void OnDetailKeyDown(object sender, KeyEventArgs e)
	{
		if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
		{
			switch (e.Key)
			{
			case Key.Up:
				Zoom(zoomIn: true);
				e.Handled = true;
				break;
			case Key.Down:
				Zoom(zoomIn: false);
				e.Handled = true;
				break;
			}
		}
	}

	private static void OnDetailScroll(object sender, MouseWheelEventArgs e)
	{
		if (!e.Handled)
		{
			e.Handled = true;
			MouseWheelEventArgs e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
			{
				RoutedEvent = UIElement.MouseWheelEvent,
				Source = sender
			};
			(((Control)sender).Parent as UIElement)?.RaiseEvent(e2);
		}
	}

	private async void OnEditorAction(object sender, string action)
	{
		TaskDetail.SetContentStyle(action);
	}

	private void OnHighlightColorRequested(object sender, ColorExtra color)
	{
		TaskDetail.SetHighlightColor(color);
	}

	public async Task LoadData(string taskId, TaskDetailView sourceDetail = null)
	{
		_isPreparing = true;
		ShowMenu();
		await TaskDetail.Navigate(taskId);
		TaskDetail.CopyEditorStateFrom(sourceDetail);
		RebindContentEditorEvents();
		RefreshEditorMenuState();
		FinishInitialPreparationAsync();
	}

	private async Task FinishInitialPreparationAsync()
	{
		await Task.Delay(1500);
		if (!EditorMenuControl.IsMouseOver)
		{
			HideMenu(checkPreparing: false);
		}
		_isPreparing = false;
	}

	private void OnExitClick(object sender, EventArgs eventArgs)
	{
		_navigateStack.Clear();
		SetNavigateBackDisabled();
		ZoomNormal();
		Utils.FindParent<IListViewParent>(this)?.ExitImmersiveMode();
	}

	private async void OnMenuMouseLeave(object sender, MouseEventArgs e)
	{
		if (!EditorMenu.HeaderPopup.IsOpen)
		{
			HideMenu();
		}
	}

	private void OnMenuMouseEnter(object sender, MouseEventArgs e)
	{
		ShowMenu();
	}

	private void HideMenu(bool checkPreparing = true)
	{
		if (!checkPreparing || !_isPreparing)
		{
			Storyboard storyboard = new Storyboard();
			DoubleAnimation doubleAnimation = new DoubleAnimation
			{
				AutoReverse = false,
				BeginTime = TimeSpan.FromMilliseconds(500.0),
				FillBehavior = FillBehavior.HoldEnd,
				From = 1.0,
				To = 0.23999999463558197,
				Duration = TimeSpan.FromMilliseconds(120.0)
			};
			storyboard.Children.Add(doubleAnimation);
			Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(UIElement.OpacityProperty));
			storyboard.Begin(EditorMenuControl);
		}
	}

	private void ShowMenu()
	{
		Storyboard storyboard = new Storyboard();
		DoubleAnimation doubleAnimation = new DoubleAnimation
		{
			AutoReverse = false,
			FillBehavior = FillBehavior.HoldEnd,
			From = 0.23999999463558197,
			To = 1.0,
			Duration = TimeSpan.FromMilliseconds(120.0)
		};
		storyboard.Children.Add(doubleAnimation);
		Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(UIElement.OpacityProperty));
		storyboard.Begin(EditorMenuControl);
	}

	private void ZoomNormal()
	{
		_scaleTransform.ScaleX = 1.0;
		_scaleTransform.ScaleY = 1.0;
		TaskDetail.RenderTransform = _scaleTransform;
	}

	public void Zoom(bool zoomIn)
	{
		double scaleX = _scaleTransform.ScaleX;
		double num = App.Window.ActualWidth / ImmersiveGrid.ActualWidth;
		int num2 = 0;
		TaskDetail.RenderTransformOrigin = new Point(0.5, num2);
		scaleX = ((!zoomIn) ? (scaleX - 0.05) : (scaleX + 0.05));
		if (scaleX >= 0.5 && scaleX <= num)
		{
			_scaleTransform.ScaleX = scaleX;
			_scaleTransform.ScaleY = scaleX;
			TaskDetail.RenderTransform = _scaleTransform;
		}
	}

	public void AddScrollHandler()
	{
		TaskDetail.ScrollViewer.PreviewMouseWheel -= OnDetailScroll;
		TaskDetail.ScrollViewer.PreviewMouseWheel += OnDetailScroll;
	}

	public void KeepOffsetInView(object sender, double offset)
	{
		double num = 10.0;
		MarkDownEditor descText = TaskDetail.GetDescText();
		MarkDownEditor contentText = TaskDetail.GetContentText();
		double num2 = TaskDetail.GetTitleText()?.ActualHeight ?? 0.0;
		ChecklistControl checklist = TaskDetail.GetChecklist();
		if (object.Equals(sender, contentText) || object.Equals(sender, descText))
		{
			num += num2;
		}
		else if (object.Equals(sender, checklist))
		{
			num += num2 + 18.0;
			if (descText != null)
			{
				num += descText.ActualHeight;
			}
		}
		double num3 = (double)FindResource("Font14") - 14.0 + 2.0;
		double num4 = offset + num3 + 56.0;
		double num5 = TaskDetailScrollViewer.VerticalOffset + TaskDetailScrollViewer.ActualHeight - num;
		double num6 = TaskDetailScrollViewer.VerticalOffset - num;
		if (num5 >= num4 && offset >= num6)
		{
			return;
		}
		if (num4 > num5)
		{
			TaskDetailScrollViewer.ScrollToVerticalOffset(num4 - TaskDetailScrollViewer.ActualHeight + num);
			return;
		}
		if (offset < num6)
		{
			TaskDetailScrollViewer.ScrollToVerticalOffset(offset + num - 10.0);
		}
		if (offset <= 24.0 + num3)
		{
			TaskDetailScrollViewer.ScrollToVerticalOffset(num - 24.0 - num3);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/markdown/immersivecontent.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (ImmersiveContent)target;
			break;
		case 2:
			TaskDetailScrollViewer = (ScrollViewer)target;
			break;
		case 3:
			ImmersiveGrid = (Grid)target;
			break;
		case 4:
			TaskDetail = (TaskDetailImmerseView)target;
			break;
		case 5:
			((Grid)target).MouseEnter += OnMenuMouseEnter;
			((Grid)target).MouseLeave += OnMenuMouseLeave;
			break;
		case 6:
			EditorMenuControl = (Grid)target;
			break;
		case 7:
			EditorMenu = (EditorMenu)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
