using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Views.Tag;

public class TagLabelControl : UserControl, IComponentConnector
{
	internal TagLabelControl Root;

	internal new Grid Tag;

	internal Border Bd;

	internal Grid DeleteGrid;

	private bool _contentLoaded;

	public TagLabelControl()
	{
		InitializeComponent();
	}

	private TagDisplayControl FindParent()
	{
		return Utils.FindParent<TagDisplayControl>(this);
	}

	private void OnDeleteClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is TagLabelViewModel tagLabelViewModel)
		{
			FindParent()?.RemoveTag(tagLabelViewModel.Tag);
		}
	}

	private void OnTagClick(object sender, MouseButtonEventArgs e)
	{
		if (!(base.DataContext is TagLabelViewModel tagLabelViewModel))
		{
			return;
		}
		TagDisplayControl tagDisplayControl = FindParent();
		if (tagDisplayControl != null && tagDisplayControl.CanClickTag)
		{
			ListViewContainer listViewContainer = Utils.FindParent<ListViewContainer>(this);
			if (listViewContainer != null)
			{
				listViewContainer.SelectTagProject(tagLabelViewModel.Tag);
			}
			else
			{
				App.SelectTagProject(tagLabelViewModel.Tag);
			}
			tagDisplayControl.NotifyTagClick(tagLabelViewModel.Tag);
		}
	}

	private void TagMouseEnter(object sender, MouseEventArgs e)
	{
		if (base.DataContext is TagLabelViewModel tagLabelViewModel)
		{
			DeleteGrid.Visibility = ((!tagLabelViewModel.Editable) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void TagMouseLeave(object sender, MouseEventArgs e)
	{
		DeleteGrid.Visibility = Visibility.Collapsed;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/taglabelcontrol.xaml", UriKind.Relative);
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
			Root = (TagLabelControl)target;
			break;
		case 2:
			Tag = (Grid)target;
			Tag.MouseLeave += TagMouseLeave;
			Tag.MouseEnter += TagMouseEnter;
			Tag.MouseLeftButtonUp += OnTagClick;
			break;
		case 3:
			Bd = (Border)target;
			break;
		case 4:
			DeleteGrid = (Grid)target;
			DeleteGrid.MouseLeftButtonUp += OnDeleteClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
