using System;
using System.Collections.Concurrent;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Util.Files;

namespace ticktick_WPF.Util.Attachment;

internal static class AttachmentRenderDiagnostic
{
	private const int MaxRememberedIssues = 2048;

	private static readonly ConcurrentDictionary<string, byte> RememberedIssues = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

	private static readonly ConcurrentQueue<string> RememberedIssueOrder = new ConcurrentQueue<string>();

	public static void Warn(string stage, string taskId, string attachmentId, string reason, AttachmentModel attachment = null, string details = null)
	{
		string text = taskId ?? attachment?.taskId ?? string.Empty;
		string text2 = attachmentId ?? attachment?.id ?? string.Empty;
		string text3 = DescribeAttachment(attachment);
		string text4 = string.Join("|", stage ?? string.Empty, text, text2, reason ?? string.Empty);
		if (RememberedIssues.TryAdd(text4, 0))
		{
			RememberedIssueOrder.Enqueue(text4);
			string result;
			while (RememberedIssues.Count > 2048 && RememberedIssueOrder.TryDequeue(out result))
			{
				RememberedIssues.TryRemove(result, out var _);
			}
			string text5 = (string.IsNullOrEmpty(details) ? string.Empty : (", " + details));
			UtilLog.Warn("AttachmentRenderDiagnostic stage:" + (stage ?? string.Empty) + ", taskId:" + text + ", attachmentId:" + text2 + ", reason:" + (reason ?? string.Empty) + ", " + text3 + text5);
		}
	}

	private static string DescribeAttachment(AttachmentModel attachment)
	{
		if (attachment == null)
		{
			return "relation:null";
		}
		return $"status:{attachment.status}, deleted:{attachment.deleted}, " + "syncStatus:" + (attachment.sync_status ?? string.Empty) + ", fileType:" + (attachment.fileType ?? string.Empty) + ", " + $"hasRemotePath:{!string.IsNullOrEmpty(attachment.path)}, " + $"hasLocalPath:{!string.IsNullOrEmpty(attachment.localPath)}, " + $"localFileExists:{!FileUtils.FileEmptyOrNotExists(attachment.localPath)}";
	}
}
