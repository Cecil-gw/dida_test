using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Theme;

namespace ticktick_WPF.Views.Misc;

public class ClockControl : StackPanel
{
	public bool ShowHour;

	public bool ShowSecond = true;

	private TextBlock _hourText;

	private TextBlock _minuteText;

	private TextBlock _secondText;

	private TextBlock _hourSplit;

	private TextBlock _secondSplit;

	public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(double), typeof(ClockControl), new PropertyMetadata(16.0, OnFontSizeChanged));

	public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(ClockControl), new PropertyMetadata(Brushes.Black, OnForegroundChanged));

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

	public Brush Foreground
	{
		get
		{
			return (Brush)GetValue(ForegroundProperty);
		}
		set
		{
			SetValue(ForegroundProperty, value);
		}
	}

	public int? ShowHourFont { get; set; }

	public ClockControl(double size, string foreground)
	{
	}

	private void SetFontFamily(object sender, EventArgs e)
	{
		FontFamily focusFontFamily = FontFamilyUtils.GetFocusFontFamily();
		_hourText.FontFamily = focusFontFamily;
		_hourSplit.FontFamily = focusFontFamily;
		_minuteText.FontFamily = focusFontFamily;
		_secondSplit.FontFamily = focusFontFamily;
		_secondText.FontFamily = focusFontFamily;
	}

	public ClockControl()
	{
		base.Orientation = Orientation.Horizontal;
		base.VerticalAlignment = VerticalAlignment.Center;
		LoadChildren();
		base.Loaded += delegate
		{
			DataChangedNotifier.FontFamilyChanged -= SetFontFamily;
			DataChangedNotifier.FontFamilyChanged += SetFontFamily;
		};
		base.Unloaded += delegate
		{
			DataChangedNotifier.FontFamilyChanged -= SetFontFamily;
		};
	}

	private void LoadChildren()
	{
		_hourText = GetTextBlock();
		base.Children.Add(_hourText);
		_hourSplit = GetTextBlock();
		_hourSplit.Text = ":";
		_hourSplit.Margin = new Thickness(0.0, 0.0, 0.0, 2.5);
		base.Children.Add(_hourSplit);
		_minuteText = GetTextBlock();
		base.Children.Add(_minuteText);
		_secondSplit = GetTextBlock();
		_secondSplit.Text = ":";
		_secondSplit.Margin = new Thickness(0.0, 0.0, 0.0, 2.5);
		base.Children.Add(_secondSplit);
		_secondText = GetTextBlock();
		base.Children.Add(_secondText);
		SetFontFamily(null, null);
	}

	private TextBlock GetTextBlock()
	{
		TextBlock textBlock = new TextBlock();
		textBlock.FontSize = 16.0;
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100_80");
		return textBlock;
	}

	private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is ClockControl clockControl && e.NewValue is double fontSize)
		{
			clockControl.SetFontSize(fontSize);
		}
	}

	private void SetFontSize(double fontsize)
	{
		_hourText.FontSize = fontsize;
		_hourSplit.FontSize = fontsize;
		_hourSplit.Margin = new Thickness(0.0, 0.0, 0.0, fontsize / 6.0);
		_minuteText.FontSize = fontsize;
		_secondSplit.FontSize = fontsize;
		_secondSplit.Margin = new Thickness(0.0, 0.0, 0.0, fontsize / 6.0);
		_secondText.FontSize = fontsize;
	}

	private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is ClockControl clockControl && e.NewValue is Brush foreground)
		{
			clockControl.SetForeground(foreground);
		}
	}

	private void SetForeground(Brush brush)
	{
		_hourText.Foreground = brush;
		_hourSplit.Foreground = brush;
		_minuteText.Foreground = brush;
		_secondSplit.Foreground = brush;
		_secondText.Foreground = brush;
	}

	public void SetTime(int second)
	{
		if (second < 0 || second > 86400)
		{
			UtilLog.Info("ErrorSecond " + second + " current " + TickFocusManager.Config.CurrentSeconds);
			second = 0;
		}
		_hourText.Visibility = ((!ShowHour || second < 3600) ? Visibility.Collapsed : Visibility.Visible);
		_hourSplit.Visibility = _hourText.Visibility;
		if (ShowHourFont.HasValue)
		{
			if (_hourText.Visibility == Visibility.Visible && Math.Abs(_hourText.FontSize - (double)ShowHourFont.Value) > 1.0)
			{
				SetFontSize(ShowHourFont.Value);
			}
			else if (_hourText.Visibility != Visibility.Visible && Math.Abs(_hourText.FontSize - FontSize) > 1.0)
			{
				SetFontSize(FontSize);
			}
		}
		if (ShowHour)
		{
			int num = second / 60;
			_hourText.Text = PaddingTimeText(num / 60);
			_minuteText.Text = PaddingTimeText(num % 60);
			_secondText.Text = PaddingTimeText(second % 60);
		}
		else
		{
			_minuteText.Text = PaddingTimeText(second / 60);
			_secondText.Text = PaddingTimeText(second % 60);
		}
	}

	private static string PaddingTimeText(int count)
	{
		if (count >= 0 && count < 10)
		{
			return "0" + count;
		}
		return count.ToString();
	}
}
