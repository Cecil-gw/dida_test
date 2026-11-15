using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Util.Files;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;

namespace ticktick_WPF.Views.MarkDown;

public class ImageElementGenerator : VisualLineElementGenerator
{
	internal sealed class TrackedAttachment
	{
		internal TextAnchor Start;

		internal TextAnchor End;

		internal string Url;
	}

	public static readonly int SmallSizeHeight = 120;

	private const int PrintImageMaxHeight = 800;

	private const double AttachmentInlineFallbackWidthInset = 29.0;

	private readonly MarkDownEditor _editor;

	private string _taskId;

	public static HashSet<int> NotImageUrlSet = new HashSet<int>();

	private readonly ConcurrentDictionary<string, FrameworkElement> _cachedControls = new ConcurrentDictionary<string, FrameworkElement>();

	private readonly ConcurrentDictionary<string, byte> _pendingVisualMatchDiagnostics = new ConcurrentDictionary<string, byte>();

	internal Action<string> EditDiagnosticSink = delegate(string message)
	{
		UtilLog.Warn(message);
	};

	private readonly AttachmentEditJournal _editJournal = new AttachmentEditJournal();

	private readonly List<TrackedAttachment> _possiblyDamagedAttachments = new List<TrackedAttachment>();

	private bool _diagnosticMetadataDirty;

	private bool _diagnosticAwaitingTaskContent;

	private string _pendingPersistedText;

	private ITextSourceVersion _pendingPersistedVersion;

	private string _checkedPersistedText;

	private ITextSourceVersion _checkedPersistedVersion;

	private string _parsedPersistedText;

	private Dictionary<string, int> _parsedPersistedCounts;

	private DispatcherOperation _pendingEditDiagnostic;

	private double _currentWidth;

	internal void RecordDocumentChange(DocumentChangeEventArgs change, bool programmatic)
	{
		try
		{
			RecordDocumentChangeCore(change, programmatic);
		}
		catch (Exception)
		{
		}
	}

	private void RecordDocumentChangeCore(DocumentChangeEventArgs change, bool programmatic)
	{
		if (change == null || AttachmentEditJournal.Exhausted || _diagnosticAwaitingTaskContent)
		{
			return;
		}
		TextEditor editBox = _editor.EditBox;
		_diagnosticMetadataDirty = true;
		_checkedPersistedVersion = null;
		_checkedPersistedText = null;
		_pendingPersistedVersion = null;
		_pendingPersistedText = null;
		_editJournal.Record($"change offset:{change.Offset} remove:{change.RemovalLength} insert:{change.InsertionLength} " + $"caret:{editBox.CaretOffset} selection:{editBox.SelectionStart}+{editBox.SelectionLength} programmatic:{programmatic} " + $"ime:{editBox.TextArea.IsImeCompositionDocumentUpdate}");
		foreach (KeyValuePair<int, AttachmentInfo> item in _editor.GetImageDict())
		{
			AttachmentInfo info = item.Value;
			if (info == null || info.Value.Length > 2048)
			{
				continue;
			}
			int start = item.Key;
			int num = start + info.Value.Length;
			if (start < 0 || num > editBox.Document.TextLength || editBox.Document.GetText(start, info.Value.Length) != info.Value)
			{
				continue;
			}
			int num2 = change.Offset + change.RemovalLength;
			if ((change.Offset > start || num2 < num || change.InsertionLength != 0) && ((change.Offset < num && num2 > start) || (change.InsertionLength > 0 && change.Offset >= start && change.Offset <= num)))
			{
				_editJournal.Record("attachmentOverlap offset:" + start + " length:" + info.Value.Length + " callers:" + string.Join(">", from frame in new StackTrace(fNeedFileInfo: false).GetFrames().Take(6)
					select frame.GetMethod()?.Name));
				if (_possiblyDamagedAttachments.Count < 32 && !_possiblyDamagedAttachments.Any((TrackedAttachment item) => item.Start.Offset == start && item.Url == info.Url))
				{
					TextAnchor textAnchor = editBox.Document.CreateAnchor(start);
					textAnchor.MovementType = AnchorMovementType.BeforeInsertion;
					textAnchor.SurviveDeletion = true;
					TextAnchor textAnchor2 = editBox.Document.CreateAnchor(num);
					textAnchor2.MovementType = AnchorMovementType.AfterInsertion;
					textAnchor2.SurviveDeletion = true;
					_possiblyDamagedAttachments.Add(new TrackedAttachment
					{
						Start = textAnchor,
						End = textAnchor2,
						Url = info.Url
					});
				}
			}
		}
		ScheduleEditDiagnostic();
	}

	private void ResetEditDiagnostic()
	{
		_pendingEditDiagnostic?.Abort();
		_pendingEditDiagnostic = null;
		_possiblyDamagedAttachments.Clear();
		_editJournal.Clear();
		_diagnosticMetadataDirty = false;
		_pendingPersistedText = null;
		_pendingPersistedVersion = null;
		_checkedPersistedText = null;
		_checkedPersistedVersion = null;
		_parsedPersistedText = null;
		_parsedPersistedCounts = null;
	}

	internal void DiagnosticMetadataRefreshed()
	{
		_diagnosticMetadataDirty = false;
		_checkedPersistedVersion = null;
		ScheduleEditDiagnostic();
	}

	internal void DiagnosticTaskContentLoaded()
	{
		_diagnosticAwaitingTaskContent = false;
		DiagnosticMetadataRefreshed();
	}

	internal void BeginDiagnosticTaskChange(string id)
	{
		ResetEditDiagnostic();
		_diagnosticAwaitingTaskContent = true;
		_taskId = id;
	}

	internal void QueuePersistedAttachmentCheck(string persistedText)
	{
		if (!_diagnosticAwaitingTaskContent && !AttachmentEditJournal.Exhausted && !string.IsNullOrEmpty(_taskId))
		{
			_pendingPersistedText = persistedText ?? string.Empty;
			_pendingPersistedVersion = _editor.EditBox.Document.Version;
			ScheduleEditDiagnostic();
		}
	}

	internal void FlushPendingAttachmentCheck()
	{
		if (!_diagnosticAwaitingTaskContent && !_diagnosticMetadataDirty && !_editor.EditBox.Document.IsInUpdate && !_editor.EditBox.TextArea.IsImeCompositionDocumentUpdate)
		{
			string pendingPersistedText = _pendingPersistedText;
			ITextSourceVersion pendingPersistedVersion = _pendingPersistedVersion;
			_pendingPersistedText = null;
			_pendingPersistedVersion = null;
			if (pendingPersistedText != null && pendingPersistedVersion == _editor.EditBox.Document.Version)
			{
				CheckPersistedAttachments(pendingPersistedText);
			}
		}
	}

	internal void CheckPersistedAttachments(string persistedText)
	{
		if (AttachmentEditJournal.Exhausted || string.IsNullOrEmpty(_taskId) || _diagnosticAwaitingTaskContent)
		{
			return;
		}
		ITextSourceVersion version = _editor.EditBox.Document.Version;
		if (version == _checkedPersistedVersion && persistedText == _checkedPersistedText)
		{
			return;
		}
		try
		{
			_checkedPersistedVersion = version;
			_checkedPersistedText = persistedText;
			string display = _editor.EditBox.Text ?? string.Empty;
			if (display.IndexOf("![", StringComparison.Ordinal) < 0)
			{
				return;
			}
			List<IGrouping<string, Match>> list = (from Match m in MarkDownEditor.AttachmentRegex.Matches(display)
				where m.Groups[1].Value == "image" && MarkDownEditor.IsRenderableAttachmentMatch(display, m) && !_editor.IsAttachmentDiagnosticLiteralRange(m.Index, m.Index + m.Length)
				group m by m.Groups[2].Value).ToList();
			if (list.Count == 0)
			{
				return;
			}
			if (_parsedPersistedCounts == null || _parsedPersistedText != persistedText)
			{
				_parsedPersistedCounts = (from m in MarkDownEditor.GetDiagnosticPersistedImageMatches(persistedText)
					group m by m.Groups[2].Value).ToDictionary((IGrouping<string, Match> g) => g.Key, (IGrouping<string, Match> g) => g.Count());
				_parsedPersistedText = persistedText;
			}
			Dictionary<string, int> parsedPersistedCounts = _parsedPersistedCounts;
			foreach (IGrouping<string, Match> item in list)
			{
				parsedPersistedCounts.TryGetValue(item.Key, out var value);
				if (value < item.Count())
				{
					WriteEditDiagnostic(item.Key, "persistence_changed", $"displayCount:{item.Count()} savedCount:{value}");
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private void ScheduleEditDiagnostic()
	{
		if (AttachmentEditJournal.Exhausted || _pendingEditDiagnostic != null || string.IsNullOrEmpty(_taskId) || _diagnosticAwaitingTaskContent || !_editor.EditBox.IsLoaded)
		{
			return;
		}
		string taskId = _taskId;
		_pendingEditDiagnostic = _editor.EditBox.Dispatcher.BeginInvoke((Action)delegate
		{
			_pendingEditDiagnostic = null;
			if (taskId != _taskId || !_editor.EditBox.IsLoaded)
			{
				return;
			}
			try
			{
				CheckStableAttachmentState();
			}
			catch (Exception)
			{
			}
		}, DispatcherPriority.ContextIdle);
	}

	private void CheckStableAttachmentState()
	{
		TextEditor editBox = _editor.EditBox;
		if (_diagnosticAwaitingTaskContent || editBox.TextArea.IsImeCompositionDocumentUpdate || editBox.Document.IsInUpdate)
		{
			return;
		}
		FlushPendingAttachmentCheck();
		string text = editBox.Text ?? string.Empty;
		foreach (TrackedAttachment possiblyDamagedAttachment in _possiblyDamagedAttachments)
		{
			int offset = possiblyDamagedAttachment.Start.Offset;
			int num = possiblyDamagedAttachment.End.Offset - offset;
			if (num <= 0 || num > 8192)
			{
				continue;
			}
			string text2 = editBox.Document.GetText(offset, num);
			string internalAttachmentId = AttachmentDao.GetInternalAttachmentId(possiblyDamagedAttachment.Url);
			if (internalAttachmentId == null || !text2.Replace("\r", "").Replace("\n", "").Contains(internalAttachmentId))
			{
				continue;
			}
			char[] array = text2.ToCharArray();
			foreach (Match item in MarkDownEditor.AttachmentRegex.Matches(text2))
			{
				if (MarkDownEditor.IsRenderableAttachmentMatch(text, MarkDownEditor.AttachmentRegex.Match(text, offset + item.Index)))
				{
					for (int i = item.Index; i < item.Index + item.Length; i++)
					{
						array[i] = ' ';
					}
				}
			}
			if (new string(array).Replace("\r", "").Replace("\n", "").Contains(internalAttachmentId))
			{
				WriteEditDiagnostic(possiblyDamagedAttachment.Url, "token_changed", $"offset:{offset} length:{num} previouslyRecognized:true");
			}
		}
		_possiblyDamagedAttachments.Clear();
		if (_diagnosticMetadataDirty)
		{
			return;
		}
		TextView textView = editBox.TextArea.TextView;
		if (!textView.VisualLinesValid)
		{
			return;
		}
		Dictionary<int, AttachmentInfo> imageDict = _editor.GetImageDict();
		foreach (VisualLine visualLine in textView.VisualLines)
		{
			int offset2 = visualLine.FirstDocumentLine.Offset;
			int num2 = visualLine.LastDocumentLine.EndOffset - offset2;
			if (num2 > 65536)
			{
				continue;
			}
			string text3 = editBox.Document.GetText(offset2, num2);
			foreach (Match item2 in MarkDownEditor.AttachmentRegex.Matches(text3))
			{
				int num3 = offset2 + item2.Index;
				string value = item2.Groups[2].Value;
				if (value.Length > 2048 || item2.Groups[1].Value != "image" || !MarkDownEditor.IsRenderableAttachmentMatch(text3, item2) || _editor.IsAttachmentDiagnosticLiteralRange(num3, num3 + item2.Length))
				{
					continue;
				}
				if (!imageDict.TryGetValue(num3, out var value2))
				{
					WriteEditDiagnostic(value, "recognition_missing", $"offset:{num3} tokenLength:{item2.Length}");
					continue;
				}
				int num4 = offset2;
				bool flag = false;
				foreach (VisualLineElement element in visualLine.Elements)
				{
					if (num4 == num3 && element is AttachmentElement)
					{
						flag = true;
					}
					num4 += element.DocumentLength;
				}
				if (!flag)
				{
					WriteEditDiagnostic(value, "element_missing", $"offset:{num3} tokenLength:{item2.Length} recordedLength:{value2.Value.Length}");
				}
			}
		}
	}

	private void WriteEditDiagnostic(string url, string reason, string details)
	{
		string internalAttachmentId = AttachmentDao.GetInternalAttachmentId(url);
		if (!string.IsNullOrEmpty(internalAttachmentId))
		{
			_editJournal.Report(_taskId, internalAttachmentId, reason, details, EditDiagnosticSink);
		}
	}

	private void SetTaskId(object sender, string id)
	{
		if (_taskId == null || _taskId != id)
		{
			BeginDiagnosticTaskChange(id);
			NotImageUrlSet.Clear();
			_cachedControls.Clear();
			_pendingVisualMatchDiagnostics.Clear();
		}
		ThemeUtil.TryClearImageCached();
		AttachmentLoadHelper.ClearFailed();
	}

	private void LogRenderIssue(AttachmentInfo info, string stage, string reason, AttachmentModel attachment = null, string details = null)
	{
		string internalAttachmentId = AttachmentDao.GetInternalAttachmentId(info?.Url);
		AttachmentRenderDiagnostic.Warn(stage, _taskId, internalAttachmentId, reason, attachment, details);
	}

	public ImageElementGenerator(MarkDownEditor editor)
	{
		_editor = editor;
		editor.EditBox.PreviewKeyDown += delegate(object s, KeyEventArgs e)
		{
			if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Return || Keyboard.Modifiers != ModifierKeys.None)
			{
				_editJournal.Record($"key:{e.Key} modifiers:{Keyboard.Modifiers}");
			}
		};
		editor.EditBox.TextArea.TextView.VisualLinesChanged += delegate
		{
			ScheduleEditDiagnostic();
		};
		editor.EditBox.TextChanged += delegate
		{
			ScheduleEditDiagnostic();
		};
		editor.SizeChanged += OnSizeChanged;
		editor.TaskIdChanged += SetTaskId;
		editor.Unloaded += delegate
		{
			ResetEditDiagnostic();
			_cachedControls.Clear();
			_pendingVisualMatchDiagnostics.Clear();
		};
	}

	private void ScheduleStableVisualMatchDiagnostic(AttachmentInfo info, int expectedOffset, string initialReason)
	{
		string attachmentId = AttachmentDao.GetInternalAttachmentId(info?.Url);
		string taskId = _taskId;
		string expectedValue = info?.Value;
		string expectedUrl = info?.Url;
		string expectedDisplayText = _editor?.EditBox?.Text ?? string.Empty;
		int expectedMatchCount = (_editor?.GetImageDict()?.Count((KeyValuePair<int, AttachmentInfo> pair) => pair.Value != null && string.Equals(pair.Value.Value, expectedValue, StringComparison.Ordinal) && string.Equals(pair.Value.Url, expectedUrl, StringComparison.Ordinal))).GetValueOrDefault();
		if (string.IsNullOrEmpty(taskId) || string.IsNullOrEmpty(attachmentId) || string.IsNullOrEmpty(expectedValue))
		{
			return;
		}
		string pendingKey = $"{taskId}|{attachmentId}|{expectedOffset}";
		if (!_pendingVisualMatchDiagnostics.TryAdd(pendingKey, 0))
		{
			return;
		}
		Dispatcher dispatcher = _editor?.Dispatcher;
		if (dispatcher == null)
		{
			_pendingVisualMatchDiagnostics.TryRemove(pendingKey, out var _);
			return;
		}
		dispatcher.BeginInvoke((Action)delegate
		{
			_pendingVisualMatchDiagnostics.TryRemove(pendingKey, out var _);
			if (string.Equals(_taskId, taskId, StringComparison.Ordinal) && string.Equals(_editor?.EditBox?.Text ?? string.Empty, expectedDisplayText, StringComparison.Ordinal))
			{
				string displayText = _editor.EditBox?.Text ?? string.Empty;
				Dictionary<int, AttachmentInfo> imageDict = _editor.GetImageDict();
				int num = imageDict?.Count((KeyValuePair<int, AttachmentInfo> pair) => pair.Value != null && string.Equals(pair.Value.Value, expectedValue, StringComparison.Ordinal) && string.Equals(pair.Value.Url, expectedUrl, StringComparison.Ordinal) && pair.Key >= 0 && pair.Key + expectedValue.Length <= displayText.Length && string.CompareOrdinal(displayText, pair.Key, expectedValue, 0, expectedValue.Length) == 0) ?? 0;
				if (expectedMatchCount <= 0 || num < expectedMatchCount)
				{
					AttachmentRenderDiagnostic.Warn("visual-match", taskId, attachmentId, "display_text_mismatch_after_redraw", null, $"expectedOffset:{expectedOffset}, expectedMatchCount:{expectedMatchCount}, stableMatchCount:{num}, valueLength:{expectedValue.Length}, metadataCount:{imageDict?.Count ?? 0}, textLength:{displayText.Length}, initialReason:{initialReason}");
				}
			}
		}, DispatcherPriority.ContextIdle);
	}

	private AttachmentViewModel GetAttachmentViewModel(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		if (_cachedControls.ContainsKey(id) && _cachedControls[id].DataContext is AttachmentViewModel result)
		{
			return result;
		}
		return null;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (Math.Abs(e.NewSize.Width - _currentWidth) > 1.0)
		{
			_editor.Redraw();
			_currentWidth = e.NewSize.Width;
		}
	}

	private void GetMatch(int startOffset, out int matchOffset)
	{
		int endOffset = base.CurrentContext.VisualLine.LastDocumentLine.EndOffset;
		StringSegment text = base.CurrentContext.GetText(startOffset, endOffset - startOffset);
		Dictionary<int, AttachmentInfo> dictionary = _editor?.GetImageDict();
		if (dictionary != null && dictionary.Any())
		{
			foreach (KeyValuePair<int, AttachmentInfo> item in dictionary)
			{
				AttachmentInfo value = item.Value;
				if (value == null || item.Key < startOffset || item.Key + value.Value.Length > endOffset)
				{
					continue;
				}
				try
				{
					if (text.Text.Substring(item.Key - startOffset + text.Offset, value.Value.Length) == value.Value)
					{
						matchOffset = item.Key;
						return;
					}
					ScheduleStableVisualMatchDiagnostic(value, item.Key, "display_text_mismatch");
				}
				catch (Exception)
				{
					ScheduleStableVisualMatchDiagnostic(value, item.Key, "display_substring_exception");
				}
			}
		}
		matchOffset = -1;
	}

	private double GetAttachmentHostWidth()
	{
		double valueOrDefault = (_editor?.EditBox?.TextArea?.TextView?.ActualWidth).GetValueOrDefault();
		if (valueOrDefault > 1.0)
		{
			return valueOrDefault;
		}
		double num = ((_editor.ActualWidth == 0.0 && !double.IsNaN(_editor.Width)) ? _editor.Width : _editor.ActualWidth);
		return Math.Max(0.0, num - 29.0);
	}

	private double GetAttachmentMaxWidth(double minimumWidth)
	{
		return Math.Max(GetAttachmentHostWidth() - 7.0, minimumWidth);
	}

	private void TrackDisplayOffset(AttachmentMDDisplayModel model, int offset)
	{
		TextDocument textDocument = base.CurrentContext?.VisualLine?.Document;
		if (model != null && textDocument != null)
		{
			TextAnchor anchor = textDocument.CreateAnchor(offset);
			anchor.MovementType = AnchorMovementType.AfterInsertion;
			model.DisplayOffsetProvider = () => (!anchor.IsDeleted) ? new int?(anchor.Offset) : ((int?)null);
		}
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		GetMatch(startOffset, out var matchOffset);
		return matchOffset;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		int endOffset = base.CurrentContext.VisualLine.LastDocumentLine.EndOffset;
		Dictionary<int, AttachmentInfo> imageDict = _editor.GetImageDict();
		if (!imageDict.ContainsKey(offset))
		{
			return null;
		}
		AttachmentInfo attachmentInfo = imageDict[offset];
		if (endOffset < offset + attachmentInfo.Value.Length)
		{
			LogRenderIssue(attachmentInfo, "element-build", "attachment_outside_visual_line", null, $"offset:{offset}, valueLength:{attachmentInfo.Value.Length}, lineEnd:{endOffset}");
			return null;
		}
		string kind = attachmentInfo.Kind;
		if (!(kind == "image"))
		{
			if (kind == "file")
			{
				return GetFileElement(attachmentInfo);
			}
			LogRenderIssue(attachmentInfo, "element-build", "unknown_attachment_kind", null, "kind:" + (attachmentInfo.Kind ?? string.Empty));
			return null;
		}
		try
		{
			return GetImageElement(attachmentInfo);
		}
		catch (Exception ex)
		{
			LogRenderIssue(attachmentInfo, "element-build", "image_element_exception", null, "exception:" + ex.GetType().Name);
			UtilLog.Error($"ImageElementGenerator.ConstructElement, GetImageElement Failed {ex}");
			return null;
		}
	}

	private AttachmentElement GetImageElement(AttachmentInfo info)
	{
		string text = info.Url;
		int imageMode = _editor.ImageMode;
		int length = info.Value.Length;
		int offset = info.Offset;
		AttachmentMDDisplayModel attachmentMDDisplayModel = new AttachmentMDDisplayModel
		{
			Info = info
		};
		TrackDisplayOffset(attachmentMDDisplayModel, offset);
		bool flag = false;
		string text2 = string.Empty;
		if (!Uri.IsWellFormedUriString(text, UriKind.Absolute))
		{
			AttachmentModel attachmentInUrl = AttachmentDao.GetAttachmentInUrl(text);
			if (attachmentInUrl == null || attachmentInUrl.deleted)
			{
				LogRenderIssue(info, "image-element", (attachmentInUrl == null) ? "relation_or_cache_missing" : "relation_deleted", attachmentInUrl);
				return null;
			}
			attachmentMDDisplayModel.Attachment = attachmentInUrl;
			if (FileUtils.FileEmptyOrNotExists(attachmentInUrl.localPath))
			{
				if (AttachmentLoadHelper.IsFailed(attachmentInUrl.id))
				{
					flag = true;
				}
				else if (!AttachmentLoadHelper.IsLoading(attachmentInUrl.id))
				{
					AttachmentDownloadUtils.AutoDownloadImgAttachment(attachmentInUrl);
				}
			}
			else
			{
				text = attachmentInUrl.localPath;
				attachmentMDDisplayModel.LocalPath = attachmentInUrl.localPath;
			}
		}
		else
		{
			string text3 = text.GetHashCode().ToString();
			if (!Directory.Exists(AppPaths.ImageDir))
			{
				Directory.CreateDirectory(AppPaths.ImageDir);
			}
			string text4 = Directory.GetFiles(AppPaths.ImageDir, text3 + ".*").FirstOrDefault();
			if (!string.IsNullOrEmpty(text4))
			{
				attachmentMDDisplayModel.LocalPath = text4;
				text = text4;
				text2 = $"{text}{offset}";
			}
			if (FileUtils.FileEmptyOrNotExists(text4))
			{
				return null;
			}
		}
		bool flag2 = _editor.EditBox.SelectionStart <= offset && _editor.EditBox.SelectionStart + _editor.EditBox.SelectionLength >= offset + length;
		_currentWidth = ((_editor.ActualWidth == 0.0 && !double.IsNaN(_editor.Width)) ? _editor.Width : _editor.ActualWidth);
		AttachmentViewModel attachmentViewModel = GetAttachmentViewModel(attachmentMDDisplayModel.Attachment?.id ?? text2);
		bool flag3 = attachmentViewModel == null;
		if (attachmentViewModel?.AttachmentModel != null && attachmentMDDisplayModel.Attachment != null)
		{
			attachmentViewModel.AttachmentModel = attachmentMDDisplayModel.Attachment;
			attachmentViewModel.FileStatus = AttachmentLoadHelper.AttachmentStatus(attachmentMDDisplayModel.Attachment);
		}
		AttachmentViewModel attachmentViewModel2 = attachmentViewModel ?? (text.StartsWith(AppPaths.ImageDir) ? new AttachmentViewModel(text, imageMode) : new AttachmentViewModel(attachmentMDDisplayModel.Attachment, imageMode));
		attachmentViewModel2.LocalPath = (flag ? "failed" : text);
		bool flag4 = imageMode == 1;
		double attachmentMaxWidth = GetAttachmentMaxWidth(40.0);
		int num = ((imageMode == 1) ? SmallSizeHeight : 15000);
		if (_editor.InPrint && !flag4)
		{
			num = 800;
			flag4 = true;
		}
		Size imageSize = ThemeUtil.GetImageSize(text);
		Size size = (flag4 ? ImageUtils.GetSmallRect(attachmentMaxWidth, num, imageSize.Width, imageSize.Height) : ImageUtils.GetNormalRect(attachmentMaxWidth, num, imageSize.Width, imageSize.Height));
		attachmentViewModel2.Width = size.Width;
		attachmentViewModel2.Height = size.Height;
		attachmentViewModel2.Tag = attachmentMDDisplayModel;
		attachmentViewModel2.SelectedBrush = (flag2 ? _editor.EditBox.TextArea.SelectionBrush : null);
		attachmentViewModel2.CornerRadius = (flag4 ? new CornerRadius(3.0) : new CornerRadius(8.0));
		attachmentViewModel2.StatusMargin = (flag4 ? new Thickness(3.0) : new Thickness(8.0));
		double baselineExtra = 0.0;
		AttachmentImageDisplayControl attachmentImageDisplayControl = null;
		if (flag3)
		{
			attachmentImageDisplayControl = new AttachmentImageDisplayControl(attachmentViewModel2, _editor.OnAttachmentMouseEnter);
			if (attachmentMDDisplayModel.Attachment != null)
			{
				_cachedControls[attachmentMDDisplayModel.Attachment.id] = attachmentImageDisplayControl;
			}
			else
			{
				_cachedControls[text2] = attachmentImageDisplayControl;
			}
		}
		else
		{
			attachmentImageDisplayControl = ((attachmentMDDisplayModel.Attachment == null) ? (_cachedControls[text2] as AttachmentImageDisplayControl) : (_cachedControls[attachmentMDDisplayModel.Attachment.id] as AttachmentImageDisplayControl));
			attachmentImageDisplayControl?.UpdateImage();
		}
		if (attachmentImageDisplayControl != null)
		{
			attachmentImageDisplayControl.Tag = attachmentMDDisplayModel;
		}
		if (attachmentImageDisplayControl == null)
		{
			LogRenderIssue(info, "image-element", "display_control_missing", attachmentMDDisplayModel.Attachment);
			return null;
		}
		return new AttachmentElement(length, attachmentImageDisplayControl, attachmentViewModel2.Width + 0.0, attachmentViewModel2.Height, baselineExtra, TextAlignment.Left);
	}

	internal bool TryGetControlBounds(int attachmentOffset, TextView textView, out Rect rect)
	{
		rect = Rect.Empty;
		if (textView == null)
		{
			return false;
		}
		foreach (FrameworkElement value in _cachedControls.Values)
		{
			if (value?.Tag is AttachmentMDDisplayModel { Info: not null } attachmentMDDisplayModel && attachmentMDDisplayModel.Info.Offset == attachmentOffset && value.IsVisible && !(value.RenderSize.Width <= 0.0) && !(value.RenderSize.Height <= 0.0))
			{
				try
				{
					rect = value.TransformToAncestor(textView).TransformBounds(new Rect(new Point(0.0, 0.0), value.RenderSize));
					return !rect.IsEmpty && rect.Width > 0.0 && rect.Height > 0.0;
				}
				catch (InvalidOperationException)
				{
				}
				catch (ArgumentException)
				{
				}
			}
		}
		rect = Rect.Empty;
		return false;
	}

	private AttachmentElement GetFileElement(AttachmentInfo info)
	{
		AttachmentModel attachmentInUrl = AttachmentDao.GetAttachmentInUrl(info.Url);
		if (attachmentInUrl != null && !attachmentInUrl.deleted)
		{
			double attachmentMaxWidth = GetAttachmentMaxWidth(100.0);
			int num = 56;
			AttachmentFileDisplayControl fileControl = GetFileControl(attachmentInUrl, attachmentMaxWidth, num, _editor.OnAttachmentMouseEnter);
			fileControl.Tag = new AttachmentMDDisplayModel
			{
				Info = info,
				Attachment = attachmentInUrl,
				LocalPath = attachmentInUrl.localPath
			};
			TrackDisplayOffset(fileControl.Tag as AttachmentMDDisplayModel, info.Offset);
			return new AttachmentElement(info.Value.Length, fileControl, attachmentMaxWidth, num, 0.0, TextAlignment.Center, isFile: true);
		}
		LogRenderIssue(info, "file-element", (attachmentInUrl == null) ? "relation_or_cache_missing" : "relation_deleted", attachmentInUrl);
		return null;
	}

	private AttachmentFileDisplayControl GetFileControl(AttachmentModel model, double width, double height, AttachmentImageDisplayControl.OnAttachementMouseEnter mouseEnterCallback)
	{
		AttachmentFileDisplayControl attachmentFileDisplayControl = null;
		AttachmentViewModel attachmentViewModel = GetAttachmentViewModel(model.id);
		bool flag = attachmentViewModel == null;
		attachmentViewModel = attachmentViewModel ?? new AttachmentViewModel(model);
		attachmentViewModel.Width = width;
		attachmentViewModel.Height = height;
		attachmentViewModel.Margin = (_editor.InPrint ? new Thickness(-12.0, 0.0, 0.0, 0.0) : new Thickness(0.0, 0.0, 0.0, 0.0));
		if (flag)
		{
			bool valueOrDefault = _editor?.FindResource("IsDarkTheme") as bool? == true;
			attachmentFileDisplayControl = new AttachmentFileDisplayControl(attachmentViewModel, mouseEnterCallback, valueOrDefault);
			double lastHeight = height;
			attachmentFileDisplayControl.SizeChanged += delegate(object s, SizeChangedEventArgs e)
			{
				if (Math.Abs(e.NewSize.Height - lastHeight) > 1.0)
				{
					lastHeight = e.NewSize.Height;
					Application.Current.Dispatcher.BeginInvoke((Action)delegate
					{
						_editor?.Redraw();
					}, DispatcherPriority.ContextIdle);
				}
			};
			_cachedControls[model.id] = attachmentFileDisplayControl;
		}
		else
		{
			if (attachmentViewModel.AttachmentModel != null)
			{
				attachmentViewModel.AttachmentModel = model;
				attachmentViewModel.FileStatus = AttachmentLoadHelper.AttachmentStatus(model);
			}
			attachmentFileDisplayControl = _cachedControls[model.id] as AttachmentFileDisplayControl;
		}
		return attachmentFileDisplayControl;
	}
}
