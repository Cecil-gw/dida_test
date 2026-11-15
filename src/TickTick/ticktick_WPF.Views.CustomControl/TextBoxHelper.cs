using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.CustomControl;

public static class TextBoxHelper
{
	public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.RegisterAttached("Placeholder", typeof(string), typeof(TextBoxHelper), new PropertyMetadata(string.Empty, OnPlaceholderChanged));

	public static string GetPlaceholder(DependencyObject obj)
	{
		return (string)obj.GetValue(PlaceholderProperty);
	}

	public static void SetPlaceholder(DependencyObject obj, string value)
	{
		obj.SetValue(PlaceholderProperty, value);
	}

	private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		TextBox textBox = d as TextBox;
		if (textBox == null)
		{
			return;
		}
		textBox.GotFocus += delegate
		{
			if (textBox.Text == (string)e.NewValue)
			{
				textBox.Text = "";
				textBox.Foreground = ThemeUtil.GetColor("BaseColorOpacity100_80");
			}
		};
		textBox.LostFocus += delegate
		{
			if (string.IsNullOrEmpty(textBox.Text))
			{
				textBox.Text = (string)e.NewValue;
				textBox.Foreground = ThemeUtil.GetColor("BaseColorOpacity20_40");
			}
		};
		if (string.IsNullOrEmpty(textBox.Text))
		{
			textBox.Text = (string)e.NewValue;
			textBox.Foreground = ThemeUtil.GetColor("BaseColorOpacity20_40");
		}
	}
}
