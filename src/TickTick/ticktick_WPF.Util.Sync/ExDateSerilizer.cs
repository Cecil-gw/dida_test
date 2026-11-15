using System.Linq;
using Newtonsoft.Json;

namespace ticktick_WPF.Util.Sync;

public static class ExDateSerilizer
{
	public static string ToString(string[] exDates)
	{
		if (exDates != null && exDates.Any())
		{
			return JsonConvert.SerializeObject(exDates.ToList());
		}
		return string.Empty;
	}

	public static string[] ToArray(string exDate)
	{
		if (!string.IsNullOrEmpty(exDate))
		{
			return JsonConvert.DeserializeObject<string[]>(exDate);
		}
		return null;
	}
}
