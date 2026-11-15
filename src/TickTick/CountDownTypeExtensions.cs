public static class CountDownTypeExtensions
{
	public static string GetDefaultIconName(this CountDownType type)
	{
		return type switch
		{
			CountDownType.Countdown => "countdown_countdown", 
			CountDownType.Anniversary => "countdown_wedding_anniversary", 
			CountDownType.Birthday => "countdown_birthday", 
			CountDownType.Holiday => "countdown_new_year_day", 
			_ => "countdown_countdown", 
		};
	}
}
