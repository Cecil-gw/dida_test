using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Theme;

public class CustomThemeDialog : Window, IComponentConnector, IStyleConnector
{
	private bool _useForCountdown;

	internal StackPanel CustomThemeGrid;

	internal TextBlock TitleText;

	internal ImageClipper ImageClipper;

	internal TextBlock ThemeText;

	internal Popup CustomThemeSetPopup;

	internal ItemsControl CustomThemes;

	internal Slider ImageOpacitySlider;

	internal Slider ImageBlurSlider;

	internal Slider ShowAreaOpacitySlider;

	internal System.Windows.Controls.Button ChooseImageBtn;

	private bool _contentLoaded;

	public event EventHandler<Rect> OnRectClip;

	public CustomThemeDialog(string imageUrl, string location = null, bool useForCountdown = false)
	{
		InitializeComponent();
		_useForCountdown = useForCountdown;
		if (!useForCountdown)
		{
			SetCustomBase();
		}
		ImageClipper.OnThemeColorsChanged += OnCustomThemeColorsChanged;
		ImageClipper.SetUseForCountdown(useForCountdown);
		ImageClipper.SetImage(imageUrl, location, !useForCountdown);
		base.InputBindings.Add(new KeyBinding(WindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		if (useForCountdown)
		{
			TitleText.Text = string.Empty;
			ThemeText.Visibility = Visibility.Collapsed;
			ChooseImageBtn.Visibility = Visibility.Collapsed;
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnThemeSetClick(object sender, MouseButtonEventArgs e)
	{
		CustomThemeSetPopup.IsOpen = true;
	}

	private void OnCustomThemeColorsChanged(object sender, List<SelectThemeColorViewModel> e)
	{
		if (!_useForCountdown)
		{
			SelectThemeColorViewModel selectThemeColorViewModel = e.FirstOrDefault((SelectThemeColorViewModel c) => c.Color == LocalSettings.Settings.CustomThemeColor);
			selectThemeColorViewModel = ((selectThemeColorViewModel == null) ? e[0] : selectThemeColorViewModel);
			LocalSettings.Settings.CustomThemeColor = selectThemeColorViewModel.Color;
			selectThemeColorViewModel.Selected = true;
			SetThemeColor(selectThemeColorViewModel.Color);
			CustomThemes.ItemsSource = e;
		}
	}

	private void OnThemeColorClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: SelectThemeColorViewModel dataContext }))
		{
			return;
		}
		if (CustomThemes.ItemsSource is List<SelectThemeColorViewModel> list)
		{
			list.ForEach(delegate(SelectThemeColorViewModel m)
			{
				m.Selected = false;
			});
		}
		dataContext.Selected = true;
		string color = dataContext.Color;
		SetThemeColor(color);
	}

	private void SetThemeColor(string color)
	{
		ResourceDictionary resourceDictionary = new ResourceDictionary();
		resourceDictionary["ProjectMenuBackGround"] = ThemeUtil.GetColorInString("#1A000000");
		if (ColorConverter.ConvertFromString(color) is Color color2)
		{
			resourceDictionary["ColorPrimary"] = color2;
			resourceDictionary["MainWindow"] = color2;
			resourceDictionary["PrimaryColor"] = new SolidColorBrush(color2);
			if (System.Windows.Application.Current.Resources["primary_blue_100"] is Color color3)
			{
				resourceDictionary["DateColorPrimary"] = new SolidColorBrush(color3);
			}
			else
			{
				resourceDictionary["DateColorPrimary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4772FA"));
			}
			resourceDictionary["KanbanContainerColor"] = new SolidColorBrush(color2)
			{
				Opacity = 0.1
			};
			resourceDictionary["CardBackColor"] = new SolidColorBrush(color2)
			{
				Opacity = 0.1
			};
		}
		if (ColorConverter.ConvertFromString("#CC" + color.Substring(color.Length - 6, 6)) is Color color4)
		{
			resourceDictionary["LeftBarBackColorTop"] = color4;
			resourceDictionary["LeftBarBackColorBottom"] = color4;
		}
		if (ColorConverter.ConvertFromString("#1E" + color.Substring(color.Length - 6, 6)) is Color color5)
		{
			resourceDictionary["ItemSelected"] = color5;
		}
		if (ColorConverter.ConvertFromString("#14" + color.Substring(color.Length - 6, 6)) is Color color6)
		{
			resourceDictionary["ItemHover"] = color6;
		}
		resourceDictionary["WindowBackColorTop"] = ColorConverter.ConvertFromString("#191919");
		resourceDictionary["WindowBackColorBottom"] = ColorConverter.ConvertFromString("#191919");
		App.Instance.Resources.MergedDictionaries.Add(new ResourceDictionary
		{
			Source = new Uri("Resource/Themes/LightNecessaryColor.xaml", UriKind.Relative)
		});
		App.Instance.Resources.MergedDictionaries.Add(resourceDictionary);
		LocalSettings.Settings.CustomThemeColor = color;
		CardThemeDictService.ReloadInterfaceStyle();
		ApplyShowAreaOpacityResources();
		LocalSettings.Settings.NotifyThemeChanged();
		DataChangedNotifier.NotifyThemeModeChanged();
	}

	private void OnImageOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		LocalSettings.Settings.ThemeImageOpacity = 0.6 + e.NewValue * 0.04;
	}

	private void OnImageBlurChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		LocalSettings.Settings.ThemeImageBlurRadius = (int)e.NewValue * 4;
	}

	private void OnShowAreaOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		LocalSettings.Settings.ShowAreaOpacity = 0.8 + e.NewValue * 0.02;
		ApplyShowAreaOpacityResources();
	}

	private void OnResetCustomImageClick(object sender, RoutedEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Multiselect = false,
			Filter = "IMAGE (*.JPG;*.JPEG;*.PJPEG;*.PNG;*.GIF;*.PJP;*.JFIF;*.BMP;*.TIFF;*.TIF;*.DIB;*.WEBP)|*.JPG;*.JPEG;*.PJPEG;*.PNG;*.GIF;*.PJP;*.JFIF;*.BMP;*.TIFF;*.TIF;*.DIB;*.WEBP",
			FilterIndex = 1
		};
		if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
		{
			return;
		}
		try
		{
			string fileName = openFileDialog.FileName;
			string text = fileName.Split('.').LastOrDefault();
			if (!string.IsNullOrEmpty(text))
			{
				string text2 = AppPaths.ThemeDir + "custom_bak." + text;
				string[] files = Directory.GetFiles(AppPaths.ThemeDir, "custom_bak.*");
				for (int i = 0; i < files.Length; i++)
				{
					FileInfo fileInfo = new FileInfo(files[i]);
					fileInfo.Attributes = FileAttributes.Normal;
					fileInfo.Delete();
				}
				File.Copy(fileName, text2);
				if (ImageAvailable(text2))
				{
					ResetCustomTheme();
					ImageClipper.SetImage(text2);
				}
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}

	private bool ImageAvailable(string saveImagePath)
	{
		return Utils.LoadBitmap(saveImagePath) != null;
	}

	private void ResetCustomTheme()
	{
		LocalSettings.Settings.ResetCustomTheme();
		ImageOpacitySlider.Value = (LocalSettings.Settings.ThemeImageOpacity - 0.6) * 25.0;
		ImageBlurSlider.Value = (double)LocalSettings.Settings.ThemeImageBlurRadius / 4.0;
		ShowAreaOpacitySlider.Value = (LocalSettings.Settings.ShowAreaOpacity - 0.8) * 50.0;
	}

	private void OnCustomSaveClick(object sender, RoutedEventArgs e)
	{
		if (_useForCountdown)
		{
			this.OnRectClip?.Invoke(this, ImageClipper.GetCurrentLocation());
		}
		else
		{
			try
			{
				string[] files = Directory.GetFiles(AppPaths.ThemeDir, "custom_bak.*");
				if (files.Length != 0)
				{
					string[] files2 = Directory.GetFiles(AppPaths.ThemeDir, "custom*.*");
					foreach (string text in files2)
					{
						if (!text.Contains("custom_bak"))
						{
							FileInfo fileInfo = new FileInfo(text);
							fileInfo.Attributes = FileAttributes.Normal;
							fileInfo.Delete();
						}
					}
					string obj = files[0];
					string destFileName = obj.Replace("custom_bak", "custom" + DateTime.Now.Ticks);
					File.Move(obj, destFileName);
				}
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex);
			}
			Rect currentLocation = ImageClipper.GetCurrentLocation();
			LocalSettings.Settings.CustomThemeLocation = currentLocation.Left + "," + currentLocation.Top + "," + currentLocation.Width + "," + currentLocation.Height;
			LocalSettings.Settings.SaveCustomTheme();
			if ("Custom" != LocalSettings.Settings.ExtraSettings.AppTheme)
			{
				UserActCollectUtils.AddClickEvent("settings", "theme", "Custom".ToLower());
				LocalSettings.Settings.ExtraSettings.AppTheme = "Custom";
			}
		}
		Close();
	}

	private void SetCustomBase()
	{
		LocalSettings.Settings.LoadCustomTheme();
		ImageOpacitySlider.Value = (LocalSettings.Settings.ThemeImageOpacity - 0.6) * 25.0;
		ImageBlurSlider.Value = (double)LocalSettings.Settings.ThemeImageBlurRadius / 4.0;
		ShowAreaOpacitySlider.Value = (LocalSettings.Settings.ShowAreaOpacity - 0.8) * 50.0;
		string themeId = LocalSettings.Settings.ThemeId;
		if (!ThemeKey.IsCustomTheme(themeId))
		{
			if (ThemeKey.IsDarkTheme(themeId))
			{
				App.Instance.Resources.MergedDictionaries.Add(new ResourceDictionary
				{
					Source = new Uri("Resource/Themes/Light.xaml", UriKind.Relative)
				});
				App.Instance.Resources.MergedDictionaries.Add(new ResourceDictionary
				{
					Source = new Uri("Resource/icons_light.xaml", UriKind.Relative)
				});
			}
			App.Instance.Resources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri("Resource/Themes/LightNecessaryColor.xaml", UriKind.Relative)
			});
			App.Instance.Resources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri("Resource/Themes/Custom.xaml", UriKind.Relative)
			});
			ResourceDictionary resourceDictionary = new ResourceDictionary();
			resourceDictionary["CardBackBottomColor"] = ThemeUtil.GetAlphaColor("#FFFFFF", (int)(LocalSettings.Settings.ShowAreaOpacity * 100.0));
			resourceDictionary["ShowAreaBottomColor"] = ThemeUtil.GetAlphaColor("#FFFFFF", (int)(LocalSettings.Settings.ShowAreaOpacity * 100.0));
			resourceDictionary["ParallelPartBackground"] = ThemeUtil.GetAlphaColor("#FFFFFF", (int)(LocalSettings.Settings.ShowAreaOpacity * 100.0));
			App.Instance.Resources.MergedDictionaries.Add(resourceDictionary);
			App.Instance.SetProjectMenuTextShadow(showShadow: true);
			LocalSettings.Settings.ThemeId = "Custom";
			if (ThemeKey.IsDarkTheme(themeId))
			{
				DataChangedNotifier.NotifyIsDarkChanged();
			}
		}
		ApplyShowAreaOpacityResources();
	}

	private void ApplyShowAreaOpacityResources()
	{
		ResourceDictionary resourceDictionary = new ResourceDictionary();
		resourceDictionary["CardBackBottomColor"] = ThemeUtil.GetAlphaColor("#FFFFFF", (int)(LocalSettings.Settings.ShowAreaOpacity * 100.0));
		resourceDictionary["ShowAreaBottomColor"] = ThemeUtil.GetAlphaColor("#FFFFFF", (int)(LocalSettings.Settings.ShowAreaOpacity * 100.0));
		resourceDictionary["ParallelPartBackground"] = ThemeUtil.GetAlphaColor("#FFFFFF", (int)(LocalSettings.Settings.ShowAreaOpacity * 100.0));
		App.Instance.Resources.MergedDictionaries.Add(resourceDictionary);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/customthemedialog.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			CustomThemeGrid = (StackPanel)target;
			break;
		case 2:
			TitleText = (TextBlock)target;
			break;
		case 3:
			ImageClipper = (ImageClipper)target;
			break;
		case 4:
			ThemeText = (TextBlock)target;
			ThemeText.MouseLeftButtonUp += OnThemeSetClick;
			break;
		case 5:
			CustomThemeSetPopup = (Popup)target;
			break;
		case 6:
			CustomThemes = (ItemsControl)target;
			break;
		case 8:
			ImageOpacitySlider = (Slider)target;
			ImageOpacitySlider.ValueChanged += OnImageOpacityChanged;
			break;
		case 9:
			ImageBlurSlider = (Slider)target;
			ImageBlurSlider.ValueChanged += OnImageBlurChanged;
			break;
		case 10:
			ShowAreaOpacitySlider = (Slider)target;
			ShowAreaOpacitySlider.ValueChanged += OnShowAreaOpacityChanged;
			break;
		case 11:
			ChooseImageBtn = (System.Windows.Controls.Button)target;
			ChooseImageBtn.Click += OnResetCustomImageClick;
			break;
		case 12:
			((System.Windows.Controls.Button)target).Click += OnCustomSaveClick;
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
		if (connectionId == 7)
		{
			((Grid)target).MouseLeftButtonUp += OnThemeColorClick;
		}
	}
}
