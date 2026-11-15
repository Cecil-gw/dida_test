using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Cache;

public class CalendarProfileCache : CacheBase<CalendarSubscribeProfileModel>
{
	public override async Task Load()
	{
		AssembleData(await CalendarSubscribeProfileDao.GetProfiles(), (CalendarSubscribeProfileModel profile) => profile.Id);
	}
}
