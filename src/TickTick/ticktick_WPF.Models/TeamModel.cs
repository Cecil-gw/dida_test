using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class TeamModel : BaseModel
{
	public string id { get; set; }

	public string name { get; set; }

	public DateTime createdTime { get; set; }

	public DateTime modifiedTime { get; set; }

	public DateTime joinedTime { get; set; }

	public bool expired { get; set; }

	public DateTime expiredDate { get; set; }

	public string userId { get; set; }

	public bool open { get; set; } = true;

	public bool needAuditCode { get; set; }

	public bool needAuditUrl { get; set; }

	public string industry { get; set; }

	public string scale { get; set; }

	public string logo { get; set; }

	[JsonIgnore]
	public bool expiredChecked { get; set; }

	public bool teamPro { get; set; }

	public bool IsPro()
	{
		if (teamPro)
		{
			return expiredDate >= DateTime.Now;
		}
		return false;
	}
}
