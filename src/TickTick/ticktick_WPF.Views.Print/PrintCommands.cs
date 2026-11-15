using System.Windows.Input;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Print;

public static class PrintCommands
{
	public static readonly ICommand PrintCommand = new RelayCommand(delegate(object o)
	{
		((PrintPreviewWindow)o).PrintCommand();
	});

	public static readonly ICommand CloseCommand = new RelayCommand(delegate(object o)
	{
		((PrintPreviewWindow)o).CloseCommand();
	});
}
