using System;
using Newtonsoft.Json;
using TickTickUtils.Lang;

public class BackgroundConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		if (!(objectType == typeof(string)))
		{
			return objectType == typeof(BackgroundModel);
		}
		return true;
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}
		if (objectType == typeof(string))
		{
			return JsonConvert.SerializeObject(serializer.Deserialize<BackgroundModel>(reader));
		}
		try
		{
			return JsonConvert.DeserializeObject<BackgroundModel>(serializer.Deserialize<string>(reader));
		}
		catch (Exception ex)
		{
			UtilLog.Warn("Error deserializing BackgroundModel: " + ex.Message);
			return null;
		}
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
		}
		else if (value is string json)
		{
			writer.WriteRawValue(json);
		}
		else if (value is BackgroundModel value2)
		{
			string json2 = JsonConvert.SerializeObject(value2);
			writer.WriteRawValue(json2);
		}
	}
}
