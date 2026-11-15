using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Dal;

internal class HabitWeekWidgetDao : WidgetBaseDao<HabitWeekWidgetModel>
{
	public static async Task CreateHabitWeekItemWidget()
	{
		HabitModel habitModel = await HabitDao.GetFirstHabit();
		HabitWeekWidgetModel habitWeekWidgetModel = new HabitWeekWidgetModel
		{
			Id = Utils.GetGuid(),
			UserId = LocalSettings.Settings.LoginUserId,
			HabitId = habitModel?.Id,
			DisplayOption = "embed",
			ThemeId = "dark",
			Opacity = 0.7f,
			Left = 300.0,
			Top = 100.0,
			AutoHide = true
		};
		Size? defaultSize = WidgetConstants.GetDefaultSize(WidgetType.HabitWeekItem);
		if (defaultSize.HasValue)
		{
			habitWeekWidgetModel.Width = defaultSize.Value.Width;
			habitWeekWidgetModel.Height = defaultSize.Value.Height;
		}
		await App.Connection.InsertAsync(habitWeekWidgetModel);
	}
}
