using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Theme;

public class AppearanceControl : System.Windows.Controls.UserControl, IComponentConnector
{
	internal TextBlock CustomTitle;

	internal AnimateGroupTitle AppearanceTitle;

	internal Grid ThemeSelectGrid;

	internal ScrollViewer Scroller;

	internal StackPanel ThemePanel;

	internal TextBlock InterfaceStyleText;

	internal TextBlock AutoDarkText;

	internal ItemsControl PureColorItems;

	internal ItemsControl SeasonItems;

	internal ItemsControl CityItems;

	internal ItemsControl PhotoItems;

	internal StackPanel CustomPanel;

	internal Border BackImage;

	internal ItemsControl AppIcons;

	internal StackPanel DisplayPanel;

	internal ContentControl FontFamilyCtrl;

	internal ContentControl FontSizeCtrl;

	internal ContentControl ProjectListColorVisCtrl;

	internal ContentControl ProjectNumSetCtrl;

	internal ContentControl CompleteLineSetCtrl;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	private bool _contentLoaded;

	public AppearanceControl()
	{
		InitializeComponent();
		InitThemeData();
		InitIconData();
		base.Loaded += OnLoaded;
		base.Loaded += delegate
		{
			Scroller.ScrollToTop();
		};
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (Utils.IsZhCn() && Utils.IsDida())
		{
			FontFamilyCtrl.Content = new FontFamilySetPanel(FontFamilyViewModel.GetChineseFontViewModel());
		}
		else if (Utils.IsEn() && !Utils.IsDida())
		{
			FontFamilyCtrl.Content = new FontFamilySetPanel(FontFamilyViewModel.GetEnFontViewModel());
		}
		FontSizeCtrl.Content = new FontSizeSetPanel();
		ProjectListColorVisCtrl.Content = new ProjectListColorVisSetPanel();
		ProjectNumSetCtrl.Content = new NumDisplaySetPanel();
		CompleteLineSetCtrl.Content = new CompleteLineDisplaySetPanel();
		base.Loaded -= OnLoaded;
	}

	private void InitThemeData()
	{
		List<ThemeBaseModel> items = new List<ThemeBaseModel>();
		List<ColorThemeViewModel> list = new List<ColorThemeViewModel>
		{
			new ColorThemeViewModel
			{
				Key = "White",
				IsWhite = true
			},
			new ColorThemeViewModel
			{
				Key = "Blue"
			},
			new ColorThemeViewModel
			{
				Key = "Mint"
			},
			new ColorThemeViewModel
			{
				Key = "SeaBlizz"
			},
			new ColorThemeViewModel
			{
				Key = "Matcha"
			},
			new ColorThemeViewModel
			{
				Key = "Sunshine"
			},
			new ColorThemeViewModel
			{
				Key = "Peach"
			},
			new ColorThemeViewModel
			{
				Key = "Lilac"
			},
			new ColorThemeViewModel
			{
				Key = "Ebony"
			},
			new ColorThemeViewModel
			{
				Key = "Navy"
			},
			new ColorThemeViewModel
			{
				Key = "Gray"
			},
			new ColorThemeViewModel
			{
				Key = "Dark",
				IsDark = true
			}
		};
		List<ProThemeViewModel> list2 = new List<ProThemeViewModel>
		{
			new ProThemeViewModel
			{
				Key = "Spring"
			},
			new ProThemeViewModel
			{
				Key = "Summer"
			},
			new ProThemeViewModel
			{
				Key = "Autumn"
			},
			new ProThemeViewModel
			{
				Key = "Winter"
			}
		};
		List<ProThemeViewModel> list3 = new List<ProThemeViewModel>
		{
			new ProThemeViewModel
			{
				Key = "London"
			},
			new ProThemeViewModel
			{
				Key = "Moscow"
			},
			new ProThemeViewModel
			{
				Key = "Sanfrancisco"
			},
			new ProThemeViewModel
			{
				Key = "Seoul"
			},
			new ProThemeViewModel
			{
				Key = "Shanghai"
			},
			new ProThemeViewModel
			{
				Key = "Sydney"
			},
			new ProThemeViewModel
			{
				Key = "Tokyo"
			},
			new ProThemeViewModel
			{
				Key = "NewYork"
			}
		};
		list3.Add(new ProThemeViewModel
		{
			Key = "Guangzhou"
		});
		list3.Add(new ProThemeViewModel
		{
			Key = "Shenzhen"
		});
		list3.Insert(0, new ProThemeViewModel
		{
			Key = "Hangzhou"
		});
		list3.Insert(0, new ProThemeViewModel
		{
			Key = "Beijing"
		});
		List<ProThemeViewModel> list4 = new List<ProThemeViewModel>
		{
			new ProThemeViewModel
			{
				Key = "Structure"
			},
			new ProThemeViewModel
			{
				Key = "Blacksea"
			},
			new ProThemeViewModel
			{
				Key = "Desert"
			},
			new ProThemeViewModel
			{
				Key = "Leaves"
			},
			new ProThemeViewModel
			{
				Key = "Birds"
			},
			new ProThemeViewModel
			{
				Key = "Dawn"
			},
			new ProThemeViewModel
			{
				Key = "Blossom"
			},
			new ProThemeViewModel
			{
				Key = "Frozen"
			},
			new ProThemeViewModel
			{
				Key = "Meadow"
			},
			new ProThemeViewModel
			{
				Key = "Silence"
			}
		};
		items.AddRange(list);
		items.AddRange(list2);
		items.AddRange(list3);
		items.AddRange(list4);
		string currentTheme = ThemeUtil.GetThemeKey(LocalSettings.Settings.ExtraSettings.AppTheme);
		items.ForEach(delegate(ThemeBaseModel item)
		{
			item.Parent = items;
			item.Selected = item.Key == currentTheme;
			if (item is ColorThemeViewModel { IsDark: not false })
			{
				item.Selected = ThemeKey.IsDarkTheme(currentTheme);
			}
			item.Color = (ThemeKey.IsGradientTheme(item.Key) ? GetGradientPreviewColor(item.Key) : ThemeUtil.GetColor("Theme" + item.Key));
			if (ThemeKey.IsGradientTheme(item.Key) && item.Key != "Gray" && item.Key != "Blue")
			{
				item.Name = Utils.GetString("Theme" + item.Key);
			}
			else
			{
				item.Name = Utils.GetString((item.Key == "Blue") ? "ThemeSkyBlue" : item.Key);
			}
		});
		CustomThemeViewModel customThemeViewModel = new CustomThemeViewModel
		{
			Parent = items
		};
		items.Add(customThemeViewModel);
		CustomPanel.DataContext = customThemeViewModel;
		PureColorItems.ItemsSource = list;
		SeasonItems.ItemsSource = list2;
		CityItems.ItemsSource = list3;
		PhotoItems.ItemsSource = list4;
		UpdateInterfaceStyleText();
		UpdateAutoDarkText();
	}

	private void UpdateInterfaceStyleText()
	{
		switch (LocalSettings.Settings.ExtraSettings.InterfaceStyle)
		{
		case InterfaceStyle.Flat:
			InterfaceStyleText.Text = Utils.GetString("FlatStyle");
			break;
		case InterfaceStyle.Card:
			InterfaceStyleText.Text = Utils.GetString("CardStyle");
			break;
		}
	}

	private void UpdateAutoDarkText()
	{
		if (LocalSettings.Settings.ExtraSettings.UseSystemTheme)
		{
			AutoDarkText.Text = Utils.GetString("Open");
		}
		else
		{
			AutoDarkText.Text = Utils.GetString("Close");
		}
	}

	private Brush GetGradientPreviewColor(string itemKey)
	{
		string text = "";
		string text2 = "";
		switch (itemKey)
		{
		case "SeaBlizz":
			text = "#91C7C8";
			text2 = "#62B0B9";
			break;
		case "Matcha":
			text = "#E1EEDC";
			text2 = "#B0CE99";
			break;
		case "Sunshine":
			text = "#FFD080";
			text2 = "#FDAC60";
			break;
		case "Peach":
			text = "#F9B2B2";
			text2 = "#EA8597";
			break;
		case "Lilac":
			text = "#E7CFEB";
			text2 = "#A3ABE4";
			break;
		case "Ebony":
			text = "#C7AC9D";
			text2 = "#A57F61";
			break;
		case "Navy":
			text = "#3C4B80";
			text2 = "#262F4E";
			break;
		case "Blue":
			text = "#6D91FF";
			text2 = "#4773FA";
			break;
		case "Gray":
			text = "#4D5055";
			text2 = "#313643";
			break;
		case "Mint":
			text = "#80DCC8";
			text2 = "#27C5A2";
			break;
		default:
			return null;
		}
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush
		{
			StartPoint = new Point(0.5, 0.0),
			EndPoint = new Point(0.5, 1.0)
		};
		try
		{
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Offset = 0.0,
				Color = (Color)ColorConverter.ConvertFromString(text)
			});
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Offset = 1.0,
				Color = (Color)ColorConverter.ConvertFromString(text2)
			});
		}
		catch (Exception ex)
		{
			UtilLog.Info(ex.Message);
		}
		return linearGradientBrush;
	}

	private void InitIconData()
	{
		List<AppIconViewModel> icons = new List<AppIconViewModel>();
		List<AppIconViewModel> collection = (from AppIconKey e in Enum.GetValues(typeof(AppIconKey))
			select new AppIconViewModel(e, icons)).ToList();
		icons.AddRange(collection);
		AppIcons.ItemsSource = icons;
	}

	private void OnCustomClick(object sender, MouseButtonEventArgs e)
	{
		if (!ProChecker.CheckPro(ProType.PremiumThemes))
		{
			return;
		}
		UserActCollectUtils.AddClickEvent("settings", "theme", "custom");
		if (!(sender is FrameworkElement { DataContext: CustomThemeViewModel dataContext }))
		{
			return;
		}
		List<string> list = Directory.GetFiles(AppPaths.ThemeDir, "custom*.*").ToList();
		if (list.Count > 0)
		{
			CustomThemeDialog customThemeDialog = new CustomThemeDialog(list[0], LocalSettings.Settings.CustomThemeLocation);
			customThemeDialog.Owner = Window.GetWindow(this);
			customThemeDialog.ShowDialog();
		}
		else
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Multiselect = false,
				Filter = "IMAGE (*.JPG;*.JPEG;*.PJPEG;*.PNG;*.GIF;*.PJP;*.JFIF;*.BMP;*.TIFF;*.TIF;*.DIB;*.WEBP)|*.JPG;*.JPEG;*.PJPEG;*.PNG;*.GIF;*.PJP;*.JFIF;*.BMP;*.TIFF;*.TIF;*.DIB;*.WEBP",
				FilterIndex = 1
			};
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
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
						if (!ImageAvailable(text2))
						{
							return;
						}
						CustomThemeDialog customThemeDialog2 = new CustomThemeDialog(text2, LocalSettings.Settings.CustomThemeLocation);
						customThemeDialog2.Owner = Window.GetWindow(this);
						customThemeDialog2.ShowDialog();
					}
				}
				catch (Exception ex)
				{
					UtilLog.Error(ex);
				}
			}
		}
		dataContext.Selected = LocalSettings.Settings.ExtraSettings.AppTheme == "Custom";
		dataContext.SetImage();
		if (dataContext.Selected)
		{
			dataContext.SetSelected();
		}
		else
		{
			App.Instance.LoadTheme("Custom", LocalSettings.Settings.ExtraSettings.AppTheme);
		}
	}

	private bool ImageAvailable(string saveImagePath)
	{
		return Utils.LoadBitmap(saveImagePath) != null;
	}

	public void Toast(string toastText)
	{
		ToastBorder.Visibility = Visibility.Visible;
		ToastText.Text = toastText;
		Storyboard storyboard = (Storyboard)FindResource("ShowToast");
		ToastBorder.BeginStoryboard(storyboard);
	}

	private void OnToasted(object sender, EventArgs e)
	{
		ToastBorder.Visibility = Visibility.Collapsed;
	}

	private void OnTabSelected(object sender, GroupTitleViewModel e)
	{
		Scroller.ScrollToVerticalOffset(0.0);
		ThemePanel.Visibility = ((e.Index != 0) ? Visibility.Collapsed : Visibility.Visible);
		AppIcons.Visibility = ((e.Index != 1) ? Visibility.Collapsed : Visibility.Visible);
		DisplayPanel.Visibility = ((e.Index != 2) ? Visibility.Collapsed : Visibility.Visible);
	}

	public void ReloadCustomImage()
	{
	}

	private void InterfaceStyleButton_Click(object sender, RoutedEventArgs e)
	{
		InterfaceStyleWindow interfaceStyleWindow = new InterfaceStyleWindow();
		interfaceStyleWindow.Owner = Window.GetWindow(this);
		interfaceStyleWindow.ShowDialog();
		UpdateInterfaceStyleText();
	}

	private void AutoDarkButton_Click(object sender, RoutedEventArgs e)
	{
		AutoDarkSettingWindow autoDarkSettingWindow = new AutoDarkSettingWindow();
		autoDarkSettingWindow.Owner = Window.GetWindow(this);
		autoDarkSettingWindow.ShowDialog();
		UpdateAutoDarkText();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/appearancecontrol.xaml", UriKind.Relative);
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
			((Storyboard)target).Completed += OnToasted;
			break;
		case 2:
			CustomTitle = (TextBlock)target;
			break;
		case 3:
			AppearanceTitle = (AnimateGroupTitle)target;
			break;
		case 4:
			ThemeSelectGrid = (Grid)target;
			break;
		case 5:
			Scroller = (ScrollViewer)target;
			break;
		case 6:
			ThemePanel = (StackPanel)target;
			break;
		case 7:
			((System.Windows.Controls.Button)target).Click += InterfaceStyleButton_Click;
			break;
		case 8:
			InterfaceStyleText = (TextBlock)target;
			break;
		case 9:
			((System.Windows.Controls.Button)target).Click += AutoDarkButton_Click;
			break;
		case 10:
			AutoDarkText = (TextBlock)target;
			break;
		case 11:
			PureColorItems = (ItemsControl)target;
			break;
		case 12:
			SeasonItems = (ItemsControl)target;
			break;
		case 13:
			CityItems = (ItemsControl)target;
			break;
		case 14:
			PhotoItems = (ItemsControl)target;
			break;
		case 15:
			CustomPanel = (StackPanel)target;
			CustomPanel.MouseLeftButtonUp += OnCustomClick;
			break;
		case 16:
			BackImage = (Border)target;
			break;
		case 17:
			AppIcons = (ItemsControl)target;
			break;
		case 18:
			DisplayPanel = (StackPanel)target;
			break;
		case 19:
			FontFamilyCtrl = (ContentControl)target;
			break;
		case 20:
			FontSizeCtrl = (ContentControl)target;
			break;
		case 21:
			ProjectListColorVisCtrl = (ContentControl)target;
			break;
		case 22:
			ProjectNumSetCtrl = (ContentControl)target;
			break;
		case 23:
			CompleteLineSetCtrl = (ContentControl)target;
			break;
		case 24:
			ToastBorder = (Border)target;
			break;
		case 25:
			ToastText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
