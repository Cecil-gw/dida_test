using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Theme;

namespace ticktick_WPF.Views;

public class FlexPanel : Panel
{
	public new static readonly DependencyProperty ActualWidthProperty = DependencyProperty.RegisterAttached("ActualWidth", typeof(double), typeof(FlexPanel), new PropertyMetadata(0.0));

	protected override Size MeasureOverride(Size availableSize)
	{
		List<UIElement> list = base.InternalChildren.OfType<UIElement>().ToList();
		if (list.Count == 0)
		{
			return new Size(0.0, 0.0);
		}
		double[] array = new double[list.Count];
		double[] array2 = new double[list.Count];
		double[] array3 = new double[list.Count];
		double[] array4 = new double[list.Count];
		double num = 0.0;
		double width = availableSize.Width;
		for (int i = 0; i < list.Count; i++)
		{
			UIElement uIElement = list[i];
			double num2 = uIElement.DesiredSize.Width;
			double num3 = 0.0;
			double num4 = double.PositiveInfinity;
			if (uIElement is FrameworkElement frameworkElement)
			{
				num3 = ((frameworkElement.MinWidth > 0.0) ? frameworkElement.MinWidth : 0.0);
				num4 = ((frameworkElement.MaxWidth > 0.0) ? frameworkElement.MaxWidth : double.PositiveInfinity);
				if (num4 > width)
				{
					num4 = width;
				}
			}
			if (uIElement is ContentPresenter { Content: TagViewModel content } && !string.IsNullOrEmpty(content.Tag))
			{
				string tag = content.Tag;
				try
				{
					FontFamily fontFamily = FontFamilyUtils.GetFontFamilyByKey(LocalSettings.Settings.ExtraSettings.AppFontFamily);
					if (fontFamily == null)
					{
						fontFamily = new FontFamily("Microsoft YaHei UI");
					}
					Typeface typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
					double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
					num2 = new FormattedText(tag, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 12.0, Brushes.Black, pixelsPerDip).WidthIncludingTrailingWhitespace + 18.0;
				}
				catch
				{
				}
			}
			if (num2 < num3)
			{
				num2 = num3;
			}
			if (num2 > num4)
			{
				num2 = num4;
			}
			array[i] = num2;
			array3[i] = num3;
			array4[i] = num4;
			num += num2;
		}
		if (num <= width)
		{
			for (int j = 0; j < list.Count; j++)
			{
				array2[j] = array[j];
			}
		}
		else
		{
			bool[] array5 = new bool[list.Count];
			double num5 = width;
			while (true)
			{
				int num6 = 0;
				double num7 = double.MaxValue;
				for (int k = 0; k < list.Count; k++)
				{
					if (!array5[k])
					{
						if (array[k] < num7)
						{
							num7 = array[k];
						}
						num6++;
					}
				}
				if (num6 == 0)
				{
					break;
				}
				double num8 = num5 / (double)num6;
				if (num7 > num8)
				{
					double num9 = num8;
					if (num9 < 0.0)
					{
						num9 = 0.0;
					}
					if (num9 > width)
					{
						num9 = width;
					}
					for (int l = 0; l < list.Count; l++)
					{
						if (!array5[l])
						{
							double num10 = num9;
							if (num10 < array3[l])
							{
								num10 = array3[l];
							}
							if (num10 > array4[l])
							{
								num10 = array4[l];
							}
							array2[l] = num10;
							array5[l] = true;
						}
					}
					break;
				}
				for (int m = 0; m < list.Count; m++)
				{
					if (!array5[m] && array[m] <= num8)
					{
						array2[m] = array[m];
						num5 -= array[m];
						array5[m] = true;
					}
				}
			}
		}
		for (int n = 0; n < list.Count; n++)
		{
			UIElement uIElement2 = list[n];
			double num11 = array2[n];
			uIElement2.Measure(new Size(num11, availableSize.Height));
			SetActualWidth(uIElement2, num11);
		}
		double num12 = 0.0;
		for (int num13 = 0; num13 < list.Count; num13++)
		{
			if (list[num13] != null)
			{
				num12 = Math.Max(num12, list[num13].DesiredSize.Height);
			}
		}
		return new Size(width, num12);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		double num = 0.0;
		foreach (UIElement internalChild in base.InternalChildren)
		{
			if (internalChild == null || !(internalChild is FrameworkElement frameworkElement))
			{
				continue;
			}
			double num2 = GetActualWidth(internalChild);
			if (double.IsNaN(num2) || num2 <= 0.0)
			{
				num2 = frameworkElement.DesiredSize.Width;
				if (double.IsNaN(num2) || num2 <= 0.0)
				{
					num2 = 10.0;
				}
			}
			num2 = Math.Max(0.0, num2);
			double height = finalSize.Height;
			double num3 = 0.0;
			num3 = Math.Max(0.0, frameworkElement.VerticalAlignment switch
			{
				VerticalAlignment.Top => 0.0, 
				VerticalAlignment.Bottom => finalSize.Height - frameworkElement.DesiredSize.Height, 
				VerticalAlignment.Center => (finalSize.Height - frameworkElement.DesiredSize.Height) / 2.0, 
				_ => 0.0, 
			});
			try
			{
				internalChild.Arrange(new Rect(num, num3, num2, height));
			}
			catch
			{
			}
			num += num2;
		}
		return finalSize;
	}

	public static double GetActualWidth(UIElement element)
	{
		return (double)element.GetValue(ActualWidthProperty);
	}

	public static void SetActualWidth(UIElement element, double value)
	{
		element.SetValue(ActualWidthProperty, value);
	}
}
