using System;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class AppLockModel : BaseModel
{
	public string UserId { get; set; }

	public string Password { get; set; }

	public bool MinLock { get; set; }

	public bool LockAfter { get; set; }

	public int LockInterval { get; set; }

	public bool LockWidget { get; set; }

	public bool Locked { get; set; }

	public bool Equal(AppLockModel model)
	{
		if (MinLock == model.MinLock && LockAfter == model.LockAfter && LockWidget == model.LockWidget)
		{
			return LockInterval == model.LockInterval;
		}
		return false;
	}
}
