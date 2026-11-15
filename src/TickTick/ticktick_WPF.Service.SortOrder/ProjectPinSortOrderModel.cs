using SQLite;
using ticktick_WPF.Models;

namespace ticktick_WPF.Service.SortOrder;

[Table("SyncSortOrderModel")]
public class ProjectPinSortOrderModel : SyncSortOrderModel
{
	public ProjectPinSortOrderModel()
	{
		base.SortOrderType = "projectPinned";
	}

	public ProjectPinSortOrderModel(SyncSortOrderModel model)
	{
		base.SortOrderType = "projectPinned";
		SyncSortOrderModel.Copy(this, model);
	}
}
