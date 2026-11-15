using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.Kanban;

public interface IKanban
{
	event EventHandler<List<string>> SetSelected;

	void AddColumn(string columnId, HorizontalDirection direction);

	void DeleteColumn(string columnId, bool prompt);

	void Reload(bool forceReload = false, bool restoreSelete = false);

	int GetColumnCount();

	Task<List<string>> GetColumnNames();

	void SetInOperation();

	void CancelOperation();

	bool IsInDragging();

	string GetProjectId();

	void ClearSelected();

	KanbanViewModel GetViewModel();

	void Toast(string text);
}
