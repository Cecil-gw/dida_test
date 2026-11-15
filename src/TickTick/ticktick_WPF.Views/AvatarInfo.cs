namespace ticktick_WPF.Views;

public class AvatarInfo
{
	public readonly string AvatarUrl;

	public readonly string UserId;

	public AvatarInfo(string userId, string url)
	{
		UserId = userId;
		AvatarUrl = url;
	}
}
