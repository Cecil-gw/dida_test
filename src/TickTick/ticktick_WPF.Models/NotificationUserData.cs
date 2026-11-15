using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class NotificationUserData
{
	public string taskId { get; set; }

	public string title { get; set; }

	public string userCode { get; set; }

	public string name { get; set; }

	public string action { get; set; }

	public string projectId { get; set; }

	public string commentId { get; set; }

	public string taskTitle { get; set; }

	public string recordId { get; set; }

	public string fromUsername { get; set; }

	public string entityId { get; set; }

	public object applyProjectId { get; set; }

	public string userDisplayName { get; set; }

	public string teamId { get; set; }

	public int role { get; set; }

	public string teamName { get; set; }

	public string adminDisplayName { get; set; }

	public int projectCount { get; set; }

	public string entityType { get; set; }

	public string entityName { get; set; }

	public string fromUserCode { get; set; }

	public string fromUserDisplayName { get; set; }

	public string fromUserId { get; set; }

	public string projectName { get; set; }

	public DateTime? expiryDate { get; set; }

	public string topicUrl { get; set; }

	public string topicTitle { get; set; }

	public string topicId { get; set; }

	public string ticketDescription { get; set; }

	public string ticketId { get; set; }

	public string applyId { get; set; }

	public string ownerUserCode { get; set; }

	public string applyUserCode { get; set; }

	public string ownerUserDisplayName { get; set; }

	public string applyUserDisplayName { get; set; }

	public string permission { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? noGraceDate { get; set; }

	public string subscribeType { get; set; }
}
