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

public class BindCalendarAccountProjectViewModel : ProjectItemViewModel
{
	public readonly BindCalendarAccountModel Account;

	public BindCalendarAccountProjectViewModel(BindCalendarAccountModel account)
	{
		if (account.Expired)
		{
			base.InfoIcon = Utils.GetGeometry("Ic.Common.Warning");
			base.Count = 0;
			base.InfoIconBrush = ThemeUtil.GetColorInString("#FFB000");
		}
		Account = account;
		base.Id = account.Id;
		base.Title = account.DisplayName;
		if (account.Site == "feishu")
		{
			base.Title = Utils.GetString("FeishuCalendar");
		}
		base.Icon = SubscribeCalendarHelper.GetCalendarProjectIcon(account);
		base.CanDrag = true;
		base.CanDrop = false;
		base.IsPtfItem = true;
		ListType = ProjectListType.Calendar;
		if (account.Order.HasValue)
		{
			base.SortOrder = account.Order.Value;
		}
	}

	public override string GetSaveIdentity()
	{
		return "bind_account:" + Account.Id;
	}

	public override async Task<IEnumerable<ContextAction>> GetContextActions()
	{
		string saveIdentity = GetSaveIdentity();
		bool windowShowing = !string.IsNullOrEmpty(saveIdentity) && TaskListWindow.Windows.ContainsKey(saveIdentity);
		List<ContextAction> list2;
		List<ContextAction> list;
		if (Account.Expired)
		{
			list = new List<ContextAction>
			{
				new ContextAction(ContextActionKey.Reauthorize)
			};
			list2 = list;
			list2.Add(new ContextAction((await ProjectPinSortOrderService.CheckIsProjectPinned(Account.Id, 9)) ? ContextActionKey.Unpin : ContextActionKey.Pin));
			list.Add(new ContextAction(windowShowing ? ContextActionKey.CloseWindow : ContextActionKey.OpenNewWindow));
			list.Add(new ContextAction(ContextActionKey.Unsubscribe));
			return list;
		}
		list2 = new List<ContextAction>
		{
			new ContextAction(ContextActionKey.Edit)
		};
		list = list2;
		list.Add(new ContextAction((await ProjectPinSortOrderService.CheckIsProjectPinned(Account.Id, 9)) ? ContextActionKey.Unpin : ContextActionKey.Pin));
		list2.Add(new ContextAction(windowShowing ? ContextActionKey.CloseWindow : ContextActionKey.OpenNewWindow));
		list2.Add(new ContextAction(ContextActionKey.Unsubscribe));
		return list2;
	}

	public override async void LoadCount()
	{
		if (LocalSettings.Settings.ExtraSettings.NumDisplayType == 2)
		{
			base.Count = 0;
			return;
		}
		base.Count = await Task.Run(async () => (await CalendarEventDao.GetDisplayEventsInBindAccount(Account.Id, DateTime.Today, DateTime.Now.AddMonths(3))).Count);
	}

	public override ProjectIdentity GetIdentity()
	{
		return new BindAccountCalendarProjectIdentity(Account);
	}

	public override PtfType GetPtfType()
	{
		return PtfType.Subscribe;
	}
}
