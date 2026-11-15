using System.Windows.Media;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Detail;

public class AttachmentOptionPanelModel : BaseViewModel
{
	private string _fileName;

	private SolidColorBrush _backBrush;

	public string FileName
	{
		get
		{
			return _fileName;
		}
		set
		{
			_fileName = value;
			OnPropertyChanged("FileName");
		}
	}

	public SolidColorBrush BackBrush
	{
		get
		{
			return _backBrush;
		}
		set
		{
			_backBrush = value;
			OnPropertyChanged("_backBrush");
		}
	}
}
