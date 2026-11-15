using System.Collections.ObjectModel;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync.Model;

public class SyncTaskBean1
{
	public ObservableCollection<TaskModel> update { get; set; }

	public ObservableCollection<TaskModel> add { get; set; }

	public ObservableCollection<TaskDeleteModel> delete { get; set; }

	public ObservableCollection<TaskDeleteModel> deletedInTrash { get; set; }

	public ObservableCollection<TaskDeleteModel> deletedForever { get; set; }
}
