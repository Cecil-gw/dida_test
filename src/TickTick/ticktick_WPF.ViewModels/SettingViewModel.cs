using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class SettingViewModel : BaseViewModel
{
	public ObservableCollection<SettingMenuItem> MenuTitleList { get; set; }

	public SettingViewModel(SettingsType settingsType)
	{
		ObservableCollection<SettingMenuItem> menuTitleList = new ObservableCollection<SettingMenuItem>
		{
			new SettingMenuItem(),
			new SettingMenuItem("FeatureModule", SettingsType.Feature)
			{
				IsSelected = (settingsType == SettingsType.Feature)
			},
			new SettingMenuItem("SmartList", SettingsType.SmartList),
			new SettingMenuItem("Notification", SettingsType.Notification),
			new SettingMenuItem("DateAndTime", SettingsType.DateTime),
			new SettingMenuItem("Appearance", SettingsType.Theme)
			{
				IsSelected = (settingsType == SettingsType.Theme)
			},
			new SettingMenuItem("AIFeatures", SettingsType.AIFeatures)
			{
				IsSelected = (settingsType == SettingsType.AIFeatures)
			},
			new SettingMenuItem("MoreSettings", SettingsType.More)
			{
				IsSelected = (settingsType == SettingsType.More)
			},
			new SettingMenuItem(),
			new SettingMenuItem("IntegrationsAndImport", SettingsType.Calendar)
			{
				IsSelected = (settingsType == SettingsType.Calendar)
			},
			new SettingMenuItem("Collaborate", SettingsType.Share),
			new SettingMenuItem("StickyNote", SettingsType.StickyNote),
			new SettingMenuItem("DesktopWidgets", SettingsType.Widget),
			new SettingMenuItem("Shortcut", SettingsType.Shortcuts),
			new SettingMenuItem(),
			new SettingMenuItem("About", SettingsType.About)
		};
		MenuTitleList = menuTitleList;
	}

	public static void ExpandSubItems(IList<SettingMenuItem> data, SettingMenuItem model)
	{
		int index = data.IndexOf(model) + 1;
		for (int num = model.Children.Count - 1; num >= 0; num--)
		{
			SettingMenuItem item = model.Children[num];
			data.Insert(index, item);
		}
	}

	public static void RemoveSubItems(ObservableCollection<SettingMenuItem> data)
	{
		SettingMenuItem settingMenuItem = data.FirstOrDefault((SettingMenuItem item) => item.Type == SettingsType.Preferences);
		if (settingMenuItem == null)
		{
			return;
		}
		settingMenuItem.Expanded = false;
		foreach (SettingMenuItem child in settingMenuItem.Children)
		{
			data.Remove(child);
		}
	}
}
