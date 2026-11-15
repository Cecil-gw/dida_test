using System.Collections.Generic;
using System.Threading.Tasks;

namespace ticktick_WPF.Util.Toast;

public interface IToastHandler
{
	Task Exec(ToastOptionModel optModel, List<KeyValuePair<string, object>> kvs);
}
