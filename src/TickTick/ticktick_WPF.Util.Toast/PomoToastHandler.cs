using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Util.Toast;

public class PomoToastHandler : IToastHandler
{
	public Task Exec(ToastOptionModel optModel, List<KeyValuePair<string, object>> kvs)
	{
		ToastType toastType = optModel.ToastType;
		if ((uint)toastType <= 1u)
		{
			if (optModel.CurrentOptionName == "Complete")
			{
				if (TickFocusManager.Status == PomoStatus.WaitingWork)
				{
					UserActCollectUtils.AddClickEvent("focus", "start_from", "reminder");
				}
				if (TickFocusManager.Status == PomoStatus.WaitingWork || TickFocusManager.Status == PomoStatus.WaitingRelax)
				{
					FocusTimer.BeginTimer();
				}
			}
			else if (optModel.CurrentOptionName == "Exit")
			{
				FocusTimer.Reset();
			}
		}
		return Task.CompletedTask;
	}
}
