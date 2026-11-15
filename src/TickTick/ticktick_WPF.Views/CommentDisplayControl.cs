using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.MainListView.DetailView;

namespace ticktick_WPF.Views;

public class CommentDisplayControl : UserControl, IComponentConnector
{
	private CommentListViewModel _model;

	private ScrollViewer _commentScrollViewer;

	private TaskDetailView _taskDetailView;

	private bool _stickToBottom = true;

	private bool _scrollingToBottom;

	internal ListView commentListView;

	private bool _contentLoaded;

	public CommentListViewModel Model
	{
		get
		{
			return _model;
		}
		set
		{
			_model = value;
		}
	}

	public event EventHandler<CommentViewModel> ShowAddComment;

	public CommentDisplayControl(CommentListViewModel model)
	{
		InitializeComponent();
		_model = model;
		base.DataContext = model;
		base.Loaded += delegate
		{
			_taskDetailView = Utils.FindParent<TaskDetailView>(this);
			ScrollToBottomNow(force: true, updateLayout: true);
		};
		base.Unloaded += delegate
		{
			_taskDetailView?.SetCanScroll(b: true);
			_taskDetailView = null;
		};
		commentListView.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(CommentListView_ScrollChanged));
		commentListView.SizeChanged += delegate
		{
			if (_stickToBottom)
			{
				ScheduleScrollToBottom();
			}
		};
	}

	public void ScrollToBottom()
	{
		ScheduleScrollToBottom(force: true);
	}

	private void TryShowAddComment(object sender, CommentViewModel e)
	{
		this.ShowAddComment?.Invoke(null, e);
	}

	public void AddNewItem(CommentModel comment)
	{
		CommentViewModel newComment = new CommentViewModel(comment)
		{
			CanEdit = true,
			ListModel = _model
		};
		_model.AddItem(newComment);
		ScrollToBottomNow(force: true, updateLayout: true);
	}

	public void ExpandComment()
	{
		_model?.Expand();
		ScheduleScrollToBottom(force: true);
	}

	public void SetModel(CommentListViewModel model)
	{
		if (_model == null)
		{
			return;
		}
		bool force = !object.Equals(base.Tag, model.EditComment?.TaskId);
		_model.ViewMode = model.ViewMode;
		_model.ShowExpand = model.ShowExpand;
		_model.EditComment = model.EditComment;
		_model.CommentList = model.CommentList;
		foreach (CommentViewModel comment in model.CommentList)
		{
			comment.ListModel = _model;
		}
		ItemsSourceHelper.CopyTo(model.DisplayList.ToList(), _model.DisplayList);
		_model.ViewMode = ViewMode.Full;
		_model.ShowExpand = false;
		ScrollToBottomNow(force, updateLayout: true);
		UpdateRecentCommentAnchor();
	}

	private void CommentListView_MouseEnter(object sender, MouseEventArgs e)
	{
		GetTaskDetailView()?.SetCanScroll(b: false);
	}

	private void CommentListView_MouseLeave(object sender, MouseEventArgs e)
	{
		GetTaskDetailView()?.SetCanScroll(b: true);
	}

	private void CommentListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		ScrollViewer commentScrollViewer = GetCommentScrollViewer();
		if (commentScrollViewer == null || commentScrollViewer.ScrollableHeight <= 0.0)
		{
			ScrollParent(e.Delta);
			e.Handled = true;
			return;
		}
		double verticalOffset = commentScrollViewer.VerticalOffset;
		bool num = verticalOffset <= 0.0 && e.Delta > 0;
		bool flag = verticalOffset >= commentScrollViewer.ScrollableHeight && e.Delta < 0;
		if (num || flag)
		{
			ScrollParent(e.Delta);
		}
		else if (commentScrollViewer is SmoothScrollViewer smoothScrollViewer)
		{
			smoothScrollViewer.HandleSmoothMouseWheel(e);
		}
		else
		{
			commentScrollViewer.ScrollToVerticalOffset(verticalOffset - (double)e.Delta / 2.0);
		}
		e.Handled = true;
	}

	private void ScrollParent(int delta)
	{
		TaskDetailView taskDetailView = GetTaskDetailView();
		taskDetailView?.ScrollViewer.ScrollToVerticalOffset(taskDetailView.ScrollViewer.VerticalOffset - (double)delta / 2.0);
	}

	private void CommentListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (!_scrollingToBottom)
		{
			if (e.ExtentHeightChange > 0.0 && _stickToBottom)
			{
				ScheduleScrollToBottom();
				return;
			}
			_stickToBottom = IsScrolledToBottom();
			UpdateRecentCommentAnchor();
		}
	}

	private bool IsScrolledToBottom()
	{
		ScrollViewer commentScrollViewer = GetCommentScrollViewer();
		if (commentScrollViewer != null)
		{
			return commentScrollViewer.ScrollableHeight - commentScrollViewer.VerticalOffset <= 1.0;
		}
		return true;
	}

	private void ScheduleScrollToBottom(bool force = false)
	{
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			ScrollToBottomNow(force);
		}, DispatcherPriority.ContextIdle);
	}

	private void ScrollToBottomNow(bool force, bool updateLayout = false)
	{
		if ((!force && !_stickToBottom) || commentListView.Items.Count == 0)
		{
			UpdateRecentCommentAnchor();
			return;
		}
		try
		{
			_scrollingToBottom = true;
			if (updateLayout)
			{
				commentListView.ApplyTemplate();
			}
			ScrollViewer commentScrollViewer = GetCommentScrollViewer();
			if (commentScrollViewer == null)
			{
				UpdateRecentCommentAnchor();
				return;
			}
			if (updateLayout)
			{
				commentScrollViewer.ApplyTemplate();
				commentListView.UpdateLayout();
			}
			ScrollViewer scrollViewer = GetTaskDetailView()?.ScrollViewer;
			double? num = scrollViewer?.VerticalOffset;
			commentScrollViewer.ScrollToBottom();
			if (updateLayout)
			{
				commentScrollViewer.UpdateLayout();
				commentScrollViewer.ScrollToBottom();
			}
			if (scrollViewer != null && num.HasValue)
			{
				scrollViewer.ScrollToVerticalOffset(num.Value);
			}
			_stickToBottom = true;
			UpdateRecentCommentAnchor();
		}
		finally
		{
			_scrollingToBottom = false;
		}
	}

	private ScrollViewer GetCommentScrollViewer()
	{
		return _commentScrollViewer ?? (_commentScrollViewer = (commentListView.Template?.FindName("ScrollViewer", commentListView) as ScrollViewer) ?? FindVisualChild<ScrollViewer>(commentListView));
	}

	public void UpdateRecentCommentAnchor()
	{
		CommentEditViewModel commentEditViewModel = _model?.EditComment;
		if (commentEditViewModel != null)
		{
			ScrollViewer commentScrollViewer = GetCommentScrollViewer();
			double val = 0.0;
			if (commentScrollViewer != null && commentScrollViewer.ScrollableHeight > 0.0)
			{
				val = Math.Max(0.0, commentScrollViewer.ScrollableHeight - commentScrollViewer.VerticalOffset);
			}
			double recentCommentHiddenHeightInParent = GetRecentCommentHiddenHeightInParent();
			double num = Math.Max(val, recentCommentHiddenHeightInParent);
			commentEditViewModel.RecentScrollHeight = ((num > 1.0) ? num : 0.0);
		}
	}

	private double GetRecentCommentHiddenHeightInParent()
	{
		ScrollViewer scrollViewer = GetTaskDetailView()?.ScrollViewer;
		if (scrollViewer == null || scrollViewer.ActualHeight <= 0.0)
		{
			return 0.0;
		}
		CommentViewModel commentViewModel = _model?.EditComment?.RecentComment;
		return GetHiddenHeightInParent(((commentViewModel == null) ? null : (commentListView.ItemContainerGenerator.ContainerFromItem(commentViewModel) as FrameworkElement)) ?? commentListView, scrollViewer);
	}

	private static double GetHiddenHeightInParent(FrameworkElement element, ScrollViewer parentScrollViewer)
	{
		if (element == null || element.ActualHeight <= 0.0)
		{
			return 0.0;
		}
		try
		{
			double y = element.TranslatePoint(new Point(0.0, 0.0), parentScrollViewer).Y;
			double y2 = element.TranslatePoint(new Point(0.0, element.ActualHeight), parentScrollViewer).Y;
			double num = ((parentScrollViewer.ViewportHeight > 0.0) ? parentScrollViewer.ViewportHeight : parentScrollViewer.ActualHeight);
			return Math.Max(0.0, Math.Max(0.0 - y, y2 - num));
		}
		catch (InvalidOperationException)
		{
			return 0.0;
		}
	}

	private TaskDetailView GetTaskDetailView()
	{
		return _taskDetailView ?? (_taskDetailView = Utils.FindParent<TaskDetailView>(this));
	}

	private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
	{
		if (parent == null)
		{
			return null;
		}
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(parent, i);
			if (child is T result)
			{
				return result;
			}
			T val = FindVisualChild<T>(child);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/commentdisplaycontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			commentListView = (ListView)target;
			commentListView.MouseEnter += CommentListView_MouseEnter;
			commentListView.MouseLeave += CommentListView_MouseLeave;
			commentListView.PreviewMouseWheel += CommentListView_PreviewMouseWheel;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
