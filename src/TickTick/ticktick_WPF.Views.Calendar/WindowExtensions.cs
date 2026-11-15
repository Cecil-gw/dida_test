using System.Reflection;
using System.Windows;

namespace ticktick_WPF.Views.Calendar;

public static class WindowExtensions
{
	public static double GetActualLeft(this Window window)
	{
		return (double)typeof(Window).GetField("_actualLeft", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
	}

	public static double GetActualTop(this Window window)
	{
		return (double)typeof(Window).GetField("_actualTop", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
	}
}
