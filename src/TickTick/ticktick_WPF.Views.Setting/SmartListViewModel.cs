using System.Collections.Generic;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Setting;

public class SmartListViewModel : BaseViewModel
{
	public SmartListType Type { get; set; }

	public int SelectedItemIndex { get; set; }

	public string Title { get; set; }

	public Geometry IconData { get; set; }

	public List<string> ShowStatusItems { get; set; }

	public SmartListViewModel(SmartListType type, bool withAuto = true)
	{
		Type = type;
		switch (type)
		{
		case SmartListType.Week:
			Title = Utils.GetString("Next7Day");
			break;
		case SmartListType.Assign:
			Title = Utils.GetString("AssignToMe");
			break;
		case SmartListType.Abandoned:
			Title = Utils.GetString("Abandoned");
			break;
		case SmartListType.Tag:
			Title = Utils.GetString("Tags");
			break;
		case SmartListType.Filter:
			Title = Utils.GetString("Filters");
			break;
		default:
			Title = Utils.GetString(type.ToString());
			break;
		}
		IconData = SpecialListUtils.GetIconBySmartType(type);
		ShowStatus selectedIndex = (ShowStatus)GetSelectedIndex(type);
		string item = Utils.GetString((type == SmartListType.Tag) ? "ShowOmitEmptyTags" : "ShowIfNotEmpty");
		if (withAuto)
		{
			SelectedItemIndex = selectedIndex switch
			{
				ShowStatus.Hide => 2, 
				ShowStatus.Show => 0, 
				_ => 1, 
			};
			ShowStatusItems = new List<string>
			{
				Utils.GetString("Show"),
				item,
				Utils.GetString("Hide")
			};
		}
		else
		{
			SelectedItemIndex = ((selectedIndex != ShowStatus.Show) ? 1 : 0);
			ShowStatusItems = new List<string>
			{
				Utils.GetString("Show"),
				Utils.GetString("Hide")
			};
		}
	}

	private static int GetSelectedIndex(SmartListType type)
	{
		LocalSettings settings = LocalSettings.Settings;
		return type switch
		{
			SmartListType.All => settings.SmartListAll, 
			SmartListType.Today => settings.SmartListToday, 
			SmartListType.Tomorrow => settings.SmartListTomorrow, 
			SmartListType.Week => settings.SmartList7Day, 
			SmartListType.Assign => settings.SmartListForMe, 
			SmartListType.Completed => settings.SmartListComplete, 
			SmartListType.Abandoned => settings.SmartListAbandoned, 
			SmartListType.Trash => settings.SmartListTrash, 
			SmartListType.Summary => settings.SmartListSummary, 
			SmartListType.Inbox => settings.SmartListInbox, 
			SmartListType.Tag => settings.SmartListTag, 
			SmartListType.Filter => settings.ShowCustomSmartList, 
			_ => 0, 
		};
	}
}
