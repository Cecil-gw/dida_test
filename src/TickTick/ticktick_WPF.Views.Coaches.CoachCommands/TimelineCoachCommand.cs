using System;
using System.Windows.Input;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Coaches.CoachCommands;

public class TimelineCoachCommand : ICommand
{
	public event EventHandler CanExecuteChanged;

	public bool CanExecute(object parameter)
	{
		return true;
	}

	public void Execute(object parameter)
	{
		UserActCollectUtils.AddClickEvent("timeline", "user_guide", "learn_more");
		if (Utils.IsDida())
		{
			Utils.TryProcessStartUrl("https://help.dida365.com/articles/6950656549178048512");
		}
		else
		{
			Utils.TryProcessStartUrl("https://help.ticktick.com/articles/7055782331050622976");
		}
	}
}
