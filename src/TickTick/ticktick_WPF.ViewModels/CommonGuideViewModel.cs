using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.NewUser;

namespace ticktick_WPF.ViewModels;

internal class CommonGuideViewModel : INotifyPropertyChanged
{
	public class PageItem : INotifyPropertyChanged
	{
		private string _title;

		private string _desc;

		private bool _isCurrentPage;

		public int Index { get; set; }

		public string Title
		{
			get
			{
				return _title;
			}
			set
			{
				_title = value;
				NotifyPropertyChanged("Title");
			}
		}

		public string Desc
		{
			get
			{
				return _desc;
			}
			set
			{
				_desc = value;
				NotifyPropertyChanged("Desc");
			}
		}

		public bool IsClearImage { get; set; }

		public string NoChooseImagePath { get; set; }

		public string NoChooseMediaUrl { get; set; }

		public string NoChooseMediaPath { get; set; }

		public bool IsLoopMedia { get; set; }

		public ObservableCollection<ChooseActionItem> Chooses { get; set; } = new ObservableCollection<ChooseActionItem>();

		public ChooseActionItem ChoosedItem => Chooses.FirstOrDefault((ChooseActionItem x) => x.Selected);

		public bool IsCurrentPage
		{
			get
			{
				return _isCurrentPage;
			}
			set
			{
				_isCurrentPage = value;
				NotifyPropertyChanged("IsCurrentPage");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class ChooseActionItem : GuideChooseViewModel
	{
		public ObservableCollection<IconLayerItem> IconLayers { get; set; }

		public Visibility ShowLayerIcon { get; set; } = Visibility.Collapsed;

		public string ImagePath { get; set; }

		public ChooseActionItem(string name, ObservableCollection<IconLayerItem> iconLayers, string imagePath)
			: base(name)
		{
			IconLayers = iconLayers;
			ImagePath = imagePath;
			ShowLayerIcon = Visibility.Visible;
		}

		public ChooseActionItem(string name, Geometry icon, string imagePath)
			: base(name, icon)
		{
			ImagePath = imagePath;
		}

		public ChooseActionItem(string name, string emoji, string imagePath)
			: base(name, emoji)
		{
			ImagePath = imagePath;
		}
	}

	public class ChooseActionItem<T> : ChooseActionItem
	{
		public T Data { get; set; }

		public ChooseActionItem(string name, ObservableCollection<IconLayerItem> iconLayers, string imagePath)
			: base(name, iconLayers, imagePath)
		{
		}

		public ChooseActionItem(string name, Geometry icon, string imagePath)
			: base(name, icon, imagePath)
		{
		}

		public ChooseActionItem(string name, string emoji, string imagePath)
			: base(name, emoji, imagePath)
		{
		}
	}

	private string _windowTitle;

	private BitmapImage _imageSource;

	private bool _isShowBackButton;

	private string _confirmButtonText;

	private PageItem _currentPageItem;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private CommunityToolkit.Mvvm.Input.RelayCommand? backCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<ChooseActionItem>? itemClickCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private CommunityToolkit.Mvvm.Input.RelayCommand? skipCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private CommunityToolkit.Mvvm.Input.RelayCommand? continueCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<PageItem>? pageIndexClickCommand;

	public Action OnEndGuide { get; set; }

	public Action<string, bool> OnMediaUrlSwitch { get; set; }

	public Action<string, bool> OnMediaPathSwitch { get; set; }

	public Action<string, bool> OnMediaBackStep { get; set; }

	public string WindowTitle
	{
		get
		{
			return _windowTitle;
		}
		set
		{
			_windowTitle = value;
			NotifyPropertyChanged("WindowTitle");
		}
	}

	public BitmapImage ImageSource
	{
		get
		{
			return _imageSource;
		}
		set
		{
			_imageSource = value;
			NotifyPropertyChanged("ImageSource");
		}
	}

	public bool IsShowBackButton
	{
		get
		{
			return _isShowBackButton;
		}
		set
		{
			_isShowBackButton = value;
			NotifyPropertyChanged("IsShowBackButton");
		}
	}

	public string ConfirmButtonText
	{
		get
		{
			return _confirmButtonText;
		}
		set
		{
			_confirmButtonText = value;
			NotifyPropertyChanged("ConfirmButtonText");
		}
	}

	public PageItem CurrentPageItem
	{
		get
		{
			return _currentPageItem;
		}
		set
		{
			_currentPageItem = value;
			NotifyPropertyChanged("CurrentPageItem");
		}
	}

	public ObservableCollection<PageItem> PageItems { get; set; } = new ObservableCollection<PageItem>();

	private int type { get; set; }

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand BackCommand => backCommand ?? (backCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(Back));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<ChooseActionItem> ItemClickCommand => itemClickCommand ?? (itemClickCommand = new RelayCommand<ChooseActionItem>(ItemClick));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand SkipCommand => skipCommand ?? (skipCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(Skip));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ContinueCommand => continueCommand ?? (continueCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(Continue));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<PageItem> PageIndexClickCommand => pageIndexClickCommand ?? (pageIndexClickCommand = new RelayCommand<PageItem>(PageIndexClick));

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public CommonGuideViewModel()
	{
		WindowTitle = Utils.GetAppName();
		ConfirmButtonText = Utils.GetString("Continue");
	}

	public void Init8FuncGuide()
	{
		type = 0;
		PageItems.Clear();
		PageItems.Add(new PageItem
		{
			Title = Utils.GetString("InterfaceStyle"),
			Desc = Utils.GetString("FeatureGuideInterfaceDesc"),
			Index = 1,
			IsCurrentPage = true,
			Chooses = new ObservableCollection<ChooseActionItem>
			{
				new ChooseActionItem<InterfaceStyle>(Utils.GetString("FlatStyle"), new ObservableCollection<IconLayerItem>
				{
					new IconLayerItem
					{
						Geometry = Utils.GetGeometry("Ic.Guide.Flat.Part1"),
						Opacity = 0.2
					},
					new IconLayerItem
					{
						Geometry = Utils.GetGeometry("Ic.Guide.Flat.Part2")
					}
				}, "pack://application:,,,/Assets/ImageSource/InterfaceStyleFlat.png")
				{
					Data = InterfaceStyle.Flat,
					Selected = true
				},
				new ChooseActionItem<InterfaceStyle>(Utils.GetString("CardStyle"), new ObservableCollection<IconLayerItem>
				{
					new IconLayerItem
					{
						Geometry = Utils.GetGeometry("Ic.Guide.Card.Part1"),
						Opacity = 0.2
					},
					new IconLayerItem
					{
						Geometry = Utils.GetGeometry("Ic.Guide.Card.Part2")
					}
				}, "pack://application:,,,/Assets/ImageSource/InterfaceStyleCard.png")
				{
					Data = InterfaceStyle.Card
				}
			}
		});
		CalendarConfig.CalendarItemStyle? calendarItemStyle = LocalSettings.Settings?.UserPreference?.CalendarConfig?.ItemStyleType;
		bool flag = !calendarItemStyle.HasValue || calendarItemStyle != CalendarConfig.CalendarItemStyle.Modern;
		PageItems.Add(new PageItem
		{
			Title = Utils.GetString("FeatureGuideCalendarStyleTitle"),
			Desc = Utils.GetString("FeatureGuideCalendarStyleDesc"),
			Index = 2,
			Chooses = new ObservableCollection<ChooseActionItem>
			{
				new ChooseActionItem<CalendarConfig.CalendarItemStyle>(Utils.GetString("Classic"), new ObservableCollection<IconLayerItem>
				{
					new IconLayerItem
					{
						Geometry = Utils.GetGeometry("Ic.Guide.Classic.Part1"),
						Opacity = 0.4
					}
				}, "pack://application:,,,/Assets/ImageSource/CalendarTaskStyleClassic.png")
				{
					Data = CalendarConfig.CalendarItemStyle.Classic,
					Selected = flag
				},
				new ChooseActionItem<CalendarConfig.CalendarItemStyle>(Utils.GetString("Modern"), new ObservableCollection<IconLayerItem>
				{
					new IconLayerItem
					{
						Geometry = Utils.GetGeometry("Ic.Guide.Modern.Part1"),
						Opacity = 0.2
					},
					new IconLayerItem
					{
						Geometry = Utils.GetGeometry("Ic.Guide.Modern.Part2")
					}
				}, "pack://application:,,,/Assets/ImageSource/CalendarTaskStyleModern.png")
				{
					Data = CalendarConfig.CalendarItemStyle.Modern,
					Selected = !flag
				}
			}
		});
		PageItems.Add(new PageItem
		{
			Title = Utils.GetString("FeatureGuideCalendarYearTitle"),
			Desc = Utils.GetString("FeatureGuideCalendarYearDesc"),
			Index = 3
		});
		string languageKey = GetLanguageKey();
		PageItem pageItem = new PageItem
		{
			Title = Utils.GetString("AISuggestedTasks"),
			Desc = Utils.GetString("FeatureGuideAIRecommendDesc"),
			Index = 4,
			IsClearImage = true,
			IsLoopMedia = true
		};
		if (languageKey == "cn" || languageKey == "en")
		{
			pageItem.NoChooseMediaPath = "Assets/Media/AIRecommend_" + languageKey + ".avi";
		}
		else
		{
			pageItem.NoChooseMediaUrl = "https://" + BaseUrl.GetPullDomain() + "/common/user_guide/8000/desktop/" + languageKey + "/ai_suggest_task.avi";
		}
		PageItems.Add(pageItem);
		LoadCurrentPage();
	}

	public void LoadCurrentPage(bool toNext = true)
	{
		try
		{
			CurrentPageItem = PageItems.FirstOrDefault((PageItem x) => x.IsCurrentPage) ?? PageItems.FirstOrDefault();
			if (CurrentPageItem == null)
			{
				return;
			}
			if (type == 0 && CurrentPageItem.Index == 3)
			{
				int num = PageItems.IndexOf(CurrentPageItem);
				if (num > 0)
				{
					string languageKey = GetLanguageKey();
					if (PageItems[num - 1].ChoosedItem is ChooseActionItem<CalendarConfig.CalendarItemStyle> chooseActionItem)
					{
						CurrentPageItem.NoChooseImagePath = chooseActionItem.ImagePath;
						if (languageKey == "cn" || languageKey == "en")
						{
							if (chooseActionItem.Data == CalendarConfig.CalendarItemStyle.Modern)
							{
								CurrentPageItem.NoChooseMediaPath = "Assets/Media/CalTaskModern_" + languageKey + ".avi";
							}
							else
							{
								CurrentPageItem.NoChooseMediaPath = "Assets/Media/CalTaskClassic_" + languageKey + ".avi";
							}
						}
						else if (chooseActionItem.Data == CalendarConfig.CalendarItemStyle.Modern)
						{
							CurrentPageItem.NoChooseMediaUrl = "https://" + BaseUrl.GetPullDomain() + "/common/user_guide/8000/desktop/" + languageKey + "/calendar_item_style_modern.avi";
						}
						else
						{
							CurrentPageItem.NoChooseMediaUrl = "https://" + BaseUrl.GetPullDomain() + "/common/user_guide/8000/desktop/" + languageKey + "/calendar_item_style_classic.avi";
						}
					}
				}
			}
			string arg = null;
			string text = null;
			PageItem currentPageItem = CurrentPageItem;
			if (currentPageItem.ChoosedItem != null)
			{
				if (string.IsNullOrEmpty(currentPageItem.ChoosedItem.ImagePath))
				{
					ImageSource = null;
				}
				else
				{
					ImageSource = new BitmapImage(new Uri(currentPageItem.ChoosedItem.ImagePath));
				}
			}
			else if (currentPageItem.Chooses == null || !currentPageItem.Chooses.Any())
			{
				if (!string.IsNullOrEmpty(currentPageItem.NoChooseImagePath))
				{
					ImageSource = new BitmapImage(new Uri(currentPageItem.NoChooseImagePath));
				}
				else if (currentPageItem.IsClearImage)
				{
					ImageSource = null;
				}
				if (!string.IsNullOrEmpty(currentPageItem.NoChooseMediaUrl))
				{
					arg = currentPageItem.NoChooseMediaUrl;
				}
				if (!string.IsNullOrEmpty(currentPageItem.NoChooseMediaPath))
				{
					text = currentPageItem.NoChooseMediaPath;
				}
			}
			if (currentPageItem.Chooses != null && currentPageItem.Chooses.Any())
			{
				foreach (ChooseActionItem choose in currentPageItem.Chooses)
				{
					if (choose.IconLayers == null || !choose.IconLayers.Any())
					{
						continue;
					}
					foreach (IconLayerItem iconLayer in choose.IconLayers)
					{
						iconLayer.Brush = (choose.Selected ? ThemeUtil.GetColor("PrimaryColor") : ThemeUtil.GetColor("BaseColorOpacity100"));
					}
				}
			}
			IsShowBackButton = currentPageItem.Index > 1;
			if (PageItems.IndexOf(CurrentPageItem) < PageItems.Count - 1)
			{
				ConfirmButtonText = Utils.GetString("Continue");
			}
			else if (type == 0)
			{
				ConfirmButtonText = Utils.GetString("Start8");
			}
			else
			{
				ConfirmButtonText = Utils.GetString("GetStartedApp");
			}
			if (!toNext && type == 0 && currentPageItem.Index == 3)
			{
				ImageSource = null;
				if (!string.IsNullOrEmpty(text))
				{
					OnMediaBackStep?.Invoke(text, arg2: true);
				}
				else
				{
					OnMediaBackStep?.Invoke(arg, arg2: false);
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				OnMediaPathSwitch?.Invoke(text, currentPageItem.IsLoopMedia);
			}
			else
			{
				OnMediaUrlSwitch?.Invoke(arg, currentPageItem.IsLoopMedia);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			UtilLog.Error("CommonGuideViewModel LoadCurrentPage Exception:" + ex);
		}
	}

	public string GetLanguageKey()
	{
		string text = App.Ci.TwoLetterISOLanguageName;
		switch (text)
		{
		case "zh":
			text = "cn";
			break;
		case "ja":
			text = "jp";
			break;
		default:
			text = "en";
			break;
		case "pt":
		case "ru":
		case "ko":
		case "en":
			break;
		}
		return text;
	}

	[RelayCommand]
	public void Back()
	{
		PageItem pageItem = PageItems.FirstOrDefault((PageItem x) => x.IsCurrentPage);
		if (pageItem != null)
		{
			int num = PageItems.IndexOf(pageItem);
			if (num <= 0)
			{
				IsShowBackButton = false;
				return;
			}
			PageItems[num - 1].IsCurrentPage = true;
			pageItem.IsCurrentPage = false;
		}
		LoadCurrentPage(toNext: false);
	}

	[RelayCommand]
	public void ItemClick(ChooseActionItem item)
	{
		if (CurrentPageItem?.Chooses == null)
		{
			return;
		}
		foreach (ChooseActionItem choose in CurrentPageItem.Chooses)
		{
			choose.Selected = item.Name == choose.Name;
			if (choose.IconLayers == null || !choose.IconLayers.Any())
			{
				continue;
			}
			foreach (IconLayerItem iconLayer in choose.IconLayers)
			{
				iconLayer.Brush = (choose.Selected ? ThemeUtil.GetColor("PrimaryColor") : ThemeUtil.GetColor("BaseColorOpacity100"));
			}
		}
		if (string.IsNullOrEmpty(item.ImagePath))
		{
			ImageSource = null;
		}
		else
		{
			ImageSource = new BitmapImage(new Uri(item.ImagePath));
		}
	}

	[RelayCommand]
	public void Skip()
	{
		ApplyItem();
		OnEndGuide?.Invoke();
	}

	[RelayCommand]
	public void Continue()
	{
		PageItem pageItem = PageItems.FirstOrDefault((PageItem x) => x.IsCurrentPage);
		if (pageItem != null)
		{
			int num = PageItems.IndexOf(pageItem);
			if (num >= PageItems.Count - 1)
			{
				ApplyItem();
				OnEndGuide?.Invoke();
				return;
			}
			PageItems[num + 1].IsCurrentPage = true;
			pageItem.IsCurrentPage = false;
			IsShowBackButton = true;
		}
		LoadCurrentPage();
	}

	[RelayCommand]
	public void PageIndexClick(PageItem item)
	{
		foreach (PageItem pageItem in PageItems)
		{
			pageItem.IsCurrentPage = pageItem.Index == item.Index;
		}
		LoadCurrentPage();
	}

	private void ApplyItem()
	{
		foreach (PageItem pageItem in PageItems)
		{
			if (pageItem.ChoosedItem == null)
			{
				continue;
			}
			ChooseActionItem choosedItem = pageItem.ChoosedItem;
			if (!(choosedItem is ChooseActionItem<InterfaceStyle> chooseActionItem))
			{
				if (choosedItem is ChooseActionItem<CalendarConfig.CalendarItemStyle> chooseActionItem2)
				{
					CalendarConfig calendarConfig = LocalSettings.Settings.UserPreference.CalendarConfig ?? new CalendarConfig();
					calendarConfig.ItemStyleType = chooseActionItem2.Data;
					LocalSettings.Settings.SaveCalendarStyleConfig(calendarConfig, saveAndPush: true);
					DataChangedNotifier.NotifyCalendarTaskbarStyleChanged();
				}
			}
			else
			{
				LocalSettings.Settings.ExtraSettings.InterfaceStyle = chooseActionItem.Data;
				CardThemeDictService.ReloadInterfaceStyle();
				DataChangedNotifier.NotifyInterfaceStyleChanged();
			}
			if (pageItem.IsCurrentPage)
			{
				break;
			}
		}
		LocalSettings.Settings.Save();
	}
}
