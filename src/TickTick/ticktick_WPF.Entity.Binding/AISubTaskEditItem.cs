using System.Windows.Media;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Entity.Binding;

internal class AISubTaskEditItem : CheckableItem
{
	private string _dateText = "";

	private SolidColorBrush _dateTextColor;

	public bool NeedFocusInLoad { get; set; }

	public string DateText
	{
		get
		{
			return _dateText;
		}
		set
		{
			_dateText = value;
			NotifyPropertyChanged("DateText");
		}
	}

	public SolidColorBrush DateTextColor
	{
		get
		{
			return _dateTextColor;
		}
		set
		{
			_dateTextColor = value;
			NotifyPropertyChanged("DateTextColor");
		}
	}

	public TimeData TimeData { get; set; }

	public string TaskId { get; set; }
}
