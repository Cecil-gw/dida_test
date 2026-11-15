using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Theme;

public class PureColorGrid : UserControl, IComponentConnector
{
	private bool _contentLoaded;

	public PureColorGrid()
	{
		InitializeComponent();
	}

	private async void OnGridClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext == null || !(base.DataContext is ThemeBaseModel themeBaseModel))
		{
			return;
		}
		themeBaseModel.SetSelected();
		string themeId = LocalSettings.Settings.ThemeId;
		if (themeId != themeBaseModel.Key)
		{
			try
			{
				App.Instance.LoadTheme(themeId, themeBaseModel.Key);
				LocalSettings.Settings.ExtraSettings.SelectTheme = themeBaseModel.Key;
				LocalSettings.Settings.ExtraSettings.AppTheme = themeBaseModel.Key;
				LocalSettings.Settings.ThemeId = themeBaseModel.Key;
				UserActCollectUtils.AddClickEvent("settings", "theme", ThemeKey.GetCollectLabel(themeBaseModel.Key));
			}
			catch (Exception ex)
			{
				UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/purecolorgrid.xaml", UriKind.Relative);
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
			((PureColorGrid)target).MouseLeftButtonUp += OnGridClick;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
