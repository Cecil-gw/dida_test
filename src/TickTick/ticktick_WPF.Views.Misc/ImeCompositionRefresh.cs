using System;
using System.Threading.Tasks;

namespace ticktick_WPF.Views.Misc;

internal sealed class ImeCompositionRefresh
{
	private readonly Func<Task> _wait;

	private int _version;

	public ImeCompositionRefresh(Func<Task> wait = null)
	{
		_wait = wait ?? ((Func<Task>)(() => Task.Delay(50)));
	}

	public void Cancel()
	{
		_version++;
	}

	public async Task RefreshAfterCompositionAsync(Func<bool> isComposing, Func<bool> canRefresh, Action refresh)
	{
		int version = ++_version;
		while (version == _version && canRefresh())
		{
			if (!isComposing())
			{
				refresh();
				break;
			}
			await _wait();
		}
	}
}
