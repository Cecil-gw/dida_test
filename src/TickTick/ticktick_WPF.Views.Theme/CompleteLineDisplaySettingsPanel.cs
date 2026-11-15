using ticktick_WPF.Framework.Collections;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Theme;

public class CompleteLineDisplaySettingsPanel : DisplaySettingsPanel
{
	public CompleteLineDisplaySettingsPanel()
	{
		Title.Text = Utils.GetString("CompletedTaskStyle");
		SetNumItems.ItemsSource = new ExtObservableCollection<DisplaySettingsViewModel>
		{
			new DisplaySettingsViewModel(0, "HideCompleteLine", Utils.GetString("Standard"))
			{
				Selected = (LocalSettings.Settings.ExtraSettings.ShowCompleteLine == 0)
			},
			new DisplaySettingsViewModel(1, "ShowCompleteLine", Utils.GetString("Strikethrough"))
			{
				Selected = (LocalSettings.Settings.ExtraSettings.ShowCompleteLine == 1)
			}
		};
	}

	protected override void OnModelSelected(DisplaySettingsViewModel model)
	{
		LocalSettings.Settings.ShowCompleteLine = model.Type;
		DataChangedNotifier.NotifyShowCompleteLineChanged();
	}
}
