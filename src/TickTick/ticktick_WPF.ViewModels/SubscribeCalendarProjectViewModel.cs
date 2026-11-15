using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.ViewModels;

public class SubscribeCalendarProjectViewModel : ProjectItemViewModel
{
	public readonly CalendarSubscribeProfileModel Profile;

	public SubscribeCalendarProjectViewModel(CalendarSubscribeProfileModel profile)
	{
		Profile = profile;
		base.Id = profile.Id;
		base.Icon = Utils.GetGeometry("Ic.ProjectList.Url");
		base.Title = profile.Name;
		base.CanDrag = true;
		base.CanDrop = false;
		base.IsPtfItem = true;
		ListType = ProjectListType.Calendar;
		if (profile.Order.HasValue)
		{
			base.SortOrder = profile.Order.Value;
		}
		if (profile.Expired)
		{
			base.InfoIcon = Utils.GetGeometry("Ic.Common.Warning");
			base.Count = 0;
			base.InfoIconBrush = ThemeUtil.GetColorInString("#FFB000");
		}
	}

	public override async Task<IEnumerable<ContextAction>> GetContextActions()
	{
		string saveIdentity = GetSaveIdentity();
		bool windowShowing = !string.IsNullOrEmpty(saveIdentity) && TaskListWindow.Windows.ContainsKey(saveIdentity);
		List<ContextAction> list = new List<ContextAction>
		{
			new ContextAction(ContextActionKey.Edit)
		};
		List<ContextAction> list2 = list;
		list2.Add(new ContextAction((await ProjectPinSortOrderService.CheckIsProjectPinned(Profile.Id, 9)) ? ContextActionKey.Unpin : ContextActionKey.Pin));
		list.Add(new ContextAction(windowShowing ? ContextActionKey.CloseWindow : ContextActionKey.OpenNewWindow));
		list.Add(new ContextAction(ContextActionKey.Unsubscribe));
		return list;
	}

	public override string GetSaveIdentity()
	{
		return "subscribe_calendar:" + Profile.Id;
	}

	public override async void LoadCount()
	{
		if (LocalSettings.Settings.ExtraSettings.NumDisplayType == 2)
		{
			base.Count = 0;
			return;
		}
		base.Count = await Task.Run(async () => (await CalendarEventDao.GetDisplayEventsInCalendar(Profile.Id, 1, DateTime.Now.Date, DateTime.Now.AddMonths(3))).Count);
	}

	public override ProjectIdentity GetIdentity()
	{
		return new SubscribeCalendarProjectIdentity(Profile);
	}

	public override PtfType GetPtfType()
	{
		return PtfType.Subscribe;
	}
}
