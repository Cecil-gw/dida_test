using System.Windows.Input;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MarkDown;

public static class ScaleCommands
{
	public static readonly ICommand ZoomInCommand = new RelayCommand(delegate(object o)
	{
		((ImmersiveContent)o).Zoom(zoomIn: true);
	});

	public static readonly ICommand ZoomOutCommand = new RelayCommand(delegate(object o)
	{
		((ImmersiveContent)o).Zoom(zoomIn: false);
	});
}
