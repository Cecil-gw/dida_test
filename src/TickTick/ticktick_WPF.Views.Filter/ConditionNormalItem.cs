using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class ConditionNormalItem : Grid, IComponentConnector
{
	internal ConditionNormalItem Root;

	internal Grid OpenIconGrid;

	internal Path OpenIcon;

	private bool _contentLoaded;

	public ConditionNormalItem()
	{
		InitializeComponent();
	}

	private void OnOpenClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is FilterListItemViewModel model)
		{
			Utils.FindParent<ConditionEditDialog>(this).OnOpenClick(model);
		}
		e.Handled = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/filter/conditionnormalitem.xaml", UriKind.Relative);
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
			Root = (ConditionNormalItem)target;
			break;
		case 2:
			OpenIconGrid = (Grid)target;
			OpenIconGrid.PreviewMouseLeftButtonUp += OnOpenClick;
			break;
		case 3:
			OpenIcon = (Path)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
