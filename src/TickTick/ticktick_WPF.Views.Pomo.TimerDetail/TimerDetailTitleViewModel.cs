using TickTickModels;

namespace ticktick_WPF.Views.Pomo.TimerDetail;

public class TimerDetailTitleViewModel : TimerDetailItemViewModel
{
	public TimerModel TModel;

	public TimerDetailTitleViewModel(TimerModel model)
	{
		TModel = model;
		base.TimerTitle = true;
	}
}
