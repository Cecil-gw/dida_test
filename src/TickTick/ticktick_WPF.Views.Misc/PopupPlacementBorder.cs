using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Misc;

public class PopupPlacementBorder : Button, IComponentConnector
{
	public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register("IsDropDownOpen", typeof(bool), typeof(PopupPlacementBorder), new PropertyMetadata(false));

	public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(PopupPlacementBorder), new PropertyMetadata(false));

	public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(double), typeof(PopupPlacementBorder), new PropertyMetadata(6.0));

	public static readonly DependencyProperty HighlightBorderOnHoverProperty = DependencyProperty.Register("HighlightBorderOnHover", typeof(bool), typeof(PopupPlacementBorder), new PropertyMetadata(false));

	internal PopupPlacementBorder Root;

	private bool _contentLoaded;

	public bool IsDropDownOpen
	{
		get
		{
			return (bool)GetValue(IsDropDownOpenProperty);
		}
		set
		{
			SetValue(IsDropDownOpenProperty, value);
		}
	}

	public bool IsSelected
	{
		get
		{
			return (bool)GetValue(IsSelectedProperty);
		}
		set
		{
			SetValue(IsSelectedProperty, value);
		}
	}

	public double Radius
	{
		get
		{
			return (double)GetValue(RadiusProperty);
		}
		set
		{
			SetValue(RadiusProperty, value);
		}
	}

	public bool HighlightBorderOnHover
	{
		get
		{
			return (bool)GetValue(HighlightBorderOnHoverProperty);
		}
		set
		{
			SetValue(HighlightBorderOnHoverProperty, value);
		}
	}

	public PopupPlacementBorder()
	{
		InitializeComponent();
		base.PreviewMouseLeftButtonUp += OnMouseUp;
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/popupplacementborder.xaml", UriKind.Relative);
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
			Root = (PopupPlacementBorder)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
