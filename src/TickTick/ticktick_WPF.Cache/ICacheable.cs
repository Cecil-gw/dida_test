using System.Threading.Tasks;

namespace ticktick_WPF.Cache;

public interface ICacheable
{
	Task Load();

	Task Clear();
}
