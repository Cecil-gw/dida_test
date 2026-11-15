namespace ticktick_WPF.Resource;

public static class ThemeKey
{
	public const string Default = "White";

	public const string Blue = "Blue";

	public const string Pink = "Pink";

	public const string Black = "Black";

	public const string Green = "Green";

	public const string Mint = "Mint";

	public const string Gray = "Gray";

	public const string Yellow = "Yellow";

	public const string Dark = "Dark";

	public const string DarkBlue = "DarkBlue";

	public const string DarkCyan = "DarkCyan";

	public const string DarkGreen = "DarkGreen";

	public const string DarkMint = "DarkMint";

	public const string DarkYellow = "DarkYellow";

	public const string DarkPink = "DarkPink";

	public const string DarkPurple = "DarkPurple";

	public const string DarkCustom = "DarkCustom";

	public const string SeaBlizz = "SeaBlizz";

	public const string Matcha = "Matcha";

	public const string Peach = "Peach";

	public const string Lilac = "Lilac";

	public const string Sunshine = "Sunshine";

	public const string Ebony = "Ebony";

	public const string Navy = "Navy";

	public const string Spring = "Spring";

	public const string Summer = "Summer";

	public const string Autumn = "Autumn";

	public const string Winter = "Winter";

	public const string Beijing = "Beijing";

	public const string Hangzhou = "Hangzhou";

	public const string London = "London";

	public const string Moscow = "Moscow";

	public const string SanFrancisco = "Sanfrancisco";

	public const string Seoul = "Seoul";

	public const string Shanghai = "Shanghai";

	public const string Sydney = "Sydney";

	public const string Tokyo = "Tokyo";

	public const string NewYork = "NewYork";

	public const string Desert = "Desert";

	public const string Structure = "Structure";

	public const string Blacksea = "Blacksea";

	public const string Leaves = "Leaves";

	public const string Birds = "Birds";

	public const string Guangzhou = "Guangzhou";

	public const string Shenzhen = "Shenzhen";

	public const string Cairo = "Cairo";

	public const string LosAngeles = "LosAngeles";

	public const string Custom = "Custom";

	public const string Blossom = "Blossom";

	public const string Dawn = "Dawn";

	public const string Frozen = "Frozen";

	public const string Meadow = "Meadow";

	public const string Silence = "Silence";

	public static bool ShowProjectTextShadow(string themeId)
	{
		if (IsProTheme(themeId) && !IsSeasonTheme(themeId) && themeId != "Frozen")
		{
			return themeId != "Blossom";
		}
		return false;
	}

	public static bool IsDarkTheme(string key)
	{
		return GetThemeGroupType(key) == ThemeGroupType.Dark;
	}

	public static bool IsGreenTheme(string key)
	{
		return key == "Green";
	}

	public static bool IsPinkTheme(string key)
	{
		return key == "Pink";
	}

	public static bool IsSeasonTheme(string key)
	{
		switch (key)
		{
		default:
			return key == "Winter";
		case "Spring":
		case "Summer":
		case "Autumn":
			return true;
		}
	}

	public static bool IsColorTheme(string key)
	{
		switch (key)
		{
		default:
			return IsGradientTheme(key);
		case "White":
		case "Blue":
		case "Pink":
		case "Black":
		case "Green":
		case "Yellow":
			return true;
		}
	}

	public static bool IsGradientTheme(string key)
	{
		switch (key)
		{
		case "SeaBlizz":
		case "Sunshine":
		case "Ebony":
		case "Lilac":
		case "Peach":
		case "Mint":
		case "Navy":
		case "Blue":
		case "Gray":
		case "Matcha":
			return true;
		default:
			return false;
		}
	}

	public static bool IsProTheme(string key)
	{
		if (!IsDarkTheme(key))
		{
			return !IsColorTheme(key);
		}
		return false;
	}

	public static bool IsCustomTheme(string key)
	{
		return key == "Custom";
	}

	public static bool IsLightColorImageTheme(string theme)
	{
		switch (theme)
		{
		default:
			return theme == "Blossom";
		case "Spring":
		case "Summer":
		case "Autumn":
		case "Winter":
		case "Frozen":
		case "Seoul":
			return true;
		}
	}

	public static string GetCollectLabel(string modelKey)
	{
		return modelKey switch
		{
			"SeaBlizz" => "teal", 
			"Matcha" => "matcha", 
			"Sunshine" => "sunshine", 
			"Peach" => "peach", 
			"Lilac" => "lilac", 
			"Ebony" => "ebony", 
			"Navy" => "navy", 
			"Mint" => "turquoise", 
			"Blue" => "sky", 
			"Blacksea" => "b&w", 
			"Birds" => "swallow", 
			"Leaves" => "greenery", 
			"Structure" => "architecture", 
			"White" => "default", 
			"DarkBlue" => "dark", 
			"DarkCyan" => "dark_cyan", 
			"DarkGreen" => "dark_green", 
			"DarkMint" => "dark_mint", 
			"DarkYellow" => "dark_yellow", 
			"DarkPink" => "dark_pink", 
			"DarkPurple" => "dark_purple", 
			_ => modelKey?.ToLower(), 
		};
	}

	public static ThemeGroupType GetThemeGroupType(string theme)
	{
		switch (theme)
		{
		case "White":
			return ThemeGroupType.Default;
		case "Dark":
		case "DarkBlue":
		case "DarkCyan":
		case "DarkMint":
		case "DarkPink":
		case "DarkGreen":
		case "DarkYellow":
		case "DarkCustom":
		case "DarkPurple":
			return ThemeGroupType.Dark;
		case "Peach":
		case "Lilac":
		case "Ebony":
		case "Black":
		case "Green":
		case "Pink":
		case "Navy":
		case "Blue":
		case "Gray":
		case "Mint":
		case "SeaBlizz":
		case "Sunshine":
		case "Matcha":
		case "Yellow":
			return ThemeGroupType.Gradient;
		case "Spring":
		case "Summer":
		case "Autumn":
		case "Winter":
			return ThemeGroupType.Season;
		case "Seoul":
		case "Tokyo":
		case "Cairo":
		case "Hangzhou":
		case "Shenzhen":
		case "Shanghai":
		case "Guangzhou":
		case "LosAngeles":
		case "Moscow":
		case "London":
		case "Sydney":
		case "NewYork":
		case "Beijing":
		case "Sanfrancisco":
			return ThemeGroupType.City;
		case "Birds":
		case "Dawn":
		case "Blacksea":
		case "Structure":
		case "Meadow":
		case "Desert":
		case "Leaves":
		case "Frozen":
		case "Blossom":
		case "Silence":
			return ThemeGroupType.Photo;
		default:
			return ThemeGroupType.Custom;
		}
	}
}
