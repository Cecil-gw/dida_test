using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Dal;

internal class HabitWidgetDao : WidgetBaseDao<HabitWidgetModel>
{
	public static async Task CreateHabitItemWidget()
	{
		HabitModel habitModel = await HabitDao.GetFirstHabit();
		HabitWidgetModel habitWidgetModel = new HabitWidgetModel
		{
			Id = Utils.GetGuid(),
			UserId = LocalSettings.Settings.LoginUserId,
			HabitId = habitModel?.Id,
			DisplayOption = "embed",
			ThemeId = "dark",
			Opacity = 1f,
			Left = 100.0,
			Top = 100.0,
			AutoHide = true
		};
		Size? defaultSize = WidgetConstants.GetDefaultSize(WidgetType.HabitItem);
		if (defaultSize.HasValue)
		{
			habitWidgetModel.Width = defaultSize.Value.Width;
			habitWidgetModel.Height = defaultSize.Value.Height;
		}
		await App.Connection.InsertAsync(habitWidgetModel);
	}
}
