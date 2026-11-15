using System;
using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class AttachmentEditJournal
{
	private const int MaxReports = 10;

	private static readonly object Gate = new object();

	private static readonly HashSet<string> Reported = new HashSet<string>();

	private readonly Queue<string> entries = new Queue<string>();

	internal static bool Exhausted
	{
		get
		{
			lock (Gate)
			{
				return Reported.Count >= 10;
			}
		}
	}

	internal void Clear()
	{
		entries.Clear();
	}

	internal void Record(string summary)
	{
		if (!Exhausted)
		{
			while (entries.Count >= 24)
			{
				entries.Dequeue();
			}
			entries.Enqueue(DateTime.UtcNow.ToString("HH:mm:ss.fff") + " " + ((summary.Length > 200) ? summary.Substring(0, 200) : summary));
		}
	}

	internal bool Report(string taskId, string attachmentId, string reason, string details, Action<string> sink)
	{
		string item = taskId + "|" + attachmentId + "|" + reason;
		lock (Gate)
		{
			if (Reported.Count >= 10 || !Reported.Add(item))
			{
				return false;
			}
		}
		string source = "AttachmentEditDiagnostic taskId:" + taskId + " attachmentId:" + attachmentId + " reason:" + reason + " " + details + " history:[" + string.Join(";", entries) + "]";
		source = new string(source.Select((char c) => (c > '\u007f') ? '?' : c).ToArray());
		sink((source.Length > 8000) ? source.Substring(0, 8000) : source);
		return true;
	}
}
