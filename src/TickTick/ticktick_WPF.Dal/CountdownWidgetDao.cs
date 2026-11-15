using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Dal;

internal class CountdownWidgetDao : WidgetBaseDao<CountdownWidgetModel>
{
	public static async Task CreateWidget()
	{
		CountdownModel countdownModel = await CountdownDao.GetNearestCountdown();
		CountdownWidgetModel countdownWidgetModel = new CountdownWidgetModel
		{
			Id = Utils.GetGuid(),
			UserId = LocalSettings.Settings.LoginUserId,
			CountdownId = null,
			Style = (countdownModel?.Style ?? "normal"),
			StyleColor = countdownModel?.StyleColor,
			LocalImagePath = countdownModel?.LocalImagePath,
			Background = countdownModel?.Background,
			DisplayOption = "embed",
			ThemeId = "dark",
			Opacity = 0.7f,
			Left = 100.0,
			Top = 100.0,
			AutoHide = true
		};
		Size? defaultSize = WidgetConstants.GetDefaultSize(WidgetType.CountdownItem);
		if (defaultSize.HasValue)
		{
			countdownWidgetModel.Width = defaultSize.Value.Width;
			countdownWidgetModel.Height = defaultSize.Value.Height;
		}
		await App.Connection.InsertAsync(countdownWidgetModel);
	}
}
