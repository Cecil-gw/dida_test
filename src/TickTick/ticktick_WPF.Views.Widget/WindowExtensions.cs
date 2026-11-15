using System.Windows;

namespace ticktick_WPF.Views.Widget;

public static class WindowExtensions
{
	public static readonly DependencyProperty SinkerProperty = DependencyProperty.RegisterAttached("Sinker", typeof(WindowSinker), typeof(WindowExtensions), new UIPropertyMetadata(null));

	public static readonly DependencyProperty AlwaysOnBottomProperty = DependencyProperty.RegisterAttached("AlwaysOnBottom", typeof(bool), typeof(WindowExtensions), new UIPropertyMetadata(false, OnAlwaysOnBottomChanged));

	public static WindowSinker GetSinker(DependencyObject obj)
	{
		return (WindowSinker)obj.GetValue(SinkerProperty);
	}

	public static void SetSinker(DependencyObject obj, WindowSinker value)
	{
		obj.SetValue(SinkerProperty, value);
	}

	public static bool GetAlwaysOnBottom(DependencyObject obj)
	{
		return (bool)obj.GetValue(AlwaysOnBottomProperty);
	}

	public static void SetAlwaysOnBottom(DependencyObject obj, bool value)
	{
		obj.SetValue(AlwaysOnBottomProperty, value);
	}

	private static void OnAlwaysOnBottomChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is Window window)
		{
			if ((bool)e.NewValue)
			{
				WindowSinker windowSinker = new WindowSinker(window);
				windowSinker.Sink();
				SetSinker(window, windowSinker);
			}
			else
			{
				GetSinker(window).Unsink();
				SetSinker(window, null);
			}
		}
	}
}
