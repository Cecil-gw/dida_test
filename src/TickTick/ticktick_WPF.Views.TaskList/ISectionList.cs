using System.Threading.Tasks;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.TaskList;

public interface ISectionList
{
	void OnSectionStatusChanged(SectionStatus status);

	Task OnTaskOpenClick(DisplayItemModel model);

	Task OnAddTaskInSectionClick(DisplayItemModel model);

	void SelectOrDeselectAll(DisplayItemModel model, bool selectAll);
}
