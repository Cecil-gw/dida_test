using System;
using SQLite;

namespace ticktick_WPF.Models;

[Serializable]
public class UserInfoModel
{
	public string etimestamp { get; set; }

	[PrimaryKey]
	public string username { get; set; }

	public string siteDomain { get; set; }

	public string createdDeviceInfo { get; set; }

	public bool filledPassword { get; set; }

	public string accountDomain { get; set; }

	public string extenalId { get; set; }

	public string email { get; set; }

	public bool verifiedEmail { get; set; }

	public bool fakedEmail { get; set; }

	public string name { get; set; }

	public string givenName { get; set; }

	public string familyName { get; set; }

	public string link { get; set; }

	public string picture { get; set; }

	public string gender { get; set; }

	public string locale { get; set; }

	public string userCode { get; set; }

	public string externalId { get; set; }

	public string displayEmail { get; set; }
}
