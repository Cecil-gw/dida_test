using Newtonsoft.Json;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Models;

public class ShareInviteRequest
{
	[JsonProperty("toUsername", NullValueHandling = NullValueHandling.Ignore)]
	public string ToUsername { get; set; }

	[JsonProperty("toPhone", NullValueHandling = NullValueHandling.Ignore)]
	public string ToPhone { get; set; }

	[JsonProperty("permission", NullValueHandling = NullValueHandling.Ignore)]
	public string Permission { get; set; }

	public static ShareInviteRequest Create(ShareInviteTarget target, Constants.ProjectPermission permission)
	{
		return new ShareInviteRequest
		{
			ToUsername = ((target != null && target.IsPhone) ? null : target?.ToUsername),
			ToPhone = ((target != null && target.IsPhone) ? target.ToPhone : null),
			Permission = ((permission == Constants.ProjectPermission.person) ? null : permission.ToString())
		};
	}
}
