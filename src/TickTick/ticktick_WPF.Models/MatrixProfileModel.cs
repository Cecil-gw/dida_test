using System;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class MatrixProfileModel : BaseModel
{
	[JsonIgnore]
	public string id { get; set; }

	[JsonProperty("sid")]
	[Indexed]
	public string sid { get; set; }

	[JsonIgnore]
	[Indexed]
	public string userId { get; set; }

	[JsonProperty("name")]
	public string name { get; set; }

	[JsonProperty("sortOrder")]
	public long? sortOrder { get; set; }

	[JsonProperty("createdTime")]
	public DateTime createdTime { get; set; }

	[JsonProperty("modifiedTime")]
	public DateTime modifiedTime { get; set; }

	[JsonProperty("etag")]
	public string etag { get; set; }

	[JsonProperty("deleted")]
	public bool deleted { get; set; }

	[JsonIgnore]
	public string status { get; set; }

	public string DisplayName
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(name))
			{
				return name;
			}
			return Utils.GetString("Matrix");
		}
	}

	public static MatrixProfileModel Create(string sid, string name, long sortOrder, string status)
	{
		DateTime now = DateTime.Now;
		return new MatrixProfileModel
		{
			sid = sid,
			id = Utils.GetGuid(),
			userId = LocalSettings.Settings.LoginUserId,
			name = name,
			sortOrder = sortOrder,
			createdTime = now,
			modifiedTime = now,
			status = status,
			deleted = false
		};
	}
}
