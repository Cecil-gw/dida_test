using System;

namespace ticktick_WPF.Models;

[Serializable]
public class UserProfile
{
	public string userCode { get; set; }

	public string avatarUrl { get; set; }

	public string email { get; set; }

	public string name { get; set; }

	public string username { get; set; }

	public bool? isMyself { get; set; }
}
