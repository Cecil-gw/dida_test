using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommonMark.Syntax;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Undo;

namespace ticktick_WPF.Util;

public static class TaskUtils
{
	private static readonly Regex TaskUrlWithTitleReg = new Regex("^" + BaseUrl.GetDomainUrl() + "/webapp#p/(.*)/tasks/(\\S+) (.*)");

	private static readonly Regex TaskUrlWithTitleRegExtra = new Regex("^" + BaseUrl.GetDomainUrl() + "/webapp/#p/(.*)/tasks/(\\S+) (.*)");

	private static readonly Regex TaskUrlReg = new Regex("^" + BaseUrl.GetDomainUrl() + "/webapp#p/(.*)/tasks/(.*)");

	private static readonly Regex TaskUrlRegExtra = new Regex("^" + BaseUrl.GetDomainUrl() + "/webapp/#p/(.*)/tasks/(.*)");

	public const string ValidUrl = "(https?:\\/\\/(?:www\\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]?\\.[^\\s]{2,}|www\\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\\.[^\\s]{2,}|https?:\\/\\/(?:www\\.|(?!www))[a-zA-Z0-9]\\.[^\\s]{2,}|www\\.[a-zA-Z0-9]\\.[^\\s]{2,})";

	private static readonly Regex LinkRegex = new Regex("(\\[.*\\])(\\(.*\\))");

	public static readonly Regex UrlRegex = new Regex("(https?:\\/\\/(?:www\\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]?\\.[^\\s]{2,}|www\\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\\.[^\\s]{2,}|https?:\\/\\/(?:www\\.|(?!www))[a-zA-Z0-9]\\.[^\\s]{2,}|www\\.[a-zA-Z0-9]\\.[^\\s]{2,})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public static ProjectTask ParseTaskUrlWithoutTitle(string url)
	{
		Match match = TaskUrlReg.Match(url);
		if (match.Success)
		{
			return new ProjectTask
			{
				Title = match.Groups[3].ToString(),
				TaskId = match.Groups[2].ToString(),
				ProjectId = match.Groups[1].ToString()
			};
		}
		match = TaskUrlRegExtra.Match(url);
		if (match.Success)
		{
			return new ProjectTask
			{
				Title = match.Groups[3].ToString(),
				TaskId = match.Groups[2].ToString(),
				ProjectId = match.Groups[1].ToString()
			};
		}
		return null;
	}

	public static ProjectTask ParseTaskUrl(string url)
	{
		Match match = TaskUrlWithTitleReg.Match(url);
		if (match.Success)
		{
			return new ProjectTask
			{
				Title = match.Groups[3].ToString(),
				TaskId = match.Groups[2].ToString(),
				ProjectId = match.Groups[1].ToString()
			};
		}
		match = TaskUrlWithTitleRegExtra.Match(url);
		if (match.Success)
		{
			return new ProjectTask
			{
				Title = match.Groups[3].ToString(),
				TaskId = match.Groups[2].ToString(),
				ProjectId = match.Groups[1].ToString()
			};
		}
		return null;
	}

	public static void CopyTaskLink(string taskId, string projectId, string title = "")
	{
		try
		{
			Clipboard.SetText(GetCopyLink(projectId, taskId, title));
			Toast("Copied");
		}
		catch (Exception)
		{
		}
	}

	public static string TryGetClipboardText()
	{
		try
		{
			return Clipboard.GetText();
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static string GetCopyLink(string projectId, string taskId, string title = "", bool isMarkdownStyle = false)
	{
		if (projectId.Contains("inbox"))
		{
			projectId = "inbox";
		}
		if (string.IsNullOrEmpty(title) && isMarkdownStyle)
		{
			title = Utils.GetString("MyTask");
		}
		if (isMarkdownStyle)
		{
			return "[" + title + "](" + BaseUrl.GetDomainUrl() + "/webapp/#p/" + projectId + "/tasks/" + taskId + ")";
		}
		return BaseUrl.GetDomainUrl() + "/webapp/#p/" + projectId + "/tasks/" + taskId + " " + title;
	}

	public static async Task<TaskModel> TryLoadTask(string taskId, string projectId, bool checkDeleted = false, string notFoundResourceKey = null)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null)
		{
			if (checkDeleted && taskModel.deleted != 0)
			{
				Toast(notFoundResourceKey ?? "CannotFindLastTask");
				return null;
			}
			return taskModel;
		}
		if (Utils.IsNetworkAvailable())
		{
			Mouse.OverrideCursor = Cursors.Wait;
			if (projectId?.Contains("inboxId") ?? false)
			{
				projectId = Utils.GetInboxId();
			}
			TaskModel remoteTask = await Communicator.GetTask(taskId, projectId);
			Mouse.OverrideCursor = null;
			if (remoteTask != null && !string.IsNullOrEmpty(remoteTask.id))
			{
				await TaskDao.BatchCreateTaskFromRemote(new List<TaskModel> { remoteTask }, needHandleItem: true);
				if (checkDeleted && remoteTask.deleted != 0)
				{
					Toast(notFoundResourceKey ?? "CannotFindLastTask");
					return null;
				}
				return remoteTask;
			}
			Toast(notFoundResourceKey ?? "NoTaskFound");
			return null;
		}
		Toast("NoNetwork");
		return null;
	}

	public static string EscapeMarkDownUrlContent(string content)
	{
		if (!string.IsNullOrEmpty(content))
		{
			string text = content;
			MatchCollection matchCollection = LinkRegex.Matches(content);
			if (matchCollection.Count > 0)
			{
				foreach (object item in matchCollection)
				{
					if (item is Match match)
					{
						string oldValue = match.Groups[2].ToString();
						text = text.Replace(oldValue, string.Empty);
					}
				}
				return text;
			}
			return content;
		}
		return string.Empty;
	}

	private static void Toast(string key)
	{
		string text = Utils.GetString(key);
		Utils.Toast(text);
		App.Window?.TryToastString(null, text);
	}

	public static string EscapeLinkContent(string content)
	{
		try
		{
			if (!string.IsNullOrEmpty(content))
			{
				return Regex.Replace(content, "(?<=\\[.*\\])\\([^)]*\\)", "");
			}
			return content;
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
			return content;
		}
	}

	public static async Task<string> GetLinkTitle(string url)
	{
		return await Communicator.GetUrlTitle(url);
	}

	public static void TryGetTaskLinkTitle(TaskModel task)
	{
		TryGetTaskLinkTitle(task?.id, task?.title);
	}

	public static void TryGetTaskLinkTitle(string id, string taskTitle, bool isCheckItem = false, Action<string> action = null)
	{
		if (!(LocalSettings.Settings.UserPreference?.GeneralConfig?.urlParseEnabled ?? true) || string.IsNullOrEmpty(taskTitle))
		{
			return;
		}
		Match urlMatch = UrlRegex.Match(taskTitle);
		if (urlMatch.Success)
		{
			Match match = LinkRegex.Match(taskTitle);
			if (!match.Success || match.Index > urlMatch.Index)
			{
				Task.Run((Func<Task>)Function);
			}
		}
		async Task Function()
		{
			try
			{
				string title = await GetLinkTitle(urlMatch.Value);
				if (!string.IsNullOrEmpty(title))
				{
					Application.Current?.Dispatcher?.InvokeAsync((Func<Task>)async delegate
					{
						if (!(await TaskService.ReplaceTitleLink(id, urlMatch.Value, title, taskTitle, isCheckItem)))
						{
							action?.Invoke(taskTitle.Replace(urlMatch.Value, "[" + title + "](" + urlMatch.Value + ")"));
						}
					});
				}
			}
			catch (Exception)
			{
			}
		}
	}

	public static string GetContentDisplayText(string text)
	{
		return MarkdownSemanticConverter.GetPlainText(MarkdownUtils.RemoveBlockMetadataComments(EscapeNonRenderableAttachmentSyntaxForPlainText(ReplaceAttachmentTextInString(text)))).Replace("\r\n", " ").Replace("\r", " ")
			.Replace("\n", " ");
	}

	public static string GetContentMarkdownDisplayText(string text)
	{
		return MarkdownSemanticConverter.DecodeEscapedSyntaxForDisplay(MarkDownEditor.ConvertColoredHighlightsToPortableSyntax(MarkdownUtils.RemoveBlockMetadataComments(ReplaceAttachmentTextInString(text))));
	}

	public static string ReplaceAttachmentTextInString(string text, bool withTypeName = true)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		MatchCollection matchCollection = MarkDownEditor.AttachmentRegex.Matches(text.ToLower());
		if (matchCollection.Count > 0)
		{
			Dictionary<int, Match> dictionary = new Dictionary<int, Match>();
			for (int i = 0; i < matchCollection.Count; i++)
			{
				Match match = matchCollection[i];
				if (withTypeName ? MarkDownEditor.IsRenderableAttachmentMatch(text, match) : MarkDownEditor.IsInternalAttachmentMatch(text, match))
				{
					dictionary[match.Index] = match;
				}
			}
			foreach (int item in from k in dictionary.Keys.ToList()
				orderby k descending
				select k)
			{
				Match match2 = dictionary[item];
				string[] array = match2.Groups[2].Value.Split('/');
				if (item + match2.Value.Length <= text.Length)
				{
					text = text.Remove(item, match2.Value.Length);
					if (withTypeName)
					{
						string text2 = ((array.Length == 2) ? AttachmentProvider.GetFileTypeName(array[1]) : Utils.GetString("Image").ToLower());
						text = text.Insert(item, "[" + text2 + "]");
					}
				}
			}
		}
		return text;
	}

	public static string RemoveAttachmentMarkdown(string text)
	{
		if (string.IsNullOrEmpty(text) || !MarkDownEditor.AttachmentRegex.IsMatch(text))
		{
			return text;
		}
		string[] array = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string line in array2)
		{
			string text2 = line;
			IOrderedEnumerable<Match> orderedEnumerable = from Match match in MarkDownEditor.AttachmentRegex.Matches(line)
				where MarkDownEditor.IsRenderableAttachmentMatch(line, match)
				orderby match.Index descending
				select match;
			bool flag = false;
			foreach (Match item in orderedEnumerable)
			{
				text2 = text2.Remove(item.Index, item.Length);
				flag = true;
			}
			if (!flag || !string.IsNullOrWhiteSpace(text2))
			{
				list.Add(flag ? text2.TrimEnd() : text2);
			}
		}
		return string.Join(Environment.NewLine, list);
	}

	public static List<string> GetAttachmentMarkdown(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return new List<string>();
		}
		return (from Match match in MarkDownEditor.AttachmentRegex.Matches(text)
			where MarkDownEditor.IsRenderableAttachmentMatch(text, match)
			select text.Substring(match.Index, match.Length)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	public static string RestoreAttachmentMarkdown(string text, IEnumerable<string> attachmentMarkdown, string originalText = null, string calendarText = null)
	{
		if (calendarText != null && AreContentsEquivalent(text, calendarText))
		{
			return originalText ?? string.Empty;
		}
		string text2 = text ?? string.Empty;
		foreach (string item in attachmentMarkdown ?? Enumerable.Empty<string>())
		{
			if (!string.IsNullOrWhiteSpace(item) && text2.IndexOf(item, StringComparison.OrdinalIgnoreCase) < 0)
			{
				if (!string.IsNullOrEmpty(text2) && !text2.EndsWith("\n") && !text2.EndsWith("\r"))
				{
					text2 += Environment.NewLine;
				}
				text2 += item;
			}
		}
		return text2;
	}

	private static bool AreContentsEquivalent(string first, string second)
	{
		return string.Equals(NormalizeLineEndings(first), NormalizeLineEndings(second), StringComparison.Ordinal);
	}

	private static string NormalizeLineEndings(string text)
	{
		return (text ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
	}

	private static string EscapeNonRenderableAttachmentSyntaxForPlainText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		MatchCollection matchCollection = MarkDownEditor.AttachmentRegex.Matches(text);
		for (int num = matchCollection.Count - 1; num >= 0; num--)
		{
			Match match = matchCollection[num];
			if (MarkDownEditor.IsUnescapedAttachmentMatch(text, match) && !MarkDownEditor.IsRenderableAttachmentMatch(text, match))
			{
				text = text.Insert(match.Index + 1, "\\");
			}
		}
		return text;
	}

	public static string CleanMarkdownMarksInString(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		try
		{
			string[] source = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			text = string.Join(" ", source.Select((string line) => (!MarkdownBlockSyntax.IsSplitLine(line)) ? line : string.Empty));
			text = "^" + text;
			StringBuilder stringBuilder = new StringBuilder();
			foreach (Block item in AbstractSyntaxTree.EnumerateSpanningBlocks(AbstractSyntaxTree.GenerateAbstractSyntaxTree(text), 0, text.Length))
			{
				foreach (Inline item2 in AbstractSyntaxTree.EnumerateInlines(item.InlineContent))
				{
					string text2 = item2.LiteralContent;
					if (text2 == null || IsHighlightMark(text2))
					{
						continue;
					}
					MatchCollection matchCollection = new Regex("#{1,6} ").Matches(text2);
					if (matchCollection.Count > 0)
					{
						text2 = (from object match in matchCollection
							select match.ToString() into it
							orderby it.Length descending
							select it).ToList().Aggregate(text2, (string current, string heading) => current.Replace(heading, string.Empty));
					}
					string value = text2.Replace("> ", " ");
					stringBuilder.Append(value);
				}
			}
			return stringBuilder.ToString().Substring(1);
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
			return text;
		}
	}

	public static string NormalizeHorizontalRuleLineEndings(string content)
	{
		if (string.IsNullOrEmpty(content) || content.IndexOf("\r\n", StringComparison.Ordinal) < 0)
		{
			return content;
		}
		StringBuilder stringBuilder = new StringBuilder(content.Length);
		int num = 0;
		while (num < content.Length)
		{
			int num2 = content.IndexOf("\r\n", num, StringComparison.Ordinal);
			if (num2 < 0)
			{
				stringBuilder.Append(content, num, content.Length - num);
				break;
			}
			string text = content.Substring(num, num2 - num);
			stringBuilder.Append(text);
			stringBuilder.Append(MarkdownBlockSyntax.IsSplitLine(text) ? "\n" : "\r\n");
			num = num2 + 2;
		}
		return stringBuilder.ToString();
	}

	private static bool IsHighlightMark(string contentText)
	{
		if (!string.Equals(contentText, "</mark>"))
		{
			if (contentText.StartsWith("<mark"))
			{
				return contentText.EndsWith(">");
			}
			return false;
		}
		return true;
	}

	public static string ReplaceAttachmentNameInString(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		MatchCollection matchCollection = MarkDownEditor.AttachmentRegex.Matches(text.ToLower());
		if (matchCollection.Count > 0)
		{
			Dictionary<int, Match> dictionary = new Dictionary<int, Match>();
			for (int i = 0; i < matchCollection.Count; i++)
			{
				Match match = matchCollection[i];
				if (MarkDownEditor.IsInternalAttachmentMatch(text, match))
				{
					dictionary[match.Index] = match;
				}
			}
			foreach (int item in from k in dictionary.Keys.ToList()
				orderby k descending
				select k)
			{
				Match match2 = dictionary[item];
				string[] array = match2.Groups[2].Value.Split('/');
				if (item + match2.Value.Length > text.Length)
				{
					continue;
				}
				text = text.Remove(item, match2.Value.Length);
				string text2;
				if (array.Length == 2)
				{
					text2 = AttachmentCache.GetFileName(array[0]);
					if (string.IsNullOrEmpty(text2))
					{
						text2 = array[1];
					}
				}
				else
				{
					text2 = Utils.GetString("Image").ToLower();
				}
				text = text.Insert(item, "[" + text2 + "]");
			}
		}
		return text;
	}

	public static bool CheckDate(TaskBaseViewModel task, List<FilterDatePair> startEndPairs, bool inTodayOrWeek = false, LogicType type = LogicType.Or, bool inCal = false, bool showOutDateNote = false)
	{
		if (startEndPairs != null && startEndPairs.Count > 0)
		{
			bool flag = false;
			DateTime? startDate = task.StartDate;
			DateTime? dateTime = task.DueDate;
			if (startDate.HasValue && dateTime.HasValue && task.IsAllDay == true && startDate.Value == dateTime.Value)
			{
				dateTime = startDate.Value.Date.AddDays(1.0);
			}
			if (task.Kind == "NOTE" && !showOutDateNote)
			{
				if (!dateTime.HasValue && startDate.HasValue && startDate.Value < DateTime.Today)
				{
					return false;
				}
				if (dateTime.HasValue && dateTime.Value <= DateTime.Today)
				{
					return false;
				}
			}
			foreach (FilterDatePair startEndPair in startEndPairs)
			{
				DateTime? start = startEndPair.Start;
				DateTime? end = startEndPair.End;
				if (!start.HasValue && !end.HasValue)
				{
					if (startEndPair.isRepeat)
					{
						flag = flag || !string.IsNullOrEmpty(task.RepeatFlag);
						continue;
					}
					if (startEndPair.IsNoDate != Utils.IsEmptyDate(startDate))
					{
						continue;
					}
				}
				if ((!start.HasValue && end.HasValue && (!startDate.HasValue || (!dateTime.HasValue && startDate >= end) || dateTime > end)) || (start.HasValue && !end.HasValue && (!startDate.HasValue || (!dateTime.HasValue && startDate < start) || (dateTime.HasValue && dateTime <= start))))
				{
					continue;
				}
				if (!Utils.IsEmptyDate(start) && !Utils.IsEmptyDate(end))
				{
					if ((Utils.IsEmptyDate(startDate) && task.Status == 0) || ((inTodayOrWeek || inCal) && Utils.IsEmptyDate(task.CompletedTime) && task.Status != 0))
					{
						continue;
					}
					if (inTodayOrWeek)
					{
						if ((task.Status == 0 && startDate >= end) || (task.Status != 0 && task.CompletedTime < start))
						{
							continue;
						}
					}
					else if (!startDate.HasValue || startDate >= end || (dateTime.HasValue && dateTime <= start) || (!dateTime.HasValue && startDate < start))
					{
						if (!inCal || !Utils.IsEmptyDate(startDate) || task.Status == 0 || Utils.IsEmptyDate(task.CompletedTime))
						{
							continue;
						}
						DateTime? completedTime = task.CompletedTime;
						DateTime? dateTime2 = start;
						if (!(completedTime.HasValue & dateTime2.HasValue) || !(completedTime.GetValueOrDefault() >= dateTime2.GetValueOrDefault()) || !(task.CompletedTime < end))
						{
							continue;
						}
					}
				}
				flag = true;
				break;
			}
			if (type != LogicType.Or)
			{
				return !flag;
			}
			return flag;
		}
		return true;
	}

	public static bool IsAvatarUrlEmpty(string avatarUrl)
	{
		if (!string.IsNullOrEmpty(avatarUrl))
		{
			return avatarUrl == "-1";
		}
		return true;
	}

	public static string ToastOnOpenSticky(string taskId)
	{
		TaskBaseViewModel taskById = TaskCache.GetTaskById(taskId);
		if (taskById == null || !taskById.Editable)
		{
			return Utils.GetString("NoEditingPermission");
		}
		return string.Empty;
	}

	public static void PostPoneTasks(TaskListView listView)
	{
		if (listView?.ViewModel?.SourceModels != null)
		{
			List<TaskBaseViewModel> list = (from t in listView.ViewModel.SourceModels.ToList()
				where t.Editable && t.Status == 0 && t.OutDate()
				select t).ToList();
			if (LocalSettings.Settings.ShowSubtasks)
			{
				list.AddRange(TaskDetailItemCache.GetAllOutDateItems());
			}
			PostponeUndoModel postponeUndoModel = new PostponeUndoModel();
			if (postponeUndoModel.InitData(list))
			{
				postponeUndoModel.Do();
				UndoToast uiElement = new UndoToast(postponeUndoModel);
				App.Window?.Toast(uiElement);
			}
		}
	}
}
