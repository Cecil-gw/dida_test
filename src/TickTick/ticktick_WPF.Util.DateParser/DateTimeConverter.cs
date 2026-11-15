using Newtonsoft.Json.Converters;

namespace ticktick_WPF.Util.DateParser;

public class DateTimeConverter : IsoDateTimeConverter
{
	public DateTimeConverter()
	{
		base.DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss";
	}
}
