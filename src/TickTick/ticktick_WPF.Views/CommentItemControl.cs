using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views;

public class CommentItemControl : UserControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty FoldProperty = DependencyProperty.Register("Fold", typeof(bool), typeof(CommentItemControl), new PropertyMetadata(true));

	private Button _saveButton;

	internal CommentItemControl Root;

	internal Grid ContentGrid;

	internal ContentControl ReplyGrid;

	internal ContentControl EditGrid;

	internal ContentControl Delete;

	internal Grid CommentTextGrid;

	internal TaskLinkNameEditor ContentTxt;

	internal Border AttachmentBorder;

	internal StackPanel ExpandPanel;

	internal TextBlock ExpandOrCollapseBtn;

	internal Path ExpandPath;

	internal RotateTransform ExpandPathRotate;

	internal Border EditorBorder;

	private bool _contentLoaded;

	public bool Fold
	{
		get
		{
			return (bool)GetValue(FoldProperty);
		}
		set
		{
			SetValue(FoldProperty, value);
		}
	}

	public event EventHandler<CommentViewModel> ShowAddComment;

	public CommentItemControl()
	{
		InitializeComponent();
		Delete.MouseLeftButtonUp += OnCommentDeleted;
		EditGrid.MouseLeftButtonUp += OnCommentEditClick;
		ReplyGrid.MouseLeftButtonUp += delegate
		{
			CommentViewModel viewModel = GetViewModel();
			if (viewModel != null)
			{
				this.ShowAddComment?.Invoke(null, viewModel);
			}
		};
		ContentTxt.EditBox.PreviewKeyDown += OnTextKeyDown;
		ContentTxt.EditBox.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
		ContentTxt.KeysUp += OnTextKeyUp;
		ContentTxt.EnterUp += OnEnterUp;
	}

	private void OnEnterUp(object sender, EventArgs e)
	{
		if (!Utils.IfShiftPressed() && ContentTxt.Text.Length <= 1024)
		{
			OnSaveClick(null, null);
		}
	}

	private void OnTextKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			OnCancelClick(null, null);
		}
	}

	private void OnTextKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return && !Utils.IfShiftPressed())
		{
			e.Handled = true;
		}
	}

	private async void OnCommentEditClick(object sender, MouseButtonEventArgs e)
	{
		ContentTxt.ReadOnly = false;
		ContentTxt.EditBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		ContentTxt.SetParseTaskName(b: false);
		ContentTxt.BorderCorner = new CornerRadius(8.0);
		ContentTxt.TextPadding = new Thickness(8.0, 8.0, 0.0, 8.0);
		ContentTxt.SetResourceReference(EmojiEditor.BorderBrushProperty, "BaseColorOpacity10");
		ContentTxt.FocusEnd();
		CommentViewModel viewModel = GetViewModel();
		if (viewModel != null)
		{
			viewModel.Editing = true;
		}
		SetEditButton();
		await Task.Delay(100);
		Utils.FindParent<TaskDetailView>(this)?.OnCommentEdit(this);
	}

	private async void OnCommentDeleted(object sender, MouseButtonEventArgs e)
	{
		CommentViewModel viewModel = GetViewModel();
		if (viewModel != null)
		{
			await viewModel.RemoveComment(viewModel);
		}
	}

	protected override void OnMouseEnter(MouseEventArgs e)
	{
		base.OnMouseEnter(e);
		CommentViewModel viewModel = GetViewModel();
		if (viewModel != null && viewModel.CanEdit && !viewModel.Editing)
		{
			if (viewModel.IsMySelf)
			{
				Delete.Visibility = Visibility.Visible;
				EditGrid.Visibility = Visibility.Visible;
				ReplyGrid.Visibility = Visibility.Collapsed;
			}
			else
			{
				ReplyGrid.Visibility = Visibility.Visible;
				Delete.Visibility = Visibility.Collapsed;
				EditGrid.Visibility = Visibility.Collapsed;
			}
		}
	}

	protected override void OnMouseLeave(MouseEventArgs e)
	{
		base.OnMouseLeave(e);
		Delete.Visibility = Visibility.Collapsed;
		ReplyGrid.Visibility = Visibility.Collapsed;
		EditGrid.Visibility = Visibility.Collapsed;
	}

	private CommentViewModel GetViewModel()
	{
		return base.DataContext as CommentViewModel;
	}

	private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		ToggleExpandOrCollapse();
		if (base.DataContext is CommentViewModel lastItemHeight)
		{
			SetLastItemHeight(lastItemHeight);
		}
		e.Handled = true;
	}

	private void ToggleExpandOrCollapse()
	{
		if (Fold)
		{
			Fold = false;
			ExpandOrCollapseBtn.Text = Utils.GetString("Collapse");
			ExpandPathRotate.Angle = 180.0;
		}
		else
		{
			Fold = true;
			ExpandOrCollapseBtn.Text = Utils.GetString("expand");
			ExpandPathRotate.Angle = 0.0;
		}
	}

	private async void OnDataBinded(object sender, DependencyPropertyChangedEventArgs e)
	{
		CommentViewModel model = e.NewValue as CommentViewModel;
		if (model == null)
		{
			return;
		}
		int times = 30;
		while (!base.IsLoaded && times-- > 0)
		{
			await Task.Delay(4);
		}
		if (base.DataContext != model)
		{
			return;
		}
		ResetExpandState();
		ContentTxt.Text = model.Content;
		await base.Dispatcher.InvokeAsync(delegate
		{
			if (base.DataContext == model)
			{
				UpdateExpandPanelVisibility();
			}
		}, DispatcherPriority.Loaded);
		if (base.DataContext == model)
		{
			SetLastItemHeight(model);
		}
	}

	private async void SetLastItemHeight(CommentViewModel model)
	{
		if (model.IsLastOne())
		{
			await Task.Delay(10);
			if (base.DataContext == model)
			{
				model.ListModel?.EditComment?.SetLastHeight(base.ActualHeight);
			}
		}
	}

	private void OnTextSizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateExpandPanelVisibility();
	}

	private void ResetExpandState()
	{
		Fold = true;
		ExpandPanel.Visibility = Visibility.Collapsed;
		ExpandPanel.Opacity = 1.0;
		ExpandOrCollapseBtn.Text = Utils.GetString("expand");
		ExpandPathRotate.Angle = 0.0;
	}

	private void UpdateExpandPanelVisibility()
	{
		if (base.IsLoaded && ContentTxt?.EditBox?.Document != null)
		{
			double y;
			try
			{
				ContentTxt.EditBox.TextArea.TextView.EnsureVisualLines();
				y = ContentTxt.EditBox.TextArea.TextView.GetVisualPosition(new TextViewPosition(ContentTxt.EditBox.Document.GetLocation(ContentTxt.Text.Length)), VisualYPosition.LineBottom).Y;
			}
			catch (InvalidOperationException)
			{
				return;
			}
			catch (VisualLinesInvalidException)
			{
				return;
			}
			bool flag = y > GetFoldedCommentMaxHeight();
			ExpandPanel.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			if (!flag)
			{
				Fold = true;
				ExpandOrCollapseBtn.Text = Utils.GetString("expand");
				ExpandPathRotate.Angle = 0.0;
			}
			else
			{
				ExpandOrCollapseBtn.Text = (Fold ? Utils.GetString("expand") : Utils.GetString("Collapse"));
				ExpandPathRotate.Angle = ((!Fold) ? 180 : 0);
			}
		}
	}

	private double GetFoldedCommentMaxHeight()
	{
		if (!(TryFindResource("Height66") is double num) || !(num > 0.0))
		{
			return 66.0;
		}
		return num;
	}

	private void OnBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		e.Handled = true;
	}

	private void SetEditButton()
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Bottom,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 0.0, 6.0, 6.0)
		};
		stackPanel.Name = "EditButtonStackPanel";
		Grid.SetRow(stackPanel, 4);
		_saveButton = new Button
		{
			MinWidth = 48.0,
			Height = 24.0,
			Content = Utils.GetString("PublicSave"),
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0)
		};
		_saveButton.SetResourceReference(FrameworkElement.StyleProperty, "SaveButtonStyle");
		stackPanel.Children.Add(_saveButton);
		_saveButton.Click += OnSaveClick;
		Button button = new Button
		{
			MinWidth = 48.0,
			Height = 24.0,
			Content = Utils.GetString("Cancel")
		};
		button.Click += OnCancelClick;
		button.SetResourceReference(FrameworkElement.StyleProperty, "CancelButtonStyle");
		stackPanel.Children.Add(button);
		ContentGrid.Children.Add(stackPanel);
		TextBlock textBlock = new TextBlock
		{
			Name = "OverflowText",
			VerticalAlignment = VerticalAlignment.Bottom,
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(8.0, 0.0, 0.0, 6.0),
			FontSize = 12.0
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		textBlock.Visibility = ((ContentTxt.Text.Length < 924) ? Visibility.Collapsed : Visibility.Visible);
		textBlock.Inlines.Add(new Run
		{
			Text = (ContentTxt.Text.Length.ToString() ?? "")
		});
		Run run = new Run
		{
			Text = "/1024"
		};
		run.SetResourceReference(TextElement.ForegroundProperty, "BaseColorOpacity60");
		textBlock.Inlines.Add(run);
		ContentGrid.Children.Add(textBlock);
		Grid.SetRow(textBlock, 4);
		Delete.Visibility = Visibility.Collapsed;
		EditGrid.Visibility = Visibility.Collapsed;
		ExpandPanel.Opacity = 0.0;
		ContentTxt.EditBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		ContentGrid.MouseEnter -= OnEditingMouseEnter;
		ContentGrid.MouseLeave -= OnEditingMouseLeave;
		ContentGrid.MouseEnter += OnEditingMouseEnter;
		ContentGrid.MouseLeave += OnEditingMouseLeave;
		Utils.FindParent<TaskDetailView>(this)?.SetCanScroll(!ContentGrid.IsMouseOver);
		AttachmentBorder.Margin = new Thickness(6.0, 0.0, 8.0, 0.0);
		EditorBorder.Visibility = Visibility.Visible;
	}

	private void OnEditingMouseLeave(object sender, MouseEventArgs e)
	{
		Utils.FindParent<TaskDetailView>(this)?.SetCanScroll(b: true);
	}

	private void OnEditingMouseEnter(object sender, MouseEventArgs e)
	{
		Utils.FindParent<TaskDetailView>(this)?.SetCanScroll(b: false);
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		CommentViewModel viewModel = GetViewModel();
		if (viewModel != null)
		{
			viewModel.Content = ContentTxt.Text;
			viewModel.Save();
		}
		ExitEditMode();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		CommentViewModel viewModel = GetViewModel();
		if (viewModel != null)
		{
			ContentTxt.Text = viewModel.Content;
		}
		ExitEditMode();
	}

	private void ExitEditMode()
	{
		EditorBorder.Visibility = Visibility.Collapsed;
		AttachmentBorder.Margin = new Thickness(-4.0, 0.0, 8.0, 0.0);
		ContentTxt.ReadOnly = true;
		ContentTxt.SetParseTaskName(b: true);
		ContentTxt.BorderCorner = new CornerRadius(0.0);
		ContentTxt.TextPadding = new Thickness(0.0);
		ContentTxt.BorderBrush = Brushes.Transparent;
		ContentTxt.EditBox.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
		CommentViewModel viewModel = GetViewModel();
		if (viewModel != null)
		{
			viewModel.Editing = false;
		}
		foreach (object child in ContentGrid.Children)
		{
			if (child is StackPanel { Name: "EditButtonStackPanel" } stackPanel)
			{
				ContentGrid.Children.Remove(stackPanel);
				break;
			}
		}
		foreach (object child2 in ContentGrid.Children)
		{
			if (child2 is TextBlock { Name: "OverflowText" } textBlock)
			{
				ContentGrid.Children.Remove(textBlock);
				break;
			}
		}
		if (base.IsMouseOver)
		{
			Delete.Visibility = Visibility.Visible;
			EditGrid.Visibility = Visibility.Visible;
		}
		ExpandPanel.Opacity = 1.0;
		_saveButton = null;
		ContentGrid.MouseEnter -= OnEditingMouseEnter;
		ContentGrid.MouseLeave -= OnEditingMouseLeave;
		Utils.FindParent<TaskDetailView>(this)?.SetCanScroll(b: true);
		UpdateExpandPanelVisibility();
	}

	private void OnCommentContentChanged(object sender, EventArgs e)
	{
		if (!(ContentGrid.Children[ContentGrid.Children.Count - 1] is TextBlock textBlock))
		{
			return;
		}
		if (textBlock.Inlines.FirstInline is Run run)
		{
			run.Text = ContentTxt.Text.Length.ToString() ?? "";
			if (ContentTxt.Text.Length > 1024)
			{
				run.Foreground = Brushes.Red;
			}
			else
			{
				run.SetResourceReference(TextElement.ForegroundProperty, "BaseColorOpacity60");
			}
		}
		textBlock.Visibility = ((ContentTxt.Text.Length < 924) ? Visibility.Collapsed : Visibility.Visible);
		if (_saveButton != null)
		{
			_saveButton.IsEnabled = ContentTxt.Text.Length <= 1024;
		}
	}

	private void OnFileScrollerMouseWheel(object sender, MouseWheelEventArgs e)
	{
	}

	private void OnAttachmentClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: AddTaskAttachmentInfo dataContext })
		{
			dataContext.OnAttachmentClick();
		}
	}

	private void OnTitleMouseScroll(object sender, MouseWheelEventArgs e)
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/commentitemcontrol.xaml", UriKind.Relative);
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
			Root = (CommentItemControl)target;
			Root.RequestBringIntoView += OnBringIntoView;
			Root.DataContextChanged += OnDataBinded;
			break;
		case 2:
			ContentGrid = (Grid)target;
			break;
		case 3:
			ReplyGrid = (ContentControl)target;
			break;
		case 4:
			EditGrid = (ContentControl)target;
			break;
		case 5:
			Delete = (ContentControl)target;
			break;
		case 6:
			CommentTextGrid = (Grid)target;
			break;
		case 7:
			ContentTxt = (TaskLinkNameEditor)target;
			break;
		case 8:
			AttachmentBorder = (Border)target;
			break;
		case 10:
			ExpandPanel = (StackPanel)target;
			ExpandPanel.MouseLeftButtonUp += TextBlock_MouseLeftButtonUp;
			break;
		case 11:
			ExpandOrCollapseBtn = (TextBlock)target;
			break;
		case 12:
			ExpandPath = (Path)target;
			break;
		case 13:
			ExpandPathRotate = (RotateTransform)target;
			break;
		case 14:
			EditorBorder = (Border)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 9)
		{
			((Border)target).MouseLeftButtonUp += OnAttachmentClick;
		}
	}
}
