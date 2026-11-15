using System.Collections.Generic;
using System.Windows;

namespace ticktick_WPF.Views.TaskList;

public interface IBatchEditable
{
	void ShowBatchOperationDialog();

	void SetSelectedTaskIds(List<string> taskIds);

	void RemoveSelectedId(string id);

	List<string> GetSelectedTaskIds();

	void ReloadList();

	UIElement BatchOperaPlacementTarget();

	void HideBatchSelectControl();
}
