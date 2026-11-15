using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.ViewModels;

public class CommentViewModel : BaseViewModel
{
	public string Id;

	private readonly CommentModel _model;

	private string _avatarUrl;

	private bool _candelete;

	private bool _editing;

	private string _content;

	private DateTime _createDate;

	private bool _deleted;

	private string _userName;

	public ObservableCollection<AddTaskAttachmentInfo> Files { get; set; } = new ObservableCollection<AddTaskAttachmentInfo>();

	public CommentListViewModel ListModel { get; set; }

	public CommentModel Model => _model;

	public string Content
	{
		get
		{
			return _content;
		}
		set
		{
			if (!(_content == value))
			{
				_content = value;
				OnPropertyChanged("Content");
				NotifyRecentCommentChanged();
			}
		}
	}

	public string UserName
	{
		get
		{
			return _userName;
		}
		set
		{
			_userName = value;
			OnPropertyChanged("UserName");
		}
	}

	public DateTime CreateDate
	{
		get
		{
			return _createDate;
		}
		set
		{
			_createDate = value;
			OnPropertyChanged("CreateDate");
		}
	}

	public bool Deleted
	{
		get
		{
			return _deleted;
		}
		set
		{
			_deleted = value;
			OnPropertyChanged("Deleted");
		}
	}

	public bool Editing
	{
		get
		{
			return _editing;
		}
		set
		{
			if (_editing != value)
			{
				_editing = value;
				OnPropertyChanged("Editing");
				ListModel?.EditComment?.RefreshRecentComment();
			}
		}
	}

	public bool CanDelete
	{
		get
		{
			return _candelete;
		}
		set
		{
			_candelete = value;
			OnPropertyChanged("CanDelete");
		}
	}

	public string AvatarUrl
	{
		get
		{
			return _avatarUrl;
		}
		set
		{
			_avatarUrl = value;
			OnPropertyChanged("AvatarUrl");
			SetAvatar();
		}
	}

	public BitmapImage Avatar { get; set; }

	public bool IsMySelf { get; }

	public bool CanEdit { get; set; }

	public CommentViewModel(CommentModel model)
	{
		_model = model;
		_userName = model.userName;
		_avatarUrl = model.avatarUrl;
		_candelete = model.candelete;
		_createDate = model.createdTime ?? DateTime.Now;
		_content = model.title.Trim();
		IsMySelf = model.isMySelf;
		Id = model.id;
		SetAvatar();
	}

	private async void SetAvatar()
	{
		Avatar = await AvatarHelper.GetAvatarByUrlAsync(AvatarUrl);
		OnPropertyChanged("Avatar");
		NotifyRecentCommentChanged();
	}

	public async Task RemoveComment(CommentViewModel model)
	{
		ListModel.RemoteComment(model);
		OnPropertyChanged("Count");
		UtilLog.Info("TaskDetail.DeleteComment taskId " + model._model.taskSid + ", commentId " + model._model.id);
		await CommentService.DeleteComment(model._model);
		await TaskService.SaveCommentCount(model._model.taskSid);
	}

	public bool IsLastOne()
	{
		int? num = ListModel?.CommentList?.IndexOf(this);
		CommentListViewModel listModel = ListModel;
		return num == ((listModel != null) ? new int?(listModel.Count - 1) : ((int?)null));
	}

	private void NotifyRecentCommentChanged()
	{
		if (ListModel?.EditComment?.RecentComment == this)
		{
			ListModel.EditComment.RefreshRecentComment();
		}
	}

	public async void Save()
	{
		_model.modifiedTime = DateTime.Now;
		_model.title = Content;
		await CommentService.UpdateComment(_model);
	}
}
