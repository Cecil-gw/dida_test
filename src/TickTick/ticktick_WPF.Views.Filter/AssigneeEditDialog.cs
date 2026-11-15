using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class AssigneeEditDialog : ConditionEditDialogBase<string>
{
	private List<string> _originAssignees = new List<string>();

	public event EventHandler<List<string>> OnSelectedAssigneeChanged;

	public AssigneeEditDialog(List<string> selectedAssignees, bool showAll)
	{
		InitializeComponent();
		_originAssignees = selectedAssignees;
		InitData(selectedAssignees, showAll);
		base.OnSelectedValueChanged += delegate(object sender, FilterConditionViewModel e)
		{
			FilterListItemViewModel filterListItemViewModel = e?.ItemsSource.FirstOrDefault((FilterListItemViewModel item) => item.Value.ToString() == "other");
			if (filterListItemViewModel != null)
			{
				filterListItemViewModel.PartSelected = false;
			}
			if (e != null && e.ItemsSource.All((FilterListItemViewModel item) => item.Selected))
			{
				foreach (FilterListItemViewModel item in e.ItemsSource)
				{
					item.Selected = false;
				}
				e.IsAllSelected = true;
			}
			this.OnSelectedAssigneeChanged?.Invoke(this, GetSelectedValues());
		};
	}

	public AssigneeEditDialog(int version)
		: this(new List<string>(), showAll: false)
	{
	}

	private void InitData(List<string> selectedAssignees, bool showAll)
	{
		ObservableCollection<FilterListItemViewModel> observableCollection = new ObservableCollection<FilterListItemViewModel>();
		observableCollection.Add(new FilterListItemViewModel
		{
			Title = Utils.GetString("NotAssigned"),
			Value = "noassignee",
			Icon = "Ic.Sharing.NoAssign",
			ShowIcon = true,
			Selected = selectedAssignees.Contains("noassignee")
		});
		FilterListItemViewModel item = new FilterListItemViewModel
		{
			Title = Utils.GetString("AssignToMe"),
			Value = "me",
			Icon = "Ic.ProjectList.AssignToMe",
			ShowIcon = true,
			Selected = selectedAssignees.Contains("me"),
			IsAssignee = true
		};
		observableCollection.Add(item);
		FilterListItemViewModel filterListItemViewModel = new FilterListItemViewModel
		{
			Title = Utils.GetString("AssignedToOthers2"),
			Value = "other",
			Icon = "Ic.Sharing.AssignToOther",
			ShowIcon = true,
			Selected = selectedAssignees.Contains("other"),
			IsAssignee = true,
			Unfold = true,
			Children = new List<FilterListItemViewModel>()
		};
		observableCollection.Add(filterListItemViewModel);
		Dictionary<string, ShareUserModel> allShareUsers = AvatarHelper.GetAllShareUsers();
		allShareUsers.Remove(LocalSettings.Settings.LoginUserId);
		List<ShareUserModel> list = (from user in allShareUsers.Values
			where user.isAccept
			orderby (!string.IsNullOrEmpty(user.displayName)) ? user.displayName : user.username
			select user).ToList();
		if (list.Count > 0)
		{
			observableCollection.Add(new FilterListItemViewModel
			{
				IsSplit = true,
				Value = "split"
			});
		}
		foreach (ShareUserModel item3 in list)
		{
			FilterListItemViewModel item2 = new FilterListItemViewModel
			{
				Title = (string.IsNullOrEmpty(item3.displayName) ? item3.username : item3.displayName),
				Value = item3.userId.ToString(),
				ImageUrl = (string.IsNullOrEmpty(item3.avatarUrl) ? "pack://application:,,,/Assets/avatar-new.png" : item3.avatarUrl),
				Selected = (selectedAssignees.Contains("other") || selectedAssignees.Contains(item3.userId.ToString())),
				IsAssignee = true,
				ShowFeishu = (item3.siteId == 10 && Utils.IsDida()),
				GroupId = "other"
			};
			observableCollection.Add(item2);
			filterListItemViewModel.Children.Add(item2);
		}
		base.ViewModel = new FilterConditionViewModel
		{
			Type = CondType.Assignee,
			ShowAll = showAll,
			ItemsSource = observableCollection,
			IsAllSelected = (selectedAssignees.Count == 0),
			SupportedLogic = new List<LogicType> { LogicType.Or }
		};
	}

	public override void Restore()
	{
		RestoreSetSelected();
		this.OnSelectedAssigneeChanged?.Invoke(this, _originAssignees);
	}

	private void RestoreSetSelected()
	{
		foreach (FilterListItemViewModel item in base.ViewModel.ItemsSource)
		{
			item.Selected = _originAssignees.Contains(item.Value.ToString());
		}
	}

	protected override void SyncOriginal()
	{
		_originAssignees = GetSelectedValues().ToList();
	}

	protected override bool CanSave()
	{
		if (GetSelectedValues().Count <= 0)
		{
			return base.CanSave();
		}
		return true;
	}
}
