using System;
using Newtonsoft.Json;

namespace ticktick_WPF.Util.Twitter;

public class Tweet
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("created_at")]
	public DateTime CreatedAt { get; set; }

	public string CreatedString
	{
		get
		{
			TimeSpan timeSpan = DateTime.Now - CreatedAt;
			if (timeSpan < new TimeSpan(1, 0, 0))
			{
				return (int)timeSpan.TotalMinutes + "min";
			}
			if (timeSpan < new TimeSpan(24, 0, 0))
			{
				return (int)timeSpan.TotalMinutes / 60 + "h";
			}
			return CreatedAt.ToString("M", App.Ci);
		}
	}

	[JsonProperty("text")]
	public string Text { get; set; }

	[JsonProperty("retweet_count")]
	public int RetweetCount { get; set; }

	[JsonProperty("favorite_count")]
	public int FavouriteCount { get; set; }

	[JsonProperty("favorited")]
	public bool IsFavourited { get; set; }

	[JsonProperty("retweeted")]
	public bool IsRetweeted { get; set; }

	[JsonProperty("user")]
	public User User { get; set; }
}
