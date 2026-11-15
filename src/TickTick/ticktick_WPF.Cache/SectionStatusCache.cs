using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Cache;

public class SectionStatusCache : CacheBase<SectionStatusModel>
{
	public override async Task Load()
	{
		AssembleData(await SectionStatusDao.GetSectionStatus(), (SectionStatusModel status) => status.Identity + "|" + status.Name);
	}
}
