using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Cache;

public class BindCalendarsCache : CacheBase<BindCalendarModel>
{
	public override async Task Load()
	{
		List<BindCalendarModel> list = await BindCalendarAccountDao.GetBindCalendars();
		int num = 0;
		foreach (BindCalendarModel item in list)
		{
			item.sortOrder = num++;
		}
		AssembleData(list, (BindCalendarModel account) => account.Id);
	}
}
