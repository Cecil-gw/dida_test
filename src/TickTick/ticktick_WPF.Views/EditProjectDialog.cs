using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickDao;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Drag;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views;

public class EditProjectDialog : Window, IOkCancelWindow, IComponentConnector, IStyleConnector
{
	private readonly bool _isAddProject;

	private readonly ticktick_WPF.ViewModels.ProjectViewModel _model = new ticktick_WPF.ViewModels.ProjectViewModel();

	private readonly string _originalName;

	private readonly string _originColor = "null";

	internal EditProjectDialog Window;

	internal TextBlock WTitle;

	internal Grid InputGrid;

	internal EmojiEditor AddListNameTextBox;

	internal Grid EmojiSelectGrid;

	internal Grid ProjectPathGrid;

	internal Path ProjectPath;

	internal Path SetEmojiIcon;

	internal EmjTextBlock EmojiText;

	internal EscPopup EmojiSelectPopup;

	internal EmojiSelectControl EmojiSelector;

	internal TextBox HideTextBox;

	internal TextBlock AddListNameRepeatTextBlock;

	internal ColorSelector ColorItems;

	internal Grid TypeSelectPanel;

	internal Border ListBorder;

	internal Path ListPath;

	internal Border KanbanBorder;

	internal Path KanbanPath;

	internal Border TimelineBorder;

	internal Path TimelinePath;

	internal Image HeadProImage;

	internal CustomComboBox ProjectGroupComboBox;

	internal TextBlock TypeText;

	internal CustomComboBox ProjectTypeComboBox;

	internal Grid ProjectBelongTo;

	internal CustomComboBox SelectTeamComBox;

	internal EscPopup SelectAddProjectPopup;

	internal ItemsControl ProjectTypeItems;

	internal CustomComboBox PrivacyComboBox;

	internal CheckBox AddListIsHideCheckBox;

	internal CustomComboBox ProjectShowTypeComboBox;

	internal Grid MutedGrid;

	internal CheckBox MutedCheckBox;

	internal CustomComboBox ProjectRemindTypeComboBox;

	internal Button SaveButton;

	internal Border BackBorder;

	internal Image ViewModeDisplayBorder;

	internal EmjTextBlock DisplayName;

	private bool _contentLoaded;

	public event EventHandler<ticktick_WPF.ViewModels.ProjectViewModel> OnProjectSaved;

	public EditProjectDialog(string groupId = "", string teamId = null)
	{
		teamId = ((teamId == "c1a7e08345e444dea187e21a692f0d7a") ? null : teamId);
		EmojiSearchHelper.Init();
		InitializeComponent();
		_model.InAll = true;
		_model.TeamId = teamId;
		_model.groupId = groupId;
		_model.IsNew = true;
		_model.IsShowReminderNew = true;
		base.DataContext = _model;
		LoadProjectGroup(teamId, groupId);
		_isAddProject = true;
		ColorItems.SetSelectedColor(string.Empty);
		SaveButton.IsEnabled = !string.IsNullOrEmpty(AddListNameTextBox.Text);
		LoadTeamInfo(teamId);
		AddListNameTextBox.EnterUp += OnTextKeyUp;
		SetSelectedView("list");
		SaveButton.Content = Utils.GetString("Add");
		SetBackground();
		ColorItems.ColorSelect += OnColorChanged;
		LoadShowReminderComboBox();
	}

	public EditProjectDialog(ticktick_WPF.ViewModels.ProjectViewModel model)
	{
		EmojiSearchHelper.Init();
		InitializeComponent();
		WTitle.Text = Utils.GetString("EditList");
		ColorItems.SetSelectedColor(model.Color);
		CheckEmojiStart(model);
		_model = model;
		base.DataContext = _model;
		MutedGrid.Visibility = ((!model.IsShareList()) ? Visibility.Collapsed : Visibility.Visible);
		SetProjectIcon(_model.Kind);
		LoadProjectGroup(model.TeamId, model.groupId);
		_isAddProject = false;
		_originalName = _model.Name;
		_originColor = _model.Color;
		LoadTeamInfo(model.TeamId);
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == model.id);
		if (projectModel == null || !projectModel.IsProjectPermit())
		{
			ProjectTypeComboBox.Visibility = Visibility.Collapsed;
			TypeText.Visibility = Visibility.Collapsed;
		}
		SetSelectedView(model.originalProject?.viewMode ?? "list");
		CheckTimeline(model.Kind);
		AddListNameTextBox.EnterUp += OnTextKeyUp;
		DisplayName.Text = EmojiText.Text.Trim() + AddListNameTextBox.Text;
		SetBackground();
		ColorItems.ColorSelect += OnColorChanged;
		if (model.originalProject?.closed == true)
		{
			TypeSelectPanel.Visibility = Visibility.Collapsed;
		}
		LoadShowReminderComboBox();
	}

	private void LoadShowReminderComboBox()
	{
		if (!_model.IsShowReminderNew)
		{
			base.Width = 720.0;
			base.Height = 430.0;
			base.Resources["HeaderColumnWidth"] = new GridLength(80.0);
		}
		bool flag = _model.IsShareList();
		ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>();
		observableCollection.Add(new ComboBoxViewModel(ShowReminderType.All, Utils.GetString("EditProjectAllTasks")));
		if (flag)
		{
			observableCollection.Add(new ComboBoxViewModel(ShowReminderType.ExceptOther, Utils.GetString("EditProjectAllTasksExceptAssignedToMe")));
			observableCollection.Add(new ComboBoxViewModel(ShowReminderType.OnlySelf, Utils.GetString("EditProjectTaskAsssignedToMe")));
		}
		observableCollection.Add(new ComboBoxViewModel(ShowReminderType.Never, Utils.GetString("EditProjectDoNotDisplay")));
		observableCollection.FirstOrDefault((ComboBoxViewModel x) => (ShowReminderType)x.Value == _model.ShowTypeState).Selected = true;
		ProjectShowTypeComboBox.Init(observableCollection, null);
		if (_model.IsShowReminderNew && _model.IsNew)
		{
			_model.ShowTypeEnum = _model.ShowTypeState;
		}
		ObservableCollection<ComboBoxViewModel> observableCollection2 = new ObservableCollection<ComboBoxViewModel>();
		observableCollection2.Add(new ComboBoxViewModel(ShowReminderType.All, Utils.GetString("EditProjectAllTasks")));
		if (flag)
		{
			observableCollection2.Add(new ComboBoxViewModel(ShowReminderType.ExceptOther, Utils.GetString("EditProjectAllTasksExceptAssignedToMe")));
			observableCollection2.Add(new ComboBoxViewModel(ShowReminderType.OnlySelf, Utils.GetString("EditProjectTaskAsssignedToMe")));
		}
		observableCollection2.Add(new ComboBoxViewModel(ShowReminderType.Never, Utils.GetString("EditProjectNoReminder")));
		observableCollection2.FirstOrDefault((ComboBoxViewModel x) => (ShowReminderType)x.Value == _model.ReminderTypeState).Selected = true;
		ProjectRemindTypeComboBox.Init(observableCollection2, null);
		if (_model.IsShowReminderNew && _model.IsNew && flag)
		{
			_model.ReminderTypeEnum = _model.ReminderTypeState;
		}
	}

	private void SetBackground()
	{
		if (ThemeUtil.IsDark())
		{
			BackBorder.Opacity = 1.0;
			BackBorder.SetResourceReference(Control.BackgroundProperty, "WindowBackgroundSecondary");
			return;
		}
		BackBorder.Opacity = 0.05;
		if (string.IsNullOrEmpty(_model.Color) || _model.Color.ToLower() == "transparent" || (_model.Color.Length == 9 && _model.Color.StartsWith("#00")))
		{
			BackBorder.SetResourceReference(Control.BackgroundProperty, "PrimaryColor");
		}
		else
		{
			BackBorder.Background = ThemeUtil.GetColorInString(_model.Color);
		}
	}

	private void OnColorChanged(object sender, string e)
	{
		_model.Color = ColorItems.GetSelectedColor();
		SetBackground();
	}

	private void OnTextKeyUp(object sender, EventArgs e)
	{
		if (SaveButton.IsEnabled && !EmojiSelectPopup.IsOpen && !ColorItems.ColorPopup.IsOpen && !ProjectGroupComboBox.IsOpen)
		{
			SaveProject();
		}
	}

	private void SetProjectIcon(string kind)
	{
		if (kind == Constants.ProjectKind.NOTE.ToString())
		{
			ProjectPath.Data = Utils.GetGeometry(_model.IsShareList() ? "Ic.ProjectList.NoteMember" : "Ic.ProjectList.Note");
		}
		else
		{
			ProjectPath.Data = Utils.GetGeometry(_model.IsShareList() ? "Ic.ProjectList.SharedList" : "Ic.ProjectList.List");
		}
	}

	private void CheckEmojiStart(ticktick_WPF.ViewModels.ProjectViewModel model)
	{
		string emojiIcon = EmojiHelper.GetEmojiIcon(model.Name);
		if (!string.IsNullOrEmpty(emojiIcon) && model.Name.StartsWith(emojiIcon))
		{
			emojiIcon = EmojiHelper.GetEmojiIcon(model.Name);
			model.Name = model.Name.Remove(0, emojiIcon.Length);
			SetProjectEmoji(emojiIcon);
		}
	}

	private void LoadTeamInfo(string teamId)
	{
		List<TeamModel> list = CacheManager.GetTeams()?.Where((TeamModel team) => !team.expired).ToList();
		if (list != null && list.Count != 0)
		{
			ProjectBelongTo.Visibility = ((list.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
			bool num = string.IsNullOrEmpty(_model.id);
			LoadTeamComBox(list, teamId);
			if (num)
			{
				PrivacyComboBox.Visibility = (string.IsNullOrEmpty(teamId) ? Visibility.Collapsed : Visibility.Visible);
			}
		}
	}

	private void LoadTeamComBox(List<TeamModel> teams, string teamId)
	{
		ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>();
		foreach (TeamModel team in teams)
		{
			observableCollection.Add(new ComboBoxViewModel(team.id, team.name)
			{
				Selected = (team.id == teamId)
			});
		}
		observableCollection.Add(new ComboBoxViewModel("c1a7e08345e444dea187e21a692f0d7a", Utils.GetString("Personal"))
		{
			Selected = string.IsNullOrEmpty(teamId)
		});
		SelectTeamComBox.Init(observableCollection, null);
		ObservableCollection<ComboBoxViewModel> items = new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel("0", Utils.GetString("PrivateToMe"))
			{
				Selected = true
			},
			new ComboBoxViewModel("1", string.Format(Utils.GetString("PublicToTeam"), teams[0].name))
		};
		PrivacyComboBox.Init(items, null);
	}

	private void OnTeamSelected(object sender, ComboBoxViewModel e)
	{
		string text = e.Value.ToString();
		if (string.IsNullOrEmpty(_model.id))
		{
			LoadProjectGroup(text, _model.groupId);
			_model.TeamId = ((text == "c1a7e08345e444dea187e21a692f0d7a") ? null : text);
			PrivacyComboBox.Visibility = (string.IsNullOrEmpty(_model.TeamId) ? Visibility.Collapsed : Visibility.Visible);
		}
		else
		{
			OnSwitchList(text);
		}
	}

	private async void OnLoad(object sender, RoutedEventArgs e)
	{
		if (_model?.originalProject?.ProjectId != null && !(CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.ProjectId == _model.originalProject.ProjectId)?.IsProjectPermit() ?? false))
		{
			AddListNameTextBox.ReadOnly = true;
			EmojiSelectGrid.IsEnabled = false;
			FocusManager.SetFocusedElement(this, this);
			Keyboard.Focus(this);
			base.OnActivated(e);
		}
		else
		{
			WindowNotifier.NotifySubWindowOpen(SubWindowType.EditProject);
			await Task.Delay(100);
			AddListNameTextBox.FocusEnd();
		}
	}

	private async Task LoadProjectGroup(string teamId = "", string groupId = "")
	{
		if (teamId == "c1a7e08345e444dea187e21a692f0d7a")
		{
			teamId = string.Empty;
		}
		List<ProjectGroupModel> projectGroups = CacheManager.GetProjectGroups();
		projectGroups = ((!string.IsNullOrEmpty(teamId)) ? projectGroups.Where((ProjectGroupModel group) => group.teamId == teamId).ToList() : projectGroups.Where((ProjectGroupModel group) => string.IsNullOrEmpty(group.teamId)).ToList());
		IOrderedEnumerable<ProjectGroupModel> orderedEnumerable = projectGroups.OrderBy((ProjectGroupModel g) => g.sortOrder);
		ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel("NONE", Utils.GetString("none"))
		};
		foreach (ProjectGroupModel item in orderedEnumerable)
		{
			observableCollection.Add(new ComboBoxViewModel(item.id, item.name)
			{
				Selected = (item.id == groupId)
			});
		}
		observableCollection.Add(new ComboBoxViewModel("AddProjectGroup", Utils.GetString("AddFolder"))
		{
			Image = Utils.GetGeometry("Ic.ProjectList.Add"),
			ShowImage = true,
			CanSelect = false
		});
		ProjectGroupComboBox.Init(observableCollection, null);
		ObservableCollection<ComboBoxViewModel> items = new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel("TASK", Utils.GetString("TaskList")),
			new ComboBoxViewModel("NOTE", Utils.GetString("NoteList"))
			{
				Selected = (_model?.Kind == Constants.ProjectKind.NOTE.ToString())
			}
		};
		ProjectTypeComboBox.Init(items, null);
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		SaveProject();
	}

	private async void SaveProject()
	{
		CheckValid();
		if (AddListNameRepeatTextBlock.IsVisible || (PrivacyComboBox.IsVisible && PrivacyComboBox.SelectedIndex == 1 && !(await TeamService.CheckFreeTeamShareCount(_model.TeamId, this))))
		{
			return;
		}
		if (!LocalSettings.Settings.ProjectTypeChangeNotified && _model.Kind != Constants.ProjectKind.NOTE.ToString() && ProjectTypeComboBox.SelectedIndex == 1)
		{
			LocalSettings.Settings.ProjectTypeChangeNotified = true;
			if (new CustomerDialog("", Utils.GetString("ProjectTypeChangeMessage"), Utils.GetString("OK"), Utils.GetString("Cancel")).ShowDialog() != true)
			{
				return;
			}
		}
		_model.SetName(EmojiText.Text.Trim() + AddListNameTextBox.Text);
		if (_model.IsShowReminderNew)
		{
			switch (_model.ShowTypeEnum)
			{
			case ShowReminderType.None:
				_model.InAll = (!AddListIsHideCheckBox.IsChecked) ?? true;
				break;
			case ShowReminderType.All:
				_model.InAll = true;
				break;
			default:
				_model.InAll = false;
				break;
			}
		}
		else
		{
			_model.InAll = (!AddListIsHideCheckBox.IsChecked) ?? true;
		}
		_model.Kind = ((ProjectTypeComboBox.SelectedIndex == 1) ? Constants.ProjectKind.NOTE : Constants.ProjectKind.TASK).ToString();
		if (_model.Kind != Constants.ProjectKind.NOTE.ToString())
		{
			if (_model.sortType == Constants.SortType.createdTime.ToString() || _model.sortType == Constants.SortType.modifiedTime.ToString())
			{
				_model.sortType = Constants.SortType.sortOrder.ToString();
			}
			if (_model.SortOption != null && (_model.SortOption.orderByType == Constants.SortType.createdTime || _model.SortOption.orderByType == Constants.SortType.modifiedTime))
			{
				_model.SortOption.orderBy = Constants.SortType.sortOrder.ToString();
			}
		}
		if (_model.Kind == Constants.ProjectKind.NOTE.ToString())
		{
			if (string.IsNullOrEmpty(_model.sortType) || _model.sortType == Constants.SortType.dueDate.ToString() || _model.sortType == Constants.SortType.priority.ToString())
			{
				_model.sortType = Constants.SortType.sortOrder.ToString();
			}
			if (_model.SortOption != null)
			{
				if (_model.SortOption.groupByType == Constants.SortType.dueDate || _model.SortOption.groupByType == Constants.SortType.priority || string.IsNullOrEmpty(_model.SortOption.groupBy))
				{
					_model.SortOption.groupBy = Constants.SortType.sortOrder.ToString();
				}
				if (_model.SortOption.orderByType == Constants.SortType.dueDate || _model.SortOption.orderByType == Constants.SortType.priority || string.IsNullOrEmpty(_model.SortOption.orderBy))
				{
					_model.SortOption.orderBy = Constants.SortType.sortOrder.ToString();
				}
			}
		}
		string selectedColor = ColorItems.GetSelectedColor();
		_model.Color = ((!string.IsNullOrEmpty(selectedColor)) ? selectedColor : null);
		if (_model.Color != null && _model.Color.Length == 9)
		{
			if (!_model.Color.StartsWith("#00"))
			{
				_model.Color = "#" + _model.Color.Substring(3);
			}
			else
			{
				_model.Color = "transparent";
			}
		}
		if (_model.Color != _originColor)
		{
			ColorSelector.TryAddClickEvent(_model.Color);
		}
		string originGroup = (string.IsNullOrEmpty(_model.groupId) ? "NONE" : _model.groupId);
		if (ProjectGroupComboBox.SelectedItem != null)
		{
			_model.groupId = ((ProjectGroupComboBox.SelectedItem.Value.ToString() != "") ? ProjectGroupComboBox.SelectedItem.Value.ToString() : "NONE");
		}
		string text = await SaveProject(_model, originGroup);
		_model.id = text;
		UtilLog.Info($"ProjectMenu.AddOrEditProject : id {text},isAdd {_model.IsNew}");
		this.OnProjectSaved?.Invoke(this, _model);
		Close();
	}

	private bool IsProjectNameExist(string projectName, string projectId, string teamId)
	{
		return CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.name.Trim() == projectName.Trim() && p.id != projectId && p.teamId == teamId && p.IsBelongGroup(ProjectGroupComboBox.SelectedItem.Value.ToString()) && !p.Isinbox) != null;
	}

	private static bool CheckProjectTitle(string title)
	{
		char[] anyOf = new char[10] { '\\', '/', '\'', '"', ':', '*', '?', '<', '>', '|' };
		return title.IndexOfAny(anyOf) != -1;
	}

	private async Task<string> SaveProject(ticktick_WPF.ViewModels.ProjectViewModel model, string originGroup)
	{
		ProjectModel projectModel = ((!model.IsNew) ? (await ProjectDao.GetProjectById(model.id)) : null);
		ProjectModel project = projectModel;
		project = project ?? new ProjectModel
		{
			_Id = model._Id,
			id = model.id,
			userCount = model.userCount,
			closed = model.closed,
			sortType = model.sortType,
			permission = model.originalProject?.permission,
			Timeline = model.originalProject?.Timeline,
			teamMemberPermission = model.teamMemberPermission,
			SortOption = model.SortOption,
			userid = LocalSettings.Settings.LoginUserId
		};
		string oldGroup = project.groupId;
		project.name = model.Name;
		project.color = model.Color;
		project.inAll = model.InAll;
		project.groupId = model.groupId;
		project.sortOrder = model.sortOrder;
		project.sync_status = model.sync_status;
		project.muted = model.Muted;
		project.teamId = model.TeamId;
		project.kind = model.Kind;
		project.needAudit = model.needAudit;
		project.openToTeam = model.openToTeam;
		project.viewMode = model.ViewMode;
		if (model.IsShowReminderNew)
		{
			project.ShowTypeEnum = model.ShowTypeEnum;
			project.ReminderTypeEnum = model.ReminderTypeEnum;
		}
		if (model.Kind == Constants.ProjectKind.NOTE.ToString() && !LocalSettings.Settings.IsNoteEnabled)
		{
			LocalSettings.Settings.IsNoteEnabled = true;
			LocalSettings.Settings.Save();
		}
		if (string.IsNullOrEmpty(project.id))
		{
			project.id = Utils.GetGuid();
		}
		if (!_isAddProject)
		{
			if (project.sync_status == Constants.SyncStatus.SYNC_DONE.ToString())
			{
				project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
			}
			if (!project.IsBelongGroup(originGroup) && project.IsBelongGroup(project.groupId))
			{
				project.sortOrder = (project.IsBelongGroup(string.Empty) ? ProjectDragHelper.GetNewSortOrder(upDown: true, originGroup, project.teamId) : ProjectDragHelper.GetNewSortOrder(upDown: true, project.groupId, isGroupAll: false, project.teamId));
			}
			ProjectModel projectModel2 = project;
			projectModel2._Id = await ProjectDao.TryUpdateProject(project);
			if (model.originalProject != null && project.muted != model.originalProject.muted)
			{
				ReminderCalculator.AssembleReminders();
			}
		}
		else
		{
			project.sync_status = Constants.SyncStatus.SYNC_NEW.ToString();
			if (!string.IsNullOrEmpty(project.teamId))
			{
				project.openToTeam = PrivacyComboBox.SelectedIndex == 1;
				TeamModel teamById = CacheManager.GetTeamById(project.teamId);
				if (teamById != null && !teamById.open)
				{
					teamById.open = true;
					await TeamDao.UpdateTeam(teamById);
				}
			}
			else if (!LocalSettings.Settings.ExpandPersonalSection)
			{
				LocalSettings.Settings.ExpandPersonalSection = true;
			}
			if (!string.IsNullOrEmpty(project.groupId))
			{
				ProjectGroupModel group = CacheManager.GetProjectGroups().FirstOrDefault((ProjectGroupModel p) => p.id == project.groupId);
				if (group != null && !group.open)
				{
					group.open = true;
					await BaseDao<ProjectGroupModel>.UpdateAsync(group);
					CacheManager.UpdateProjectGroup(group);
				}
				project.sortOrder = ((group != null) ? ProjectDragHelper.GetNewSortOrder(upDown: true, group.id, isGroupAll: false, project.teamId) : ProjectDao.GetNewProjectOrder(project.teamId));
			}
			else
			{
				project.sortOrder = ProjectDao.GetNewProjectOrder(project.teamId);
			}
			await ProjectDao.CreateProject(project);
			if (project.viewMode == "kanban")
			{
				await ColumnDao.InitColumns(project.id);
			}
		}
		if (project.IsShareList() && oldGroup != project.groupId)
		{
			ProjectGroupDao.CheckGroupGroupBy(oldGroup);
		}
		return project.id;
	}

	private async void OnProjectGroupEdit(object sender, ProjectGroupModel projectGroup)
	{
		_model.groupId = projectGroup.id;
		await LoadProjectGroup(_model.TeamId, _model.groupId);
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void CheckValid(object sender, EventArgs e)
	{
		CheckValid();
	}

	private async void CheckExist()
	{
		string projectName = EmojiText.Text + AddListNameTextBox.Text;
		if (IsProjectNameExist(projectName, _model.id, _model.TeamId))
		{
			AddListNameRepeatTextBlock.Text = Utils.GetString("DuplicateListName");
			AddListNameRepeatTextBlock.Visibility = Visibility.Visible;
		}
		else if (AddListNameRepeatTextBlock.Text == Utils.GetString("DuplicateListName"))
		{
			AddListNameRepeatTextBlock.Visibility = Visibility.Collapsed;
		}
		SaveButton.IsEnabled = AddListNameRepeatTextBlock.Visibility != Visibility.Visible;
		DisplayName.Text = EmojiText.Text.Trim() + AddListNameTextBox.Text;
	}

	private async void CheckValid()
	{
		string text = EmojiText.Text + AddListNameTextBox.Text;
		if (string.IsNullOrEmpty(text.Trim()))
		{
			AddListNameRepeatTextBlock.Text = Utils.GetString("AddOrEditProjectNameCantNull");
			AddListNameRepeatTextBlock.Visibility = Visibility.Visible;
		}
		else if (text[0] == '#' || text[0] == '＃')
		{
			AddListNameRepeatTextBlock.Text = Utils.GetString("ListNameBeginError");
			AddListNameRepeatTextBlock.Visibility = Visibility.Visible;
		}
		else if (!NameUtils.IsValidNameNoCheckSharp(text, checkSpace: false))
		{
			AddListNameRepeatTextBlock.Text = Utils.GetString("ListNameCantContain");
			AddListNameRepeatTextBlock.Visibility = Visibility.Visible;
		}
		else if (IsProjectNameExist(text, _model.id, _model.TeamId))
		{
			AddListNameRepeatTextBlock.Text = Utils.GetString("DuplicateListName");
			AddListNameRepeatTextBlock.Visibility = Visibility.Visible;
		}
		else
		{
			AddListNameRepeatTextBlock.Visibility = Visibility.Collapsed;
		}
		SaveButton.IsEnabled = AddListNameRepeatTextBlock.Visibility != Visibility.Visible;
		DisplayName.Text = EmojiText.Text.Trim() + AddListNameTextBox.Text;
	}

	private async void OnSwitchList(string targetTeamId = "")
	{
		if (string.IsNullOrEmpty(_model.TeamId))
		{
			TryUpgradeProject(targetTeamId);
			return;
		}
		if (!MakeSureMoveProject(_model.originalProject.id, ""))
		{
			SelectTeamComBox.SetSelected(string.IsNullOrEmpty(_model.TeamId) ? "c1a7e08345e444dea187e21a692f0d7a" : _model.TeamId);
			return;
		}
		ErrorModel errorModel = await Communicator.DegradeTeamProject(_model.originalProject.id);
		if (errorModel == null)
		{
			_model.TeamId = null;
			Utils.Toast(Utils.GetString("DowngradeSuccessfulMessage"));
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == _model.id);
			if (projectModel != null)
			{
				projectModel.teamId = null;
				projectModel.groupId = null;
				projectModel.sortOrder = TeamDao.GetNewProjectInPersonal();
				projectModel.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
				await ProjectDao.TryUpdateProject(projectModel);
				SyncManager.Sync();
			}
			ListViewContainer.ReloadProjectData(refreshCount: false);
		}
		else
		{
			TeamModel teamModel = CacheManager.GetTeams().FirstOrDefault((TeamModel t) => t.id == _model.TeamId);
			switch (errorModel.errorCode)
			{
			case "no_team_project_permission":
				new CustomerDialog(Utils.GetString("TeamToPersonError"), Utils.GetString("NoProjectExist"), Utils.GetString("GotIt"), "").ShowDialog();
				break;
			case "degrade_team_project_failed":
				new CustomerDialog(Utils.GetString("TeamToPersonError"), Utils.GetString("TeamToPersonNoPermission"), Utils.GetString("GotIt"), "").ShowDialog();
				break;
			case "not_project_owner":
				new CustomerDialog(Utils.GetString("TeamToPersonError"), Utils.GetString("NotOwnerTeamToPerson"), Utils.GetString("GotIt"), "").ShowDialog();
				break;
			case "team_expired":
				if (new CustomerDialog(Utils.GetString("TeamToPersonError"), string.Format(Utils.GetString("TeamExpiredTeamToPerson"), teamModel?.name ?? ""), Utils.GetString("GotIt"), "").ShowDialog() == true)
				{
					string arg = await Communicator.GetSignOnToken();
					Utils.TryProcessStartUrl(string.Format(BaseUrl.GetApiDomain() + "/sign/autoSignOn?token={0}&dest={1}", arg, string.Format(Utils.GetString("RenewTeamUrl"), _model.TeamId)));
				}
				break;
			}
		}
		Close();
	}

	private async Task TryUpgradeProject(string projectId, string teamId)
	{
		if (!MakeSureMoveProject(projectId, teamId))
		{
			SelectTeamComBox.SetSelected(string.IsNullOrEmpty(_model.TeamId) ? "c1a7e08345e444dea187e21a692f0d7a" : _model.TeamId);
			return;
		}
		ErrorModel errorModel = await Communicator.UpgradeTeamProject(projectId, teamId);
		TeamModel team = CacheManager.GetTeams().FirstOrDefault((TeamModel t) => t.id == teamId);
		if (errorModel == null)
		{
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == _model.id);
			if (projectModel != null)
			{
				projectModel.teamId = teamId;
				projectModel.groupId = null;
				projectModel.sortOrder = TeamDao.GetNewProjectOrderInTeam(teamId);
				projectModel.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
				await ProjectDao.TryUpdateProject(projectModel);
				SyncManager.Sync();
			}
			Utils.Toast(string.Format(Utils.GetString("UpgradeSuccessfulMessage"), team?.name));
			ListViewContainer.ReloadProjectData(refreshCount: false);
		}
		else
		{
			switch (errorModel.errorCode)
			{
			case "no_team_project_permission":
				new CustomerDialog(string.Format(Utils.GetString("PersonToTeamError"), team?.name), Utils.GetString("NoProjectExist"), Utils.GetString("GotIt"), "").ShowDialog();
				break;
			case "team_project_exceed_quota":
				ProChecker.ShowUpgradeDialog(ProType.TeamShareLimit, this, teamId);
				break;
			case "not_project_owner":
				new CustomerDialog(string.Format(Utils.GetString("PersonToTeamError"), team?.name), string.Format(Utils.GetString("NotOwnerPersonToTeam"), team?.name), Utils.GetString("GotIt"), "").ShowDialog();
				break;
			case "no_admin_permission":
			case "upgrade_team_project_failed":
				new CustomerDialog(string.Format(Utils.GetString("PersonToTeamError"), team?.name), string.Format(Utils.GetString("SbNotTeamMember"), team?.name ?? ""), Utils.GetString("GotIt"), "").ShowDialog();
				break;
			case "team_expired":
			{
				TeamModel teamModel = CacheManager.GetTeams().FirstOrDefault((TeamModel t) => t.id == teamId);
				if (new CustomerDialog(string.Format(Utils.GetString("PersonToTeamError"), team?.name), string.Format(Utils.GetString("TeamExpiredPersonToTeam"), teamModel?.name ?? ""), Utils.GetString("GotIt"), "").ShowDialog() == true)
				{
					string arg = await Communicator.GetSignOnToken();
					Utils.TryProcessStartUrl(string.Format(BaseUrl.GetApiDomain() + "/sign/autoSignOn?token={0}&dest={1}", arg, string.Format(Utils.GetString("RenewTeamUrl"), teamId)));
				}
				break;
			}
			}
		}
		Close();
	}

	private bool MakeSureMoveProject(string projectId, string teamId)
	{
		ProjectModel projectById = CacheManager.GetProjectById(projectId);
		TeamModel team = CacheManager.GetTeam();
		if (projectById == null || team == null)
		{
			return false;
		}
		if (!LocalSettings.Settings.ExtraSettings.ShowMoveProjToTeam)
		{
			return true;
		}
		string title;
		string content;
		if (string.IsNullOrEmpty(teamId))
		{
			title = Utils.GetString("MoveToPerson");
			content = string.Format(Utils.GetString("MoveToPersonDesc"), projectById.name, team.name);
		}
		else
		{
			title = string.Format(Utils.GetString("MoveToTeam"), team.name);
			content = string.Format(Utils.GetString("MoveToTeamDesc"), projectById.name, team.name);
		}
		CustomerDialog customerDialog = new CustomerDialog(title, content, Utils.GetString("Move"), Utils.GetString("Cancel"), this);
		CheckBox checkBox = new CheckBox
		{
			Content = Utils.GetString("DoNotShowAgain"),
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		};
		checkBox.SetResourceReference(Control.ForegroundProperty, "BaseColorOpacity60");
		checkBox.SetResourceReference(FrameworkElement.StyleProperty, "SimpleCheckBoxStyle");
		customerDialog.SetExtraContent(checkBox);
		customerDialog.ShowDialog();
		bool valueOrDefault = customerDialog.DialogResult == true;
		if (valueOrDefault && checkBox.IsChecked == true)
		{
			LocalSettings.Settings.ExtraSettings.ShowMoveProjToTeam = false;
		}
		return valueOrDefault;
	}

	private async void OnSelectTypeClick(object sender, MouseButtonEventArgs e)
	{
		SelectAddProjectPopup.IsOpen = false;
		if (sender is Button { Tag: string tag })
		{
			TryUpgradeProject(tag);
		}
	}

	private async void TryUpgradeProject(string teamId)
	{
		if (!string.IsNullOrEmpty(_model.id))
		{
			await TryUpgradeProject(_model.originalProject.ProjectId, teamId);
		}
	}

	public void OnCancel()
	{
		if (!ProjectGroupComboBox.IsOpen && !ProjectTypeComboBox.IsOpen)
		{
			Close();
		}
	}

	public void Ok()
	{
		if (!ProjectGroupComboBox.IsOpen && !ProjectTypeComboBox.IsOpen && SaveButton.IsEnabled)
		{
			SaveProject();
		}
	}

	private void ShowEmojiSelector(object sender, MouseButtonEventArgs e)
	{
		EmojiSelector.GetItems();
		EmojiSelector.SetCanReset(!string.IsNullOrEmpty(EmojiText.Text));
		EmojiSelectPopup.IsOpen = true;
	}

	private void SetProjectEmoji(string emoji)
	{
		EmojiText.Text = emoji ?? string.Empty;
		ProjectPathGrid.Visibility = ((!string.IsNullOrEmpty(emoji)) ? Visibility.Collapsed : Visibility.Visible);
		AddListNameTextBox.MaxLength = 64 - EmojiText.Text.Length;
		DisplayName.Text = EmojiText.Text.Trim() + AddListNameTextBox.Text;
	}

	private void OnEmojiSelected(string emoji, bool closePopup)
	{
		SetProjectEmoji(emoji);
		CheckValid();
		if (closePopup)
		{
			EmojiSelectPopup.IsOpen = false;
		}
	}

	private void OnKindChanged(object sender, ComboBoxViewModel e)
	{
		if (e.Value is string text)
		{
			_model.Kind = text;
			SetProjectIcon(text);
			CheckTimeline(text);
		}
	}

	private void OnShowTypeSelected(object sender, ComboBoxViewModel e)
	{
		if (e.Value is ShowReminderType showTypeEnum)
		{
			_model.ShowTypeEnum = showTypeEnum;
		}
	}

	private void OnReminderTypeSelected(object sender, ComboBoxViewModel e)
	{
		if (e.Value is ShowReminderType reminderTypeEnum)
		{
			_model.ReminderTypeEnum = reminderTypeEnum;
		}
	}

	private void CheckTimeline(string kind)
	{
		bool flag = kind == Constants.ProjectKind.NOTE.ToString();
		TimelineBorder.Opacity = (flag ? 0.6 : 1.0);
		TimelineBorder.Cursor = (flag ? Cursors.No : Cursors.Hand);
		if (flag && _model.ViewMode == "timeline")
		{
			SetSelectedView("list");
		}
	}

	private void OnGroupSelected(object sender, ComboBoxViewModel e)
	{
		if (e.Value.ToString() == "AddProjectGroup")
		{
			AddProjectGroupDialog addProjectGroupDialog = new AddProjectGroupDialog(_model.id, _model.TeamId);
			addProjectGroupDialog.Owner = System.Windows.Window.GetWindow(this);
			addProjectGroupDialog.ProjectGroupEdit += OnProjectGroupEdit;
			addProjectGroupDialog.ShowDialog();
			Activate();
		}
		else
		{
			CheckExist();
		}
	}

	private void OnPrivacySelected(object sender, ComboBoxViewModel e)
	{
		bool value = e.Value.ToString() == "1";
		_model.openToTeam = value;
		SetProjectIcon(_model.Kind);
		MutedGrid.Visibility = ((!_model.IsShareList()) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void SwitchViewClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement frameworkElement && frameworkElement.Cursor != Cursors.No && frameworkElement.Tag is string selectedView)
		{
			SetSelectedView(selectedView);
		}
	}

	private void SetSelectedView(string mode)
	{
		if (!(mode == "timeline") || ProChecker.CheckPro(ProType.TimeLine, this))
		{
			ListBorder.SetResourceReference(Control.BackgroundProperty, (mode == "list") ? "PrimaryColor5" : "BaseColorOpacity4");
			KanbanBorder.SetResourceReference(Control.BackgroundProperty, (mode == "kanban") ? "PrimaryColor5" : "BaseColorOpacity4");
			TimelineBorder.SetResourceReference(Control.BackgroundProperty, (mode == "timeline") ? "PrimaryColor5" : "BaseColorOpacity4");
			ListBorder.BorderThickness = ((mode == "list") ? new Thickness(1.0) : new Thickness(0.0));
			KanbanBorder.BorderThickness = ((mode == "kanban") ? new Thickness(1.0) : new Thickness(0.0));
			TimelineBorder.BorderThickness = ((mode == "timeline") ? new Thickness(1.0) : new Thickness(0.0));
			ListPath.SetResourceReference(Shape.FillProperty, (mode == "list") ? "PrimaryColor" : "BaseColorOpacity40");
			KanbanPath.SetResourceReference(Shape.FillProperty, (mode == "kanban") ? "PrimaryColor" : "BaseColorOpacity40");
			TimelinePath.SetResourceReference(Shape.FillProperty, (mode == "timeline") ? "PrimaryColor" : "BaseColorOpacity40");
			_model.ViewMode = mode;
			string text = (ThemeUtil.IsDark() ? "dark" : "light");
			switch (mode)
			{
			case "list":
				ViewModeDisplayBorder.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Theme/" + text + "/ListPreview.png"));
				break;
			case "kanban":
				ViewModeDisplayBorder.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Theme/" + text + "/KanbanPreview.png"));
				break;
			case "timeline":
				ViewModeDisplayBorder.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Theme/" + text + "/TimelinePreview.png"));
				break;
			}
		}
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (!InputGrid.IsMouseOver && !EmojiSelectPopup.IsOpen)
		{
			FocusManager.SetFocusedElement(this, this);
			Keyboard.Focus(this);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/editprojectdialog.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Window = (EditProjectDialog)target;
			Window.Loaded += OnLoad;
			Window.MouseLeftButtonUp += OnMouseUp;
			break;
		case 2:
			WTitle = (TextBlock)target;
			break;
		case 3:
			InputGrid = (Grid)target;
			break;
		case 4:
			AddListNameTextBox = (EmojiEditor)target;
			break;
		case 5:
			EmojiSelectGrid = (Grid)target;
			EmojiSelectGrid.MouseLeftButtonUp += ShowEmojiSelector;
			break;
		case 6:
			ProjectPathGrid = (Grid)target;
			break;
		case 7:
			ProjectPath = (Path)target;
			break;
		case 8:
			SetEmojiIcon = (Path)target;
			break;
		case 9:
			EmojiText = (EmjTextBlock)target;
			break;
		case 10:
			EmojiSelectPopup = (EscPopup)target;
			break;
		case 11:
			EmojiSelector = (EmojiSelectControl)target;
			break;
		case 12:
			HideTextBox = (TextBox)target;
			break;
		case 13:
			AddListNameRepeatTextBlock = (TextBlock)target;
			break;
		case 14:
			ColorItems = (ColorSelector)target;
			break;
		case 15:
			TypeSelectPanel = (Grid)target;
			break;
		case 16:
			ListBorder = (Border)target;
			ListBorder.MouseLeftButtonUp += SwitchViewClick;
			break;
		case 17:
			ListPath = (Path)target;
			break;
		case 18:
			KanbanBorder = (Border)target;
			KanbanBorder.MouseLeftButtonUp += SwitchViewClick;
			break;
		case 19:
			KanbanPath = (Path)target;
			break;
		case 20:
			TimelineBorder = (Border)target;
			TimelineBorder.MouseLeftButtonUp += SwitchViewClick;
			break;
		case 21:
			TimelinePath = (Path)target;
			break;
		case 22:
			HeadProImage = (Image)target;
			break;
		case 23:
			ProjectGroupComboBox = (CustomComboBox)target;
			break;
		case 24:
			TypeText = (TextBlock)target;
			break;
		case 25:
			ProjectTypeComboBox = (CustomComboBox)target;
			break;
		case 26:
			ProjectBelongTo = (Grid)target;
			break;
		case 27:
			SelectTeamComBox = (CustomComboBox)target;
			break;
		case 28:
			SelectAddProjectPopup = (EscPopup)target;
			break;
		case 29:
			ProjectTypeItems = (ItemsControl)target;
			break;
		case 31:
			PrivacyComboBox = (CustomComboBox)target;
			break;
		case 32:
			AddListIsHideCheckBox = (CheckBox)target;
			break;
		case 33:
			ProjectShowTypeComboBox = (CustomComboBox)target;
			break;
		case 34:
			MutedGrid = (Grid)target;
			break;
		case 35:
			MutedCheckBox = (CheckBox)target;
			break;
		case 36:
			ProjectRemindTypeComboBox = (CustomComboBox)target;
			break;
		case 37:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 38:
			((Button)target).Click += OnCancelClick;
			break;
		case 39:
			BackBorder = (Border)target;
			break;
		case 40:
			ViewModeDisplayBorder = (Image)target;
			break;
		case 41:
			DisplayName = (EmjTextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 30)
		{
			((Button)target).PreviewMouseLeftButtonUp += OnSelectTypeClick;
		}
	}
}
