using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace ticktick_WPF.Views;

public class ArrowHintUserControl : UserControl, IComponentConnector
{
	public enum ArrowDirectionEnum
	{
		None,
		Up,
		Down
	}

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ArrowHintUserControl), new PropertyMetadata());

	public static readonly DependencyProperty ArrowDirectionProperty = DependencyProperty.Register("ArrowDirection", typeof(ArrowDirectionEnum), typeof(ArrowHintUserControl), new PropertyMetadata(ArrowDirectionEnum.Down, OnArrowDirectionChanged));

	public static readonly DependencyProperty ContentPaddingProperty = DependencyProperty.Register("ContentPadding", typeof(Thickness), typeof(ArrowHintUserControl), new PropertyMetadata(new Thickness(10.0, 6.0, 10.0, 6.0), OnContentPaddingChanged));

	public static readonly DependencyProperty ArrowHorizontalAlignmentProperty = DependencyProperty.Register("ArrowHorizontalAlignment", typeof(HorizontalAlignment), typeof(ArrowHintUserControl), new PropertyMetadata(HorizontalAlignment.Right, OnArrowAlignmentChanged));

	public static readonly DependencyProperty ArrowOffsetProperty = DependencyProperty.Register("ArrowOffset", typeof(double), typeof(ArrowHintUserControl), new PropertyMetadata(17.0, OnArrowOffsetChanged));

	internal Border MainBorder;

	internal Path ArrowDown;

	internal Path ArrowUp;

	internal Grid ContentGrid;

	private bool _contentLoaded;

	public string Text
	{
		get
		{
			return (string)GetValue(TextProperty);
		}
		set
		{
			SetValue(TextProperty, value);
		}
	}

	public ArrowDirectionEnum ArrowDirection
	{
		get
		{
			return (ArrowDirectionEnum)GetValue(ArrowDirectionProperty);
		}
		set
		{
			SetValue(ArrowDirectionProperty, value);
		}
	}

	public Thickness ContentPadding
	{
		get
		{
			return (Thickness)GetValue(ContentPaddingProperty);
		}
		set
		{
			SetValue(ContentPaddingProperty, value);
		}
	}

	public HorizontalAlignment ArrowHorizontalAlignment
	{
		get
		{
			return (HorizontalAlignment)GetValue(ArrowHorizontalAlignmentProperty);
		}
		set
		{
			SetValue(ArrowHorizontalAlignmentProperty, value);
		}
	}

	public double ArrowOffset
	{
		get
		{
			return (double)GetValue(ArrowOffsetProperty);
		}
		set
		{
			SetValue(ArrowOffsetProperty, value);
		}
	}

	private static void OnArrowDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is ArrowHintUserControl arrowHintUserControl)
		{
			arrowHintUserControl.UpdateArrowVisibility();
		}
	}

	private static void OnContentPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is ArrowHintUserControl arrowHintUserControl)
		{
			arrowHintUserControl.ContentGrid.Margin = (Thickness)e.NewValue;
		}
	}

	private static void OnArrowAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is ArrowHintUserControl arrowHintUserControl)
		{
			arrowHintUserControl.UpdateArrowAlignment();
		}
	}

	private static void OnArrowOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is ArrowHintUserControl arrowHintUserControl)
		{
			arrowHintUserControl.UpdateArrowAlignment();
		}
	}

	public ArrowHintUserControl()
	{
		InitializeComponent();
		UpdateArrowVisibility();
		UpdateArrowAlignment();
	}

	internal Point GetArrowCenterOnScreen()
	{
		Path path = ((ArrowUp.Visibility == Visibility.Visible) ? ArrowUp : ArrowDown);
		return path.PointToScreen(new Point(path.ActualWidth / 2.0, path.ActualHeight / 2.0));
	}

	private void UpdateArrowVisibility()
	{
		ArrowUp.Visibility = ((ArrowDirection != ArrowDirectionEnum.Up) ? Visibility.Collapsed : Visibility.Visible);
		ArrowDown.Visibility = ((ArrowDirection != ArrowDirectionEnum.Down) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void UpdateArrowAlignment()
	{
		ArrowUp.HorizontalAlignment = ArrowHorizontalAlignment;
		ArrowDown.HorizontalAlignment = ArrowHorizontalAlignment;
		if (ArrowHorizontalAlignment == HorizontalAlignment.Left)
		{
			ArrowUp.Margin = new Thickness(ArrowOffset, 0.0, 0.0, 0.0);
			ArrowDown.Margin = new Thickness(ArrowOffset, 0.0, 0.0, 0.0);
		}
		else if (ArrowHorizontalAlignment == HorizontalAlignment.Right)
		{
			ArrowUp.Margin = new Thickness(0.0, 0.0, ArrowOffset, 0.0);
			ArrowDown.Margin = new Thickness(0.0, 0.0, ArrowOffset, 0.0);
		}
		else
		{
			ArrowUp.Margin = default(Thickness);
			ArrowDown.Margin = default(Thickness);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/arrowhintusercontrol.xaml", UriKind.Relative);
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
			MainBorder = (Border)target;
			break;
		case 2:
			ArrowDown = (Path)target;
			break;
		case 3:
			ArrowUp = (Path)target;
			break;
		case 4:
			ContentGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
