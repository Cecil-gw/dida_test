using System;
using Newtonsoft.Json;
using SQLite;
using TickTickUtils.Lang;

namespace ticktick_WPF.Models;

[Serializable]
public class CountdownWidgetModel : WidgetBaseModel
{
	public enum DisplayDescType
	{
		UntilOrSince,
		Detail
	}

	public string CountdownId { get; set; }

	public string Style { get; set; }

	public string StyleColor { get; set; }

	[Ignore]
	public string[] StyleColors
	{
		get
		{
			if (!string.IsNullOrEmpty(StyleColor))
			{
				return StyleColor.Split(',');
			}
			return new string[0];
		}
		set
		{
			if (value != null && value.Length != 0)
			{
				StyleColor = string.Join(",", value);
			}
			else
			{
				StyleColor = string.Empty;
			}
		}
	}

	public string LocalImagePath { get; set; }

	[JsonConverter(typeof(BackgroundConverter))]
	public string Background { get; set; }

	[JsonIgnore]
	[Ignore]
	public BackgroundModel BackgroundObject
	{
		get
		{
			if (string.IsNullOrEmpty(Background))
			{
				return null;
			}
			try
			{
				return JsonConvert.DeserializeObject<BackgroundModel>(Background);
			}
			catch (Exception ex)
			{
				UtilLog.Warn("Error deserializing Background: " + ex.Message);
				return null;
			}
		}
		set
		{
			if (value == null)
			{
				Background = null;
			}
			else
			{
				Background = JsonConvert.SerializeObject(value);
			}
		}
	}

	public string DateDisplayFormat { get; set; } = "day";

	[Ignore]
	[JsonIgnore]
	public DateDisplayFormatType DateDisplayFormatType
	{
		get
		{
			if (Enum.TryParse<DateDisplayFormatType>(DateDisplayFormat, ignoreCase: true, out var result))
			{
				return result;
			}
			return DateDisplayFormatType.Day;
		}
		set
		{
			DateDisplayFormat = value.ToString().ToLower();
		}
	}

	public DisplayDescType DescType { get; set; }
}
