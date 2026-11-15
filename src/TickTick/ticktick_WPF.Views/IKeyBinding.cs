using System;

namespace ticktick_WPF.Views;

public interface IKeyBinding
{
	void BatchSetPriorityCommand(int priority);

	void BatchSetDateCommand(DateTime? date);

	void BatchPinTaskCommand();

	void BatchOpenStickyCommand();

	void BatchDeleteCommand();
}
