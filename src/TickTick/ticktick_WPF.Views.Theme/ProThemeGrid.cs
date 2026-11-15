using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Files;

namespace ticktick_WPF.Views.Theme;

public class ProThemeGrid : UserControl, IComponentConnector
{
	internal Border BackImage;

	private bool _contentLoaded;

	public ProThemeGrid()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext != null && base.DataContext is ProThemeViewModel proThemeViewModel)
		{
			string text = proThemeViewModel.Key.ToLower() + ".png";
			BackImage.Background = new ImageBrush
			{
				ImageSource = ImageUtils.GetResourceImage("pack://application:,,,/Assets/ImageSource/themePreview/" + text)
			};
		}
	}

	private async void OnGridClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (!(dataContext is ProThemeViewModel model) || !ProChecker.CheckPro(ProType.PremiumThemes))
		{
			return;
		}
		model.Loading = true;
		if (!(await FileUtils.TryDownloadBackground(model.Key.ToLower() + ".png")))
		{
			Utils.FindParent<AppearanceControl>(this)?.Toast(Utils.GetString("NetworkDownloadTheme"));
			model.Loading = false;
			return;
		}
		model.SetSelected();
		string themeId = LocalSettings.Settings.ThemeId;
		if (themeId != model.Key)
		{
			try
			{
				App.Instance.LoadTheme(themeId, model.Key);
				LocalSettings.Settings.ExtraSettings.SelectTheme = model.Key;
				LocalSettings.Settings.ExtraSettings.AppTheme = model.Key;
				LocalSettings.Settings.ThemeId = model.Key;
				UserActCollectUtils.AddClickEvent("settings", "theme", ThemeKey.GetCollectLabel(model.Key));
			}
			catch (Exception ex)
			{
				UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			}
		}
		model.Loading = false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/prothemegrid.xaml", UriKind.Relative);
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
			((ProThemeGrid)target).MouseLeftButtonUp += OnGridClick;
			break;
		case 2:
			BackImage = (Border)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
