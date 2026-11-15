using System;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

public class UserModel
{
	public string token { get; set; }

	[PrimaryKey]
	public string userId { get; set; }

	public string code { get; set; }

	public string userCode { get; set; }

	public string username { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? proEndDate { get; set; }

	public string subscribeType { get; set; }

	public string subscribeFreq { get; set; }

	public bool needSubscribe { get; set; }

	public string inboxId { get; set; }

	public bool pro { get; set; }

	public long checkPoint { get; set; }

	public long columnCheckPoint { get; set; }

	public bool teamUser { get; set; }

	public bool activeTeamUser { get; set; }

	public bool teamPro { get; set; }

	public string phone { get; set; }

	public string authId { get; set; }

	public long expireTime { get; set; }

	public bool? gracePeriod { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? noGraceDate { get; set; }

	public int? errorType { get; set; }
}
