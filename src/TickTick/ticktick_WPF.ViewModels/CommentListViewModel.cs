using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ticktick_WPF.Models;

namespace ticktick_WPF.ViewModels;

public class CommentListViewModel : BaseViewModel
{
	private List<CommentViewModel> _commentList;

	private ObservableCollection<CommentViewModel> _displayList;

	private bool _showExpand;

	private ViewMode _viewMode;

	public CommentEditViewModel EditComment { get; set; }

	public bool ShowExpand
	{
		get
		{
			return _showExpand;
		}
		set
		{
			_showExpand = value;
			OnPropertyChanged("ShowExpand");
		}
	}

	public ViewMode ViewMode
	{
		get
		{
			return _viewMode;
		}
		set
		{
			_viewMode = value;
			OnPropertyChanged("ViewMode");
		}
	}

	public ObservableCollection<CommentViewModel> DisplayList
	{
		get
		{
			return _displayList;
		}
		set
		{
			_displayList = value;
			OnPropertyChanged("DisplayList");
		}
	}

	public List<CommentViewModel> CommentList
	{
		get
		{
			return _commentList;
		}
		set
		{
			_commentList = value;
			OnPropertyChanged("Count");
			OnPropertyChanged("CommentList");
			CommentEditViewModel editComment = EditComment;
			List<CommentViewModel> commentList = _commentList;
			editComment.RecentComment = ((commentList != null && commentList.Count > 0) ? _commentList[_commentList.Count - 1] : null);
		}
	}

	public int Count => _commentList.Count;

	public CommentListViewModel(List<CommentViewModel> commentList, CommentEditViewModel editComment, bool canEdit = true)
	{
		_viewMode = ViewMode.Full;
		_commentList = commentList.OrderBy((CommentViewModel c) => c.CreateDate).ToList();
		_displayList = new ObservableCollection<CommentViewModel>();
		if (_commentList.Count > 0)
		{
			foreach (CommentViewModel comment in _commentList)
			{
				comment.ListModel = this;
				comment.CanEdit = canEdit;
			}
		}
		SetFullDisplayList();
		ShowExpand = false;
		EditComment = editComment;
	}

	private void SetRecentDisplayList()
	{
		ObservableCollection<CommentViewModel> observableCollection = new ObservableCollection<CommentViewModel>();
		for (int i = Math.Max(0, _commentList.Count - 5); i < _commentList.Count; i++)
		{
			observableCollection.Add(_commentList[i]);
		}
		DisplayList = observableCollection;
	}

	private void SetFullDisplayList()
	{
		ObservableCollection<CommentViewModel> observableCollection = new ObservableCollection<CommentViewModel>();
		for (int i = 0; i < _commentList.Count; i++)
		{
			observableCollection.Add(_commentList[i]);
		}
		DisplayList = observableCollection;
	}

	public void ExpandOrCollapse()
	{
		Expand();
	}

	public void Expand()
	{
		if (ViewMode != ViewMode.Full)
		{
			ViewMode = ViewMode.Full;
			SetFullDisplayList();
		}
		ShowExpand = false;
	}

	public void AddItem(CommentViewModel newComment)
	{
		_commentList.Add(newComment);
		ShowExpand = false;
		DisplayList.Add(newComment);
		OnPropertyChanged("Count");
	}

	public bool CheckComments(List<CommentModel> localComments)
	{
		foreach (CommentModel comment in localComments)
		{
			CommentViewModel commentViewModel = _commentList.FirstOrDefault((CommentViewModel c) => c.Id == comment.id);
			if (commentViewModel == null)
			{
				return true;
			}
			if (commentViewModel.Content != comment.title.Trim())
			{
				commentViewModel.Content = comment.title.Trim();
				return true;
			}
		}
		return false;
	}

	public void RemoteComment(CommentViewModel model)
	{
		_commentList.Remove(model);
		DisplayList.Remove(model);
		ShowExpand = false;
		OnPropertyChanged("Count");
		OnPropertyChanged("CommentList");
		CommentEditViewModel editComment = EditComment;
		List<CommentViewModel> commentList = _commentList;
		editComment.RecentComment = ((commentList != null && commentList.Count > 0) ? _commentList[_commentList.Count - 1] : null);
	}

	public bool Editing()
	{
		return _commentList.Any((CommentViewModel m) => m.Editing);
	}
}
