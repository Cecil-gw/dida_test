using Newtonsoft.Json.Linq;

namespace ticktick_WPF.Models;

public class ShareUsersError
{
	public string errorCode { get; set; }

	public string toUsername { get; set; }

	public JObject data { get; set; }
}
