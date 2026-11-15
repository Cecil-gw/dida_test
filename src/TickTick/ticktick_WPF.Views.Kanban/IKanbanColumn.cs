namespace ticktick_WPF.Views.Kanban;

public interface IKanbanColumn
{
	bool TaskChanged { get; set; }

	string GetColumnId();

	void ReloadColumn();
}
