using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Team;

public class InviteUserModel : BaseViewModel
{
	private string _id;

	private string _userCode;

	private string _avatarUrl;

	private string _userName;

	private string _email;

	private DateTime _lstTime;

	private bool _inGroup;

	private bool _selected;

	private bool _enable = true;

	private int _siteId;

	public string Id { get; set; }

	public string UserCode { get; set; }

	public string AvatarUrl { get; set; }

	public string UserName { get; set; }

	public string Email { get; set; }

	public string Phone { get; set; }

	public ShareInviteTarget Target { get; set; }

	public DateTime LstTime { get; set; }

	public bool InGroup { get; set; }

	public bool CanDelete { get; set; }

	public BitmapImage Avatar { get; set; }

	public bool IsFeiShu
	{
		get
		{
			if (_siteId == 10)
			{
				return Utils.IsDida();
			}
			return false;
		}
	}

	public bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			_selected = value;
			OnPropertyChanged("Selected");
		}
	}

	public async void SetAvatar()
	{
		Avatar = await AvatarHelper.GetAvatarByUrlAsync(AvatarUrl);
		OnPropertyChanged("Avatar");
	}

	private InviteUserModel()
	{
	}

	public InviteUserModel(ShareContactsModel model)
	{
		Id = model.toUserId;
		UserCode = model.userCode;
		AvatarUrl = model?.avatarUrl ?? "";
		Email = model.email;
		Phone = model.phone;
		Target = ShareInviteTarget.FromContact(model);
		LstTime = model.lstTime;
		InGroup = false;
		Selected = false;
		_siteId = model.siteId.GetValueOrDefault();
		SetAvatar();
	}

	public InviteUserModel(TeamContactsModel model)
	{
		UserCode = model.userCode;
		Email = model.email;
		Target = ShareInviteTarget.FromManualEmail(model.email, model.userCode);
		LstTime = model.lstTime;
		InGroup = false;
		Selected = false;
	}

	private InviteUserModel(ShareUserModel userModel)
	{
		Id = userModel.userId.ToString();
		UserCode = userModel.userCode;
		AvatarUrl = userModel.avatarUrl;
		Email = userModel.username;
		Phone = userModel.phone;
		Target = ShareInviteTarget.FromShareUser(userModel);
		if (!Utils.IsDida())
		{
			UserName = (string.IsNullOrEmpty(userModel.displayName) ? userModel.username : userModel.displayName);
		}
		else
		{
			string maskedMainlandPhone = Utils.GetMaskedMainlandPhone(userModel.displayName);
			UserName = ((!string.IsNullOrEmpty(maskedMainlandPhone)) ? maskedMainlandPhone : (ShareInviteTarget.IsSafeDisplayName(userModel.displayName) ? userModel.displayName : Target?.DisplayName));
		}
		LstTime = userModel.createdTime ?? DateTime.Now;
		InGroup = true;
		Selected = false;
		SetAvatar();
	}

	public InviteUserModel(TeamMember userMode)
	{
		UserCode = userMode.userCode;
		AvatarUrl = userMode.avatarUrl;
		UserName = userMode.displayName;
		Email = userMode.email;
		Target = ShareInviteTarget.FromManualEmail(userMode.email, userMode.userCode);
		InGroup = false;
		Selected = false;
		SetAvatar();
	}

	public static List<InviteUserModel> Build(List<ShareUserModel> shareUsers, List<string> shareUserCodeList)
	{
		List<InviteUserModel> list = new List<InviteUserModel>();
		foreach (ShareUserModel shareUser in shareUsers)
		{
			if (!shareUserCodeList.Contains(shareUser.userCode))
			{
				list.Add(new InviteUserModel(shareUser));
			}
		}
		return list;
	}
}
