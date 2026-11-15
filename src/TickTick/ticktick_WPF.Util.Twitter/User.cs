using Newtonsoft.Json;

namespace ticktick_WPF.Util.Twitter;

public class User
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("screen_name")]
	public string ScreenName { get; set; }

	[JsonProperty("location")]
	public string Location { get; set; }

	[JsonProperty("description")]
	public string Description { get; set; }

	[JsonProperty("followers_count")]
	public int FollowersCount { get; set; }

	[JsonProperty("friends_count")]
	public int FriendsCount { get; set; }

	[JsonProperty("profile_background_color")]
	public string ProfileBackgroundColor { get; set; }

	[JsonProperty("profile_background_image_url")]
	public string ProfileBackgroudImageUrl { get; set; }

	[JsonProperty("profile_image_url")]
	public string ProfileImageUrl { get; set; }

	[JsonProperty("following")]
	public bool IsFollowing { get; set; }
}
