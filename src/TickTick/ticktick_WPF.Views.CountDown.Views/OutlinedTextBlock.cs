using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace ticktick_WPF.Views.CountDown.Views;

[ContentProperty("Text")]
public class OutlinedTextBlock : FrameworkElement
{
	public static readonly DependencyProperty FillProperty = DependencyProperty.Register("Fill", typeof(Brush), typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(3.0, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(SystemFonts.MessageFontSize, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty FontStretchProperty = TextElement.FontStretchProperty.AddOwner(typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(TextElement.FontStretchProperty.DefaultMetadata.DefaultValue, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(SystemFonts.MessageFontStyle, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(FontWeights.Bold, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty TextTrimmingProperty = DependencyProperty.Register("TextTrimming", typeof(TextTrimming), typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(TextTrimming.None, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty ParentWidthProperty = DependencyProperty.Register("ParentWidth", typeof(double), typeof(OutlinedTextBlock), new PropertyMetadata(0.0, OnParentWidthChanged));

	private Geometry _textGeometry;

	public Brush Fill
	{
		get
		{
			return (Brush)GetValue(FillProperty);
		}
		set
		{
			SetValue(FillProperty, value);
		}
	}

	public Brush Stroke
	{
		get
		{
			return (Brush)GetValue(StrokeProperty);
		}
		set
		{
			SetValue(StrokeProperty, value);
		}
	}

	public double StrokeThickness
	{
		get
		{
			return (double)GetValue(StrokeThicknessProperty);
		}
		set
		{
			SetValue(StrokeThicknessProperty, value);
		}
	}

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

	public FontFamily FontFamily
	{
		get
		{
			return (FontFamily)GetValue(FontFamilyProperty);
		}
		set
		{
			SetValue(FontFamilyProperty, value);
		}
	}

	public double FontSize
	{
		get
		{
			return (double)GetValue(FontSizeProperty);
		}
		set
		{
			SetValue(FontSizeProperty, value);
		}
	}

	public FontStretch FontStretch
	{
		get
		{
			return (FontStretch)GetValue(FontStretchProperty);
		}
		set
		{
			SetValue(FontStretchProperty, value);
		}
	}

	public FontStyle FontStyle
	{
		get
		{
			return (FontStyle)GetValue(FontStyleProperty);
		}
		set
		{
			SetValue(FontStyleProperty, value);
		}
	}

	public FontWeight FontWeight
	{
		get
		{
			return (FontWeight)GetValue(FontWeightProperty);
		}
		set
		{
			SetValue(FontWeightProperty, value);
		}
	}

	public TextTrimming TextTrimming
	{
		get
		{
			return (TextTrimming)GetValue(TextTrimmingProperty);
		}
		set
		{
			SetValue(TextTrimmingProperty, value);
		}
	}

	public double ParentWidth
	{
		get
		{
			return (double)GetValue(ParentWidthProperty);
		}
		set
		{
			SetValue(ParentWidthProperty, value);
		}
	}

	private static void OnParentWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		(d as OutlinedTextBlock).ReRender();
	}

	private void ReRender()
	{
		_textGeometry = null;
		InvalidateVisual();
		InvalidateMeasure();
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		EnsureGeometry();
		if (_textGeometry != null)
		{
			drawingContext.DrawGeometry(null, new Pen(Stroke, StrokeThickness), _textGeometry);
			drawingContext.DrawGeometry(Fill, null, _textGeometry);
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		EnsureGeometry();
		if (_textGeometry == null)
		{
			return new Size(0.0, 0.0);
		}
		Rect bounds = _textGeometry.Bounds;
		Size result = new Size(bounds.Width, bounds.Height);
		result.Width += StrokeThickness * 2.0;
		result.Height += StrokeThickness * 2.0;
		return result;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		return finalSize;
	}

	private void EnsureGeometry()
	{
		if (_textGeometry != null)
		{
			return;
		}
		FormattedText formattedText = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
		if (TextTrimming != TextTrimming.None)
		{
			string text = Text;
			double parentWidth = ParentWidth;
			if (formattedText.Width > parentWidth)
			{
				FormattedText formattedText2 = new FormattedText("...", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
				while (formattedText.Width + formattedText2.Width > parentWidth && text.Length > 0)
				{
					text = text.Substring(0, text.Length - 1);
					formattedText = new FormattedText(text + "...", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
				}
			}
		}
		_textGeometry = formattedText.BuildGeometry(new Point(StrokeThickness, StrokeThickness));
	}

	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		if (e.Property == TextProperty || e.Property == FontFamilyProperty || e.Property == FontSizeProperty || e.Property == FontStretchProperty || e.Property == FontStyleProperty || e.Property == FontWeightProperty || e.Property == StrokeThicknessProperty)
		{
			_textGeometry = null;
			InvalidateVisual();
			InvalidateMeasure();
		}
		base.OnPropertyChanged(e);
	}
}
