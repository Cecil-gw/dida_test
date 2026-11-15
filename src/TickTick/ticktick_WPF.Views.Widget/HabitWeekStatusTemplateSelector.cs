using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Widget;

internal class HabitWeekStatusTemplateSelector : DataTemplateSelector
{
	public DataTemplate UnFinishRealTemplate { get; set; }

	public DataTemplate UnFinishBoolTemplate { get; set; }

	public DataTemplate SkipDayTemplate { get; set; }

	public DataTemplate AbandonTemplate { get; set; }

	public DataTemplate CompletedTemplate { get; set; }

	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is HabitDayCheckModel habitDayCheckModel)
		{
			if (habitDayCheckModel.Completed)
			{
				return CompletedTemplate;
			}
			if (habitDayCheckModel.Uncompleted)
			{
				return AbandonTemplate;
			}
			if (!habitDayCheckModel.NeedCheck && habitDayCheckModel.Percent <= 0.0)
			{
				return SkipDayTemplate;
			}
			if (habitDayCheckModel.IsBooleanHabit)
			{
				return UnFinishBoolTemplate;
			}
			return UnFinishRealTemplate;
		}
		return base.SelectTemplate(item, container);
	}
}
