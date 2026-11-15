using ticktick_WPF.Models;

namespace ticktick_WPF.Views.Setting;

public class LockViewModel
{
	public bool MinLock { get; set; }

	public bool LockAfter { get; set; }

	public int LockInterval { get; set; }

	public bool LockWidget { get; set; }

	public LockViewModel(AppLockModel model)
	{
		MinLock = model.MinLock;
		LockAfter = model.LockAfter;
		LockInterval = model.LockInterval;
		LockWidget = model.LockWidget;
	}
}
