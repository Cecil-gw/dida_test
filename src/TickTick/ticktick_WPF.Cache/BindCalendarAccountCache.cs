using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Cache;

public class BindCalendarAccountCache : CacheBase<BindCalendarAccountModel>
{
	public override async Task Load()
	{
		AssembleData(await BindCalendarAccountDao.GetBindCalendarAccounts(), (BindCalendarAccountModel account) => account.Id);
	}
}
