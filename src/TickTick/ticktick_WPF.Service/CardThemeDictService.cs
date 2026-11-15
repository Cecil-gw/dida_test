using System;
using System.Collections.Generic;
using System.Windows;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Service;

internal class CardThemeDictService
{
	private static ResourceDictionary cardBaseResource { get; set; }

	private static ResourceDictionary cardBaseIconBarResource { get; set; }

	private static Dictionary<ThemeGroupType, ResourceDictionary> themeGroupCardDicts { get; set; } = new Dictionary<ThemeGroupType, ResourceDictionary>();

	private static Dictionary<string, ResourceDictionary> specialIconBarDicts { get; set; } = new Dictionary<string, ResourceDictionary>();

	private static ResourceDictionary immersePatchResource { get; set; }

	public static void ReloadInterfaceStyle()
	{
		bool flag = LocalSettings.Settings.ExtraSettings.InterfaceStyle == InterfaceStyle.Card;
		if (flag)
		{
			InitCardResource();
		}
		if (cardBaseResource == null)
		{
			return;
		}
		AddOrRemoveResource(cardBaseResource, flag);
		string themeId = LocalSettings.Settings.ThemeId;
		ThemeGroupType themeGroupType = ThemeKey.GetThemeGroupType(themeId);
		foreach (ThemeGroupType key in themeGroupCardDicts.Keys)
		{
			bool add = flag && key == themeGroupType;
			AddOrRemoveResource(themeGroupCardDicts[key], add);
		}
		foreach (string key2 in specialIconBarDicts.Keys)
		{
			bool add2 = flag && themeId == key2;
			AddOrRemoveResource(specialIconBarDicts[key2], add2);
		}
		AddOrRemoveResource(cardBaseIconBarResource, flag && !string.IsNullOrEmpty(themeId) && specialIconBarDicts.ContainsKey(themeId));
	}

	private static void InitCardResource()
	{
		if (cardBaseResource == null)
		{
			cardBaseResource = new ResourceDictionary
			{
				Source = new Uri("Resource\\ThemeCard\\BaseCardTheme.xaml", UriKind.Relative)
			};
			cardBaseIconBarResource = new ResourceDictionary
			{
				Source = new Uri("Resource\\ThemeCard\\BaseCardIconBar.xaml", UriKind.Relative)
			};
			themeGroupCardDicts.Add(ThemeGroupType.Default, InitResourceDictionary($"{ThemeGroupType.Default}Theme"));
			themeGroupCardDicts.Add(ThemeGroupType.Dark, InitResourceDictionary($"{ThemeGroupType.Dark}Theme"));
			themeGroupCardDicts.Add(ThemeGroupType.Gradient, InitResourceDictionary("ColorTheme"));
			themeGroupCardDicts.Add(ThemeGroupType.Season, InitResourceDictionary($"{ThemeGroupType.Season}Theme"));
			themeGroupCardDicts.Add(ThemeGroupType.Photo, InitResourceDictionary($"{ThemeGroupType.Photo}Theme"));
			themeGroupCardDicts.Add(ThemeGroupType.City, InitResourceDictionary($"{ThemeGroupType.City}Theme"));
			themeGroupCardDicts.Add(ThemeGroupType.Custom, InitResourceDictionary($"{ThemeGroupType.Custom}Theme"));
			specialIconBarDicts.Add("Blossom", InitResourceDictionary("BlossomIconBar"));
			specialIconBarDicts.Add("Frozen", InitResourceDictionary("FrozenIconBar"));
			specialIconBarDicts.Add("Spring", InitResourceDictionary("SpringIconBar"));
			specialIconBarDicts.Add("Summer", InitResourceDictionary("SummerIconBar"));
			specialIconBarDicts.Add("Autumn", InitResourceDictionary("AutumnIconBar"));
			specialIconBarDicts.Add("Winter", InitResourceDictionary("WinterIconBar"));
			immersePatchResource = InitResourceDictionary("ImmersePatch");
		}
	}

	private static ResourceDictionary InitResourceDictionary(string name, string path = "Resource\\ThemeCard\\")
	{
		return new ResourceDictionary
		{
			Source = new Uri(path + name + ".xaml", UriKind.Relative)
		};
	}

	private static void AddOrRemoveResource(ResourceDictionary resource, bool add)
	{
		if (App.Instance.Resources.MergedDictionaries.Contains(resource))
		{
			App.Instance.Resources.MergedDictionaries.Remove(resource);
		}
		if (add)
		{
			App.Instance.Resources.MergedDictionaries.Add(resource);
		}
	}

	public static void OnImmerseChanged(bool enable)
	{
		if (LocalSettings.Settings.ExtraSettings.InterfaceStyle == InterfaceStyle.Card && !ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId))
		{
			AddOrRemoveResource(immersePatchResource, enable);
		}
	}
}
