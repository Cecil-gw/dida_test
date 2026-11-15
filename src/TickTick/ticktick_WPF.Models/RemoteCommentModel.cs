using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SQLite;

namespace ticktick_WPF.Models;

public class RemoteCommentModel
{
	public string title { get; set; }

	public string taskId { get; set; }

	public string projectId { get; set; }

	public string id { get; set; }

	public string modifiedTime { get; set; }

	public string createdTime { get; set; }

	public string replyCommentId { get; set; }

	public List<Mention> mentions { get; set; }

	public UserProfile userProfile { get; set; }

	public ReplyUserProfile replyUserProfile { get; set; }

	public bool local { get; set; }

	public List<AttachmentIdModel> attachments { get; set; }

	[Ignore]
	public int attachmentCount { get; set; }

	public RemoteCommentModel(CommentModel comment)
	{
		id = comment.id;
		local = true;
		mentions = JsonConvert.DeserializeObject<List<Mention>>(comment.mentionstring ?? "");
		replyUserProfile = new ReplyUserProfile();
		taskId = comment.taskSid;
		projectId = comment.projectSid;
		title = comment.title;
		createdTime = (comment.createdTime ?? DateTime.Now).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.000", App.Ci) + "+0000";
		modifiedTime = "";
		replyCommentId = comment.replyCommentId;
		userProfile = new UserProfile
		{
			isMyself = true
		};
		attachmentCount = comment.attachmentCount;
		attachments = comment.Attachments?.Select((AttachmentModel it) => new AttachmentIdModel
		{
			id = it.id
		}).ToList();
	}
}
