using System.Threading.Tasks;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public interface IListViewParent
{
	void OnProjectWidthChanged(double width);

	void OnDetailWidthChanged(double width);

	void SaveSelectedProject(string saveProjectId);

	double GetProjectWidth();

	double GetDetailWidth();

	string GetSelectedProject();

	Task NavigateTask(ProjectTask task);

	void EnterImmersiveMode(string taskId, int caretIndex);

	void ExitImmersiveMode();

	void SetMinSize(int width, int height);

	double GetListContainerWidth();
}
