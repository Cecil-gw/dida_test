using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views;

public class PasswordBoxMonitor : DependencyObject
{
	public static readonly DependencyProperty IsMonitoringProperty = DependencyProperty.RegisterAttached("IsMonitoring", typeof(bool), typeof(PasswordBoxMonitor), new UIPropertyMetadata(false, OnIsMonitoringChanged));

	public static readonly DependencyProperty PasswordLengthProperty = DependencyProperty.RegisterAttached("PasswordLength", typeof(int), typeof(PasswordBoxMonitor), new UIPropertyMetadata(0));

	public static bool GetIsMonitoring(DependencyObject obj)
	{
		return (bool)obj.GetValue(IsMonitoringProperty);
	}

	public static void SetIsMonitoring(DependencyObject obj, bool value)
	{
		obj.SetValue(IsMonitoringProperty, value);
	}

	public static int GetPasswordLength(DependencyObject obj)
	{
		return (int)obj.GetValue(PasswordLengthProperty);
	}

	public static void SetPasswordLength(DependencyObject obj, int value)
	{
		obj.SetValue(PasswordLengthProperty, value);
	}

	private static void OnIsMonitoringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is PasswordBox passwordBox)
		{
			if ((bool)e.NewValue)
			{
				passwordBox.PasswordChanged += PasswordChanged;
			}
			else
			{
				passwordBox.PasswordChanged -= PasswordChanged;
			}
		}
	}

	private static void PasswordChanged(object sender, RoutedEventArgs e)
	{
		if (sender is PasswordBox passwordBox)
		{
			SetPasswordLength(passwordBox, passwordBox.Password.Length);
		}
	}
}
