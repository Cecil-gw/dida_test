using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace ticktick_WPF.Views.Misc;

public class OptionItemWithImageIcon : ContentControl, IComponentConnector
{
	public static readonly DependencyProperty HoverColorProperty = DependencyProperty.Register("HoverColor", typeof(SolidColorBrush), typeof(OptionItemWithImageIcon), new PropertyMetadata(Brushes.Transparent));

	public static readonly DependencyProperty HoverSelectedProperty = DependencyProperty.Register("HoverSelected", typeof(bool), typeof(OptionItemWithImageIcon), new PropertyMetadata(false));

	public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(OptionItemWithImageIcon), new PropertyMetadata(null));

	public static readonly DependencyProperty ImageKeyProperty = DependencyProperty.Register("ImageKey", typeof(string), typeof(OptionItemWithImageIcon), new PropertyMetadata(string.Empty, OnImageKeyChanged));

	public new static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(string), typeof(OptionItemWithImageIcon), new PropertyMetadata(string.Empty));

	public static readonly DependencyProperty ImageWidthProperty = DependencyProperty.Register("ImageWidth", typeof(double), typeof(OptionItemWithImageIcon), new PropertyMetadata(20.0));

	public static readonly DependencyProperty TextPaddingProperty = DependencyProperty.Register("TextPadding", typeof(Thickness), typeof(OptionItemWithImageIcon), new PropertyMetadata(new Thickness(6.0, 0.0, 14.0, 0.0)));

	public static readonly DependencyProperty ImageMarginProperty = DependencyProperty.Register("ImageMargin", typeof(Thickness), typeof(OptionItemWithImageIcon), new PropertyMetadata(new Thickness(14.0, 0.0, 0.0, 0.0)));

	public static readonly DependencyProperty ImageOpacityProperty = DependencyProperty.Register("ImageOpacity", typeof(double), typeof(OptionItemWithImageIcon), new PropertyMetadata(0.6));

	public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register("Selected", typeof(bool), typeof(OptionItemWithImageIcon), new PropertyMetadata(false));

	public static readonly DependencyProperty IsAIProperty = DependencyProperty.Register("IsAI", typeof(bool), typeof(OptionItemWithImageIcon), new PropertyMetadata(false));

	internal OptionItemWithImageIcon Root;

	internal Image Image;

	internal TextBlock ContentText;

	private bool _contentLoaded;

	public SolidColorBrush HoverColor
	{
		get
		{
			return (SolidColorBrush)GetValue(HoverColorProperty);
		}
		set
		{
			SetValue(HoverColorProperty, value);
		}
	}

	public bool HoverSelected
	{
		get
		{
			return (bool)GetValue(HoverSelectedProperty);
		}
		set
		{
			SetValue(HoverSelectedProperty, value);
		}
	}

	public ImageSource ImageSource
	{
		get
		{
			return (ImageSource)GetValue(ImageSourceProperty);
		}
		set
		{
			SetValue(ImageSourceProperty, value);
		}
	}

	public string ImageKey
	{
		get
		{
			return (string)GetValue(ImageKeyProperty);
		}
		set
		{
			SetValue(ImageKeyProperty, value);
		}
	}

	public double CustomFontSize
	{
		get
		{
			return ContentText.FontSize;
		}
		set
		{
			ContentText.FontSize = value;
		}
	}

	public new string Content
	{
		get
		{
			return (string)GetValue(ContentProperty);
		}
		set
		{
			SetValue(ContentProperty, value);
		}
	}

	public double ImageWidth
	{
		get
		{
			return (double)GetValue(ImageWidthProperty);
		}
		set
		{
			SetValue(ImageWidthProperty, value);
		}
	}

	public Thickness TextPadding
	{
		get
		{
			return (Thickness)GetValue(TextPaddingProperty);
		}
		set
		{
			SetValue(TextPaddingProperty, value);
		}
	}

	public Thickness ImageMargin
	{
		get
		{
			return (Thickness)GetValue(ImageMarginProperty);
		}
		set
		{
			SetValue(ImageMarginProperty, value);
		}
	}

	public double ImageOpacity
	{
		get
		{
			return (double)GetValue(ImageOpacityProperty);
		}
		set
		{
			SetValue(ImageOpacityProperty, value);
		}
	}

	public bool Selected
	{
		get
		{
			return (bool)GetValue(SelectedProperty);
		}
		set
		{
			SetValue(SelectedProperty, value);
		}
	}

	public bool IsAI
	{
		get
		{
			return (bool)GetValue(IsAIProperty);
		}
		set
		{
			SetValue(IsAIProperty, value);
		}
	}

	private static void OnImageKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is OptionItemWithImageIcon optionItemWithImageIcon && !string.IsNullOrEmpty(optionItemWithImageIcon.ImageKey))
		{
			optionItemWithImageIcon.Image.SetResourceReference(Image.SourceProperty, optionItemWithImageIcon.ImageKey);
		}
	}

	public OptionItemWithImageIcon()
	{
		InitializeComponent();
		SetResourceReference(HoverColorProperty, "BaseColorOpacity4");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/optionitemwithimageicon.xaml", UriKind.Relative);
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
			Root = (OptionItemWithImageIcon)target;
			break;
		case 2:
			Image = (Image)target;
			break;
		case 3:
			ContentText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
