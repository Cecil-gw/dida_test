using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Team;

namespace ticktick_WPF.Views.Project;

public class ShareProjectDialog : Window, IComponentConnector, IStyleConnector
{
	private readonly string _projectId = "";

	private readonly ObservableCollection<ShareUserViewMode> _shareUserList = new ObservableCollection<ShareUserViewMode>();

	private readonly List<InviteUserModel> _teamMemberModels = new List<InviteUserModel>();

	private List<string> _invitedUserCodeList = new List<string>();

	private bool _isTeam;

	private bool _needSync;

	private List<string> _notifications;

	private ShareUserModel _owner;

	private ProjectModel _project;

	private List<string> _projectNotifications;

	private ObservableCollection<ShareContactsModel> _shareContactsList = new ObservableCollection<ShareContactsModel>();

	private string _teamId;

	private TeamModel _team;

	private bool _canInputDelete;

	private readonly SemaphoreSlim _inviteSelectionLock = new SemaphoreSlim(1, 1);

	private int _inviteOperationVersion;

	private bool _isInviteDialogClosed;

	private int _inviteInputVersion;

	private string _lastInvalidPhoneToast;

	private bool _selectAfterContactsLoad;

	private Task _shareContactsLoadTask = Task.CompletedTask;

	private readonly Dictionary<string, ShareInviteTarget> _pendingInviteTargets = new Dictionary<string, ShareInviteTarget>();

	internal Grid EmailInviteGrid;

	internal CustomComboBox userContactsComboBox;

	internal Border InputBorder;

	internal System.Windows.Controls.TextBox addUserTextBox;

	internal HoverIconButton InviteImage;

	internal EscPopup InvitePopup;

	internal ProjectPermissionSetControl EmailJoinPermission;

	internal ScrollViewer SelectedUsersViewer;

	internal ItemsControl SelectedUsers;

	internal System.Windows.Controls.ProgressBar addUserProgressBar;

	internal StackPanel ApplyEditGrid;

	internal TextBlock PermissionNotEditText;

	internal TextBlock ApplyEditText;

	internal Grid BodyGrid;

	internal TextBlock NotifyButton;

	internal ScrollViewer MemberList;

	internal ItemsControl shareUserListView;

	internal Grid MemberEmpty;

	internal EscPopup MessagePopup;

	internal TeamNotification NotificationSetting;

	internal Grid PersonalPage;

	internal Grid LinkShareGrid;

	internal RowDefinition LinkFirstRow;

	internal System.Windows.Controls.CheckBox ShareLinkCheckBox;

	internal TextBlock EnableLinkText;

	internal ProjectPermissionSetControl LinkJoinPermission;

	internal TextBlock CopyLink;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	internal Grid ButtonGrid;

	internal System.Windows.Controls.Button SaveButton;

	private bool _contentLoaded;

	public static async void TryShowShareDialog(string projectId, Window owner = null)
	{
		ProjectModel projectById = CacheManager.GetProjectById(projectId);
		if (projectById != null && !string.IsNullOrEmpty(projectById.teamId) && !projectById.IsShareList() && !(await TeamService.CheckFreeTeamShareCount(projectById.teamId)))
		{
			return;
		}
		UserInfoModel userInfoModel = await UserManager.GetUserInfo(force: true);
		if (userInfoModel != null)
		{
			if (userInfoModel.verifiedEmail || userInfoModel.fakedEmail)
			{
				ShareProjectDialog shareProjectDialog = new ShareProjectDialog(projectId);
				shareProjectDialog.Owner = owner ?? App.Window;
				shareProjectDialog.ShowDialog();
			}
			else if (new CustomerDialog(Utils.GetString("VerifiedEmail"), Utils.GetString("SendVerifyEmailHint"), Utils.GetString("SendVerifyEmail"), Utils.GetString("Cancel")).ShowDialog() == true)
			{
				await Communicator.ResentVerifyEmail();
			}
		}
	}

	public ShareProjectDialog(string projectId)
	{
		InitializeComponent();
		addUserTextBox.Tag = Utils.GetString(Utils.IsDida() ? "EnterEmailPhoneOrName" : "InviteContent");
		_projectId = projectId;
		SetProject();
		shareUserListView.ItemsSource = _shareUserList;
		InitTeam();
		InitLoad();
		base.InputBindings.Add(new KeyBinding(new RelayCommand(delegate(object o)
		{
			((ShareProjectDialog)o).OnEsc();
		}), new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
	}

	private void OnEsc()
	{
		InvalidateInviteOperations();
		if (userContactsComboBox.ListPopup.IsOpen)
		{
			userContactsComboBox.ListPopup.IsOpen = false;
		}
		else if (InvitePopup.IsOpen)
		{
			InvitePopup.IsOpen = false;
		}
		else if (SelectedUsersViewer.IsVisible)
		{
			(SelectedUsers.ItemsSource as ObservableCollection<InviteSelectViewModel>)?.Clear();
			SetInviteView();
		}
		else
		{
			Close();
		}
	}

	private async void InitLoad()
	{
		_shareContactsLoadTask = InitData();
		InitLink();
		LoadLocalUsers();
		userContactsComboBox.ListView.ItemTemplate = (DataTemplate)FindResource("UserComboboxItemTemplate");
		userContactsComboBox.ListPopup.StaysOpen = true;
		userContactsComboBox.ListPopup.NeedFocus = false;
		SelectedUsers.ItemsSource = new ObservableCollection<InviteSelectViewModel>();
		await LoadRemoteUsers();
	}

	protected override void OnContentRendered(EventArgs e)
	{
		base.OnContentRendered(e);
		if (EmailInviteGrid.IsVisible && addUserTextBox.IsVisible && !SelectedUsersViewer.IsVisible)
		{
			FocusManager.SetFocusedElement(this, addUserTextBox);
			Keyboard.Focus(addUserTextBox);
			addUserTextBox.CaretIndex = addUserTextBox.Text.Length;
		}
	}

	private async Task InitLink()
	{
		if (_project == null || _project.IsProjectPermit())
		{
			await GetLink(isInviteUrl: true);
		}
		else
		{
			SetNoPermission();
		}
	}

	private void SetNoPermission()
	{
		EmailInviteGrid.Visibility = Visibility.Collapsed;
		ApplyEditGrid.Visibility = Visibility.Visible;
		string arg = "";
		string permission = _project.permission;
		if (!(permission == "read"))
		{
			if (permission == "comment")
			{
				arg = Utils.GetString("CanComment");
			}
		}
		else
		{
			arg = Utils.GetString("ReadOnly");
		}
		PermissionNotEditText.Text = string.Format(Utils.GetString("PermissionIsNotEdit"), arg);
		LinkShareGrid.Visibility = Visibility.Collapsed;
	}

	private async Task InitData()
	{
		GetShareContacts();
		if (!_isTeam)
		{
			await InviteHelper.SetProjectUsers();
		}
		else
		{
			await InviteHelper.SetTeamContacts(_teamId);
			await InviteHelper.SetTeamMembers(_teamId, _projectId, this);
			GetTeamMember();
		}
		await InviteHelper.SetShareContacts();
		GetShareContacts();
		RefreshCurrentContactSuggestion();
		SetCheckBox();
		InitCheckEvent();
		ProjectPermissionSetControl linkJoinPermission = LinkJoinPermission;
		linkJoinPermission.PermissionSelect = (EventHandler<int>)Delegate.Combine(linkJoinPermission.PermissionSelect, new EventHandler<int>(SetLinkJoinPermission));
		ProjectPermissionSetControl linkJoinPermission2 = LinkJoinPermission;
		linkJoinPermission2.NeedAuditChange = (EventHandler<bool>)Delegate.Combine(linkJoinPermission2.NeedAuditChange, new EventHandler<bool>(OnNeedAuditChanged));
		LinkJoinPermission.SetShowNeedAuditPanel(_project.isOwner, _project.needAudit, _isTeam);
		InviteImage.Visibility = Visibility.Visible;
	}

	private void InitTeam()
	{
		_teamId = _project?.teamId;
		_team = CacheManager.GetTeamById(_teamId);
		_isTeam = _team != null;
		if (!string.IsNullOrEmpty(_teamId))
		{
			LinkFirstRow.Height = new GridLength(0.0);
			ShareLinkCheckBox.Visibility = Visibility.Collapsed;
		}
	}

	protected override async void OnClosed(EventArgs e)
	{
		_isInviteDialogClosed = true;
		InvalidateInviteOperations();
		if (_project != null && _project.notificationOptions?.Join(";") != _projectNotifications?.Join(";"))
		{
			_project.notificationOptions = _projectNotifications?.ToArray();
			_project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
			NotificationHelper.SaveNtfOptions(_projectId, _project.notificationOptions);
			await ProjectDao.TryUpdateProject(_project);
			SyncManager.Sync();
		}
		else if (_needSync)
		{
			SyncManager.Sync();
		}
		base.OnClosed(e);
	}

	private void SetProject()
	{
		_project = CacheManager.GetProjectById(_projectId);
		if (_project != null)
		{
			base.Title = base.Title + " \"" + _project.name + "\"";
			_projectNotifications = _project.notificationOptions?.ToList();
		}
	}

	protected override void OnDeactivated(EventArgs e)
	{
		userContactsComboBox.IsOpen = false;
		base.Topmost = false;
		base.OnDeactivated(e);
	}

	protected override async void OnActivated(EventArgs e)
	{
		base.Topmost = true;
		await Task.Delay(1);
		base.Topmost = false;
		base.OnActivated(e);
	}

	private void GetShareContacts()
	{
		List<ShareContactsModel> list = new List<ShareContactsModel>();
		List<ShareContactsModel> shareContacts = InviteHelper.GetShareContacts();
		if (shareContacts != null && shareContacts.Count != 0)
		{
			list = (from c in shareContacts
				where c != null && (!string.IsNullOrEmpty(c.userCode) || !string.IsNullOrEmpty(c.email) || !string.IsNullOrEmpty(Utils.NormalizeMainlandPhone(c.phone)))
				group c by string.IsNullOrEmpty(c.userCode) ? (string.IsNullOrEmpty(c.email) ? ("phone:" + Utils.NormalizeMainlandPhone(c.phone)) : ("email:" + c.email.ToLowerInvariant())) : ("user:" + c.userCode) into @group
				select @group.First()).ToList();
		}
		List<ProjectUsersMode> projectUsers = InviteHelper.GetProjectUsers();
		if (projectUsers != null && projectUsers.Count != 0)
		{
			foreach (ProjectUsersMode item in projectUsers)
			{
				foreach (ShareUserModel shareUser in item.shareUsers)
				{
					ShareContactsModel contact = new ShareContactsModel(shareUser);
					string contactPhone = Utils.NormalizeMainlandPhone(contact.phone);
					string contactEmail = ((!string.IsNullOrEmpty(contact.email) && string.IsNullOrEmpty(Utils.GetMaskedMainlandPhone(contact.email))) ? contact.email : null);
					if (!list.Any((ShareContactsModel c) => (!string.IsNullOrEmpty(contact.userCode) && c.userCode == contact.userCode) || (!string.IsNullOrEmpty(contactEmail) && string.IsNullOrEmpty(Utils.GetMaskedMainlandPhone(c.email)) && string.Equals(c.email, contactEmail, StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrEmpty(contactPhone) && Utils.NormalizeMainlandPhone(c.phone) == contactPhone)))
					{
						contact.canSearchByPhone = false;
						list.Add(contact);
					}
				}
			}
		}
		_shareContactsList = new ObservableCollection<ShareContactsModel>(list);
	}

	private async void GetTeamMember()
	{
		List<TeamMember> list = InviteHelper.GetTeamMembers(_teamId)?.ToList();
		_teamMemberModels.Clear();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (TeamMember item in list)
		{
			if (!item.isMyself)
			{
				_teamMemberModels.Add(new InviteUserModel(item));
			}
		}
	}

	private async Task GetLink(bool isInviteUrl = false, Constants.ProjectPermission permission = Constants.ProjectPermission.person)
	{
		try
		{
			string text = await Communicator.GetShareLink(_projectId, isInviteUrl, permission);
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			try
			{
				ShareLinkModel shareLinkModel = JsonConvert.DeserializeObject<ShareLinkModel>(text);
				if (shareLinkModel == null)
				{
					return;
				}
				if (!string.IsNullOrEmpty(shareLinkModel.inviteUrl))
				{
					string tag = BaseUrl.GetDomainUrl() + shareLinkModel.inviteUrl;
					if (Enum.TryParse<Constants.ProjectPermission>(shareLinkModel.permission, ignoreCase: true, out var result))
					{
						LinkJoinPermission.SwitchPermission((int)result);
					}
					ShareLinkCheckBox.IsChecked = true;
					CopyLink.Tag = tag;
				}
				else
				{
					ShareLinkCheckBox.IsChecked = false;
				}
			}
			catch (Exception)
			{
				string text2 = JObject.Parse(text)["errorCode"].ToString();
				if (!(text2 == "no_project_permission"))
				{
					if (!(text2 == "project_access_permission"))
					{
					}
					new CustomerDialog(Utils.GetString("OperateFailed"), Utils.GetString("OperateFailedMessage"), Utils.GetString("GotIt"), "").ShowDialog();
				}
				else
				{
					CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("EnableLinkShareFailedTitle"), Utils.GetString("EnableLinkShareFailedContent"), MessageBoxButton.OK);
					customerDialog.Owner = Window.GetWindow(this);
					customerDialog.ShowDialog();
					ShareLinkCheckBox.IsChecked = false;
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private void SetLinkJoinPermission(object sender, int e)
	{
		GetLink(isInviteUrl: false, (Constants.ProjectPermission)e);
	}

	private async Task LoadRemoteUsers(bool checkAll = false)
	{
		if (!_isInviteDialogClosed)
		{
			await AvatarHelper.ResetProjectAvatars(_projectId, force: true, checkAll);
			if (!_isInviteDialogClosed)
			{
				await LoadLocalUsers();
			}
		}
	}

	private async Task LoadLocalUsers()
	{
		ProjectModel project = _project;
		if (project == null || _isInviteDialogClosed)
		{
			return;
		}
		List<ShareUserViewMode> shareUserList = new List<ShareUserViewMode>();
		List<ShareUserModel> list = await AvatarHelper.GetProjectUsersAsync(_projectId, ownerTop: true, fetchRemote: false, checkUserList: false);
		if (_isInviteDialogClosed)
		{
			return;
		}
		bool noMembers = list?.All((ShareUserModel u) => u.isOwner || u.visitor) ?? false;
		if (list != null && list.Count != 0)
		{
			foreach (ShareUserModel item in list)
			{
				ShareUserViewMode shareUserViewMode = new ShareUserViewMode(item)
				{
					openToTeam = project.OpenToTeam,
					noMembers = noMembers,
					editable = project.IsProjectPermit(),
					userCount = project.userCount
				};
				if (item.userId?.ToString() == LocalSettings.Settings.LoginUserId)
				{
					shareUserViewMode.displayName = Utils.GetString("Me");
				}
				else if (item.deleted)
				{
					shareUserViewMode.displayName = Utils.GetString("AccountNotExist");
				}
				shareUserViewMode.ApplyPendingInviteTarget(GetPendingInviteTarget(item));
				shareUserList.Add(shareUserViewMode);
				if (item.userCode != null)
				{
					UserPublicProfilesDao.InsertUser(new UserPublicProfilesModel(item));
				}
			}
			_owner = list.FirstOrDefault((ShareUserModel user) => user.isOwner);
			_invitedUserCodeList = list.Select((ShareUserModel mode) => mode.userCode).ToList();
		}
		else
		{
			UserInfoModel userInfoModel = await UserManager.GetUserInfo();
			if (_isInviteDialogClosed)
			{
				return;
			}
			if (!long.TryParse(LocalSettings.Settings.LoginUserId, out var result))
			{
				result = -1L;
			}
			shareUserList.Add(new ShareUserViewMode
			{
				userId = result,
				avatarUrl = ((userInfoModel == null || string.IsNullOrEmpty(userInfoModel.picture) || userInfoModel.picture.Contains("avatar-new.png")) ? "../Assets/avatar-new.png" : userInfoModel.picture),
				username = (Utils.IsFakeEmail(userInfoModel?.username) ? string.Empty : userInfoModel?.username),
				displayName = Utils.GetString("Me"),
				isOwner = true,
				isProjectShare = true,
				isAccept = true,
				noMembers = true,
				userCode = userInfoModel?.userCode,
				acceptStatus = 1,
				userCount = _project.userCount
			});
			_owner = new ShareUserModel
			{
				userId = result,
				isOwner = true
			};
		}
		if (_team != null)
		{
			ImageSource teamLogo = TeamService.GetTeamLogo(_team);
			ShareUserViewMode shareUserViewMode2 = new ShareUserViewMode
			{
				userId = 0L,
				avatar = teamLogo,
				username = ShareUserViewMode.GetPermissionTextOfTeam(_project.teamMemberPermission, _team.name),
				permission = _project.teamMemberPermission,
				displayName = _team.name,
				openToTeam = _project.OpenToTeam,
				isTeam = true,
				editable = _project.IsProjectPermit(),
				isAccept = true
			};
			if (_shareUserList.Count > 0 && _shareUserList[0].isTeam)
			{
				_shareUserList[0].SetProperty(shareUserViewMode2);
			}
			else
			{
				_shareUserList.Insert(0, shareUserViewMode2);
			}
		}
		_shareUserList.Where((ShareUserViewMode m) => !m.isTeam).ToList().ForEach(delegate(ShareUserViewMode m)
		{
			_shareUserList.Remove(m);
		});
		shareUserList.ForEach(_shareUserList.Add);
		ShowOrHidePages();
	}

	private void ShowOrHidePages()
	{
		if (_shareUserList.Count > 1)
		{
			MemberList.Visibility = Visibility.Visible;
			MemberEmpty.Visibility = Visibility.Collapsed;
			PersonalPage.Visibility = Visibility.Collapsed;
			NotifyButton.Visibility = ((!_project.IsProjectPermit()) ? Visibility.Collapsed : Visibility.Visible);
		}
		else if (ShareLinkCheckBox.IsChecked == true && _shareUserList.Count == 1)
		{
			MemberList.Visibility = Visibility.Visible;
			MemberEmpty.Visibility = Visibility.Visible;
			PersonalPage.Visibility = Visibility.Collapsed;
			NotifyButton.Visibility = Visibility.Visible;
		}
		else
		{
			MemberList.Visibility = Visibility.Collapsed;
			MemberEmpty.Visibility = Visibility.Visible;
			PersonalPage.Visibility = Visibility.Visible;
			NotifyButton.Visibility = Visibility.Collapsed;
		}
	}

	private async void ShareLinkCheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		if (ShareLinkCheckBox.IsChecked == false)
		{
			await ShowShareLink();
		}
		else
		{
			HideShareLink();
		}
		ShowOrHidePages();
		e.Handled = true;
	}

	private async Task ShowShareLink()
	{
		await GetLink(isInviteUrl: false, Constants.ProjectPermission.write);
		EnableLinkText.Text = Utils.GetString("EnableLinkShare");
	}

	private async void HideShareLink()
	{
		if (new CustomerDialog(Utils.GetString("StopLinkShare"), Utils.GetString("StopLinkShareContent"), MessageBoxButton.OKCancel)
		{
			Owner = Window.GetWindow(this)
		}.ShowDialog() == true)
		{
			EnableLinkText.Text = Utils.GetString("DisableLinkShare");
			ShareLinkCheckBox.IsChecked = false;
			await Communicator.DeleteShareLink(_projectId);
		}
	}

	private void InviteUserOnEnter(System.Windows.Controls.TextBox input)
	{
		if (userContactsComboBox.IsOpen && (!(userContactsComboBox.ListView.ItemsSource is IEnumerable<ShareContactsViewModel> source) || source.Any((ShareContactsViewModel model) => model.IsEnable)))
		{
			userContactsComboBox.HandleEnter();
			userContactsComboBox.IsOpen = false;
			input.Text = "";
		}
	}

	private void addUserTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		System.Windows.Controls.TextBox textBox = sender as System.Windows.Controls.TextBox;
		switch (e.Key)
		{
		case Key.Up:
			userContactsComboBox.UpDownSelect(isUp: true);
			break;
		case Key.Down:
			userContactsComboBox.UpDownSelect(isUp: false);
			break;
		case Key.Return:
		{
			string text = textBox?.Text?.Trim();
			if (Utils.IsDida() && !string.IsNullOrEmpty(text) && Utils.IsMobilePhone(text) && _shareContactsLoadTask != null && !_shareContactsLoadTask.IsCompleted)
			{
				_selectAfterContactsLoad = true;
			}
			if (Utils.IsDida() && !string.IsNullOrEmpty(text) && text.All(char.IsDigit) && text.Length == 11 && !Utils.IsMobilePhone(text))
			{
				ShowInvalidPhoneToast(text);
			}
			InviteUserOnEnter(textBox);
			break;
		}
		}
	}

	private async void OnInviteInputTextChanged(object sender, TextChangedEventArgs e)
	{
		System.Windows.Controls.TextBox textBox = sender as System.Windows.Controls.TextBox;
		_lastInvalidPhoneToast = null;
		_selectAfterContactsLoad = false;
		if (!string.IsNullOrWhiteSpace(textBox?.Text))
		{
			int inputVersion = ++_inviteInputVersion;
			string input = textBox.Text.Trim();
			Task shareContactsLoadTask = _shareContactsLoadTask;
			if (shareContactsLoadTask != null && !shareContactsLoadTask.IsCompleted)
			{
				ShowManualInviteSuggestion(ShareInviteTarget.FromInvalidInviteCandidate(input), isInviteable: false);
				await shareContactsLoadTask;
				if (inputVersion != _inviteInputVersion || textBox.Text.Trim() != input)
				{
					return;
				}
			}
			if (_shareContactsList == null || _shareContactsList.Count == 0)
			{
				GetShareContacts();
			}
			List<ShareContactsModel> list = _shareContactsList?.Where((ShareContactsModel p) => ShareInviteTarget.IsContactSearchMatch(p, input)).ToList();
			if (list != null && list.Count != 0)
			{
				ShowContactSuggestions(list, input);
				return;
			}
			ShareInviteTarget shareInviteTarget = CreateManualInviteTarget(input);
			ShareInviteTarget target = ShareInviteTarget.FromInvalidInviteCandidate(input);
			if (shareInviteTarget != null)
			{
				ShowManualInviteSuggestion(shareInviteTarget, isInviteable: true);
			}
			else
			{
				ShowManualInviteSuggestion(target, isInviteable: false);
			}
		}
		else
		{
			_selectAfterContactsLoad = false;
			_inviteInputVersion++;
			userContactsComboBox.IsOpen = false;
		}
	}

	private static ShareInviteTarget CreateManualInviteTarget(string input)
	{
		if (Utils.IsValidEmail(input) && input != UserManager.GetUserEmail())
		{
			return ShareInviteTarget.FromManualEmail(input);
		}
		if (!Utils.IsDida() || !Utils.IsMobilePhone(input))
		{
			return null;
		}
		return ShareInviteTarget.FromManualPhone(input);
	}

	private void ShowInvalidPhoneToast(string input)
	{
		if (!(_lastInvalidPhoneToast == input))
		{
			_lastInvalidPhoneToast = input;
			Toast(Utils.GetString("InvalidPhoneFormat"));
		}
	}

	private void ShowManualInviteSuggestion(ShareInviteTarget target, bool isInviteable)
	{
		if (base.IsActive && target != null)
		{
			ObservableCollection<ShareContactsViewModel> items = new ObservableCollection<ShareContactsViewModel>
			{
				new ShareContactsViewModel(target, isInviteable)
				{
					HoverSelected = isInviteable
				}
			};
			userContactsComboBox.Init(items, null);
			userContactsComboBox.IsOpen = true;
		}
	}

	private void RefreshCurrentContactSuggestion()
	{
		string input = addUserTextBox?.Text?.Trim();
		if (string.IsNullOrEmpty(input))
		{
			return;
		}
		bool selectAfterContactsLoad = _selectAfterContactsLoad;
		_selectAfterContactsLoad = false;
		_inviteInputVersion++;
		List<ShareContactsModel> list = _shareContactsList?.Where((ShareContactsModel contact) => ShareInviteTarget.IsContactSearchMatch(contact, input)).ToList();
		if (list == null || list.Count == 0)
		{
			ShareInviteTarget shareInviteTarget = CreateManualInviteTarget(input);
			ShowManualInviteSuggestion(shareInviteTarget ?? ShareInviteTarget.FromInvalidInviteCandidate(input), shareInviteTarget != null);
			if (shareInviteTarget != null && selectAfterContactsLoad)
			{
				InviteUserOnEnter(addUserTextBox);
			}
		}
		else if (ShowContactSuggestions(list, input) && selectAfterContactsLoad)
		{
			InviteUserOnEnter(addUserTextBox);
		}
	}

	private bool ShowContactSuggestions(IEnumerable<ShareContactsModel> contacts, string input)
	{
		if (!base.IsActive)
		{
			userContactsComboBox.IsOpen = false;
			return false;
		}
		ObservableCollection<ShareContactsViewModel> observableCollection = new ObservableCollection<ShareContactsViewModel>();
		foreach (ShareContactsModel item in contacts ?? Enumerable.Empty<ShareContactsModel>())
		{
			if (item.email != LocalSettings.Settings.LoginUserName)
			{
				observableCollection.Add(new ShareContactsViewModel(item, Utils.IsDida() && ShareInviteTarget.IsRecentPhoneMatch(item, input)));
			}
		}
		if (observableCollection.Count == 0)
		{
			userContactsComboBox.IsOpen = false;
			return false;
		}
		observableCollection[0].HoverSelected = true;
		userContactsComboBox.Init(observableCollection, null);
		userContactsComboBox.IsOpen = true;
		return true;
	}

	private async Task<bool> InviteUser(string toUsername, int index)
	{
		Constants.ProjectPermission selectedPermission = EmailJoinPermission.SelectedPermission;
		ShareAddUserMode shareAddUserMode = await Communicator.AddShareUser(_projectId, toUsername, selectedPermission);
		if (shareAddUserMode?.toUsername != null && shareAddUserMode.toUsername != "")
		{
			if (!string.IsNullOrEmpty(shareAddUserMode.toUserCode))
			{
				UserPublicProfilesModel userPublicProfilesModel = await UserPublicProfilesDao.GetUserInfoByUserCode(shareAddUserMode.toUserCode);
				_shareUserList.Insert(index, new ShareUserViewMode(shareAddUserMode)
				{
					displayName = ((!string.IsNullOrEmpty(userPublicProfilesModel?.nickName)) ? userPublicProfilesModel.nickName : userPublicProfilesModel?.displayName),
					avatarUrl = ((userPublicProfilesModel == null) ? "../Assets/avatar-new.png" : ((string.IsNullOrEmpty(userPublicProfilesModel.avatarUrl) || userPublicProfilesModel.avatarUrl.Contains("avatar-new")) ? "../Assets/avatar-new.png" : userPublicProfilesModel.avatarUrl)),
					editable = true,
					siteId = (userPublicProfilesModel?.siteId).GetValueOrDefault()
				});
				_invitedUserCodeList.Add(shareAddUserMode.toUserCode);
			}
			return true;
		}
		if (shareAddUserMode != null)
		{
			_ = shareAddUserMode.fromUsername != "";
		}
		return false;
	}

	public async void TransferProjectOwner()
	{
		if ((await CalendarConnectService.GetConnectedProjectIds()).Item1.Contains(_projectId))
		{
			ProjectModel projectById = CacheManager.GetProjectById(_projectId);
			new CustomerDialog(string.Format(Utils.GetString("TransferlistA"), projectById?.name), Utils.GetString("TransferIntegratedListMessage"), Utils.GetString("GotIt"), Utils.GetString("Cancel"), this).ShowDialog();
			return;
		}
		bool? flag = new TransferProjectWindow(_projectId)
		{
			Owner = this
		}.ShowDialog();
		if (flag.HasValue && flag.Value)
		{
			_needSync = true;
			_project.isOwner = false;
			LinkJoinPermission.SetShowNeedAuditPanel(show: false);
			await LoadRemoteUsers();
		}
	}

	public async void DeleteUser(object sender, EventArgs e)
	{
		try
		{
			if (new CustomerDialog(Utils.GetString("RemoveShareUser"), string.Format(Utils.GetString("RemoveShareUserContent"), (sender as ShareUserViewMode)?.displayName), Utils.GetString("RemoveMember"), Utils.GetString("Cancel"), base.Owner).ShowDialog() != true)
			{
				return;
			}
			ShareUserViewMode temp = _shareUserList.FirstOrDefault((ShareUserViewMode p) => p.userCode == (sender as ShareUserViewMode)?.userCode);
			if (temp != null)
			{
				string text = await Communicator.DeleteShareUser(_projectId, temp.recordId);
				if (!string.IsNullOrEmpty(text))
				{
					try
					{
						_ = JObject.Parse(text)["errorCode"].ToString() == "project_access_permission";
						new CustomerDialog(Utils.GetString("OperateFailed"), Utils.GetString("OperateFailedMessage"), Utils.GetString("GotIt"), "").ShowDialog();
						return;
					}
					catch (Exception)
					{
					}
				}
				ForgetPendingInviteTarget(temp.recordId, temp.userCode);
				_shareUserList.Remove(temp);
				_invitedUserCodeList.Remove(temp.userCode);
				await TaskService.ClearAssignByProjectId(_projectId, temp.userId.ToString());
				if (_project != null)
				{
					_project.userCount--;
					if (_project.userCount < 1)
					{
						_project.userCount = 1;
					}
					if (_project.userCount == 1)
					{
						await TaskService.ClearAssignByProjectId(_projectId, LocalSettings.Settings["LoginUserId"].ToString());
					}
					AvatarHelper.RemoveProjectUser(_project.id, temp.userId);
				}
				await ProjectDao.TryUpdateProject(_project);
				SyncManager.Sync();
			}
			ShowOrHidePages();
		}
		catch (Exception)
		{
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void CopyShareLink(object sender, RoutedEventArgs e)
	{
		string text = CopyLink.Tag as string;
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				System.Windows.Forms.Clipboard.SetText(text);
				Utils.Toast(Utils.GetString("Copied"));
			}
			catch (Exception)
			{
			}
		}
	}

	private async void ShowInvitePopup(object sender, MouseButtonEventArgs e)
	{
		int operationVersion = _inviteOperationVersion;
		bool flag = !IsInviteOperationCurrent(operationVersion);
		if (!flag)
		{
			bool flag2 = !_isTeam;
			if (flag2)
			{
				flag2 = !(await CheckShareLimit(1, operationVersion));
			}
			flag = flag2;
		}
		if (!flag && IsInviteOperationCurrent(operationVersion))
		{
			if (_isTeam)
			{
				GetTeamMember();
			}
			InviteUsersDialog inviteUsersDialog = (_isTeam ? new InviteUsersDialog(InviteHelper.GetRecentInvite(), _teamMemberModels, _invitedUserCodeList) : new InviteUsersDialog(InviteHelper.GetRecentInvite(), InviteHelper.GetProjectUsers(), _invitedUserCodeList));
			inviteUsersDialog.OnCancel += delegate
			{
				InvitePopup.IsOpen = false;
			};
			inviteUsersDialog.OnInvited += OnInviteUsersSelect;
			InvitePopup.Child = inviteUsersDialog;
			InvitePopup.IsOpen = true;
		}
	}

	private async Task<bool> CheckShareLimit(int inviteNum, int operationVersion)
	{
		if (!IsInviteOperationCurrent(operationVersion))
		{
			return false;
		}
		if (_team != null)
		{
			return CheckTeamLimit(inviteNum);
		}
		if (_team == null && !string.IsNullOrEmpty(_project.teamId))
		{
			return true;
		}
		if (inviteNum == 0)
		{
			return true;
		}
		int? num = await Communicator.CheckShareProjectQuota(_projectId);
		if (!IsInviteOperationCurrent(operationVersion))
		{
			return false;
		}
		if (!num.HasValue)
		{
			Utils.Toast(Utils.GetString("OperateFailedMessage"));
			return false;
		}
		if (num.Value < inviteNum)
		{
			string content = null;
			long limitNumber = 0L;
			if (_owner?.userId.ToString() == LocalSettings.Settings.LoginUserId)
			{
				if (UserDao.IsPro())
				{
					limitNumber = Utils.GetUserLimit(Constants.LimitKind.ShareUserNumber);
					content = Utils.GetString("ProShareListLimit");
				}
				else
				{
					ProChecker.ShowUpgradeDialog(_isTeam ? ProType.MoreTeamGuest : ProType.MoreSharingMembers, null, (!_isTeam) ? null : _team?.id);
				}
			}
			else if (_shareUserList.Count + num.Value <= 2)
			{
				await RemindOwnerToUpgrade(operationVersion);
			}
			else
			{
				content = Utils.GetString("MaximumReached");
			}
			if (content != null)
			{
				CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), string.Format(content, limitNumber), MessageBoxButton.OK);
				customerDialog.Owner = Window.GetWindow(this);
				customerDialog.ShowDialog();
			}
			return false;
		}
		return true;
	}

	private async Task RemindOwnerToUpgrade(int operationVersion)
	{
		if (IsInviteOperationCurrent(operationVersion) && new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("ShareListNotOwnerFreeLimit"), _owner?.displayName), MessageBoxButton.OKCancel)
		{
			Owner = Window.GetWindow(this)
		}.ShowDialog() == true && IsInviteOperationCurrent(operationVersion))
		{
			await Communicator.ReminderToPay(_projectId);
			if (IsInviteOperationCurrent(operationVersion))
			{
				Utils.Toast(Utils.GetString("AlreadyReminded"));
			}
		}
	}

	private bool CheckTeamLimit(int inviteNum)
	{
		long num = (_team.IsPro() ? LimitCache.GetProLimitByKey(Constants.LimitKind.VisitorNumber) : LimitCache.GetFreeLimitByKey(Constants.LimitKind.VisitorNumber));
		int num2 = _shareUserList.Count((ShareUserViewMode u) => u.visitor);
		if (num < num2 + inviteNum)
		{
			ShowTeamGuestDialog(num);
			return false;
		}
		return true;
	}

	private async void ShowTeamGuestDialog(long limit)
	{
		string text = "";
		if (_team.IsPro())
		{
			text = string.Format(Utils.GetString("ProLimitedSharingGuests"), limit);
		}
		else
		{
			ProChecker.ShowUpgradeDialog(ProType.MoreTeamGuest, null, _team?.id);
		}
		if (!string.IsNullOrEmpty(text))
		{
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), text, MessageBoxButton.OK);
			customerDialog.Owner = Window.GetWindow(this);
			customerDialog.ShowDialog();
		}
	}

	private async void ShowGuestLimitDialog(long limit)
	{
		string content = string.Format(Utils.GetString((limit <= 1) ? "FreeLimitedSharingGuests" : "ProLimitedSharingGuests"), limit);
		CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), content, MessageBoxButton.OK);
		customerDialog.Owner = Window.GetWindow(this);
		customerDialog.ShowDialog();
	}

	private void OnInviteUsersSelect(object sender, List<ShareInviteTarget> targets)
	{
		OnSelectedUserChanged(targets, isAdd: true);
		InvitePopup.IsOpen = false;
	}

	private async Task InviteUsers(List<ShareInviteTarget> targets, int operationVersion)
	{
		if (!IsInviteOperationCurrent(operationVersion))
		{
			return;
		}
		targets = NormalizeInviteTargets(targets);
		if (targets.Count == 0)
		{
			(SelectedUsers.ItemsSource as ObservableCollection<InviteSelectViewModel>)?.Clear();
			SetInviteView();
		}
		else
		{
			if (targets.Count <= 0)
			{
				return;
			}
			InvitePopup.IsOpen = false;
			int inviteNum = CountQuotaInvites(targets);
			if (await CheckShareLimit(inviteNum, operationVersion) && IsInviteOperationCurrent(operationVersion))
			{
				await BatchInviteUsersAsync(targets, operationVersion);
				if (!IsInviteOperationCurrent(operationVersion))
				{
					return;
				}
				ShowOrHidePages();
			}
			if (IsInviteOperationCurrent(operationVersion))
			{
				InvitePopup.IsOpen = false;
				AvatarHelper.ResetPullTime(_projectId);
			}
		}
	}

	private async Task BatchInviteUsersAsync(List<ShareInviteTarget> targets, int operationVersion)
	{
		if (!IsInviteOperationCurrent(operationVersion))
		{
			return;
		}
		targets = NormalizeInviteTargets(targets);
		if (targets.Count == 0)
		{
			(SelectedUsers.ItemsSource as ObservableCollection<InviteSelectViewModel>)?.Clear();
			SetInviteView();
			return;
		}
		ShareAddUsersMode shareAddUsersMode = await Communicator.AddShareUsers(_projectId, targets, EmailJoinPermission.SelectedPermission);
		AvatarHelper.ResetPullTime(_projectId);
		if (IsInviteOperationCurrent(operationVersion) && shareAddUsersMode?.errors != null && shareAddUsersMode.errors.Count > 0)
		{
			if (targets.Count == 1)
			{
				if (!HandleSinglePhoneInviteError(shareAddUsersMode.errors[0], targets[0]))
				{
					switch (shareAddUsersMode.errors[0].errorCode)
					{
					case "team_project_exceed_quota":
						ProChecker.ShowUpgradeDialog(ProType.TeamShareLimit, this, _project.teamId);
						break;
					case "unknown_exception":
						new CustomerDialog(Utils.GetString("AddFailed"), Utils.GetString("UnknownException"), Utils.GetString("OK"), "", base.Owner).ShowDialog();
						break;
					case "share_exist":
					case "illegal_share":
						new CustomerDialog(Utils.GetString("AddFailed"), Utils.GetString("ShareExist"), Utils.GetString("OK"), "", base.Owner).ShowDialog();
						break;
					case "invalidate_email":
						new CustomerDialog(Utils.GetString("AddFailed"), Utils.GetString("InvalidateEmail"), Utils.GetString("OK"), "", base.Owner).ShowDialog();
						break;
					case "visitor_exceed_quota":
					{
						long result = 1L;
						if (shareAddUsersMode.errors[0].data != null && shareAddUsersMode.errors[0].data.TryGetValue("limit", out JToken value))
						{
							long.TryParse(value.ToString(), out result);
						}
						if (_team != null)
						{
							ShowTeamGuestDialog(result);
						}
						else
						{
							ShowGuestLimitDialog(result);
						}
						break;
					}
					case "exceed_max_share_limit":
						if (_owner?.userId == int.Parse(LocalSettings.Settings["LoginUserId"].ToString()))
						{
							ProChecker.ShowUpgradeDialog(ProType.MoreSharingMembers);
						}
						else if (_shareUserList.Count + targets.Count > 2)
						{
							CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("ProShareListLimit"), "29"), MessageBoxButton.OK);
							customerDialog.Owner = Window.GetWindow(this);
							customerDialog.ShowDialog();
						}
						else
						{
							await RemindOwnerToUpgrade(operationVersion);
						}
						break;
					case "exceed_pro_max_share_limit":
					{
						long userLimit = Utils.GetUserLimit(Constants.LimitKind.ShareUserNumber);
						string format = Utils.GetString("ProShareListLimit");
						new CustomerDialog(Utils.GetString("LimitTips"), string.Format(format, userLimit), Utils.GetString("OK"), "", base.Owner).ShowDialog();
						break;
					}
					case "user_email_not_verified":
					{
						UserInfoModel userInfoModel = await UserManager.GetUserInfo();
						if (userInfoModel != null && IsInviteOperationCurrent(operationVersion))
						{
							new CustomerDialog(Utils.GetString("VerifiedEmail"), string.Format(Utils.GetString("ShareEmailNotVerified"), string.IsNullOrWhiteSpace(userInfoModel.email) ? userInfoModel.username : userInfoModel.email), Utils.GetString("OK"), "", base.Owner).ShowDialog();
						}
						break;
					}
					case "project_access_permission":
						if (_team != null)
						{
							goto case "no_team_permission";
						}
						goto default;
					case "no_team_permission":
						new CustomerDialog(Utils.GetString("OperateFailed"), Utils.GetString("CannotInvitePeopleOutsideTeam"), Utils.GetString("OK"), "", base.Owner).ShowDialog();
						break;
					case "exceed_team_max_share_limit":
					{
						long num = Utils.GetUserLimit(Constants.LimitKind.ShareUserNumber) + 1;
						new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("ProShareListLimit"), num), Utils.GetString("OK"), "", base.Owner).ShowDialog();
						break;
					}
					default:
						new CustomerDialog(Utils.GetString("OperateFailed"), Utils.GetString("OperateFailedMessage"), Utils.GetString("GotIt"), "").ShowDialog();
						break;
					}
				}
			}
			else
			{
				HandleBatchInviteErrors(targets, shareAddUsersMode);
			}
		}
		if (shareAddUsersMode?.records != null && shareAddUsersMode.records.Count > 0)
		{
			if (_isInviteDialogClosed)
			{
				SyncManager.Sync();
				return;
			}
			await RememberPendingInviteTargetsAsync(shareAddUsersMode.records, shareAddUsersMode.errors, targets);
			if (_isInviteDialogClosed)
			{
				SyncManager.Sync();
				return;
			}
			await LoadRemoteUsers();
		}
		if (IsInviteOperationCurrent(operationVersion))
		{
			(SelectedUsers.ItemsSource as ObservableCollection<InviteSelectViewModel>)?.Clear();
			SetInviteView();
		}
	}

	private bool HandleSinglePhoneInviteError(ShareUsersError error, ShareInviteTarget target)
	{
		if (error == null || target == null || !target.IsPhone)
		{
			return false;
		}
		switch (error.errorCode)
		{
		case "unsupported_phone_num":
			Toast(Utils.GetString("InvalidPhoneFormat"));
			return true;
		case "user_not_exist":
			InviteHelper.CleanupMissingPhone(target.ToPhone);
			GetShareContacts();
			new CustomerDialog(Utils.GetString("OperationFailed"), Utils.GetString("InvitePhoneNotRegistered"), Utils.GetString("GotIt"), "", base.Owner).ShowDialog();
			return true;
		case "exceed_quota":
		case "exceed_share_freq":
			Toast(Utils.GetString("SendTooManyInvitationsToday"));
			return true;
		case "share_exist":
		case "illegal_share":
			Toast(Utils.GetString("InvitePhoneInSharedList"));
			return true;
		default:
			return false;
		}
	}

	private void HandleBatchInviteErrors(List<ShareInviteTarget> targets, ShareAddUsersMode result)
	{
		List<ShareUsersError> source = result.errors ?? new List<ShareUsersError>();
		bool flag = source.Any((ShareUsersError error) => error.errorCode == "exceed_quota" || error.errorCode == "exceed_share_freq");
		if (flag)
		{
			Toast(Utils.GetString("SendTooManyInvitationsToday"));
		}
		List<ShareUsersError> list = source.Where((ShareUsersError error) => error.errorCode != "exceed_quota" && error.errorCode != "exceed_share_freq").ToList();
		if (list.Count == 0)
		{
			return;
		}
		List<ShareUsersError> list2 = list.Where((ShareUsersError error) => FindInviteTarget(error, targets)?.IsPhone ?? false).ToList();
		List<ShareUsersError> list3 = list2.Where((ShareUsersError error) => error.errorCode == "user_not_exist").ToList();
		foreach (ShareUsersError item in list3)
		{
			InviteHelper.CleanupMissingPhone(FindInviteTarget(item, targets)?.ToPhone);
		}
		if (list3.Count > 0)
		{
			GetShareContacts();
		}
		List<ShareUsersError> list4 = list2.Where((ShareUsersError error) => error.errorCode == "user_not_exist" || error.errorCode == "unsupported_phone_num").ToList();
		if (list.Count != list2.Count)
		{
			ShowGenericBatchInviteError(targets, result);
			return;
		}
		bool hasOtherFailures = list2.Count != list4.Count || flag;
		SharePhoneInviteOutcome sharePhoneInviteOutcome = SharePhoneInviteErrorHelper.Classify(list4, result.records?.Count ?? 0, hasOtherFailures);
		string title;
		string content;
		switch (sharePhoneInviteOutcome)
		{
		case SharePhoneInviteOutcome.None:
			ShowGenericBatchInviteError(targets, result);
			return;
		case SharePhoneInviteOutcome.Partial:
			title = Utils.GetString("InvitesSend");
			content = string.Format(Utils.GetString("YouChooseOnlySomeCanReceive"), targets.Count, result.records?.Count ?? 0);
			break;
		case SharePhoneInviteOutcome.AllUnregistered:
			title = Utils.GetString("OperationFailed");
			content = Utils.GetString("AllInvitePhoneNotRegistered");
			break;
		case SharePhoneInviteOutcome.AllInvalid:
			title = Utils.GetString("OperationFailed");
			content = Utils.GetString("AllInvitePhoneInvalid");
			break;
		default:
			title = Utils.GetString("OperationFailed");
			content = Utils.GetString("AllInvitePhoneUnableReceive");
			break;
		}
		CustomerDialog customerDialog = new CustomerDialog(title, content, Utils.GetString("GotIt"), "", base.Owner);
		if (sharePhoneInviteOutcome == SharePhoneInviteOutcome.Partial || sharePhoneInviteOutcome == SharePhoneInviteOutcome.AllUnable)
		{
			List<string> list5 = (from error in list4
				where error.errorCode == "user_not_exist"
				select Utils.GetMaskedMainlandPhone(FirstNotWhiteSpace(FindInviteTarget(error, targets)?.ToPhone, error.toUsername)) into phone
				where !string.IsNullOrEmpty(phone)
				select phone).Distinct().ToList();
			if (list5.Count > 0)
			{
				string separator = (Utils.IsCn() ? "、" : ", ");
				customerDialog.SetEmphasizeText(string.Format(Utils.GetString("ThesePhoneNotRegistered"), string.Join(separator, list5)));
			}
		}
		customerDialog.ShowDialog();
	}

	private void ShowGenericBatchInviteError(ICollection<ShareInviteTarget> targets, ShareAddUsersMode result)
	{
		CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("InvitesSend"), string.Format(Utils.GetString("InviteUsersError"), targets?.Count ?? 0, result.records?.Count ?? 0), Utils.GetString("GotIt"), "", base.Owner);
		if (_team != null)
		{
			customerDialog.SetEmphasizeText(Utils.GetString("CannotInvitePeopleOutsideTeam"));
		}
		customerDialog.ShowDialog();
	}

	private static ShareInviteTarget FindInviteTarget(ShareUsersError error, IEnumerable<ShareInviteTarget> targets)
	{
		if (error == null)
		{
			return null;
		}
		return FindUniqueInviteTarget(error.toUsername, targets);
	}

	private static ShareInviteTarget FindUniqueInviteTarget(string recipient, IEnumerable<ShareInviteTarget> targets)
	{
		List<ShareInviteTarget> list = targets?.Where((ShareInviteTarget target) => InviteTargetMatches(target, recipient)).ToList() ?? new List<ShareInviteTarget>();
		if (list.Count != 1)
		{
			return null;
		}
		return list[0];
	}

	private static bool InviteTargetMatches(ShareInviteTarget target, string recipient)
	{
		if (target == null || string.IsNullOrWhiteSpace(recipient))
		{
			return false;
		}
		if (target.IsPhone)
		{
			string text = Utils.NormalizeMainlandPhone(recipient);
			if (!string.IsNullOrEmpty(text))
			{
				return text == Utils.NormalizeMainlandPhone(target.ToPhone);
			}
			string maskedMainlandPhone = Utils.GetMaskedMainlandPhone(recipient);
			if (!string.IsNullOrEmpty(maskedMainlandPhone))
			{
				return maskedMainlandPhone == Utils.GetMaskedMainlandPhone(target.ToPhone);
			}
			return false;
		}
		return string.Equals(target.ToUsername, recipient, StringComparison.OrdinalIgnoreCase);
	}

	private async Task RememberPendingInviteTargetsAsync(IEnumerable<ShareAddUserMode> records, IEnumerable<ShareUsersError> errors, IEnumerable<ShareInviteTarget> targets)
	{
		List<ShareInviteTarget> remainingTargets = (targets ?? Enumerable.Empty<ShareInviteTarget>()).ToList();
		foreach (ShareUsersError item in errors ?? Enumerable.Empty<ShareUsersError>())
		{
			ShareInviteTarget shareInviteTarget = FindInviteTarget(item, remainingTargets);
			if (shareInviteTarget != null)
			{
				remainingTargets.Remove(shareInviteTarget);
			}
		}
		List<ShareAddUserMode> recordList = (records ?? Enumerable.Empty<ShareAddUserMode>()).Where((ShareAddUserMode shareAddUserMode) => shareAddUserMode != null).ToList();
		Dictionary<ShareAddUserMode, ShareInviteTarget> resolvedTargets = new Dictionary<ShareAddUserMode, ShareInviteTarget>();
		foreach (ShareAddUserMode record in recordList)
		{
			ForgetPendingInviteTarget(record.recordId, record.toUserCode);
			List<ShareInviteTarget> list = remainingTargets.Where((ShareInviteTarget item) => !string.IsNullOrEmpty(record.toUserCode) && item.UserCode == record.toUserCode).ToList();
			ShareInviteTarget shareInviteTarget2 = ((list.Count == 1) ? list[0] : FindUniqueInviteTarget(record.toUsername, remainingTargets));
			if (shareInviteTarget2 != null)
			{
				resolvedTargets[record] = shareInviteTarget2;
				remainingTargets.Remove(shareInviteTarget2);
			}
		}
		await ResolvePendingInviteTargetsFromContactsAsync(recordList, remainingTargets, resolvedTargets);
		List<ShareAddUserMode> list2 = recordList.Where((ShareAddUserMode key2) => !resolvedTargets.ContainsKey(key2)).ToList();
		if (list2.Count == 1 && remainingTargets.Count == 1)
		{
			resolvedTargets[list2[0]] = remainingTargets[0];
		}
		foreach (KeyValuePair<ShareAddUserMode, ShareInviteTarget> item2 in resolvedTargets)
		{
			ShareAddUserMode key = item2.Key;
			ShareInviteTarget value = item2.Value;
			if (value.IsPhone)
			{
				if (!string.IsNullOrEmpty(key.recordId))
				{
					_pendingInviteTargets["record:" + key.recordId] = value;
				}
				else if (!string.IsNullOrEmpty(key.toUserCode))
				{
					_pendingInviteTargets["user:" + key.toUserCode] = value;
				}
			}
		}
	}

	private static async Task ResolvePendingInviteTargetsFromContactsAsync(IEnumerable<ShareAddUserMode> records, ICollection<ShareInviteTarget> remainingTargets, IDictionary<ShareAddUserMode, ShareInviteTarget> resolvedTargets)
	{
		List<ShareAddUserMode> unresolvedRecords = records.Where((ShareAddUserMode key) => !resolvedTargets.ContainsKey(key)).ToList();
		if (!Utils.IsDida() || unresolvedRecords.Count == 0 || remainingTargets.All((ShareInviteTarget target) => target == null || !target.IsPhone))
		{
			return;
		}
		List<ShareContactsModel> list;
		try
		{
			list = await Communicator.GetShareContactsList();
		}
		catch (Exception ex)
		{
			UtilLog.Warn("Resolve pending phone invite targets failed: " + ex.Message);
			return;
		}
		if (list == null || list.Count == 0)
		{
			return;
		}
		foreach (ShareAddUserMode record in unresolvedRecords)
		{
			List<string> contactPhones = (from contact in list
				where contact != null && ((!string.IsNullOrWhiteSpace(record.toUserCode) && string.Equals(contact.userCode, record.toUserCode, StringComparison.Ordinal)) || (record.toUserId.HasValue && !string.IsNullOrWhiteSpace(contact.toUserId) && string.Equals(contact.toUserId, record.toUserId.Value.ToString(), StringComparison.Ordinal)))
				select Utils.NormalizeMainlandPhone(contact.phone) into phone
				where !string.IsNullOrEmpty(phone)
				select phone).Distinct().ToList();
			if (contactPhones.Count != 0)
			{
				List<ShareInviteTarget> list2 = remainingTargets.Where((ShareInviteTarget target) => target != null && target.IsPhone && contactPhones.Contains(Utils.NormalizeMainlandPhone(target.ToPhone))).ToList();
				if (list2.Count == 1)
				{
					ShareInviteTarget shareInviteTarget = list2[0];
					resolvedTargets[record] = shareInviteTarget;
					remainingTargets.Remove(shareInviteTarget);
				}
			}
		}
	}

	private void ForgetPendingInviteTarget(string recordId, string userCode)
	{
		if (!string.IsNullOrEmpty(recordId))
		{
			_pendingInviteTargets.Remove("record:" + recordId);
		}
		if (!string.IsNullOrEmpty(userCode))
		{
			_pendingInviteTargets.Remove("user:" + userCode);
		}
	}

	private ShareInviteTarget GetPendingInviteTarget(ShareUserModel user)
	{
		if (user == null)
		{
			return null;
		}
		if (!string.IsNullOrEmpty(user.recordId) && _pendingInviteTargets.TryGetValue("record:" + user.recordId, out var value))
		{
			return value;
		}
		if (!string.IsNullOrEmpty(user.userCode) && _pendingInviteTargets.TryGetValue("user:" + user.userCode, out var value2))
		{
			return value2;
		}
		return null;
	}

	private void InitCheckEvent()
	{
		NotificationSetting.DoCheck += OnDoCheck;
		NotificationSetting.CreateCheck += OnCreateCheck;
		NotificationSetting.DelCheck += OnDelCheck;
	}

	private void OnDelCheck(object sender, bool Checked)
	{
		if (_notifications != null)
		{
			_notifications.Remove("DELETE");
			_notifications.Remove("MOVE_OUT");
			if (Checked)
			{
				_notifications.Add("DELETE");
				_notifications.Add("MOVE_OUT");
			}
			else
			{
				_notifications.Remove("DELETE");
				_notifications.Remove("MOVE_OUT");
			}
			_projectNotifications = _notifications;
		}
	}

	private void OnCreateCheck(object sender, bool Checked)
	{
		if (_notifications != null)
		{
			_notifications.Remove("CREATE");
			if (Checked)
			{
				_notifications.Add("CREATE");
			}
			else
			{
				_notifications.Remove("CREATE");
			}
			_projectNotifications = _notifications;
		}
	}

	private void OnDoCheck(object sender, bool Checked)
	{
		if (_notifications != null)
		{
			_notifications.Remove("DONE");
			_notifications.Remove("UNDONE");
			if (Checked)
			{
				_notifications.Add("DONE");
				_notifications.Add("UNDONE");
			}
			else
			{
				_notifications.Remove("DONE");
				_notifications.Remove("UNDONE");
			}
			_projectNotifications = _notifications;
		}
	}

	private void SetCheckBox()
	{
		string notificationOptions = LocalSettings.Settings.NotificationOptions;
		_notifications = (_project?.notificationOptions ?? (notificationOptions ?? "").Split(';')).ToList();
		if (_notifications != null)
		{
			_notifications.Remove("");
			NotificationSetting.DoOrUndo.IsChecked = _notifications.Contains("DONE") || _notifications.Contains("UNDONE");
			NotificationSetting.Create.IsChecked = _notifications.Contains("CREATE");
			NotificationSetting.Delete.IsChecked = _notifications.Contains("DELETE") || _notifications.Contains("MOVE_OUT");
		}
	}

	public async void ExitProject(object sender, EventArgs e)
	{
		(List<string>, List<string>) obj = await CalendarConnectService.GetConnectedProjectIds();
		var (list, _) = obj;
		if (obj.Item2.Contains(_projectId))
		{
			CalendarConnectInfo calendarConnectInfo = await CalendarConnectService.GetConnectInfoByDefaultId(_projectId);
			if (calendarConnectInfo == null)
			{
				return;
			}
			if (!Utils.IsNetworkAvailable())
			{
				Utils.Toast(Utils.GetString("NetworkException"));
				return;
			}
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("CannotDeleteDefault"), string.Format(Utils.GetString("DeleteConnectDefaultProjectMessage"), calendarConnectInfo?.account), Utils.GetString("Modify"), Utils.GetString("Cancel"), Window.GetWindow(this));
			customerDialog.ShowDialog();
			if (customerDialog.DialogResult == true)
			{
				CalendarContentProject calendarContentProject = calendarConnectInfo.relProjects?.FirstOrDefault((CalendarContentProject f) => f.syncProjectId == _projectId);
				if (calendarContentProject != null)
				{
					LinkGoogleCalendarWindow linkGoogleCalendarWindow = new LinkGoogleCalendarWindow(calendarConnectInfo, calendarContentProject.Copy());
					linkGoogleCalendarWindow.Owner = Window.GetWindow(this);
					linkGoogleCalendarWindow.ShowDialog();
				}
			}
			return;
		}
		bool disConnectShow = false;
		if (list.Contains(_projectId))
		{
			if (!Utils.IsNetworkAvailable())
			{
				Utils.Toast(Utils.GetString("NetworkException"));
				return;
			}
			List<CalendarConnectInfo> list2 = await CalendarConnectService.GetConnectInfosByProjectId(_projectId);
			if (list2 != null && list2.Count > 0)
			{
				CustomerDialog customerDialog2 = new CustomerDialog(string.Format(Utils.GetString("ExitProjectTitle"), _project.name), string.Format(Utils.GetString("ExitConnectProjectMessage"), list2[0].account), Utils.GetString("OK"), Utils.GetString("Cancel"), Window.GetWindow(this));
				customerDialog2.ShowDialog();
				if (!customerDialog2.DialogResult.HasValue || !customerDialog2.DialogResult.Value)
				{
					return;
				}
				disConnectShow = true;
				if (list2 != null && list2.Count > 0)
				{
					foreach (CalendarConnectInfo item in list2)
					{
						CalendarContentProject calendarContentProject2 = item.relProjects.FirstOrDefault((CalendarContentProject r) => r.syncProjectScope == _project.id);
						bool flag = calendarContentProject2 != null;
						if (flag)
						{
							flag = !(await Communicator.UnbindProjectCalendar(item.id, calendarContentProject2.id));
						}
						if (flag)
						{
							Utils.Toast("NetworkException");
							return;
						}
					}
					CalendarService.PullCalendarConnects();
				}
			}
		}
		if ((disConnectShow || new CustomerDialog(string.Format(Utils.GetString("ExitProjectTitle"), _project.name), string.Format(Utils.GetString("ExitProjectSummary"), _project.name), Utils.GetString("Delete"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog() == true) && await ProjectDao.DeleteProjectById(_project?.id))
		{
			if (TaskStickyWindow.TryCloseInProject(_project?.id))
			{
				Utils.Toast(Utils.GetString("StickyClosed"));
			}
			await TaskService.DeleteAllTasksInProject(_project?.id);
			ListViewContainer.ReloadProjectData();
			SyncManager.Sync();
			Close();
		}
	}

	public async void SetUserPermission(ShareUserViewMode mode, int e)
	{
		if (mode.isTeam)
		{
			ProjectModel project = _project;
			Constants.ProjectPermission projectPermission = (Constants.ProjectPermission)e;
			project.teamMemberPermission = projectPermission.ToString();
			mode.permission = _project.teamMemberPermission;
			mode.SetTeamPermissionText();
			await UploadProject();
			LoadRemoteUsers();
			return;
		}
		string text = await Communicator.SetPermission(_projectId, (Constants.ProjectPermission)e, mode.recordId);
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		try
		{
			_ = JObject.Parse(text)["errorCode"].ToString() == "project_access_permission";
			new CustomerDialog(Utils.GetString("OperateFailed"), Utils.GetString("OperateFailedMessage"), Utils.GetString("GotIt"), "").ShowDialog();
		}
		catch (Exception)
		{
		}
	}

	private async Task UploadProject()
	{
		ProjectModel projectById = CacheManager.GetProjectById(_project.id);
		projectById.teamMemberPermission = _project.teamMemberPermission;
		projectById.openToTeam = _project.openToTeam;
		projectById.needAudit = _project.needAudit;
		projectById.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
		await ProjectDao.TryUpdateProject(projectById);
		SyncManager.Sync();
	}

	private async void ApplyEdit(object sender, RoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.Button button)
		{
			button.Click -= ApplyEdit;
			button.Cursor = System.Windows.Input.Cursors.Arrow;
			ApplyEditText.Foreground = ThemeUtil.GetColor("BaseColorOpacity60");
			ApplyEditText.MouseEnter -= ShowUnderLine;
			ApplyEditText.TextDecorations = null;
			await Communicator.ApplyPermission(_projectId, Constants.ProjectPermission.write);
			Utils.Toast(Utils.GetString("ApplySent"));
		}
	}

	private void ShowUnderLine(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (sender is TextBlock textBlock)
		{
			textBlock.TextDecorations = TextDecorations.Underline;
		}
	}

	private void HideUnderLine(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (sender is TextBlock textBlock)
		{
			textBlock.TextDecorations = null;
		}
	}

	private async void OnLinkBoxClick(object sender, RoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.TextBox box)
		{
			await Task.Delay(50);
			box.SelectAll();
		}
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void OnInviteClick(object sender, RoutedEventArgs e)
	{
		int operationVersion = _inviteOperationVersion;
		await _inviteSelectionLock.WaitAsync();
		try
		{
			if (IsInviteOperationCurrent(operationVersion) && SelectedUsers.ItemsSource is ObservableCollection<InviteSelectViewModel> source)
			{
				List<ShareInviteTarget> list = (from m in source
					where m.Target != null
					select m.Target).ToList();
				if (list.Count > 0)
				{
					await InviteUsers(list, operationVersion);
				}
			}
		}
		finally
		{
			_inviteSelectionLock.Release();
		}
	}

	private void ReturnClick(object sender, RoutedEventArgs e)
	{
		InvalidateInviteOperations();
		(SelectedUsers.ItemsSource as ObservableCollection<InviteSelectViewModel>)?.Clear();
		SetInviteView();
	}

	private void InvalidateInviteOperations()
	{
		_inviteOperationVersion++;
		_inviteInputVersion++;
		_selectAfterContactsLoad = false;
	}

	private bool IsInviteOperationCurrent(int operationVersion)
	{
		if (!_isInviteDialogClosed)
		{
			return operationVersion == _inviteOperationVersion;
		}
		return false;
	}

	private void OnInputKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		_canInputDelete = false;
		if (e.Key == Key.Back && sender is System.Windows.Controls.TextBox { CaretIndex: 0 } textBox && string.IsNullOrEmpty(textBox.Text))
		{
			_canInputDelete = true;
		}
		else
		{
			if (!(SelectedUsers.ItemsSource is ObservableCollection<InviteSelectViewModel> observableCollection))
			{
				return;
			}
			foreach (InviteSelectViewModel item in observableCollection)
			{
				if (item.PreDelete)
				{
					item.PreDelete = false;
				}
			}
		}
	}

	private void OnInputKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Back && _canInputDelete)
		{
			if (SelectedUsers.ItemsSource is ObservableCollection<InviteSelectViewModel> { Count: >=2 } observableCollection)
			{
				InviteSelectViewModel inviteSelectViewModel = observableCollection[observableCollection.Count - 2];
				if (inviteSelectViewModel.PreDelete)
				{
					OnSelectedUserChanged(new List<ShareInviteTarget> { inviteSelectViewModel.Target }, isAdd: false);
				}
				else
				{
					inviteSelectViewModel.PreDelete = true;
				}
			}
		}
		else
		{
			addUserTextBox_KeyUp(sender, e);
		}
	}

	private void OnDeleteSelectClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is Border { DataContext: InviteSelectViewModel dataContext })
		{
			OnSelectedUserChanged(new List<ShareInviteTarget> { dataContext.Target }, isAdd: false);
			e.Handled = true;
		}
	}

	private async void OnSelectedUserChanged(List<ShareInviteTarget> targets, bool isAdd)
	{
		int operationVersion = _inviteOperationVersion;
		await _inviteSelectionLock.WaitAsync();
		try
		{
			if (!IsInviteOperationCurrent(operationVersion))
			{
				return;
			}
			IEnumerable itemsSource = SelectedUsers.ItemsSource;
			if (itemsSource is ObservableCollection<InviteSelectViewModel> selected)
			{
				List<ShareInviteTarget> list = NormalizeInviteTargets(from inviteSelectViewModel in selected
					where !inviteSelectViewModel.IsInput
					select inviteSelectViewModel.Target, null, excludeInvited: false);
				if (isAdd)
				{
					targets = NormalizeInviteTargets(targets, list);
					if (targets.Count == 0)
					{
						SetInviteView();
						return;
					}
					int num = CountQuotaInvites(list);
					int num2 = CountQuotaInvites(targets);
					if (!(await CheckShareLimit(num2 + num, operationVersion)) || !IsInviteOperationCurrent(operationVersion))
					{
						return;
					}
				}
				if (isAdd)
				{
					if (selected.Count == 0)
					{
						selected.Add(new InviteSelectViewModel
						{
							IsInput = true
						});
					}
					foreach (ShareInviteTarget target in targets.Where((ShareInviteTarget t) => t != null))
					{
						if (selected.All((InviteSelectViewModel m) => !AreSameInviteTarget(m.Target, target)))
						{
							selected.Insert(selected.Count - 1, new InviteSelectViewModel
							{
								Target = target,
								DisplayName = (string.IsNullOrEmpty(target.DisplayName) ? target.RequestValue : target.DisplayName)
							});
						}
					}
				}
				else
				{
					foreach (ShareInviteTarget target2 in targets.Where((ShareInviteTarget t) => t != null))
					{
						InviteSelectViewModel item = selected.FirstOrDefault((InviteSelectViewModel m) => AreSameInviteTarget(m.Target, target2));
						selected.Remove(item);
					}
					if (selected.Count == 1 && selected[0].IsInput)
					{
						selected.Clear();
					}
				}
			}
			SetInviteView();
		}
		finally
		{
			_inviteSelectionLock.Release();
		}
	}

	private void SetInviteView()
	{
		ScrollViewer selectedUsersViewer = SelectedUsersViewer;
		ObservableCollection<InviteSelectViewModel> obj = SelectedUsers.ItemsSource as ObservableCollection<InviteSelectViewModel>;
		selectedUsersViewer.Visibility = ((obj == null || obj.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
		if (!SelectedUsersViewer.IsVisible)
		{
			addUserTextBox.Visibility = Visibility.Visible;
			BodyGrid.Visibility = Visibility.Visible;
			addUserTextBox.Focus();
			LinkShareGrid.Visibility = Visibility.Visible;
		}
		else
		{
			addUserTextBox.Visibility = Visibility.Collapsed;
			BodyGrid.Visibility = Visibility.Collapsed;
			LinkShareGrid.Visibility = Visibility.Collapsed;
		}
	}

	private void OnInputLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.TextBox textBox)
		{
			textBox.Focus();
		}
	}

	private void OnUserComboBoxItemSelect(object sender, ComboBoxViewModel e)
	{
		if (!(e is ShareContactsViewModel { IsEnable: not false, Target: var target } shareContactsViewModel))
		{
			return;
		}
		bool flag = CheckUsernameInvited(target);
		bool flag2 = CheckPhoneInvited(target);
		if (!flag2 && !flag)
		{
			if (target != null && !shareContactsViewModel.IsNewAdd && !string.IsNullOrWhiteSpace(shareContactsViewModel.displayName))
			{
				target.DisplayName = shareContactsViewModel.displayName;
			}
			OnSelectedUserChanged(new List<ShareInviteTarget> { target }, isAdd: true);
		}
		else
		{
			bool flag3 = flag2 && ((target != null && target.IsPhone) || !flag);
			Toast(Utils.GetString(flag3 ? "InvitePhoneInSharedList" : "ShareUserExist"));
		}
		addUserTextBox.Text = string.Empty;
		System.Windows.Controls.TextBox textBox = Utils.FindSingleVisualChildren<System.Windows.Controls.TextBox>(SelectedUsersViewer);
		if (textBox != null)
		{
			textBox.Text = string.Empty;
		}
	}

	private bool CheckInvited(ShareInviteTarget target)
	{
		if (!CheckUsernameInvited(target))
		{
			return CheckPhoneInvited(target);
		}
		return true;
	}

	private bool CheckUsernameInvited(ShareInviteTarget target)
	{
		if (target == null)
		{
			return false;
		}
		return _shareUserList.Any((ShareUserViewMode member) => (!string.IsNullOrWhiteSpace(target.UserCode) && string.Equals(member.userCode, target.UserCode, StringComparison.Ordinal)) || (!string.IsNullOrWhiteSpace(target.ToUsername) && (string.Equals(member.username, target.ToUsername, StringComparison.OrdinalIgnoreCase) || string.Equals(member.inviteIdentifier, target.ToUsername, StringComparison.OrdinalIgnoreCase))));
	}

	private bool CheckPhoneInvited(ShareInviteTarget target)
	{
		if (!Utils.IsDida() || target == null)
		{
			return false;
		}
		string maskedPhone = Utils.GetMaskedMainlandPhone(GetTargetPhone(target));
		if (string.IsNullOrEmpty(maskedPhone))
		{
			return false;
		}
		return _shareUserList.Any((ShareUserViewMode m) => Utils.GetMaskedMainlandPhone(m.phone) == maskedPhone || Utils.GetMaskedMainlandPhone(m.username) == maskedPhone);
	}

	private List<ShareInviteTarget> NormalizeInviteTargets(IEnumerable<ShareInviteTarget> targets, IEnumerable<ShareInviteTarget> existingTargets = null, bool excludeInvited = true)
	{
		List<ShareInviteTarget> list = new List<ShareInviteTarget>();
		List<ShareInviteTarget> source = existingTargets?.Where((ShareInviteTarget shareInviteTarget) => shareInviteTarget != null).ToList() ?? new List<ShareInviteTarget>();
		foreach (ShareInviteTarget target in targets?.Where((ShareInviteTarget shareInviteTarget) => shareInviteTarget != null) ?? Enumerable.Empty<ShareInviteTarget>())
		{
			if ((!excludeInvited || !CheckInvited(target)) && !source.Any((ShareInviteTarget item) => AreSameInviteTarget(item, target)) && !list.Any((ShareInviteTarget item) => AreSameInviteTarget(item, target)))
			{
				list.Add(target);
			}
		}
		return list;
	}

	private int CountQuotaInvites(IEnumerable<ShareInviteTarget> targets)
	{
		return targets?.Count((ShareInviteTarget target) => _team == null || _teamMemberModels.All((InviteUserModel member) => (string.IsNullOrWhiteSpace(target?.UserCode) || !string.Equals(member.UserCode, target.UserCode, StringComparison.Ordinal)) && (string.IsNullOrWhiteSpace(target?.ToUsername) || !string.Equals(member.Email, target.ToUsername, StringComparison.OrdinalIgnoreCase)))) ?? 0;
	}

	private static bool AreSameInviteTarget(ShareInviteTarget left, ShareInviteTarget right)
	{
		if (left == null || right == null)
		{
			return false;
		}
		if (string.Equals(left.IdentityKey, right.IdentityKey, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (!string.IsNullOrWhiteSpace(left.UserCode) && string.Equals(left.UserCode, right.UserCode, StringComparison.Ordinal))
		{
			return true;
		}
		if (!string.IsNullOrWhiteSpace(left.ToUsername) && !string.IsNullOrWhiteSpace(right.ToUsername) && string.Equals(left.ToUsername, right.ToUsername, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (!Utils.IsDida())
		{
			return false;
		}
		string text = Utils.NormalizeMainlandPhone(GetTargetPhone(left));
		string b = Utils.NormalizeMainlandPhone(GetTargetPhone(right));
		if (!string.IsNullOrEmpty(text))
		{
			return string.Equals(text, b, StringComparison.Ordinal);
		}
		return false;
	}

	private static string GetTargetPhone(ShareInviteTarget target)
	{
		return FirstNotWhiteSpace(target?.ToPhone, target?.Phone);
	}

	private static string FirstNotWhiteSpace(params string[] values)
	{
		return values?.FirstOrDefault((string value) => !string.IsNullOrWhiteSpace(value));
	}

	public async void OpenToTeamChanged(bool isChecked)
	{
		(string, bool) obj = await Communicator.SetProjectOpenToTeam(_project.id, isChecked);
		var (text, _) = obj;
		if (obj.Item2)
		{
			_project.openToTeam = isChecked;
			ProjectModel cacheProject = CacheManager.GetProjectById(_project.id);
			if (cacheProject != null)
			{
				cacheProject.openToTeam = isChecked;
			}
			await LoadRemoteUsers(isChecked);
			_project.userCount = cacheProject?.userCount ?? _project.userCount;
			await ProjectDao.TryUpdateProject(_project);
			ListViewContainer.ReloadProjectData(refreshCount: false);
			if (string.IsNullOrEmpty(_project.teamMemberPermission))
			{
				_project.teamMemberPermission = "write";
				await UploadProject();
				return;
			}
			if (!isChecked)
			{
				ShareUserViewMode shareUserViewMode = _shareUserList.FirstOrDefault((ShareUserViewMode m) => m.userId == Utils.GetCurrentUserIdInt());
				if (shareUserViewMode != null && !shareUserViewMode.isOwner && shareUserViewMode.permission != "write")
				{
					_project.permission = shareUserViewMode.permission;
					SetNoPermission();
				}
			}
			SyncManager.Sync();
			return;
		}
		try
		{
			if (JsonConvert.DeserializeObject<ErrorModel>(text).errorCode == "team_project_exceed_quota")
			{
				ProChecker.ShowUpgradeDialog(ProType.TeamShareLimit, this, _project.teamId);
			}
		}
		catch
		{
		}
		UtilLog.Info("SetProjectOpenToTeamFail " + text);
	}

	private async void OnNeedAuditChanged(object sender, bool e)
	{
		_project.needAudit = e;
		UserActCollectUtils.AddClickEvent("collaborate", "approval_by_owner_is_required", e ? "enable" : "disable");
		await UploadProject();
	}

	private void OnNotifyClick(object sender, MouseButtonEventArgs e)
	{
		MessagePopup.IsOpen = true;
	}

	private void OnTextBoxFocus(object sender, RoutedEventArgs e)
	{
		InputBorder.SetResourceReference(Border.BorderBrushProperty, "PrimaryColor");
	}

	private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
	{
		InputBorder.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");
	}

	private void OnScrollMouseUp(object sender, MouseButtonEventArgs e)
	{
		Utils.FindSingleVisualChildren<System.Windows.Controls.TextBox>(SelectedUsersViewer)?.Focus();
	}

	private void OnInputBorderSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (userContactsComboBox.IsOpen)
		{
			userContactsComboBox.IsOpen = false;
			userContactsComboBox.IsOpen = true;
		}
	}

	public void Toast(string toastText)
	{
		ToastBorder.Visibility = Visibility.Visible;
		ToastText.Text = toastText;
		Storyboard storyboard = (Storyboard)FindResource("ShowToast");
		ToastBorder.BeginStoryboard(storyboard);
	}

	private void OnToasted(object sender, EventArgs e)
	{
		ToastBorder.Visibility = Visibility.Collapsed;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/project/shareprojectdialog.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			((Storyboard)target).Completed += OnToasted;
			break;
		case 4:
			EmailInviteGrid = (Grid)target;
			break;
		case 5:
			userContactsComboBox = (CustomComboBox)target;
			break;
		case 6:
			InputBorder = (Border)target;
			InputBorder.SizeChanged += OnInputBorderSizeChanged;
			break;
		case 7:
			addUserTextBox = (System.Windows.Controls.TextBox)target;
			addUserTextBox.GotFocus += OnTextBoxFocus;
			addUserTextBox.LostFocus += OnTextBoxLostFocus;
			addUserTextBox.KeyUp += addUserTextBox_KeyUp;
			addUserTextBox.TextChanged += OnInviteInputTextChanged;
			break;
		case 8:
			InviteImage = (HoverIconButton)target;
			break;
		case 9:
			InvitePopup = (EscPopup)target;
			break;
		case 10:
			EmailJoinPermission = (ProjectPermissionSetControl)target;
			break;
		case 11:
			SelectedUsersViewer = (ScrollViewer)target;
			SelectedUsersViewer.MouseLeftButtonUp += OnScrollMouseUp;
			break;
		case 12:
			SelectedUsers = (ItemsControl)target;
			break;
		case 13:
			addUserProgressBar = (System.Windows.Controls.ProgressBar)target;
			break;
		case 14:
			ApplyEditGrid = (StackPanel)target;
			break;
		case 15:
			PermissionNotEditText = (TextBlock)target;
			break;
		case 16:
			((System.Windows.Controls.Button)target).Click += ApplyEdit;
			break;
		case 17:
			ApplyEditText = (TextBlock)target;
			ApplyEditText.MouseEnter += ShowUnderLine;
			ApplyEditText.MouseLeave += HideUnderLine;
			break;
		case 18:
			BodyGrid = (Grid)target;
			break;
		case 19:
			NotifyButton = (TextBlock)target;
			NotifyButton.MouseLeftButtonUp += OnNotifyClick;
			break;
		case 20:
			MemberList = (ScrollViewer)target;
			break;
		case 21:
			shareUserListView = (ItemsControl)target;
			break;
		case 22:
			MemberEmpty = (Grid)target;
			break;
		case 23:
			MessagePopup = (EscPopup)target;
			break;
		case 24:
			NotificationSetting = (TeamNotification)target;
			break;
		case 25:
			PersonalPage = (Grid)target;
			break;
		case 26:
			LinkShareGrid = (Grid)target;
			break;
		case 27:
			LinkFirstRow = (RowDefinition)target;
			break;
		case 28:
			ShareLinkCheckBox = (System.Windows.Controls.CheckBox)target;
			ShareLinkCheckBox.PreviewMouseLeftButtonDown += ShareLinkCheckBoxClick;
			break;
		case 29:
			EnableLinkText = (TextBlock)target;
			break;
		case 30:
			LinkJoinPermission = (ProjectPermissionSetControl)target;
			break;
		case 31:
			CopyLink = (TextBlock)target;
			CopyLink.PreviewMouseLeftButtonUp += CopyShareLink;
			break;
		case 32:
			ToastBorder = (Border)target;
			break;
		case 33:
			ToastText = (TextBlock)target;
			break;
		case 34:
			ButtonGrid = (Grid)target;
			break;
		case 35:
			((System.Windows.Controls.Button)target).Click += ReturnClick;
			break;
		case 36:
			SaveButton = (System.Windows.Controls.Button)target;
			SaveButton.Click += OnInviteClick;
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
		switch (connectionId)
		{
		case 2:
			((Border)target).MouseLeftButtonUp += OnDeleteSelectClick;
			break;
		case 3:
			((System.Windows.Controls.TextBox)target).GotFocus += OnTextBoxFocus;
			((System.Windows.Controls.TextBox)target).LostFocus += OnTextBoxLostFocus;
			((System.Windows.Controls.TextBox)target).PreviewKeyDown += OnInputKeyDown;
			((System.Windows.Controls.TextBox)target).Loaded += OnInputLoaded;
			((System.Windows.Controls.TextBox)target).KeyUp += OnInputKeyUp;
			((System.Windows.Controls.TextBox)target).TextChanged += OnInviteInputTextChanged;
			break;
		}
	}
}
