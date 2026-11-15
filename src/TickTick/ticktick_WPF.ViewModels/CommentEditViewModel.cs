using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.ViewModels;

public class CommentEditViewModel : BaseViewModel
{
	private string _content = string.Empty;

	private CommentViewModel _recentComment;

	private string _replyName = string.Empty;

	private bool _showLeftCount;

	private double _recentScrollHeight;

	private bool _showTopLine = true;

	public string CommentId;

	public string TaskId;

	public string ProjectId;

	public bool Init;

	private double _lastItemHeight;

	private bool _contentFocused;

	private int _actionPanelRow;

	public string Content
	{
		get
		{
			return _content;
		}
		set
		{
			_content = value;
			OnPropertyChanged("Content");
			OnPropertyChanged("LeftInputCount");
			OnPropertyChanged("ShowLeftCount");
			OnPropertyChanged("TextPadding");
		}
	}

	public bool ShowLeftCount => LeftInputCount >= 924;

	public int LeftInputCount
	{
		get
		{
			return _content.Length;
		}
		set
		{
		}
	}

	public string ReplyName
	{
		get
		{
			return _replyName;
		}
		set
		{
			string replyName = _replyName;
			_replyName = value;
			Content = GetReplyContent(Content.Trim(), replyName);
		}
	}

	public string ReplyCommentId { get; set; } = string.Empty;

	public CommentViewModel RecentComment
	{
		get
		{
			return _recentComment;
		}
		set
		{
			_recentComment = value;
			OnPropertyChanged("RecentComment");
			OnPropertyChanged("ShowRecentComment");
		}
	}

	public double RecentScrollHeight
	{
		get
		{
			return _recentScrollHeight;
		}
		set
		{
			_recentScrollHeight = value;
			OnPropertyChanged("ShowRecentComment");
		}
	}

	public bool ShowRecentComment
	{
		get
		{
			if (RecentScrollHeight > 1.0 && RecentComment?.ListModel != null)
			{
				return !RecentComment.ListModel.Editing();
			}
			return false;
		}
	}

	public bool ShowTopLine
	{
		get
		{
			return _showTopLine;
		}
		set
		{
			_showTopLine = value;
			OnPropertyChanged("ShowTopLine");
		}
	}

	public bool ContentFocused
	{
		get
		{
			return _contentFocused;
		}
		set
		{
			_contentFocused = value;
			OnPropertyChanged("ContentFocused");
		}
	}

	public int ActionPanelRow
	{
		get
		{
			return _actionPanelRow;
		}
		set
		{
			_actionPanelRow = value;
			OnPropertyChanged("ActionPanelRow");
		}
	}

	public ObservableCollection<AddTaskAttachmentInfo> Files { get; } = new ObservableCollection<AddTaskAttachmentInfo>();

	public void RefreshRecentComment()
	{
		OnPropertyChanged("RecentComment");
		OnPropertyChanged("ShowRecentComment");
	}

	public void SetLastHeight(double height)
	{
		_lastItemHeight = height;
		OnPropertyChanged("ShowRecentComment");
	}

	public string GetReplyContent(string oldContent = null, string oldName = null)
	{
		string text = Utils.GetString("reply") + " " + oldName + ":";
		if (!string.IsNullOrEmpty(oldContent) && oldContent.StartsWith(text))
		{
			oldContent = oldContent.Remove(0, text.Length);
		}
		return Utils.GetString("reply") + " " + _replyName + ": " + oldContent;
	}

	public void AddFile(string path)
	{
		Constants.AttachmentKind fileType = AttachmentProvider.GetFileType(path);
		FileUtils.CollectFileSize(path);
		if (FileUtils.FileOverSize(path) && fileType != Constants.AttachmentKind.IMAGE)
		{
			Utils.Toast(LocalSettings.Settings.IsPro ? Utils.GetString("AttachmentSizeLimitPro") : Utils.GetString("AttachmentSizeLimit"));
		}
		else if (Files.All((AddTaskAttachmentInfo f) => f.Path != path))
		{
			Files.Add(new AddTaskAttachmentInfo(path));
		}
	}

	public void ClearFiles(bool needDelete)
	{
		List<AddTaskAttachmentInfo> list = Files.ToList();
		Application.Current?.Dispatcher?.Invoke(delegate
		{
			Files.Clear();
		});
		if (!needDelete)
		{
			return;
		}
		foreach (AddTaskAttachmentInfo item in list)
		{
			try
			{
				FileInfo fileInfo = new FileInfo(item.Path);
				fileInfo.Attributes = FileAttributes.Normal;
				fileInfo.Delete();
			}
			catch (Exception ex)
			{
				UtilLog.Info(ex.Message);
			}
		}
	}

	public List<AddTaskAttachmentInfo> ClearAndGetFiles()
	{
		List<AddTaskAttachmentInfo> result = Files.ToList();
		ClearFiles(needDelete: false);
		return result;
	}

	~CommentEditViewModel()
	{
		if (Files.Count > 0)
		{
			ClearFiles(needDelete: true);
		}
	}
}
