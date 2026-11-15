using System;
using System.ComponentModel;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class HabitSectionModel : BaseModel
{
	private static HabitSectionModel _default = new HabitSectionModel
	{
		UserId = string.Empty,
		Id = "-1",
		SortOrder = 0L,
		Name = Utils.GetString("HabitSectionOthers")
	};

	[JsonProperty(PropertyName = "id")]
	public string Id { get; set; }

	[JsonProperty(PropertyName = "userId")]
	public string UserId { get; set; }

	[JsonProperty(PropertyName = "name")]
	public string Name { get; set; }

	[JsonIgnore]
	[Ignore]
	public string DisplayName
	{
		get
		{
			if (Name.StartsWith("_") && (Name == "_morning" || Name == "_afternoon" || Name == "_night"))
			{
				return Utils.GetString("HabitSection" + Name.Substring(1, 1).ToUpper() + Name.Substring(2));
			}
			return Name;
		}
	}

	[JsonIgnore]
	public bool IsOpen { get; set; } = true;

	[JsonProperty(PropertyName = "sortOrder")]
	public long SortOrder { get; set; }

	[JsonProperty(PropertyName = "etag")]
	public string Etag { get; set; }

	[JsonProperty(PropertyName = "cratedTime")]
	public DateTime CreatedTime { get; set; }

	[JsonProperty(PropertyName = "modifiedTime")]
	public DateTime ModifiedTime { get; set; }

	[JsonIgnore]
	[NotNull]
	[DefaultValue("2")]
	public int SyncStatus { get; set; }

	public HabitSectionModel()
	{
		UserId = Utils.GetCurrentUserIdInt().ToString();
		Id = Utils.GetGuid();
		SortOrder = 0L;
		CreatedTime = DateTime.Now;
	}

	public static HabitSectionModel GetDefault()
	{
		return _default;
	}

	public override bool Equals(object obj)
	{
		if (obj is HabitSectionModel habitSectionModel)
		{
			return Id == habitSectionModel.Id;
		}
		return false;
	}

	public HabitSectionModel(string name)
	{
		UserId = Utils.GetCurrentUserIdInt().ToString();
		Id = Utils.GetGuid();
		SortOrder = -1L;
		CreatedTime = DateTime.Now;
		Name = name;
	}
}
