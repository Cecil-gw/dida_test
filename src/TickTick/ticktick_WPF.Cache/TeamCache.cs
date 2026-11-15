using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Cache;

public class TeamCache : CacheBase<TeamModel>
{
	private bool _loaded;

	public override async Task Load()
	{
		if (!_loaded)
		{
			_loaded = true;
			AssembleData(await TeamDao.GetAllTeams(), (TeamModel team) => team.id);
		}
	}
}
