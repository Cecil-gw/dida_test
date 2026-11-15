using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public class CalendarHeadWeekModel : BaseViewModel
{
	private string _textValue;

	public string TextValue
	{
		get
		{
			return _textValue;
		}
		set
		{
			_textValue = value;
			OnPropertyChanged("TextValue");
		}
	}
}
