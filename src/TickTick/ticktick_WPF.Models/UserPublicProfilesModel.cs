using TickTickModels;

namespace ticktick_WPF.Models;

public class UserPublicProfilesModel : BaseModel
{
	public long? userId { get; set; }

	public string userCode { get; set; }

	public string displayName { get; set; }

	public string nickName { get; set; }

	public string avatarUrl { get; set; }

	public string email { get; set; }

	public int? siteId { get; set; }

	public UserPublicProfilesModel()
	{
	}

	public UserPublicProfilesModel(ShareUserModel shareUserModel)
	{
		userId = shareUserModel.userId;
		userCode = shareUserModel.userCode;
		displayName = shareUserModel.displayName;
		avatarUrl = shareUserModel.avatarUrl;
		email = shareUserModel.username;
	}
}
