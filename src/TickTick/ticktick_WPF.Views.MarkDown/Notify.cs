using System.Media;
using System.Windows;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

internal static class Notify
{
	public static void Alert(string alert, Window owner = null)
	{
		Show(alert, MessageBoxButton.OK, MessageBoxImage.Hand, owner);
	}

	public static MessageBoxResult ConfirmYesNo(string question, Window owner = null)
	{
		return Show(question, MessageBoxButton.YesNo, MessageBoxImage.Question, owner);
	}

	public static MessageBoxResult ConfirmYesNoCancel(string question, Window owner = null)
	{
		return Show(question, MessageBoxButton.YesNoCancel, MessageBoxImage.Question, owner);
	}

	private static MessageBoxResult Show(string message, MessageBoxButton button, MessageBoxImage image, Window owner)
	{
		string text = message ?? "null";
		Window window = owner ?? Application.Current?.MainWindow;
		if (Application.Current == null)
		{
			return MessageBoxResult.None;
		}
		return Application.Current.Dispatcher.Invoke(() => (window == null) ? MessageBox.Show(text, Utils.GetString("PublicTickTick"), button, image) : MessageBox.Show(window, text, Utils.GetString("PublicTickTick"), button, image));
	}

	public static void Beep()
	{
		SystemSounds.Beep.Play();
	}
}
