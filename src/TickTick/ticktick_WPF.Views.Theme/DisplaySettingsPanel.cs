using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Notifier;

namespace ticktick_WPF.Views.Theme;

public class DisplaySettingsPanel : UserControl, IComponentConnector, IStyleConnector
{
	internal TextBlock Title;

	internal ItemsControl SetNumItems;

	private bool _contentLoaded;

	public DisplaySettingsPanel()
	{
		InitializeComponent();
		base.Loaded += delegate
		{
			DataChangedNotifier.IsDarkChanged += OnIsDarkChanged;
		};
		base.Unloaded += delegate
		{
			DataChangedNotifier.IsDarkChanged -= OnIsDarkChanged;
		};
	}

	private void OnIsDarkChanged(object sender, EventArgs e)
	{
		if (!(SetNumItems.ItemsSource is ObservableCollection<DisplaySettingsViewModel> observableCollection))
		{
			return;
		}
		foreach (DisplaySettingsViewModel item in observableCollection)
		{
			item.ChangeImage();
		}
	}

	private async void OnItemSelect(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: DisplaySettingsViewModel dataContext })
		{
			TrySetNumDisplayType(dataContext);
		}
	}

	private async Task TrySetNumDisplayType(DisplaySettingsViewModel model)
	{
		if (SetNumItems.ItemsSource is ObservableCollection<DisplaySettingsViewModel> observableCollection)
		{
			foreach (DisplaySettingsViewModel item in observableCollection)
			{
				if (item.Selected && item.Type != model.Type)
				{
					item.Selected = false;
				}
			}
		}
		model.Selected = true;
		OnModelSelected(model);
	}

	protected virtual void OnModelSelected(DisplaySettingsViewModel model)
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/displaysettingspanel.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Title = (TextBlock)target;
			break;
		case 2:
			SetNumItems = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 3)
		{
			((Border)target).MouseLeftButtonUp += OnItemSelect;
		}
	}
}
