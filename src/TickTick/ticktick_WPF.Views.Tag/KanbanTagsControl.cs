using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Views.Tag;

public class KanbanTagsControl : UserControl, IComponentConnector, IStyleConnector
{
	private string _mouseDownTag;

	internal KanbanTagsControl Root;

	internal Grid MainTagGrid;

	internal ItemsControl Items;

	private bool _contentLoaded;

	public event EventHandler<string> TagDelete;

	public KanbanTagsControl()
	{
		InitializeComponent();
	}

	private void OnDeleteClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is Grid { DataContext: TagViewModel dataContext })
		{
			this.TagDelete?.Invoke(this, dataContext.Tag);
		}
		e.Handled = true;
	}

	private void OnTagClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is Grid { DataContext: TagViewModel { IsMore: false } dataContext } && _mouseDownTag == dataContext.Tag)
		{
			ListViewContainer listViewContainer = Utils.FindParent<ListViewContainer>(this);
			if (listViewContainer != null)
			{
				MainWindow mainWindow = Utils.FindParent<MainWindow>(listViewContainer);
				if (mainWindow != null)
				{
					LocalSettings.Settings.SelectProjectId = "tag:" + dataContext.Tag;
					mainWindow.StopSearch();
				}
				listViewContainer.SelectTagProject(dataContext.Tag);
			}
			else
			{
				App.SelectTagProject(dataContext.Tag);
			}
		}
		e.Handled = true;
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is Grid { DataContext: TagViewModel { IsMore: false } dataContext })
		{
			_mouseDownTag = dataContext.Tag;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/kanbantagscontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (KanbanTagsControl)target;
			break;
		case 4:
			MainTagGrid = (Grid)target;
			break;
		case 5:
			Items = (ItemsControl)target;
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
		switch (connectionId)
		{
		case 2:
			((Grid)target).MouseLeftButtonUp += OnTagClick;
			((Grid)target).MouseLeftButtonDown += OnMouseDown;
			break;
		case 3:
			((Grid)target).MouseLeftButtonUp += OnDeleteClick;
			break;
		}
	}
}
