using System;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class ShareContactsModel
{
	public string toUserId { get; set; }

	public string email { get; set; }

	public string phone { get; set; }

	public string userCode { get; set; }

	public int freq { get; set; }

	public int? siteId { get; set; }

	public DateTime lstTime { get; set; }

	public string avatarUrl { get; set; }

	public string displayName { get; set; }

	public string displayEmail { get; set; }

	public bool canSearchByPhone { get; set; }

	public ShareContactsModel()
	{
	}

	public ShareContactsModel(ShareUserModel model)
	{
		email = ((Utils.IsDida() && !string.IsNullOrWhiteSpace(model.inviteIdentifier)) ? model.inviteIdentifier : model.username);
		phone = model.phone;
		userCode = model.userCode;
		avatarUrl = model.avatarUrl;
		if (!Utils.IsDida())
		{
			displayName = (string.IsNullOrEmpty(model.displayName) ? model.username : model.displayName);
			displayEmail = model.username;
			canSearchByPhone = false;
			return;
		}
		string text = (ShareInviteTarget.IsRealEmail(email) ? email : null);
		string value = ((ShareInviteTarget.IsUsableUsername(email) && string.IsNullOrEmpty(text)) ? email : null);
		string maskedMainlandPhone = Utils.GetMaskedMainlandPhone((!string.IsNullOrWhiteSpace(model.phone)) ? model.phone : email);
		string maskedMainlandPhone2 = Utils.GetMaskedMainlandPhone(model.displayName);
		if (ShareInviteTarget.IsSafeDisplayName(model.displayName))
		{
			displayName = model.displayName;
		}
		else if (!string.IsNullOrEmpty(maskedMainlandPhone2))
		{
			displayName = maskedMainlandPhone2;
		}
		else if (!string.IsNullOrEmpty(maskedMainlandPhone))
		{
			displayName = maskedMainlandPhone;
		}
		else if (!string.IsNullOrEmpty(value))
		{
			displayName = value;
		}
		else
		{
			displayName = text ?? Utils.GetString("InvitationPending");
		}
		displayEmail = text ?? maskedMainlandPhone;
		canSearchByPhone = false;
	}

	public ShareContactsModel(TeamMember model)
	{
		email = model.email;
		userCode = model.email;
		avatarUrl = model.avatarUrl;
		displayName = (string.IsNullOrEmpty(model.nickname) ? model.displayName : model.nickname);
		displayEmail = model.email;
	}
}
