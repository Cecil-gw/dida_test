using System;
using System.Linq;
using Newtonsoft.Json;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class ShareInviteTarget
{
	public string ToUsername { get; set; }

	public string ToPhone { get; set; }

	public string DisplayName { get; set; }

	public string Email { get; set; }

	public string Phone { get; set; }

	public string UserCode { get; set; }

	[JsonIgnore]
	public bool IsPhone => !string.IsNullOrWhiteSpace(ToPhone);

	[JsonIgnore]
	public string RequestValue
	{
		get
		{
			if (!IsPhone)
			{
				return ToUsername;
			}
			return ToPhone;
		}
	}

	[JsonIgnore]
	public string IdentityKey
	{
		get
		{
			if (!IsPhone)
			{
				return "username:" + (ToUsername ?? string.Empty).ToLowerInvariant();
			}
			return "phone:" + Utils.NormalizeMainlandPhone(ToPhone);
		}
	}

	public static ShareInviteTarget FromManualEmail(string email, string userCode = null)
	{
		return new ShareInviteTarget
		{
			ToUsername = email,
			Email = email,
			DisplayName = email,
			UserCode = userCode
		};
	}

	public static ShareInviteTarget FromManualPhone(string phone)
	{
		string text = Utils.NormalizeMainlandPhone(phone);
		return new ShareInviteTarget
		{
			ToPhone = text,
			Phone = text,
			DisplayName = Utils.GetMaskedMainlandPhone(text)
		};
	}

	public static ShareInviteTarget FromInvalidInviteCandidate(string input)
	{
		return new ShareInviteTarget
		{
			ToUsername = input,
			DisplayName = input
		};
	}

	public static ShareInviteTarget FromContact(ShareContactsModel model)
	{
		if (model == null)
		{
			return null;
		}
		if (!Utils.IsDida())
		{
			if (string.IsNullOrWhiteSpace(model.email))
			{
				return null;
			}
			return new ShareInviteTarget
			{
				ToUsername = model.email,
				Email = model.email,
				UserCode = model.userCode,
				DisplayName = (string.IsNullOrWhiteSpace(model.displayName) ? model.email : model.displayName)
			};
		}
		string text = Utils.NormalizeMainlandPhone(model.email);
		bool flag = !string.IsNullOrEmpty(Utils.GetMaskedMainlandPhone(model.email));
		string text2 = (IsUsableUsername(model.email) ? model.email : null);
		string text3 = (IsRealEmail(model.email) ? model.email : null);
		string text4 = ((!string.IsNullOrEmpty(text2) && string.IsNullOrEmpty(text3)) ? text2 : null);
		string text5 = Utils.NormalizeMainlandPhone(model.phone);
		if (string.IsNullOrEmpty(text5))
		{
			text5 = text;
		}
		string maskedMainlandPhone = Utils.GetMaskedMainlandPhone((!string.IsNullOrWhiteSpace(model.phone)) ? model.phone : model.email);
		ShareInviteTarget shareInviteTarget = new ShareInviteTarget
		{
			Email = model.email,
			Phone = text5,
			UserCode = model.userCode
		};
		if (!string.IsNullOrEmpty(text2))
		{
			shareInviteTarget.ToUsername = text2;
		}
		else if (!string.IsNullOrEmpty(text5))
		{
			shareInviteTarget.ToPhone = text5;
		}
		else
		{
			if (string.IsNullOrWhiteSpace(model.email) || flag)
			{
				return null;
			}
			shareInviteTarget.ToUsername = model.email;
		}
		string maskedMainlandPhone2 = Utils.GetMaskedMainlandPhone(model.displayName);
		string text6 = ((!string.IsNullOrEmpty(maskedMainlandPhone2)) ? maskedMainlandPhone2 : (IsSafeDisplayName(model.displayName) ? model.displayName : null));
		shareInviteTarget.DisplayName = text6 ?? ((!string.IsNullOrEmpty(maskedMainlandPhone)) ? maskedMainlandPhone : (text4 ?? text3 ?? Utils.GetString("InvitationPending")));
		return shareInviteTarget;
	}

	public static bool IsUsableUsername(string value)
	{
		if (!string.IsNullOrWhiteSpace(value) && !Utils.IsFakeEmail(value))
		{
			return string.IsNullOrEmpty(Utils.GetMaskedMainlandPhone(value));
		}
		return false;
	}

	public static bool IsRealEmail(string value)
	{
		if (IsUsableUsername(value))
		{
			return Utils.IsValidEmail(value);
		}
		return false;
	}

	public static bool IsSafeDisplayName(string displayName)
	{
		if (!string.IsNullOrWhiteSpace(displayName) && string.IsNullOrEmpty(Utils.GetMaskedMainlandPhone(displayName)) && !Utils.IsValidEmail(displayName))
		{
			return !Utils.IsFakeEmail(displayName);
		}
		return false;
	}

	public static ShareInviteTarget FromShareUser(ShareUserModel model)
	{
		if (model == null)
		{
			return null;
		}
		return FromContact(new ShareContactsModel(model));
	}

	public static bool IsRecentPhoneMatch(ShareContactsModel contact, string input)
	{
		if (contact == null || !contact.canSearchByPhone || string.IsNullOrEmpty(input) || input.Any((char c) => !char.IsDigit(c)))
		{
			return false;
		}
		string text = Utils.NormalizeMainlandPhone(contact.phone);
		if (!string.IsNullOrEmpty(text))
		{
			return text.StartsWith(input, StringComparison.Ordinal);
		}
		return false;
	}

	public static bool IsContactSearchMatch(ShareContactsModel contact, string input)
	{
		if (contact == null || string.IsNullOrWhiteSpace(input))
		{
			return false;
		}
		bool num = Utils.IsDida() && !string.IsNullOrEmpty(Utils.GetMaskedMainlandPhone(contact.displayName));
		bool flag = Utils.IsDida() && !string.IsNullOrEmpty(Utils.GetMaskedMainlandPhone(contact.displayEmail));
		bool flag2 = Utils.IsDida() && !string.IsNullOrEmpty(Utils.GetMaskedMainlandPhone(contact.email));
		if ((num || string.IsNullOrEmpty(contact.displayName) || contact.displayName.IndexOf(input, StringComparison.CurrentCultureIgnoreCase) < 0) && (flag || string.IsNullOrEmpty(contact.displayEmail) || contact.displayEmail.IndexOf(input, StringComparison.OrdinalIgnoreCase) < 0) && (flag2 || string.IsNullOrEmpty(contact.email) || (Utils.IsDida() && Utils.IsFakeEmail(contact.email)) || contact.email.IndexOf(input, StringComparison.OrdinalIgnoreCase) < 0))
		{
			if (Utils.IsDida())
			{
				return IsRecentPhoneMatch(contact, input);
			}
			return false;
		}
		return true;
	}
}
