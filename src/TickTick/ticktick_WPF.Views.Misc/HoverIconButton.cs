using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ticktick_WPF.Views.Misc;

public class HoverIconButton : Grid
{
	public readonly Border BackBorder;

	public string IconColor = "BaseColorOpacity100";

	private Path _icon;

	private Image _image;

	public static readonly DependencyProperty IconDataProperty = DependencyProperty.Register("IconData", typeof(Geometry), typeof(HoverIconButton), new PropertyMetadata(null));

	public static readonly DependencyProperty IsImageProperty = DependencyProperty.Register("IsImage", typeof(bool), typeof(HoverIconButton), new PropertyMetadata(true));

	public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(HoverIconButton), new PropertyMetadata(null));

	public static readonly DependencyProperty ImageWidthProperty = DependencyProperty.Register("ImageWidth", typeof(double), typeof(HoverIconButton), new PropertyMetadata(18.0));

	public static readonly DependencyProperty ImageOpacityProperty = DependencyProperty.Register("ImageOpacity", typeof(double), typeof(HoverIconButton), new PropertyMetadata(1.0));

	public Geometry IconData
	{
		get
		{
			return (Geometry)GetValue(IconDataProperty);
		}
		set
		{
			SetValue(IconDataProperty, value);
			if (_icon != null)
			{
				_icon.Data = value;
			}
		}
	}

	public bool IsImage
	{
		get
		{
			return (bool)GetValue(IsImageProperty);
		}
		set
		{
			SetValue(IsImageProperty, value);
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

	public double ImageOpacity
	{
		get
		{
			return (double)GetValue(ImageOpacityProperty);
		}
		set
		{
			SetValue(ImageOpacityProperty, value);
			if (_icon != null)
			{
				_icon.Opacity = value;
			}
			if (_image != null)
			{
				_image.Opacity = value;
			}
		}
	}

	public HoverIconButton()
	{
		base.Cursor = Cursors.Hand;
		base.HorizontalAlignment = HorizontalAlignment.Center;
		base.VerticalAlignment = VerticalAlignment.Center;
		BackBorder = new Border();
		BackBorder.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle");
		base.Children.Add(BackBorder);
		base.Width = 28.0;
		base.Height = 28.0;
		base.Loaded += OnLoaded;
		SetValue(ToolTipService.BetweenShowDelayProperty, 0);
		SetValue(ToolTipService.InitialShowDelayProperty, 500);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		if (IsImage)
		{
			_image = new Image
			{
				Width = ImageWidth,
				Height = ImageWidth,
				Opacity = ImageOpacity,
				Margin = new Thickness(2.0),
				IsHitTestVisible = false
			};
			_image.SetBinding(Image.SourceProperty, new Binding("ImageSource")
			{
				Source = this
			});
			_image.SetResourceReference(FrameworkElement.StyleProperty, "Icon01");
			base.Children.Add(_image);
		}
		else
		{
			_icon = new Path
			{
				Data = IconData,
				Width = ImageWidth,
				Height = ImageWidth,
				Opacity = ImageOpacity,
				Margin = new Thickness(2.0),
				IsHitTestVisible = false,
				Stretch = Stretch.Uniform
			};
			_icon.SetBinding(Path.DataProperty, new Binding("IconData")
			{
				Source = this
			});
			_icon.SetResourceReference(Shape.FillProperty, IconColor);
			base.Children.Add(_icon);
		}
	}

	public void SetIconColor(string color)
	{
		IconColor = color;
		_icon?.SetResourceReference(Shape.FillProperty, color);
	}
}
