using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TickTickUtils;

namespace ticktick_WPF.Util;

public class UiUtils
{
	private static Storyboard _toastStory;

	private static Storyboard GetToastStory(Border toastBorder)
	{
		if (_toastStory == null)
		{
			_toastStory = new Storyboard();
			DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, 1.0, 150);
			DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(null, 0.0, 150);
			doubleAnimation2.BeginTime = TimeSpan.FromSeconds(1.8);
			_toastStory.Children.Add(doubleAnimation);
			_toastStory.Children.Add(doubleAnimation2);
			_toastStory.Completed += delegate
			{
				_toastStory = null;
				toastBorder.Visibility = Visibility.Collapsed;
			};
			Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(UIElement.OpacityProperty));
			Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath(UIElement.OpacityProperty));
		}
		return _toastStory;
	}

	public static void SetSaveButtonTabSelected(Button button, bool tabSelected)
	{
		if (tabSelected)
		{
			button.BorderThickness = new Thickness(1.0);
			button.SetResourceReference(Control.BorderBrushProperty, "TabBorderColor");
			button.SetResourceReference(Control.BackgroundProperty, "PrimaryColor60");
		}
		else
		{
			button.BorderThickness = new Thickness(0.0);
			button.SetResourceReference(Control.BackgroundProperty, "TabBorderColor");
		}
	}

	public static void SetCancelButtonTabSelected(Button button, bool tabSelected)
	{
		button.SetResourceReference(Control.BorderBrushProperty, tabSelected ? "TabBorderColor" : "BaseColorOpacity20");
	}

	public static void SetBorderTabSelected(Border border, bool tabSelected)
	{
		if (tabSelected)
		{
			border.SetResourceReference(Control.BorderBrushProperty, "TabBorderColor");
		}
		else
		{
			border.BorderBrush = Brushes.Transparent;
		}
	}

	public static Path GetArrow(double size, double angle, string color)
	{
		Path path = new Path
		{
			Height = size,
			Width = size,
			Data = Utils.GetGeometry("Ic.Common.DownArrow"),
			RenderTransformOrigin = new Point(0.5, 0.5)
		};
		if (angle != 0.0)
		{
			path.RenderTransform = new RotateTransform(angle);
		}
		path.SetResourceReference(FrameworkElement.StyleProperty, "Path01");
		path.SetResourceReference(Shape.FillProperty, color);
		return path;
	}

	public static void SetTimeTextRun(TextBlock text, long minutes, int size, string foreground)
	{
		long num = minutes / 60;
		long num2 = minutes % 60;
		text.Inlines.Clear();
		if (num > 0)
		{
			text.Inlines.Add(new Run(num.ToString() ?? ""));
			Run run = new Run(" h ")
			{
				FontSize = size
			};
			run.SetResourceReference(TextElement.ForegroundProperty, foreground);
			text.Inlines.Add(run);
		}
		if (num == 0L || num2 > 0)
		{
			text.Inlines.Add(new Run(num2.ToString() ?? ""));
			Run run2 = new Run(" m ")
			{
				FontSize = size
			};
			run2.SetResourceReference(TextElement.ForegroundProperty, foreground);
			text.Inlines.Add(run2);
		}
	}

	public static Path CreatePath(string geoKey, string colorKey, string styleKey, double size = -1.0, bool geoKeyIsGeo = false)
	{
		Path path = new Path();
		if (!string.IsNullOrEmpty(geoKey))
		{
			path.Data = (geoKeyIsGeo ? Utils.GetGeometry(geoKey) : Utils.GetIcon(geoKey));
		}
		if (!string.IsNullOrEmpty(colorKey))
		{
			path.SetResourceReference(Shape.FillProperty, colorKey);
		}
		if (!string.IsNullOrEmpty(styleKey))
		{
			path.SetResourceReference(FrameworkElement.StyleProperty, styleKey);
		}
		if (size > 0.0)
		{
			path.Width = size;
			path.Height = size;
		}
		return path;
	}

	public static FrameworkElement GetLabel(string getString)
	{
		Border border = new Border();
		border.VerticalAlignment = VerticalAlignment.Center;
		border.HorizontalAlignment = HorizontalAlignment.Left;
		border.CornerRadius = new CornerRadius(2.0);
		border.SetResourceReference(Border.BackgroundProperty, "PrimaryColor20");
		TextBlock textBlock = new TextBlock
		{
			Text = Utils.GetString("Add"),
			Margin = new Thickness(4.0, 0.0, 4.0, 0.0),
			FontSize = 10.0,
			VerticalAlignment = VerticalAlignment.Center,
			TextAlignment = TextAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			LineHeight = 14.0
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryColor");
		border.Child = textBlock;
		return border;
	}

	public static void Toast(Grid grid, string text, int columnSpan = 1, int rowSpan = 1, Thickness margin = default(Thickness))
	{
		Border border = null;
		foreach (object child2 in grid.Children)
		{
			if (child2 is Border { Name: "ToastBorder" } border2)
			{
				border = border2;
			}
		}
		if (border == null)
		{
			border = new Border
			{
				CornerRadius = new CornerRadius(6.0),
				MinHeight = 40.0,
				Visibility = Visibility.Collapsed,
				HorizontalAlignment = HorizontalAlignment.Center,
				Name = "ToastBorder",
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = ((margin == default(Thickness)) ? new Thickness(0.0, 0.0, 0.0, 40.0) : margin)
			};
			border.SetValue(Grid.ColumnSpanProperty, columnSpan);
			border.SetValue(Grid.RowSpanProperty, rowSpan);
			border.SetValue(Panel.ZIndexProperty, 1000);
			border.SetResourceReference(Border.BackgroundProperty, "ToastBackground");
			Border border3 = new Border
			{
				CornerRadius = new CornerRadius(6.0),
				BorderThickness = new Thickness(1.0)
			};
			border.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity0_10");
			border.Child = border3;
			TextBlock textBlock = new TextBlock
			{
				FontSize = 13.0,
				Foreground = Brushes.White,
				Margin = new Thickness(12.0, 0.0, 12.0, 0.0),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			textBlock.Text = text;
			border3.Child = textBlock;
			grid.Children.Add(border);
		}
		else if (border.Child is Border { Child: TextBlock child })
		{
			child.Text = text;
		}
		border.Visibility = Visibility.Visible;
		Storyboard toastStory = GetToastStory(border);
		toastStory.Stop();
		border.BeginStoryboard(toastStory);
	}
}
