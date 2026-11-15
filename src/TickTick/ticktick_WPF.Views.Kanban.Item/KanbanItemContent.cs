using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Kanban.Item;

public class KanbanItemContent : Grid, IComponentConnector
{
	private KanbanItemController _controller;

	internal KanbanItemContent Root;

	internal Border FoldGrid;

	internal Canvas CheckBox;

	internal Border CheckIconBorder;

	internal Path CheckPath;

	internal TaskTitleBox TitleBox;

	private bool _contentLoaded;

	public KanbanItemContent()
	{
		InitializeComponent();
		TitleBox.SetWordWrap(wrap: true);
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			if (TitleBox.Text != displayItemModel.Title)
			{
				TitleBox.SetText(displayItemModel.Title);
			}
			TitleBox.SetTextForeground();
			displayItemModel.SetIcon();
			CheckIconBorder.Opacity = ((displayItemModel.Status != 0) ? 0.0 : 0.15);
		}
	}

	private void OnCheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (base.DataContext is DisplayItemModel { IsToggling: false } displayItemModel)
		{
			displayItemModel.IsToggling = true;
			_controller?.OnCheckBoxClick(displayItemModel);
		}
	}

	private void OnOpenPathClick(object sender, MouseButtonEventArgs e)
	{
		_controller?.OnOpenPathClick(sender, e);
		e.Handled = true;
	}

	public void SetController(KanbanItemController controller)
	{
		_controller = controller;
	}

	public bool CheckBoxMouseOver()
	{
		if (!FoldGrid.IsMouseOver)
		{
			return CheckBox.IsMouseOver;
		}
		return true;
	}

	private void OnCheckBoxRightMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		_controller?.OnCheckBoxRightMouseUp(CheckBox);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/kanban/item/kanbanitemcontent.xaml", UriKind.Relative);
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
			Root = (KanbanItemContent)target;
			Root.DataContextChanged += OnDataBind;
			break;
		case 2:
			FoldGrid = (Border)target;
			FoldGrid.MouseLeftButtonUp += OnOpenPathClick;
			break;
		case 3:
			CheckBox = (Canvas)target;
			CheckBox.PreviewMouseLeftButtonUp += OnCheckBoxClick;
			CheckBox.PreviewMouseRightButtonUp += OnCheckBoxRightMouseUp;
			break;
		case 4:
			CheckIconBorder = (Border)target;
			break;
		case 5:
			CheckPath = (Path)target;
			break;
		case 6:
			TitleBox = (TaskTitleBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
