using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TickTickModels;

public class UtcDateTimeWithMillConverter : DateTimeConverterBase
{
	private static string _timeZoneString;

	public static readonly IsoDateTimeConverter DtConverter = new IsoDateTimeConverter
	{
		DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'" + TimeZoneString + "'",
		Culture = CultureInfo.InvariantCulture
	};

	private static string TimeZoneString
	{
		get
		{
			if (string.IsNullOrWhiteSpace(_timeZoneString))
			{
				_timeZoneString = DateTime.Now.ToString("zzz", CultureInfo.InvariantCulture).Replace(":", "");
			}
			return _timeZoneString;
		}
	}

	public static string GetConverterValue(DateTime date)
	{
		string text = date.ToString("zzz", CultureInfo.InvariantCulture).Replace(":", "");
		return "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'" + text + "'";
	}

	private static IsoDateTimeConverter GetConverter(DateTime date)
	{
		return new IsoDateTimeConverter
		{
			DateTimeFormat = GetConverterValue(date),
			Culture = CultureInfo.InvariantCulture
		};
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		return DtConverter.ReadJson(reader, objectType, existingValue, serializer);
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		try
		{
			if (value != null && ((DateTime)value).Year != 1)
			{
				GetConverter((DateTime)value).WriteJson(writer, value, serializer);
				return;
			}
			IsoDateTimeConverter isoDateTimeConverter = new IsoDateTimeConverter();
			isoDateTimeConverter.DateTimeFormat = null;
			isoDateTimeConverter.Culture = CultureInfo.InvariantCulture;
			isoDateTimeConverter.WriteJson(writer, value, serializer);
		}
		catch (Exception innerException)
		{
			throw new Exception("value = " + value, innerException);
		}
	}
}
