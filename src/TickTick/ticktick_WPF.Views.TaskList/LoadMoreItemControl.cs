using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView.TaskList;

namespace ticktick_WPF.Views.TaskList;

public class LoadMoreItemControl : UserControl, IComponentConnector
{
	private TaskListView _parent;

	private bool _loading;

	internal LoadingIndicator LoadingIndicator;

	internal TextBlock LoadMoreText;

	private bool _contentLoaded;

	public event EventHandler<string> LoadMore;

	public LoadMoreItemControl()
	{
		InitializeComponent();
		base.Unloaded += delegate
		{
			GetParent()?.UnregisterLoadMore(this);
		};
	}

	private void OnDataBinded(object sender, DependencyPropertyChangedEventArgs e)
	{
		_loading = false;
		GetParent()?.RegisterLoadMore(this);
		LoadingIndicator.Visibility = Visibility.Collapsed;
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			if (displayItemModel.InDetail)
			{
				LoadMoreText.Text = string.Format(Utils.GetString((displayItemModel.MoreCount > 1) ? "MoreCompleteTasks" : "MoreCompleteTask"), displayItemModel.MoreCount);
			}
			else
			{
				LoadMoreText.Text = Utils.GetString("LoadMore");
			}
		}
	}

	private TaskListView GetParent()
	{
		return _parent ?? (_parent = FindParent(this));
	}

	private static TaskListView FindParent(DependencyObject child)
	{
		if (child == null)
		{
			return null;
		}
		DependencyObject parent = VisualTreeHelper.GetParent(child);
		if (parent == null)
		{
			return null;
		}
		if (parent is TaskListView)
		{
			return parent as TaskListView;
		}
		return FindParent(parent);
	}

	private void OnLoadMoreClick(object sender, MouseButtonEventArgs e)
	{
		OnLoadMore(force: true);
	}

	public void OnLoadMore(bool force)
	{
		if (_loading)
		{
			return;
		}
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			if (!force)
			{
				Section section = displayItemModel.Section;
				if (section != null && section.Children?.Count <= 5)
				{
					return;
				}
			}
			_loading = true;
			this.LoadMore?.Invoke(this, displayItemModel.ParentId);
		}
		LoadMoreText.Text = Utils.GetString("Loading");
		LoadingIndicator.Visibility = Visibility.Visible;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tasklist/loadmoreitemcontrol.xaml", UriKind.Relative);
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
			((LoadMoreItemControl)target).DataContextChanged += OnDataBinded;
			break;
		case 2:
			((Grid)target).MouseLeftButtonUp += OnLoadMoreClick;
			break;
		case 3:
			LoadingIndicator = (LoadingIndicator)target;
			break;
		case 4:
			LoadMoreText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
