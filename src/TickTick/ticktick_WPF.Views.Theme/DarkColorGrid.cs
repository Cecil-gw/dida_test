using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.Theme;

public class DarkColorGrid : UserControl, IComponentConnector, IStyleConnector
{
	private INotifyPropertyChanged? _currentDataContext;

	internal DarkColorGrid Root;

	internal StackPanel ChoosePoints;

	internal Popup ChoosePopup;

	internal ItemsControl ChooseItemsControl;

	internal Image CustomColorView;

	internal Grid CustomColorSelectView;

	internal Border SelectedCustomBorder;

	internal Popup CustomColorPopup;

	internal ColorItemSelector CustomSelector;

	private bool _contentLoaded;

	private ObservableCollection<ColorThemeViewModel> colors { get; set; }

	public DarkColorGrid()
	{
		InitializeComponent();
		colors = new ObservableCollection<ColorThemeViewModel>
		{
			new ColorThemeViewModel
			{
				Key = "DarkBlue"
			},
			new ColorThemeViewModel
			{
				Key = "DarkCyan"
			},
			new ColorThemeViewModel
			{
				Key = "DarkGreen"
			},
			new ColorThemeViewModel
			{
				Key = "DarkYellow"
			},
			new ColorThemeViewModel
			{
				Key = "DarkPink"
			},
			new ColorThemeViewModel
			{
				Key = "DarkPurple"
			}
		};
		foreach (ColorThemeViewModel color in colors)
		{
			color.IsDark = true;
			color.Color = ThemeUtil.GetColor("Theme" + color.Key);
		}
		ChooseItemsControl.ItemsSource = colors;
		LoadState();
	}

	public void LoadState()
	{
		string text = LocalSettings.Settings.ExtraSettings.LastDarkTheme;
		if (string.IsNullOrEmpty(text))
		{
			string appTheme = LocalSettings.Settings.ExtraSettings.AppTheme;
			if (ThemeKey.IsDarkTheme(appTheme))
			{
				text = appTheme;
			}
			if (text == "Dark")
			{
				text = "DarkBlue";
			}
		}
		foreach (ColorThemeViewModel color in colors)
		{
			color.Selected = text == color.Key;
		}
		if (text == "DarkCustom")
		{
			SwitchCustomSelectState(selected: true);
			SelectedCustomBorder.Background = ThemeUtil.GetColorInString(LocalSettings.Settings.ExtraSettings.CustomDarkThemeColor);
			CustomSelector.SetItemSelected(LocalSettings.Settings.ExtraSettings.CustomDarkThemeColor);
		}
		else
		{
			SwitchCustomSelectState(selected: false);
		}
		UpdatePointColor();
	}

	private void Root_Loaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.ChooseDarkThemeChanged += DataChangedNotifier_ChooseDarkThemeChanged;
	}

	private void Root_Unloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.ChooseDarkThemeChanged -= DataChangedNotifier_ChooseDarkThemeChanged;
	}

	private void DataChangedNotifier_ChooseDarkThemeChanged(object sender, EventArgs e)
	{
		LoadState();
	}

	private async void OnGridClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext == null || !(base.DataContext is ThemeBaseModel themeBaseModel))
		{
			return;
		}
		themeBaseModel.SetSelected();
		string themeId = LocalSettings.Settings.ThemeId;
		string text = LocalSettings.Settings.ExtraSettings.LastDarkTheme ?? "DarkBlue";
		if (themeId != text)
		{
			try
			{
				App.Instance.LoadTheme(themeId, text);
				LocalSettings.Settings.ExtraSettings.AppTheme = text;
				LocalSettings.Settings.ThemeId = text;
				LocalSettings.Settings.ExtraSettings.SelectTheme = text;
				UserActCollectUtils.AddClickEvent("settings", "theme", ThemeKey.GetCollectLabel(text));
				DataChangedNotifier.NotifyChooseDarkThemeChanged();
			}
			catch (Exception ex)
			{
				UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			}
		}
	}

	private async void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: ColorThemeViewModel dataContext }))
		{
			return;
		}
		if (colors != null)
		{
			foreach (ColorThemeViewModel color in colors)
			{
				color.Selected = color == dataContext;
			}
		}
		(base.DataContext as ThemeBaseModel)?.SetSelected();
		SwitchCustomSelectState(selected: false);
		string themeId = LocalSettings.Settings.ThemeId;
		string key = dataContext.Key;
		if (themeId != key)
		{
			try
			{
				LocalSettings.Settings.ExtraSettings.LastDarkTheme = key;
				LocalSettings.Settings.ExtraSettings.AppTheme = key;
				LocalSettings.Settings.ExtraSettings.SelectTheme = key;
				LocalSettings.Settings.ThemeId = key;
				App.Instance.LoadTheme(themeId, key);
				UserActCollectUtils.AddClickEvent("settings", "theme", ThemeKey.GetCollectLabel(key));
			}
			catch (Exception ex)
			{
				UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			}
		}
		DataChangedNotifier.NotifyChooseDarkThemeChanged();
		ChoosePopup.IsOpen = false;
	}

	private void Root_MouseEnter(object sender, MouseEventArgs e)
	{
		ChoosePopup.IsOpen = true;
	}

	private async void Root_MouseLeave(object sender, MouseEventArgs e)
	{
		await CheckClosePopup();
	}

	private async void ChoosePopup_MouseLeave(object sender, MouseEventArgs e)
	{
		await CheckClosePopup();
	}

	private void ChoosePopup_Closed(object sender, EventArgs e)
	{
		ChoosePopup.IsOpen = false;
	}

	public void ClearItemSelect()
	{
		if (colors == null)
		{
			return;
		}
		foreach (ColorThemeViewModel color in colors)
		{
			color.Selected = false;
		}
	}

	private void SwitchCustomSelectState(bool selected)
	{
		if (selected)
		{
			CustomColorSelectView.Visibility = Visibility.Visible;
			CustomColorView.Visibility = Visibility.Collapsed;
		}
		else
		{
			CustomColorSelectView.Visibility = Visibility.Collapsed;
			CustomColorView.Visibility = Visibility.Visible;
			CustomSelector.SetItemSelected(null);
		}
	}

	private void CustomColor_ColorSelect(object sender, string color)
	{
		(base.DataContext as ThemeBaseModel)?.SetSelected();
		ClearItemSelect();
		SwitchCustomSelectState(selected: true);
		try
		{
			string themeId = LocalSettings.Settings.ThemeId;
			string text = "DarkCustom";
			LocalSettings.Settings.ExtraSettings.CustomDarkThemeColor = color;
			LocalSettings.Settings.ExtraSettings.LastDarkTheme = text;
			LocalSettings.Settings.ExtraSettings.AppTheme = text;
			LocalSettings.Settings.ExtraSettings.SelectTheme = text;
			LocalSettings.Settings.ThemeId = text;
			App.Instance.LoadTheme(themeId, text);
			UserActCollectUtils.AddClickEvent("settings", "theme", ThemeKey.GetCollectLabel(text));
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		DataChangedNotifier.NotifyChooseDarkThemeChanged();
	}

	private void CustomGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		CustomColorPopup.IsOpen = true;
	}

	private async void CustomColorPopup_Closed(object sender, EventArgs e)
	{
		CustomColorPopup.IsOpen = false;
		await CheckClosePopup();
	}

	private async Task CheckClosePopup()
	{
		await Task.Delay(200);
		if (!base.IsMouseOver && !ChoosePopup.IsMouseOver && !CustomColorPopup.IsMouseOver)
		{
			ChoosePopup.IsOpen = false;
		}
	}

	private void UpdatePointColor()
	{
		string text = LocalSettings.Settings.ThemeId;
		if (!ThemeKey.IsDarkTheme(text))
		{
			text = LocalSettings.Settings.ExtraSettings.LastDarkTheme;
		}
		SolidColorBrush value = ThemeUtil.GetColor("ThemeDarkBlue");
		if (!string.IsNullOrEmpty(text))
		{
			if (text == "Dark")
			{
				text = "DarkBlue";
			}
			value = ((!(text != "DarkCustom")) ? ThemeUtil.GetColorInString(LocalSettings.Settings.ExtraSettings.CustomDarkThemeColor) : ThemeUtil.GetColor("Theme" + text));
		}
		base.Resources["PointsFill"] = value;
	}

	private void Root_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		e.Handled = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/darkcolorgrid.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (DarkColorGrid)target;
			Root.MouseEnter += Root_MouseEnter;
			Root.MouseLeave += Root_MouseLeave;
			Root.PreviewMouseWheel += Root_PreviewMouseWheel;
			Root.Loaded += Root_Loaded;
			Root.Unloaded += Root_Unloaded;
			break;
		case 2:
			((Border)target).PreviewMouseLeftButtonUp += OnGridClick;
			break;
		case 3:
			ChoosePoints = (StackPanel)target;
			break;
		case 4:
			ChoosePopup = (Popup)target;
			ChoosePopup.Closed += ChoosePopup_Closed;
			ChoosePopup.MouseLeave += ChoosePopup_MouseLeave;
			break;
		case 5:
			ChooseItemsControl = (ItemsControl)target;
			break;
		case 7:
			((Grid)target).PreviewMouseLeftButtonUp += CustomGrid_PreviewMouseLeftButtonUp;
			break;
		case 8:
			CustomColorView = (Image)target;
			break;
		case 9:
			CustomColorSelectView = (Grid)target;
			break;
		case 10:
			SelectedCustomBorder = (Border)target;
			break;
		case 11:
			CustomColorPopup = (Popup)target;
			CustomColorPopup.Closed += CustomColorPopup_Closed;
			break;
		case 12:
			CustomSelector = (ColorItemSelector)target;
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
		if (connectionId == 6)
		{
			((Grid)target).MouseLeftButtonUp += OnItemClick;
		}
	}
}
