using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

internal class AudioAttachmentAIResultModel : BaseModel
{
	public string AttachmentId { get; set; }

	public string Language { get; set; }

	public string Raw { get; set; }

	public string TextJson
	{
		get
		{
			if (Text == null)
			{
				return null;
			}
			return JsonConvert.SerializeObject(Text);
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				Text = null;
				return;
			}
			try
			{
				Text = JsonConvert.DeserializeObject<List<string>>(value);
			}
			catch (Exception)
			{
				Text = null;
			}
		}
	}

	[Ignore]
	public List<string> Text { get; set; }

	public string TimeStampJson
	{
		get
		{
			if (TimeStamp == null)
			{
				return null;
			}
			return JsonConvert.SerializeObject(TimeStamp);
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				TimeStamp = null;
				return;
			}
			try
			{
				TimeStamp = JsonConvert.DeserializeObject<List<long[]>>(value);
			}
			catch (Exception)
			{
				TimeStamp = null;
			}
		}
	}

	[Ignore]
	public List<long[]> TimeStamp { get; set; }

	public string Topic { get; set; }

	public string Summary { get; set; }

	public string ErrorCode { get; set; }

	public string UserId { get; set; }
}
