using Newtonsoft.Json;

namespace TickTickUtils.Lang;

public class UtilJson
{
	public static T DeserializeObject<T>(string json)
	{
		if (json == null)
		{
			return default(T);
		}
		return JsonConvert.DeserializeObject<T>(json);
	}
}
