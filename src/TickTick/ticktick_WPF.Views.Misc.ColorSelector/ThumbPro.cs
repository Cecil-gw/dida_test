using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ticktick_WPF.Views.Misc.ColorSelector;

public class ThumbPro : Thumb
{
	public static readonly DependencyProperty TopProperty = DependencyProperty.Register("Top", typeof(double), typeof(ThumbPro), new PropertyMetadata(0.0));

	public static readonly DependencyProperty LeftProperty = DependencyProperty.Register("Left", typeof(double), typeof(ThumbPro), new PropertyMetadata(0.0));

	private double FirstTop;

	private double FirstLeft;

	public double Top
	{
		get
		{
			return (double)GetValue(TopProperty);
		}
		set
		{
			SetValue(TopProperty, value);
		}
	}

	public double Left
	{
		get
		{
			return (double)GetValue(LeftProperty);
		}
		set
		{
			SetValue(LeftProperty, value);
		}
	}

	public double Xoffset { get; set; }

	public double Yoffset { get; set; }

	public bool VerticalOnly { get; set; }

	public bool HorizontalOnly { get; set; }

	public double Xpercent => (Left + Xoffset) / base.ActualWidth;

	public double Ypercent => (Top + Yoffset) / base.ActualHeight;

	public event Action<double, double> ValueChanged;

	public void SetTopLeftByPercent(double xpercent, double ypercent)
	{
		if (!HorizontalOnly)
		{
			Top = ypercent * base.ActualHeight - Yoffset;
		}
		if (!VerticalOnly)
		{
			Left = xpercent * base.ActualWidth - Xoffset;
		}
	}

	public ThumbPro()
	{
		base.Loaded += delegate
		{
			if (!VerticalOnly)
			{
				Left = 0.0 - Xoffset;
			}
			if (!HorizontalOnly)
			{
				Top = 0.0 - Yoffset;
			}
		};
		base.DragStarted += delegate(object sender, DragStartedEventArgs e)
		{
			if (!VerticalOnly)
			{
				Left = e.HorizontalOffset - Xoffset;
				FirstLeft = Left;
			}
			if (!HorizontalOnly)
			{
				Top = e.VerticalOffset - Yoffset;
				FirstTop = Top;
			}
			this.ValueChanged?.Invoke(Xpercent, Ypercent);
		};
		base.DragDelta += delegate(object sender, DragDeltaEventArgs e)
		{
			if (!VerticalOnly)
			{
				double num = FirstLeft + e.HorizontalChange;
				if (num < 0.0 - Xoffset)
				{
					Left = 0.0 - Xoffset;
				}
				else if (num > base.ActualWidth - Xoffset)
				{
					Left = base.ActualWidth - Xoffset;
				}
				else
				{
					Left = num;
				}
			}
			if (!HorizontalOnly)
			{
				double num2 = FirstTop + e.VerticalChange;
				if (num2 < 0.0 - Yoffset)
				{
					Top = 0.0 - Yoffset;
				}
				else if (num2 > base.ActualHeight - Yoffset)
				{
					Top = base.ActualHeight - Yoffset;
				}
				else
				{
					Top = num2;
				}
			}
			this.ValueChanged?.Invoke(Xpercent, Ypercent);
		};
	}
}
