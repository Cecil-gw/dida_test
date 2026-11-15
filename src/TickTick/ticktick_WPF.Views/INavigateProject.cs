namespace ticktick_WPF.Views;

public interface INavigateProject
{
	void NavigateProjectById(string projectId);

	void NavigateTodayProject();

	void NavigateTomorrowProject();
}
