using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Theme;

public class CompleteLineDisplaySetPanel : UserControl, IComponentConnector, IStyleConnector
{
	internal ItemsControl MainItems;

	private bool _contentLoaded;

	private ObservableCollection<DisplaySettingsViewModel> models { get; set; } = new ObservableCollection<DisplaySettingsViewModel>();

	public CompleteLineDisplaySetPanel()
	{
		InitializeComponent();
		MainItems.ItemsSource = models;
		models.Add(new DisplaySettingsViewModel(0, "HideCompleteLine", Utils.GetString("Standard"))
		{
			Selected = (LocalSettings.Settings.ExtraSettings.ShowCompleteLine == 0)
		});
		models.Add(new DisplaySettingsViewModel(1, "ShowCompleteLine", Utils.GetString("Strikethrough"))
		{
			Selected = (LocalSettings.Settings.ExtraSettings.ShowCompleteLine == 1)
		});
	}

	private void OnItemSelect(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: DisplaySettingsViewModel dataContext }))
		{
			return;
		}
		foreach (DisplaySettingsViewModel model in models)
		{
			if (model.Selected && model.Type != dataContext.Type)
			{
				model.Selected = false;
			}
		}
		dataContext.Selected = true;
		OnModelSelected(dataContext);
	}

	private void OnModelSelected(DisplaySettingsViewModel model)
	{
		LocalSettings.Settings.ShowCompleteLine = model.Type;
		DataChangedNotifier.NotifyShowCompleteLineChanged();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/completelinedisplaysetpanel.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			MainItems = (ItemsControl)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			((Grid)target).MouseLeftButtonUp += OnItemSelect;
		}
	}
}
