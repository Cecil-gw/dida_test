using System;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class NotificationModel
{
	public string id { get; set; }

	public string title { get; set; }

	public string type { get; set; }

	[JsonProperty(PropertyName = "data")]
	public NotificationUserData notificationUserData { get; set; }

	public int actionStatus { get; set; }

	public DateTime createdTime { get; set; }

	public object category { get; set; }

	public object unread { get; set; }
}
