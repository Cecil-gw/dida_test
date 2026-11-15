using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Dal;

public static class SingleWidgetDao
{
	public static async Task<CalendarWidgetModel> CreateWidget(UniqueWidgetMode mode)
	{
		CalendarWidgetModel model = new CalendarWidgetModel
		{
			id = Utils.GetGuid(),
			userId = LocalSettings.Settings.LoginUserId,
			themeId = "dark",
			displayOption = "embed",
			autoHide = true,
			hideComplete = false,
			opacity = 0.7f,
			left = 300.0,
			top = 100.0,
			mode = mode
		};
		WidgetType type = WidgetType.Calendar;
		switch (mode)
		{
		case UniqueWidgetMode.Calendar:
			type = WidgetType.Calendar;
			break;
		case UniqueWidgetMode.Matrix:
			type = WidgetType.Matrix;
			model.activeViewId = LocalSettings.Settings.MatrixActiveViewId ?? "";
			break;
		case UniqueWidgetMode.CountdownList:
			type = WidgetType.CountdownList;
			break;
		case UniqueWidgetMode.HabitList:
			type = WidgetType.HabitList;
			break;
		case UniqueWidgetMode.AI:
			type = WidgetType.AI;
			break;
		}
		Size? defaultSize = WidgetConstants.GetDefaultSize(type);
		if (defaultSize.HasValue)
		{
			model.width = defaultSize.Value.Width;
			model.height = defaultSize.Value.Height;
		}
		await App.Connection.InsertAsync(model);
		return model;
	}

	public static async Task DeleteWidget(UniqueWidgetMode mode)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<CalendarWidgetModel> list = await (from widget in App.Connection.Table<CalendarWidgetModel>()
			where widget.userId == userId && (int)widget.mode == (int)mode
			select widget).ToListAsync();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (CalendarWidgetModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task SaveWidget(CalendarWidgetModel settingModel)
	{
		await App.Connection.UpdateAsync(settingModel);
	}

	public static async Task<CalendarWidgetModel> TryGetWidget(UniqueWidgetMode mode)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		CalendarWidgetModel widget = await (from calendarWidgetModel in App.Connection.Table<CalendarWidgetModel>()
			where calendarWidgetModel.userId == userId && (int)calendarWidgetModel.mode == (int)mode
			select calendarWidgetModel).FirstOrDefaultAsync();
		if (widget != null && mode == UniqueWidgetMode.Matrix && string.IsNullOrEmpty(widget.activeViewId))
		{
			widget.activeViewId = LocalSettings.Settings.MatrixActiveViewId ?? "";
			await SaveWidget(widget);
		}
		return widget;
	}
}
