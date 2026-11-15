using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.Theme;

public class AutoDarkSettingWindow : Window, IComponentConnector
{
	private const string colorBlue = "#3957B6";

	private const string colorCyan = "#3F6696";

	private const string colorGreen = "#36786F";

	private const string colorYellow = "#937041";

	private const string colorPink = "#9C5B71";

	private const string colorPurple = "#6353A1";

	internal CheckBox UseSystemThemeCheckBox;

	internal ColorSelector DarkSelector;

	private bool _contentLoaded;

	private string initThemeId { get; set; }

	private bool initAutoDark { get; set; }

	private string initDarkCustomColor { get; set; }

	public AutoDarkSettingWindow()
	{
		InitializeComponent();
		initThemeId = LocalSettings.Settings.ThemeId;
		UseSystemThemeCheckBox.IsChecked = (initAutoDark = LocalSettings.Settings.ExtraSettings.UseSystemTheme);
		initDarkCustomColor = LocalSettings.Settings.ExtraSettings.CustomDarkThemeColor;
		DarkSelector.SetCustomColorList(new List<string> { "#3957B6", "#3F6696", "#36786F", "#937041", "#9C5B71", "#6353A1" });
		DarkSelector.SelectedColorStr = GetSelectedColorByTheme(LocalSettings.Settings.ExtraSettings.LastDarkTheme);
	}

	public string GetSelectedColorByTheme(string theme)
	{
		return theme switch
		{
			"DarkCyan" => "#3F6696", 
			"DarkGreen" => "#36786F", 
			"DarkYellow" => "#937041", 
			"DarkPink" => "#9C5B71", 
			"DarkPurple" => "#6353A1", 
			"DarkCustom" => LocalSettings.Settings.ExtraSettings.CustomDarkThemeColor, 
			_ => "#3957B6", 
		};
	}

	public string GetThemeKeyBySelectedColor(string color)
	{
		if (color == null)
		{
			goto IL_00b6;
		}
		if (color != null)
		{
			int length = color.Length;
			if (length == 0)
			{
				goto IL_00b6;
			}
			if (length == 7)
			{
				switch (color[4])
				{
				case '7':
					break;
				case '6':
					if (!(color == "#3F6696"))
					{
						goto IL_00da;
					}
					return "DarkCyan";
				case '8':
					if (!(color == "#36786F"))
					{
						goto IL_00da;
					}
					return "DarkGreen";
				case '0':
					if (!(color == "#937041"))
					{
						goto IL_00da;
					}
					return "DarkYellow";
				case 'B':
					if (!(color == "#9C5B71"))
					{
						goto IL_00da;
					}
					return "DarkPink";
				case '3':
					if (!(color == "#6353A1"))
					{
						goto IL_00da;
					}
					return "DarkPurple";
				default:
					goto IL_00da;
				}
				if (color == "#3957B6")
				{
					goto IL_00b6;
				}
			}
		}
		goto IL_00da;
		IL_00da:
		return "DarkCustom";
		IL_00b6:
		return "DarkBlue";
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		SwitchAutoDark(initAutoDark);
		string themeId = LocalSettings.Settings.ThemeId;
		string text = initThemeId;
		if (themeId != text || (text == "DarkCustom" && initDarkCustomColor != LocalSettings.Settings.ExtraSettings.CustomDarkThemeColor))
		{
			try
			{
				LocalSettings.Settings.ExtraSettings.CustomDarkThemeColor = initDarkCustomColor;
				App.Instance.LoadTheme(themeId, text);
			}
			catch (Exception ex)
			{
				UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			}
		}
		Close();
	}

	private void ConfirmButton_Click(object sender, RoutedEventArgs e)
	{
		string selectedColorStr = DarkSelector.SelectedColorStr;
		string themeKeyBySelectedColor = GetThemeKeyBySelectedColor(selectedColorStr);
		LocalSettings.Settings.ExtraSettings.LastDarkTheme = themeKeyBySelectedColor;
		if (ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId))
		{
			LocalSettings.Settings.ExtraSettings.AppTheme = themeKeyBySelectedColor;
			LocalSettings.Settings.ThemeId = themeKeyBySelectedColor;
		}
		UpdateTheme(selectedColorStr, force: true);
		DataChangedNotifier.NotifyChooseDarkThemeChanged();
		Close();
	}

	private void OnUseSystemClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			checkBox.IsChecked = !checkBox.IsChecked;
			SwitchAutoDark(checkBox.IsChecked ?? true);
		}
	}

	private void SwitchAutoDark(bool enable)
	{
		bool flag = LocalSettings.Settings.ExtraSettings.UseSystemTheme != enable;
		LocalSettings.Settings.ExtraSettings.UseSystemTheme = enable;
		LocalSettings.Settings.Save();
		if (LocalSettings.Settings.ExtraSettings.UseSystemTheme)
		{
			if (flag)
			{
				ThemeUtil.TrySetSystemTheme(onlyDark: true);
			}
		}
		else if (ThemeUtil.IsSystemDark() && !ThemeKey.IsDarkTheme(initThemeId))
		{
			App.Instance.LoadTargetTheme(initThemeId);
		}
	}

	private void DarkSelector_ColorSelect(object sender, string color)
	{
		UpdateTheme(color);
	}

	private void UpdateTheme(string color, bool force = false)
	{
		string themeKeyBySelectedColor = GetThemeKeyBySelectedColor(color);
		string themeId = LocalSettings.Settings.ThemeId;
		if (!ThemeKey.IsDarkTheme(themeId) || (!(themeId != themeKeyBySelectedColor || force) && (!(themeKeyBySelectedColor == "DarkCustom") || !(color != LocalSettings.Settings.ExtraSettings.CustomDarkThemeColor))))
		{
			return;
		}
		try
		{
			if (themeKeyBySelectedColor == "DarkCustom")
			{
				LocalSettings.Settings.ExtraSettings.CustomDarkThemeColor = color;
			}
			App.Instance.LoadTheme(themeId, themeKeyBySelectedColor);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/autodarksettingwindow.xaml", UriKind.Relative);
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
			UseSystemThemeCheckBox = (CheckBox)target;
			UseSystemThemeCheckBox.PreviewMouseLeftButtonUp += OnUseSystemClick;
			break;
		case 2:
			DarkSelector = (ColorSelector)target;
			break;
		case 3:
			((Button)target).Click += ConfirmButton_Click;
			break;
		case 4:
			((Button)target).Click += OnCloseClick;
			break;
		case 5:
			((Button)target).Click += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
