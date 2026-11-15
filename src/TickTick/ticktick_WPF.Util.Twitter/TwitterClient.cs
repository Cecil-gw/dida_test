namespace ticktick_WPF.Util.Twitter;

public class TwitterClient : BaseRequest
{
	private readonly OAuthInfo oauth;

	public TwitterClient(OAuthInfo oauth)
		: base("https://api.twitter.com/1.1/statuses/", oauth)
	{
		this.oauth = oauth;
	}
}
