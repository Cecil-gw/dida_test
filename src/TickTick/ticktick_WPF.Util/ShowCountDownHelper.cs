using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Util;

public static class ShowCountDownHelper
{
	private static async Task<ShowCountDownModel> GetShowCountDownModel(string projectId)
	{
		List<ShowCountDownModel> list = await (from e in App.Connection.Table<ShowCountDownModel>()
			where e.UserId == LocalSettings.Settings.LoginUserId && e.ProjectId == projectId
			select e).ToListAsync();
		if (list != null && list.Count > 0)
		{
			return list[0];
		}
		ShowCountDownModel model = new ShowCountDownModel
		{
			UserId = LocalSettings.Settings.LoginUserId,
			ProjectId = projectId
		};
		await App.Connection.InsertAsync(model);
		return model;
	}

	public static async Task<bool> GetShowCountDown(string projectId)
	{
		return (await GetShowCountDownModel(projectId)).ShowCountDown;
	}

	public static async void SetShowCountDown(string projectId, bool showCountDown)
	{
		ShowCountDownModel showCountDownModel = await GetShowCountDownModel(projectId);
		showCountDownModel.ShowCountDown = showCountDown;
		await App.Connection.UpdateAsync(showCountDownModel);
		ProjectWidgetsHelper.OnShowCountDownChanged(projectId, showCountDown);
	}
}
