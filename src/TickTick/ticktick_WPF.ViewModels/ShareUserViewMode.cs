using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class ShareUserViewMode : BaseViewModel
{
	private string _avatarUrl;

	private int _avatarLoadVersion;

	private bool _hasSafeProfileDisplayName;

	public int siteId;

	private static string _teamName;

	private bool _usePhonePendingStyle;

	public string avatarUrl
	{
		get
		{
			return _avatarUrl;
		}
		set
		{
			_avatarUrl = value;
			SetAvatar(value, ++_avatarLoadVersion);
		}
	}

	public string recordId { get; set; }

	public long userId { get; set; }

	public string username { get; set; }

	public string inviteIdentifier { get; set; }

	public string phone { get; set; }

	public string displayName { get; set; }

	public bool isOwner { get; set; }

	public bool isProjectShare { get; set; }

	public bool isAccept { get; set; }

	public DateTime createdTime { get; set; }

	public string userCode { get; set; }

	public int acceptStatus { get; set; }

	public bool deleted { get; set; }

	public bool editable { get; set; }

	public bool isTeam { get; set; }

	public bool Enable { get; set; } = true;

	public bool openToTeam { get; set; }

	public bool noMembers { get; set; }

	public string permission { get; set; }

	public int userCount { get; set; }

	public bool isFeishu
	{
		get
		{
			if (userId.ToString() != LocalSettings.Settings.LoginUserId && siteId == 10)
			{
				return Utils.IsDida();
			}
			return false;
		}
	}

	public ImageSource avatar { get; set; }

	public bool visitor { get; set; }

	public bool usePhonePendingStyle
	{
		get
		{
			if (Utils.IsDida() && !isAccept)
			{
				return _usePhonePendingStyle;
			}
			return false;
		}
	}

	public ShareUserViewMode()
	{
	}

	public ShareUserViewMode(ShareAddUserMode shareAddUserMode)
	{
		recordId = shareAddUserMode.recordId;
		userId = (shareAddUserMode.toUserId.HasValue ? shareAddUserMode.toUserId.Value : (-1));
		avatarUrl = "../Assets/avatar-new.png";
		inviteIdentifier = shareAddUserMode.toUsername;
		InitializePendingInviteDisplay(shareAddUserMode.toUsername);
		userCode = shareAddUserMode.toUserCode;
		isOwner = false;
		isProjectShare = true;
		isAccept = false;
		acceptStatus = 0;
		permission = shareAddUserMode.permission;
	}

	public ShareUserViewMode(ShareUserModel shareUserModel)
	{
		recordId = shareUserModel.recordId;
		userId = (int)(shareUserModel.userId.HasValue ? shareUserModel.userId.Value : (-1));
		avatarUrl = ((shareUserModel.avatarUrl != null && !shareUserModel.avatarUrl.Contains("avatar-new")) ? shareUserModel.avatarUrl : "../Assets/avatar-new.png");
		inviteIdentifier = FirstNotWhiteSpace(shareUserModel.inviteIdentifier, shareUserModel.username);
		isOwner = shareUserModel.isOwner;
		isAccept = shareUserModel.isOwner || shareUserModel.isAccept || userId.ToString() == LocalSettings.Settings.LoginUserId;
		if (Utils.IsDida())
		{
			InitializeDidaMemberDisplay(shareUserModel);
		}
		else
		{
			InitializeTickMemberDisplay(shareUserModel);
		}
		isProjectShare = shareUserModel.isProjectShare;
		createdTime = shareUserModel.createdTime ?? DateTime.Now;
		userCode = shareUserModel.userCode;
		acceptStatus = shareUserModel.acceptStatus;
		deleted = shareUserModel.deleted;
		permission = shareUserModel.permission;
		siteId = shareUserModel.siteId.GetValueOrDefault();
		visitor = shareUserModel.visitor;
	}

	private void InitializePendingInviteDisplay(string inviteUsername)
	{
		if (!Utils.IsDida())
		{
			phone = string.Empty;
			username = (Utils.IsFakeEmail(inviteUsername) ? string.Empty : inviteUsername);
		}
		else
		{
			phone = Utils.GetMaskedMainlandPhone(inviteUsername);
			username = ((!string.IsNullOrEmpty(phone)) ? phone : (Utils.IsFakeEmail(inviteUsername) ? string.Empty : inviteUsername));
			_usePhonePendingStyle = !string.IsNullOrEmpty(phone);
		}
	}

	private void InitializeTickMemberDisplay(ShareUserModel shareUserModel)
	{
		phone = string.Empty;
		username = (Utils.IsFakeEmail(shareUserModel.username) ? string.Empty : shareUserModel.username);
		displayName = shareUserModel.displayName;
		_usePhonePendingStyle = false;
	}

	private void InitializeDidaMemberDisplay(ShareUserModel shareUserModel)
	{
		phone = FirstNotWhiteSpace(new string[3] { shareUserModel.phone, shareUserModel.username, inviteIdentifier }.Select(Utils.GetMaskedMainlandPhone).ToArray());
		string[] source = new string[2] { shareUserModel.username, inviteIdentifier };
		string text = source.FirstOrDefault(ShareInviteTarget.IsRealEmail);
		string text2 = source.FirstOrDefault((string value) => ShareInviteTarget.IsUsableUsername(value) && !ShareInviteTarget.IsRealEmail(value));
		string maskedMainlandPhone = Utils.GetMaskedMainlandPhone(shareUserModel.displayName);
		bool flag = (_hasSafeProfileDisplayName = ShareInviteTarget.IsSafeDisplayName(shareUserModel.displayName));
		string maskedMainlandPhone2 = Utils.GetMaskedMainlandPhone(inviteIdentifier);
		bool flag2 = (_usePhonePendingStyle = !isAccept && !string.IsNullOrEmpty(phone) && (!string.IsNullOrEmpty(maskedMainlandPhone2) || string.IsNullOrWhiteSpace(inviteIdentifier) || Utils.IsFakeEmail(inviteIdentifier)));
		username = (isAccept ? FirstNotWhiteSpace(text, phone) : (flag2 ? phone : FirstNotWhiteSpace(text, text2, phone)));
		displayName = (flag ? shareUserModel.displayName : (flag2 ? Utils.GetString("InvitationPending") : FirstNotWhiteSpace(text2, maskedMainlandPhone, text, phone, Utils.GetString(isAccept ? "Member" : "InvitationPending"))));
		if (string.Equals(displayName, username, StringComparison.Ordinal))
		{
			username = string.Empty;
		}
		if (flag2 && !flag)
		{
			avatarUrl = "../Assets/avatar-new.png";
		}
	}

	public void ApplyPendingInviteTarget(ShareInviteTarget target)
	{
		if (isAccept || target == null)
		{
			return;
		}
		if (target.IsPhone)
		{
			phone = Utils.GetMaskedMainlandPhone(target.ToPhone);
		}
		if (target.IsPhone && !ShareInviteTarget.IsRealEmail(inviteIdentifier) && !ShareInviteTarget.IsRealEmail(username))
		{
			_usePhonePendingStyle = true;
			OnPropertyChanged("usePhonePendingStyle");
			username = phone;
			string value = ((_hasSafeProfileDisplayName && ShareInviteTarget.IsSafeDisplayName(displayName)) ? displayName : (ShareInviteTarget.IsSafeDisplayName(target.DisplayName) ? target.DisplayName : null));
			if (!string.IsNullOrWhiteSpace(value))
			{
				displayName = value;
			}
			else
			{
				displayName = Utils.GetString("InvitationPending");
				avatarUrl = "../Assets/avatar-new.png";
			}
		}
		if (!string.IsNullOrWhiteSpace(target.ToUsername))
		{
			inviteIdentifier = target.ToUsername;
		}
		OnPropertyChanged("phone");
		OnPropertyChanged("username");
		OnPropertyChanged("displayName");
		OnPropertyChanged("inviteIdentifier");
	}

	private static string FirstNotWhiteSpace(params string[] values)
	{
		return values?.FirstOrDefault((string value) => !string.IsNullOrWhiteSpace(value));
	}

	private async void SetAvatar(string requestedUrl, int loadVersion)
	{
		BitmapImage bitmapImage = await AvatarHelper.GetAvatarByUrlAsync(requestedUrl);
		if (loadVersion == _avatarLoadVersion)
		{
			avatar = bitmapImage;
			OnPropertyChanged("avatar");
		}
	}

	public void SetTeamPermissionText()
	{
		username = GetPermissionTextOfTeam(permission, _teamName);
		OnPropertyChanged("username");
	}

	public static string GetPermissionTextOfTeam(string permit, string teamName)
	{
		_teamName = teamName;
		string key = "MembersCanEdit";
		if (!(permit == "read"))
		{
			if (permit == "comment")
			{
				key = "MembersCanComment";
			}
		}
		else
		{
			key = "MembersCanRead";
		}
		return string.Format(Utils.GetString(key), teamName);
	}

	public void SetProperty(ShareUserViewMode teamVModel)
	{
		avatar = teamVModel.avatar;
		username = teamVModel.username;
		permission = teamVModel.permission;
		displayName = teamVModel.displayName;
		openToTeam = teamVModel.openToTeam;
		Enable = teamVModel.Enable;
		editable = teamVModel.editable;
		OnPropertyChanged("editable");
		OnPropertyChanged("displayName");
		OnPropertyChanged("username");
		OnPropertyChanged("permission");
		OnPropertyChanged("avatar");
	}
}
