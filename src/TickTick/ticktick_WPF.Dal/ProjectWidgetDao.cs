using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Dal;

public static class ProjectWidgetDao
{
	public static async Task CreateInboxWidget()
	{
		SortOption smartProjectSortOption = SmartProjectService.GetSmartProjectSortOption("inbox", inKanban: false);
		WidgetSettingModel widgetSettingModel = new WidgetSettingModel
		{
			id = Utils.GetGuid(),
			type = "normal",
			userId = LocalSettings.Settings.LoginUserId,
			identity = Utils.GetInboxId(),
			sortType = smartProjectSortOption.orderBy,
			groupType = smartProjectSortOption.groupBy,
			themeId = "dark",
			displayOption = "embed",
			autoHide = true,
			hideComplete = false,
			showCountDown = LocalSettings.Settings.ShowCountDown,
			opacity = 0.7f,
			left = 100.0,
			top = 100.0
		};
		Size? defaultSize = WidgetConstants.GetDefaultSize(WidgetType.List);
		if (defaultSize.HasValue)
		{
			widgetSettingModel.width = defaultSize.Value.Width;
			widgetSettingModel.height = defaultSize.Value.Height;
		}
		await App.Connection.InsertAsync(widgetSettingModel);
	}

	public static async Task<WidgetSettingModel> GetWidgetById(string widgetId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		WidgetSettingModel model = (await App.Connection.Table<WidgetSettingModel>().ToListAsync()).Where((WidgetSettingModel widget) => widget.id == widgetId && widget.userId == userId).FirstOrDefault();
		await ApplyDateDisplayDefault(model);
		return model;
	}

	public static async Task<List<WidgetSettingModel>> GetWidgets()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<WidgetSettingModel> models = await (from widget in App.Connection.Table<WidgetSettingModel>()
			where widget.userId == userId
			select widget).ToListAsync();
		foreach (WidgetSettingModel item in models)
		{
			await ApplyDateDisplayDefault(item);
		}
		return models;
	}

	private static async Task ApplyDateDisplayDefault(WidgetSettingModel model)
	{
		if (model != null && !model.showCountDown.HasValue)
		{
			model.showCountDown = LocalSettings.Settings.ShowCountDown;
			await App.Connection.UpdateAsync(model);
		}
	}

	public static async Task DeleteWidgetById(string id)
	{
		List<WidgetSettingModel> list = await (from v in App.Connection.Table<WidgetSettingModel>()
			where v.id == id
			select v).ToListAsync();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (WidgetSettingModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task UpdateWidget(WidgetSettingModel settingModel)
	{
		await App.Connection.UpdateAsync(settingModel);
	}
}
