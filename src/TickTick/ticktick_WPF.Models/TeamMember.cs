using System;

namespace ticktick_WPF.Models;

public class TeamMember
{
	public int role { get; set; }

	public string userCode { get; set; }

	public string displayName { get; set; }

	public string avatarUrl { get; set; }

	public bool isMyself { get; set; }

	public string email { get; set; }

	public string nickname { get; set; }

	public DateTime joinedTime { get; set; }

	public DateTime becameAdminTime { get; set; }
}
