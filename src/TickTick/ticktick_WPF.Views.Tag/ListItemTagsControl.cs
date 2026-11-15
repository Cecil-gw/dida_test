using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Views.Tag;

public class ListItemTagsControl : UserControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty TagMaxWidthProperty = DependencyProperty.Register("TagMaxWidth", typeof(double), typeof(ListItemTagsControl), new PropertyMetadata(72.0));

	private const double TagHorizontalChromeWidth = 18.0;

	private string _mouseDownTag;

	internal ListItemTagsControl Root;

	internal ItemsControl Items;

	private bool _contentLoaded;

	public double TagMaxWidth
	{
		get
		{
			return (double)GetValue(TagMaxWidthProperty);
		}
		set
		{
			SetValue(TagMaxWidthProperty, value);
		}
	}

	public event EventHandler<string> TagDelete;

	public static int CalculateVisibleTagCount(IReadOnlyList<string> tags, double availableWidth, double tagMaxWidth)
	{
		if (tags == null || tags.Count == 0)
		{
			return 0;
		}
		if (tags.Count == 1)
		{
			return 1;
		}
		List<double> list = tags.Select((string tag) => GetDesiredTagWidth(tag, tagMaxWidth)).ToList();
		if (list.Sum() <= availableWidth)
		{
			return tags.Count;
		}
		double num = 0.0;
		int result = 0;
		for (int num2 = 0; num2 < tags.Count; num2++)
		{
			double desiredTagWidth = GetDesiredTagWidth("+" + (tags.Count - num2), tagMaxWidth);
			if (num + desiredTagWidth <= availableWidth)
			{
				result = num2;
			}
			num += list[num2];
		}
		return result;
	}

	private static double GetDesiredTagWidth(string tag, double tagMaxWidth)
	{
		return ((tagMaxWidth <= 0.0) ? 0.0 : Math.Min(Utils.MeasureStringWidth(tag ?? string.Empty, 12.0), tagMaxWidth)) + 18.0;
	}

	public ListItemTagsControl()
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
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/listitemtagscontrol.xaml", UriKind.Relative);
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
			Root = (ListItemTagsControl)target;
			break;
		case 4:
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
