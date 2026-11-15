using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Kanban.Item;

public class KanbanDragItem : UserControl, IComponentConnector
{
	private KanbanItemContent _itemContent;

	private ListItemTagsControl _tagPanel;

	internal Grid ContentGrid;

	internal Border TagBd;

	private bool _contentLoaded;

	public KanbanDragItem()
	{
		InitializeComponent();
	}

	private async void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.DataContext is DisplayItemModel { IsSection: false })
		{
			if (_itemContent == null)
			{
				_itemContent = new KanbanItemContent();
				_itemContent.SetValue(Grid.RowProperty, 0);
				_itemContent.SetValue(Grid.ColumnProperty, 0);
				_itemContent.SetValue(Panel.ZIndexProperty, -1);
				_itemContent.SetResourceReference(FrameworkElement.MinHeightProperty, "Height30");
				_itemContent.TitleBox.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
				ContentGrid.Children.Add(_itemContent);
			}
			await Task.Delay(10);
			if (_tagPanel == null)
			{
				_tagPanel = new ListItemTagsControl();
				TagBd.Child = _tagPanel;
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/kanban/item/kanbandragitem.xaml", UriKind.Relative);
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
			((KanbanDragItem)target).DataContextChanged += OnDataBind;
			break;
		case 2:
			ContentGrid = (Grid)target;
			break;
		case 3:
			TagBd = (Border)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
