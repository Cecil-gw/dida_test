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
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Team;

public class InviteUsersDialog : UserControl, IComponentConnector
{
	private readonly bool _isTeam;

	private readonly ObservableCollection<InviteGroupModel> _projectUsersDatas = new ObservableCollection<InviteGroupModel>();

	private readonly List<InviteUserModel> _recentInviteModels;

	internal AnimateGroupTitle Titles;

	internal ItemsControl RecentInvite;

	internal ItemsControl SharedProject;

	internal ItemsControl TeamMembers;

	internal StackPanel EmptyPage;

	internal TextBlock EmptyText1;

	internal TextBlock EmptyText2;

	internal Button InviteButton;

	private bool _contentLoaded;

	public event EventHandler OnCancel;

	public event EventHandler OnSwitch;

	public event EventHandler<List<ShareInviteTarget>> OnInvited;

	public InviteUsersDialog(List<InviteUserModel> recentInviteModels, List<ProjectUsersMode> projectUsersDatas, ICollection<string> existUsers)
	{
		_isTeam = false;
		InitializeComponent();
		base.Height = (Utils.IsCn() ? 316 : 326);
		if (recentInviteModels != null)
		{
			recentInviteModels = recentInviteModels.Where((InviteUserModel model) => !existUsers.Contains(model.UserCode)).ToList();
			_recentInviteModels = recentInviteModels.ToList();
			_recentInviteModels.ForEach(delegate(InviteUserModel i)
			{
				i.CanDelete = true;
			});
			if (recentInviteModels.Count() > 10)
			{
				recentInviteModels.RemoveRange(10, recentInviteModels.Count() - 10);
			}
			RecentInvite.ItemsSource = recentInviteModels;
		}
		SetSharedProjectSource(projectUsersDatas, existUsers);
		EmptyPage.Visibility = ((RecentInvite.Items.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		Titles.ItemMaxWidth = 110.0;
	}

	private async void SetSharedProjectSource(List<ProjectUsersMode> projectUsersDatas, ICollection<string> existUsers)
	{
		if (projectUsersDatas == null)
		{
			return;
		}
		foreach (ProjectUsersMode projectUsersData in projectUsersDatas)
		{
			InviteGroupModel inviteGroupModel = await InviteGroupModel.Build(projectUsersData, existUsers.ToList());
			if (inviteGroupModel != null)
			{
				inviteGroupModel.Users = inviteGroupModel.Users.Where((InviteUserModel user) => !existUsers.Contains(user.UserCode)).ToList();
				_projectUsersDatas.Add(inviteGroupModel);
			}
		}
		SharedProject.ItemsSource = _projectUsersDatas;
	}

	public InviteUsersDialog(IEnumerable<InviteUserModel> recentInviteModels, IEnumerable<InviteUserModel> teamMemberModels, ICollection<string> existUsers)
	{
		_isTeam = true;
		InitializeComponent();
		base.Height = (Utils.IsCn() ? 316 : 346);
		if (recentInviteModels != null)
		{
			recentInviteModels = recentInviteModels.Where((InviteUserModel model) => !existUsers.Contains(model.UserCode)).ToList();
			_recentInviteModels = recentInviteModels.ToList();
			_recentInviteModels.ForEach(delegate(InviteUserModel i)
			{
				i.CanDelete = true;
			});
			if (recentInviteModels.Count() > 10)
			{
				recentInviteModels.ToList().RemoveRange(10, recentInviteModels.Count() - 10);
			}
			RecentInvite.ItemsSource = recentInviteModels;
		}
		TeamMembers.ItemsSource = teamMemberModels?.Where((InviteUserModel model) => !existUsers.Contains(model.UserCode)).ToList();
		EmptyPage.Visibility = ((RecentInvite.Items.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		Titles.Titles = "InviteRecently|TeamMembers";
		Titles.ItemMaxWidth = 110.0;
	}

	public void OnProjectItemSelected(List<string> userCodes, bool selected, bool clickAll = true)
	{
		foreach (InviteGroupModel data in _projectUsersDatas)
		{
			data.Users.ForEach(delegate(InviteUserModel user)
			{
				if (userCodes.Contains(user.UserCode))
				{
					user.Selected = selected;
					if (!clickAll)
					{
						data.SelectNum += (selected ? 1 : (-1));
					}
				}
			});
			if (clickAll)
			{
				data.SelectNum = data.Users.Where((InviteUserModel user) => user.Selected).ToList().Count;
			}
		}
		InviteButton.IsEnabled = selected || _projectUsersDatas.Any((InviteGroupModel inviteGroupModel) => inviteGroupModel.SelectNum > 0);
	}

	public void CheckInviteEnable(bool modelSelected)
	{
		List<InviteUserModel> source = new List<InviteUserModel>();
		if (RecentInvite.IsVisible)
		{
			source = RecentInvite.ItemsSource as List<InviteUserModel>;
		}
		if (TeamMembers.IsVisible)
		{
			source = TeamMembers.ItemsSource as List<InviteUserModel>;
		}
		InviteButton.IsEnabled = modelSelected || source.Any((InviteUserModel mode) => mode.Selected);
	}

	private void OnInviteClick(object sender, RoutedEventArgs e)
	{
		List<ShareInviteTarget> list = new List<ShareInviteTarget>();
		if (RecentInvite.IsVisible)
		{
			if (RecentInvite.ItemsSource is List<InviteUserModel> source)
			{
				list = (from user2 in source
					where user2.Selected && user2.Target != null
					select PrepareInviteTarget(user2)).ToList();
			}
		}
		else if (_isTeam)
		{
			if (TeamMembers.ItemsSource is List<InviteUserModel> source2)
			{
				list = (from user2 in source2
					where user2.Selected && user2.Target != null
					select PrepareInviteTarget(user2)).ToList();
			}
		}
		else
		{
			foreach (InviteGroupModel projectUsersData in _projectUsersDatas)
			{
				foreach (InviteUserModel user in projectUsersData.Users)
				{
					if (user.Selected && user.Target != null && list.All((ShareInviteTarget u) => u.IdentityKey != user.Target.IdentityKey))
					{
						list.Add(PrepareInviteTarget(user));
					}
				}
			}
		}
		this.OnInvited?.Invoke(this, list);
	}

	private static ShareInviteTarget PrepareInviteTarget(InviteUserModel user)
	{
		ShareInviteTarget shareInviteTarget = user?.Target;
		if (shareInviteTarget == null || string.IsNullOrWhiteSpace(user.UserName))
		{
			return shareInviteTarget;
		}
		if (!Utils.IsDida() || ShareInviteTarget.IsSafeDisplayName(user.UserName))
		{
			shareInviteTarget.DisplayName = user.UserName;
			return shareInviteTarget;
		}
		string maskedMainlandPhone = Utils.GetMaskedMainlandPhone(user.UserName);
		if (!string.IsNullOrEmpty(maskedMainlandPhone))
		{
			shareInviteTarget.DisplayName = maskedMainlandPhone;
		}
		return shareInviteTarget;
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		this.OnCancel?.Invoke(this, null);
	}

	private void OnRecentShow()
	{
		if (!RecentInvite.IsVisible)
		{
			if (RecentInvite.ItemsSource is List<InviteUserModel> list)
			{
				list.ForEach(delegate(InviteUserModel data)
				{
					data.Selected = false;
				});
			}
			RecentInvite.Visibility = Visibility.Visible;
			TeamMembers.Visibility = Visibility.Collapsed;
			SharedProject.Visibility = Visibility.Collapsed;
			EmptyPage.Visibility = ((RecentInvite.Items.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		}
		EmptyText1.Text = Utils.GetString("ShareEmpty1");
		EmptyText2.Text = Utils.GetString("ShareEmpty2");
		InviteButton.IsEnabled = false;
	}

	private void OnSharedOrTeamShow()
	{
		if (RecentInvite.IsVisible)
		{
			if (_isTeam && TeamMembers.ItemsSource is List<InviteUserModel> list)
			{
				list.ForEach(delegate(InviteUserModel data)
				{
					data.Selected = false;
				});
			}
			else
			{
				this.OnSwitch?.Invoke(null, null);
			}
			RecentInvite.Visibility = Visibility.Collapsed;
			TeamMembers.Visibility = ((!_isTeam) ? Visibility.Collapsed : Visibility.Visible);
			SharedProject.Visibility = (_isTeam ? Visibility.Collapsed : Visibility.Visible);
			EmptyPage.Visibility = ((SharedProject.Items.Count != 0 || TeamMembers.Items.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
			EmptyText1.Text = (_isTeam ? Utils.GetString("InviteMemberEmpty1") : Utils.GetString("InviteProjectEmpty1"));
			EmptyText2.Text = (_isTeam ? Utils.GetString("InviteMemberEmpty2") : Utils.GetString("ShareEmpty2"));
		}
		InviteButton.IsEnabled = false;
	}

	private void OnTitleSelected(object sender, GroupTitleViewModel e)
	{
		if (e.Index == 0)
		{
			OnRecentShow();
		}
		else
		{
			OnSharedOrTeamShow();
		}
	}

	public async Task DeleteRecentRecord(InviteUserModel model)
	{
		bool flag = model != null;
		if (flag)
		{
			flag = await Communicator.DeleteShareContact(model.Email, model.Phone);
		}
		if (flag)
		{
			InviteHelper.DeleteRecentUser(model.Email, model.Phone);
			string normalizedPhone = (Utils.IsDida() ? Utils.NormalizeMainlandPhone(model.Phone) : string.Empty);
			_recentInviteModels.RemoveAll((InviteUserModel r) => (!string.IsNullOrEmpty(model.UserCode) && r.UserCode == model.UserCode) || (!string.IsNullOrEmpty(model.Email) && string.Equals(r.Email, model.Email, StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrEmpty(normalizedPhone) && Utils.NormalizeMainlandPhone(r.Phone) == normalizedPhone));
			List<InviteUserModel> list = _recentInviteModels.ToList();
			if (list.Count() > 10)
			{
				list.RemoveRange(10, list.Count() - 10);
			}
			RecentInvite.ItemsSource = list;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/team/inviteusersdialog.xaml", UriKind.Relative);
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
			Titles = (AnimateGroupTitle)target;
			break;
		case 2:
			RecentInvite = (ItemsControl)target;
			break;
		case 3:
			SharedProject = (ItemsControl)target;
			break;
		case 4:
			TeamMembers = (ItemsControl)target;
			break;
		case 5:
			EmptyPage = (StackPanel)target;
			break;
		case 6:
			EmptyText1 = (TextBlock)target;
			break;
		case 7:
			EmptyText2 = (TextBlock)target;
			break;
		case 8:
			InviteButton = (Button)target;
			InviteButton.Click += OnInviteClick;
			break;
		case 9:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
