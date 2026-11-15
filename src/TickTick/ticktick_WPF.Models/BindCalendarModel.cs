using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Cache;

namespace ticktick_WPF.Models;

[Serializable]
public class BindCalendarModel : BaseModel
{
	[JsonProperty("id")]
	public string Id { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("show")]
	public string Show { get; set; }

	[JsonProperty("timeZone")]
	public string TimeZone { get; set; }

	[JsonProperty("visible")]
	public bool Visible { get; set; }

	[JsonProperty("color")]
	public string Color { get; set; }

	[JsonProperty("outId")]
	public string OutId { get; set; }

	[JsonIgnore]
	public string UserId { get; set; }

	[JsonIgnore]
	public string AccountId { get; set; }

	[JsonProperty("accessRole")]
	public string AccessRole { get; set; }

	[Ignore]
	[JsonProperty("events")]
	public List<CalendarEventModel> Events { get; set; }

	[Ignore]
	[JsonProperty("currentUserPrivilegeSet")]
	public List<string> CurrentUserPrivilegeSet { get; set; } = new List<string>();

	[JsonIgnore]
	public string LocalCurrentUserPrivilegeSet
	{
		get
		{
			if (CurrentUserPrivilegeSet != null)
			{
				return string.Join(",", CurrentUserPrivilegeSet);
			}
			return "";
		}
		set
		{
			CurrentUserPrivilegeSet = value?.Split(',').ToList();
		}
	}

	[JsonIgnore]
	public int sortOrder { get; set; }

	[Ignore]
	[JsonIgnore]
	public bool Accessible
	{
		get
		{
			BindCalendarAccountModel accountCalById = CacheManager.GetAccountCalById(AccountId);
			if (accountCalById == null || accountCalById.IsFeiShu())
			{
				return false;
			}
			bool num = !string.IsNullOrEmpty(AccessRole) && (AccessRole.ToLower().Trim() == "writer" || AccessRole.ToLower().Trim() == "owner");
			bool flag = !string.IsNullOrEmpty(LocalCurrentUserPrivilegeSet) && (LocalCurrentUserPrivilegeSet.ToLower().Contains("write") || LocalCurrentUserPrivilegeSet.ToLower().Contains("all"));
			return num || flag;
		}
	}

	[Ignore]
	[JsonIgnore]
	public CalendarAvailableType AvailableType
	{
		get
		{
			if (!Accessible)
			{
				return CalendarAvailableType.NoAccess;
			}
			BindCalendarAccountModel accountCalById = CacheManager.GetAccountCalById(AccountId);
			if (accountCalById != null && accountCalById.IsDingTalk())
			{
				return CalendarAvailableType.OnlyAccessible;
			}
			return CalendarAvailableType.Editable;
		}
	}

	[Ignore]
	[JsonIgnore]
	public CalendarShowType CalendarShowType
	{
		get
		{
			if (!Enum.TryParse<CalendarShowType>(Show, ignoreCase: true, out var result))
			{
				return CalendarShowType.Show;
			}
			return result;
		}
	}

	public bool IsEqual(BindCalendarModel cal)
	{
		if (Color == cal.Color && LocalCurrentUserPrivilegeSet == cal.LocalCurrentUserPrivilegeSet && Show == cal.Show && AccessRole == cal.AccessRole && Name == cal.Name)
		{
			return sortOrder == cal.sortOrder;
		}
		return false;
	}
}
