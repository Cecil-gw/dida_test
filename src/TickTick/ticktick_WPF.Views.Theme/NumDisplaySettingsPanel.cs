using ticktick_WPF.Cache;
using ticktick_WPF.Framework.Collections;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Theme;

public class NumDisplaySettingsPanel : DisplaySettingsPanel
{
	public NumDisplaySettingsPanel()
	{
		Title.Text = Utils.GetString("SidebarCount");
		SetNumItems.ItemsSource = new ExtObservableCollection<DisplaySettingsViewModel>
		{
			new DisplaySettingsViewModel(0, "ShowNum", Utils.GetString("ShowAll"))
			{
				Selected = (LocalSettings.Settings.ExtraSettings.NumDisplayType == 0)
			},
			new DisplaySettingsViewModel(1, "HideNoteNum", Utils.GetString("HideNoteNum"))
			{
				Selected = (LocalSettings.Settings.ExtraSettings.NumDisplayType == 1)
			},
			new DisplaySettingsViewModel(2, "HideNum", Utils.GetString("HideAll"))
			{
				Selected = (LocalSettings.Settings.ExtraSettings.NumDisplayType == 2)
			}
		};
	}

	protected override void OnModelSelected(DisplaySettingsViewModel model)
	{
		LocalSettings.Settings.ExtraSettings.NumDisplayType = model.Type;
		TaskCountCache.SetNeedLoad();
		LocalSettings.Settings.NotifyPropertyChanged("ProjectNum");
	}
}
