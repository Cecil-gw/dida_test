using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class ShareUserModel : BaseModel
{
	public string recordId { get; set; }

	public long? userId { get; set; }

	public string avatarUrl { get; set; }

	public string username { get; set; }

	[JsonIgnore]
	public string inviteIdentifier { get; set; }

	public string phone { get; set; }

	public string displayName { get; set; }

	public bool isOwner { get; set; }

	public bool isProjectShare { get; set; }

	public bool isAccept { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? createdTime { get; set; }

	public string userCode { get; set; }

	public int acceptStatus { get; set; }

	public bool deleted { get; set; }

	public string permission { get; set; }

	public int? siteId { get; set; }

	public bool visitor { get; set; }
}
