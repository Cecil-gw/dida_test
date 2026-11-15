using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace ticktick_WPF.Resource;

public class MultiLayerIcon : UserControl, IComponentConnector
{
	public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(ObservableCollection<IconLayerItem>), typeof(MultiLayerIcon), new PropertyMetadata(new ObservableCollection<IconLayerItem>()));

	public static readonly DependencyProperty CanvasWidthProperty = DependencyProperty.Register("CanvasWidth", typeof(double), typeof(MultiLayerIcon), new PropertyMetadata(32.0));

	public static readonly DependencyProperty CanvasHeightProperty = DependencyProperty.Register("CanvasHeight", typeof(double), typeof(MultiLayerIcon), new PropertyMetadata(32.0));

	public static readonly DependencyProperty StretchProperty = DependencyProperty.Register("Stretch", typeof(Stretch), typeof(MultiLayerIcon), new PropertyMetadata(Stretch.Uniform));

	internal ItemsControl MainItems;

	private bool _contentLoaded;

	public ObservableCollection<IconLayerItem> Items
	{
		get
		{
			return (ObservableCollection<IconLayerItem>)GetValue(ItemsProperty);
		}
		set
		{
			SetValue(ItemsProperty, value);
		}
	}

	public double CanvasWidth
	{
		get
		{
			return (double)GetValue(CanvasWidthProperty);
		}
		set
		{
			SetValue(CanvasWidthProperty, value);
		}
	}

	public double CanvasHeight
	{
		get
		{
			return (double)GetValue(CanvasHeightProperty);
		}
		set
		{
			SetValue(CanvasHeightProperty, value);
		}
	}

	public Stretch Stretch
	{
		get
		{
			return (Stretch)GetValue(StretchProperty);
		}
		set
		{
			SetValue(StretchProperty, value);
		}
	}

	public MultiLayerIcon()
	{
		InitializeComponent();
		MainItems.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Items")
		{
			Source = this
		});
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/resource/multilayericon.xaml", UriKind.Relative);
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
			MainItems = (ItemsControl)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
