using System.Collections.Generic;

namespace ticktick_WPF.Views.QuickAdd;

public interface IQuickInput
{
	bool Filter(string key, List<string> selected = null);

	bool IsTag();

	void Move(bool forward);

	void TrySelectItem(bool exactly = false);
}
