using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Models.Interface;

namespace ticktick_WPF.Models;

[Serializable]
public class BindCalendarAccountModel : BaseModel, OrderInterface
{
	[JsonProperty("id")]
	public string Id { get; set; }

	[JsonProperty("account")]
	public string Account { get; set; } = string.Empty;

	[JsonProperty("site")]
	public string Site { get; set; }

	[JsonProperty("createdTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? CreatedTime { get; set; }

	[JsonProperty("modifiedTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? ModifiedTime { get; set; }

	[JsonIgnore]
	public string UserId { get; set; }

	[Ignore]
	[JsonProperty("calendars")]
	public List<BindCalendarModel> Calendars { get; set; }

	[JsonProperty("kind")]
	public string Kind { get; set; }

	[JsonProperty("username")]
	public string Username { get; set; }

	[JsonProperty("password")]
	public string Password { get; set; }

	[JsonProperty("domain")]
	public string Domain { get; set; }

	[JsonProperty("desc")]
	public string Description { get; set; }

	[JsonProperty("order")]
	public long? Order { get; set; }

	[JsonIgnore]
	public bool Expired { get; set; }

	[Ignore]
	[JsonIgnore]
	public string DisplayName
	{
		get
		{
			if (!string.IsNullOrEmpty(Description))
			{
				return Description;
			}
			return Account;
		}
		set
		{
			Description = value;
		}
	}

	public bool IsFeiShu()
	{
		if (!string.IsNullOrEmpty(Site))
		{
			return Site.Contains("feishu");
		}
		return false;
	}

	public bool IsDingTalk()
	{
		if (!string.IsNullOrEmpty(Site))
		{
			if (!Site.Contains("dingtalk"))
			{
				return Site.Contains("dingding");
			}
			return true;
		}
		return false;
	}

	public bool IsBindAccountPassword()
	{
		if (!(Kind == "caldav"))
		{
			return Kind == "exchange";
		}
		return true;
	}

	public bool IsCalDav()
	{
		if (!(Kind == "caldav"))
		{
			return Kind == "icloud";
		}
		return true;
	}
}
