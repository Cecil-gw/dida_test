using System;

namespace ticktick_WPF.Models;

public class TeamMessageModel
{
	public string id { get; set; }

	public string name { get; set; }

	public DateTime createdTime { get; set; }

	public DateTime modifiedTime { get; set; }

	public DateTime expireDate { get; set; }

	public int seat { get; set; }

	public int price { get; set; }

	public int planCount { get; set; }

	public bool trial { get; set; }

	public int role { get; set; }
}
