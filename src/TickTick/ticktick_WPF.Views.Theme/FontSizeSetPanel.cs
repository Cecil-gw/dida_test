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
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Theme;

public class FontSizeSetPanel : UserControl, IComponentConnector, IStyleConnector
{
	internal ItemsControl FontSizeItems;

	private bool _contentLoaded;

	public FontSizeSetPanel()
	{
		InitializeComponent();
		FontSizeItems.ItemsSource = FontSizeViewModel.BuildModels();
	}

	private async void OnFontSizeSelect(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: FontSizeViewModel dataContext })
		{
			TrySetFontSize(dataContext);
		}
	}

	private async Task TrySetFontSize(FontSizeViewModel model)
	{
		if (FontSizeItems.ItemsSource is ObservableCollection<FontSizeViewModel> observableCollection)
		{
			foreach (FontSizeViewModel item in observableCollection)
			{
				if (item.Selected && item.Size != model.Size)
				{
					item.Selected = false;
				}
			}
		}
		model.Selected = true;
		LocalSettings.Settings.BaseFontSize = model.Size;
		switch (model.Size)
		{
		case 14:
			CalendarGeoHelper.SetFontSize(2);
			break;
		case 16:
			CalendarGeoHelper.SetFontSize(3);
			break;
		case 18:
			CalendarGeoHelper.SetFontSize(5);
			break;
		}
		App.SetFontSize();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/fontsizesetpanel.xaml", UriKind.Relative);
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
			FontSizeItems = (ItemsControl)target;
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
			((Border)target).MouseLeftButtonUp += OnFontSizeSelect;
		}
	}
}
