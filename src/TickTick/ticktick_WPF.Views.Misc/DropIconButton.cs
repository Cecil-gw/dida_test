using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;

namespace ticktick_WPF.Views.Misc;

public class DropIconButton : UserControl, IComponentConnector
{
	public static readonly DependencyProperty ItemSelectedProperty = DependencyProperty.Register("ItemSelected", typeof(bool), typeof(DropIconButton), new PropertyMetadata(false));

	public static readonly DependencyProperty IndenteProperty = DependencyProperty.Register("Indent", typeof(double), typeof(DropIconButton), new PropertyMetadata(0.0));

	public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register("IconSize", typeof(double), typeof(DropIconButton), new PropertyMetadata(16.0));

	public static readonly DependencyProperty HoversSelectProperty = DependencyProperty.Register("HoversSelect", typeof(bool), typeof(DropIconButton), new PropertyMetadata(false));

	public static readonly DependencyProperty IconColorProperty = DependencyProperty.Register("IconColor", typeof(SolidColorBrush), typeof(DropIconButton), new PropertyMetadata(Brushes.Transparent));

	public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(DropIconButton), new PropertyMetadata(string.Empty));

	public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(Geometry), typeof(DropIconButton), new PropertyMetadata(null, OnDataChangeCallback));

	internal DropIconButton Root;

	internal Path TitleIcon;

	internal EmjTextBlock TitleText;

	private bool _contentLoaded;

	public bool ItemSelected
	{
		get
		{
			return (bool)GetValue(ItemSelectedProperty);
		}
		set
		{
			SetValue(ItemSelectedProperty, value);
		}
	}

	public double Indent
	{
		get
		{
			return (double)GetValue(IndenteProperty);
		}
		set
		{
			SetValue(IndenteProperty, value);
		}
	}

	public string Title
	{
		get
		{
			return (string)GetValue(TitleProperty);
		}
		set
		{
			SetValue(TitleProperty, value);
		}
	}

	public Geometry Icon
	{
		get
		{
			return (Geometry)GetValue(IconProperty);
		}
		set
		{
			SetValue(IconProperty, value);
		}
	}

	public double IconSize
	{
		get
		{
			return (double)GetValue(IconSizeProperty);
		}
		set
		{
			SetValue(IconSizeProperty, value);
		}
	}

	public SolidColorBrush IconColor
	{
		get
		{
			return (SolidColorBrush)GetValue(IconColorProperty);
		}
		set
		{
			SetValue(IconColorProperty, value);
		}
	}

	public bool HoversSelect
	{
		get
		{
			return (bool)GetValue(HoversSelectProperty);
		}
		set
		{
			SetValue(HoversSelectProperty, value);
		}
	}

	public DropIconButton()
	{
		InitializeComponent();
	}

	private static void OnDataChangeCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is DropIconButton dropIconButton && e.NewValue != null)
		{
			dropIconButton.TitleIcon.Data = (Geometry)e.NewValue;
			dropIconButton.TitleIcon.Visibility = Visibility.Visible;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/dropiconbutton.xaml", UriKind.Relative);
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
			Root = (DropIconButton)target;
			break;
		case 2:
			TitleIcon = (Path)target;
			break;
		case 3:
			TitleText = (EmjTextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
