using System;
using System.Windows.Media.Imaging;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.ViewModels;

public class ShareContactsViewModel : ComboBoxViewModel
{
	private int _siteId;

	public string toUserId { get; set; }

	public string email { get; set; }

	public string phone { get; set; }

	public string avatarUrl { get; set; }

	public string userCode { get; set; }

	public int freq { get; set; }

	public DateTime lstTime { get; set; }

	public string displayName { get; set; }

	public string displayEmail { get; set; }

	public bool isFeishu
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

	public BitmapImage avatar { get; set; }

	public bool IsNewAdd { get; set; }

	public bool IsPhoneTarget { get; set; }

	public ShareInviteTarget Target { get; set; }

	public ShareContactsViewModel(string addEmail)
		: this(ShareInviteTarget.FromManualEmail(addEmail))
	{
	}

	public ShareContactsViewModel(ShareInviteTarget target, bool isInviteable = true)
	{
		IsNewAdd = true;
		string text = (target.IsPhone ? target.ToPhone : target.DisplayName);
		displayName = string.Format(Utils.GetString("InviteUserJoinString"), (text.Length > 30) ? (text.Substring(0, 30) + "...") : text);
		email = ((!string.IsNullOrWhiteSpace(target.Email)) ? target.Email : target.ToUsername);
		phone = ((!string.IsNullOrWhiteSpace(target.Phone)) ? target.Phone : target.ToPhone);
		Target = target;
		IsPhoneTarget = target.IsPhone && isInviteable;
		base.IsEnable = isInviteable;
	}

	public ShareContactsViewModel(ShareContactsModel shareContactsModel, bool phoneMatched = false)
	{
		toUserId = shareContactsModel.toUserId;
		email = shareContactsModel.email;
		phone = shareContactsModel.phone;
		userCode = shareContactsModel.userCode;
		freq = shareContactsModel.freq;
		lstTime = shareContactsModel.lstTime;
		avatarUrl = shareContactsModel.avatarUrl;
		displayName = shareContactsModel.displayName;
		displayEmail = (phoneMatched ? Utils.GetMaskedMainlandPhone(shareContactsModel.phone) : shareContactsModel.displayEmail);
		if (Utils.IsDida() && string.IsNullOrEmpty(displayEmail))
		{
			displayEmail = Utils.GetMaskedMainlandPhone(shareContactsModel.phone);
		}
		if (Utils.IsDida() && string.Equals(displayName, displayEmail, StringComparison.Ordinal))
		{
			displayEmail = null;
		}
		Target = ShareInviteTarget.FromContact(shareContactsModel);
		IsPhoneTarget = Target?.IsPhone ?? false;
		_siteId = shareContactsModel.siteId.GetValueOrDefault();
		CanSelect = false;
		SetAvatar();
	}

	private async void SetAvatar()
	{
		avatar = await AvatarHelper.GetAvatarByUrlAsync(avatarUrl);
		OnPropertyChanged("avatar");
	}
}
