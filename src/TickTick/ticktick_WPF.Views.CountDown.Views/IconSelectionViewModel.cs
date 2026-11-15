using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CountDown.Infrastructure;
using ticktick_WPF.Views.CountDown.ViewModels;

namespace ticktick_WPF.Views.CountDown.Views;

public class IconSelectionViewModel : ObservableObject
{
	public class IconGroup : ObservableObject
	{
		private bool _isExpanded = true;

		public string Name { get; set; }

		public bool IsExpanded
		{
			get
			{
				return _isExpanded;
			}
			set
			{
				SetProperty(ref _isExpanded, value, "IsExpanded");
			}
		}

		public ObservableCollection<IconItem> Items { get; set; } = new ObservableCollection<IconItem>();
	}

	public class IconItem : ObservableObject
	{
		private bool _isSelected;

		public string IconName { get; set; }

		public string DisplayName { get; set; }

		public bool Selected
		{
			get
			{
				return _isSelected;
			}
			set
			{
				SetProperty(ref _isSelected, value, "Selected");
			}
		}
	}

	private string _name;

	private string _selectedIconName;

	private Brush _selectedIconBackground;

	private int _currentTabIndex;

	private IconItem _selectedIcon;

	private string _customText;

	private string _selectedColor;

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			SetProperty(ref _name, value, "Name");
		}
	}

	public string SelectedIconName
	{
		get
		{
			return _selectedIconName;
		}
		set
		{
			SetProperty(ref _selectedIconName, value, "SelectedIconName");
		}
	}

	public Brush SelectedIconBackground
	{
		get
		{
			return _selectedIconBackground;
		}
		set
		{
			SetProperty(ref _selectedIconBackground, value, "SelectedIconBackground");
		}
	}

	public int CurrentTabIndex
	{
		get
		{
			return _currentTabIndex;
		}
		set
		{
			if (SetProperty(ref _currentTabIndex, value, "CurrentTabIndex"))
			{
				OnPropertyChanged("IsIconTabSelected");
				OnPropertyChanged("IsEmojiTabSelected");
				OnPropertyChanged("IsTextTabSelected");
			}
		}
	}

	public bool IsIconTabSelected => CurrentTabIndex == 0;

	public bool IsEmojiTabSelected => CurrentTabIndex == 1;

	public bool IsTextTabSelected => CurrentTabIndex == 2;

	public ObservableCollection<IconGroup> Icons { get; set; } = new ObservableCollection<IconGroup>();

	public IconItem SelectedIcon
	{
		get
		{
			return _selectedIcon;
		}
		set
		{
			if (SetProperty(ref _selectedIcon, value, "SelectedIcon") && value != null)
			{
				SelectedIconName = value.IconName;
			}
		}
	}

	public string CustomText
	{
		get
		{
			return _customText;
		}
		set
		{
			SetProperty(ref _customText, value, "CustomText");
		}
	}

	public string SelectedColor
	{
		get
		{
			return _selectedColor;
		}
		set
		{
			SetProperty(ref _selectedColor, value, "SelectedColor");
		}
	}

	public ICommand SelectIconCommand { get; }

	public event EventHandler<string> OnSelectIconChanged;

	public IconSelectionViewModel(string name, string iconName, string iconBackground)
	{
		SelectIconCommand = new RelayCommandEx(OnSelectIcon);
		Name = name;
		SelectedIconName = iconName ?? "Default";
		if (iconBackground != null)
		{
			SelectedIconBackground = ThemeUtil.GetColorInString(iconBackground) ?? ThemeUtil.GetPrimaryColor(0.1);
		}
		CurrentTabIndex = 0;
	}

	private void OnSelectIcon(object parameter)
	{
		if (!(parameter is IconItem iconItem))
		{
			return;
		}
		SelectedIcon = iconItem;
		iconItem.Selected = true;
		this.OnSelectIconChanged?.Invoke(this, iconItem.IconName);
		foreach (IconGroup icon in Icons)
		{
			foreach (IconItem item in icon.Items)
			{
				if (item.Selected && item.IconName != iconItem.IconName)
				{
					item.Selected = false;
				}
			}
		}
	}

	public void LoadIcons(CountDownType type)
	{
		Dictionary<string, List<string>> obj = (Utils.IsTickPackage() ? new Dictionary<string, List<string>>
		{
			{
				"CountdownEvent",
				new List<string>
				{
					"countdown_wedding_anniversary", "countdown_countdown", "countdown_birthday", "countdown_new_year_day", "countdown_soon", "countdown_100_days_together", "countdown_exam", "countdown_dating", "countdown_valentine_day", "countdown_qixi",
					"countdown_first_charity_event", "countdown_proposal_day", "countdown_marriage_license_day", "countdown_star", "countdown_trophies", "countdown_medal", "countdown_flag", "countdown_bullseye"
				}
			},
			{
				"CountdownPerson",
				new List<string>
				{
					"countdown_medium_girl", "countdown_medium_boy", "countdown_medium_woman", "countdown_medium_man", "countdown_medium_woman_2", "countdown_medium_man_2", "countdown_medium_woman_3", "countdown_medium_man_3", "countdown_medium_elderly_woman", "countdown_medium_elderly_man",
					"countdown_light_girl", "countdown_light_boy", "countdown_light_woman", "countdown_light_man", "countdown_light_woman_2", "countdown_light_man_2", "countdown_light_woman_3", "countdown_light_man_3", "countdown_light_elderly_woman", "countdown_light_elderly_man",
					"countdown_dark_boy", "countdown_dark_girl", "countdown_dark_woman", "countdown_dark_man", "countdown_dark_woman_2", "countdown_dark_man_2", "countdown_dark_woman_3", "countdown_dark_man_3", "countdown_dark_elderly_woman", "countdown_dark_elderly_man"
				}
			},
			{
				"CountdownActivity",
				new List<string>
				{
					"countdown_children_day", "countdown_party", "countdown_shake_hands", "countdown_first_speech", "countdown_concert", "countdown_rocket", "countdown_rose", "countdown_lixia", "countdown_earth", "countdown_trip",
					"countdown_airplane", "countdown_island", "countdown_holiday", "countdown_lidong", "countdown_enrollment_anniversary", "countdown_graduation", "countdown_work_anniversary", "countdown_school_starts", "countdown_health_check", "countdown_chongyang",
					"countdown_ticktick"
				}
			},
			{
				"CountdownObjects",
				new List<string>
				{
					"countdown_first_pot_of_gold", "countdown_credit_card_bill_due", "countdown_payday", "countdown_first_road_trip", "countdown_first_car", "countdown_license_renewal_due", "countdown_visa_renewal_due", "countdown_first_home", "countdown_pay_rent", "countdown_mortgage_payment",
					"countdown_first_startup", "countdown_pay_insurance", "countdown_candle"
				}
			},
			{
				"type_holiday",
				new List<string> { "countdown_christmas", "countdown_christmas_eve", "countdown_halloween", "countdown_thanksgiving", "countdown_april_fool_day", "countdown_teachers_day" }
			},
			{
				"CountdownSport",
				new List<string>
				{
					"countdown_basketball", "countdown_football", "countdown_volleyball", "countdown_table_tennis", "countdown_tennis", "countdown_baseball", "countdown_badminton", "countdown_cycling", "countdown_first_marathon", "countdown_swimming",
					"countdown_fitness", "countdown_esports"
				}
			},
			{
				"CountdownAnimal",
				new List<string> { "countdown_pet_health_check", "countdown_cat", "countdown_cat_2", "countdown_dog", "countdown_dog_2", "countdown_dog_3" }
			}
		} : new Dictionary<string, List<string>>
		{
			{
				"CountdownEvent",
				new List<string>
				{
					"countdown_wedding_anniversary", "countdown_countdown", "countdown_birthday", "countdown_new_year_day", "countdown_soon", "countdown_100_days_together", "countdown_exam", "countdown_dating", "countdown_valentine_day", "countdown_qixi",
					"countdown_first_charity_event", "countdown_proposal_day", "countdown_marriage_license_day", "countdown_star", "countdown_trophies", "countdown_medal", "countdown_flag", "countdown_bullseye"
				}
			},
			{
				"CountdownPerson",
				new List<string>
				{
					"countdown_medium_girl", "countdown_medium_boy", "countdown_medium_woman", "countdown_medium_man", "countdown_medium_woman_2", "countdown_medium_man_2", "countdown_medium_woman_3", "countdown_medium_man_3", "countdown_medium_elderly_woman", "countdown_medium_elderly_man",
					"countdown_light_girl", "countdown_light_boy", "countdown_light_woman", "countdown_light_man", "countdown_light_woman_2", "countdown_light_man_2", "countdown_light_woman_3", "countdown_light_man_3", "countdown_light_elderly_woman", "countdown_light_elderly_man"
				}
			},
			{
				"CountdownActivity",
				new List<string>
				{
					"countdown_children_day", "countdown_party", "countdown_shake_hands", "countdown_first_speech", "countdown_concert", "countdown_rocket", "countdown_lixia", "countdown_earth", "countdown_trip", "countdown_airplane",
					"countdown_island", "countdown_holiday", "countdown_enrollment_anniversary", "countdown_graduation", "countdown_work_anniversary", "countdown_school_starts", "countdown_health_check", "countdown_candle", "countdown_ticktick"
				}
			},
			{
				"CountdownObjects",
				new List<string>
				{
					"countdown_first_pot_of_gold", "countdown_credit_card_bill_due", "countdown_payday", "countdown_first_road_trip", "countdown_first_car", "countdown_license_renewal_due", "countdown_rose", "countdown_first_home", "countdown_pay_rent", "countdown_mortgage_payment",
					"countdown_first_startup", "countdown_visa_renewal_due", "countdown_pay_insurance"
				}
			},
			{
				"type_holiday",
				new List<string>
				{
					"countdown_chunjie", "countdown_chuxi", "countdown_national_day", "countdown_army_founding_day", "countdown_party_founding_day", "countdown_labor_day", "countdown_duanwu", "countdown_yuanxiao", "countdown_nanxiaonian", "countdown_beixiaonian",
					"countdown_thanksgiving", "countdown_zhongqiu", "countdown_april_fool_day", "countdown_halloween", "countdown_qingming", "countdown_chongyang", "countdown_teachers_day", "countdown_lidong", "countdown_christmas_eve", "countdown_christmas"
				}
			},
			{
				"CountdownSport",
				new List<string>
				{
					"countdown_basketball", "countdown_football", "countdown_volleyball", "countdown_table_tennis", "countdown_tennis", "countdown_baseball", "countdown_badminton", "countdown_cycling", "countdown_first_marathon", "countdown_swimming",
					"countdown_fitness", "countdown_esports"
				}
			},
			{
				"CountdownAnimal",
				new List<string> { "countdown_pet_health_check", "countdown_cat", "countdown_cat_2", "countdown_dog", "countdown_dog_2", "countdown_dog_3" }
			}
		});
		Icons.Clear();
		foreach (KeyValuePair<string, List<string>> item in obj)
		{
			IconGroup iconGroup = new IconGroup
			{
				Name = Utils.GetString(item.Key)
			};
			foreach (string item2 in item.Value)
			{
				IconItem iconItem = new IconItem
				{
					DisplayName = item2,
					IconName = item2
				};
				if (iconItem.IconName.Equals(SelectedIconName, StringComparison.OrdinalIgnoreCase))
				{
					SelectedIcon = iconItem;
					iconItem.Selected = true;
				}
				iconGroup.Items.Add(iconItem);
			}
			Icons.Add(iconGroup);
		}
		IEnumerable<IconItem> source = Icons.SelectMany((IconGroup x) => x.Items);
		if (SelectedIcon == null && source.Any())
		{
			string iconName = type.GetDefaultIconName();
			SelectedIcon = source.FirstOrDefault((IconItem it) => it.IconName == iconName);
			if (SelectedIcon == null)
			{
				SelectedIcon = source.FirstOrDefault();
				source.FirstOrDefault().Selected = true;
			}
			else
			{
				SelectedIcon.Selected = true;
			}
		}
	}
}
