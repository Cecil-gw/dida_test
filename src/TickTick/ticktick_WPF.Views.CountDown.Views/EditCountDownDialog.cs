using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.CountDown.Views;

public class EditCountDownDialog : Window, IOkCancelWindow, IComponentConnector
{
	private string repeatFrom = "2";

	internal new Border Icon;

	internal ContentPresenter DefaultIcon;

	internal Path DefaultEmoji;

	internal Grid DefaultText;

	internal EmjTextBlock SelectedText;

	internal TextBox CountdownTitleBox;

	internal Button RecommendIcon;

	internal EscPopup SetRecommendPopup;

	internal Border DateSelectBorder;

	internal TextBlock DateText;

	internal EscPopup SetDatePopup;

	internal Border ReminderSelectBorder;

	internal TextBlock RemindText;

	internal EscPopup SetReminderPopup;

	internal Border RepeatBorder;

	internal TextBlock RepeatText;

	internal Path RepeatDropdown;

	internal EscPopup SetRepeatPopup;

	internal Border TypeBorder;

	internal EscPopup SetTypePopup;

	internal Border CalModeBorder;

	internal EscPopup CalModePopup;

	internal Border DayOptionBorder;

	internal EscPopup SetDayOptionPopup;

	internal Border DisplayBorder;

	internal EscPopup SettingsPopup;

	internal Button SaveButton;

	internal Grid ToastGrid;

	private bool _contentLoaded;

	private DatePickerPopup _datePickerControl { get; set; }

	public EditCountDownViewModel ViewModel { get; private set; }

	public bool Confirm { get; set; }

	public CountDownType? ChangedType { get; set; }

	public bool IsChangeDate { get; set; }

	public CountdownModel FinalModel { get; set; }

	public EditCountDownDialog(CountdownModel existingModel = null, CountDownType countdownType = CountDownType.Countdown)
	{
		ViewModel = new EditCountDownViewModel(existingModel, countdownType);
		base.DataContext = ViewModel;
		InitializeComponent();
		string iconText = existingModel?.IconRes;
		InitializeControls();
		InitIconSelector(iconText);
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.Owner?.Activate();
		base.OnClosing(e);
	}

	private void InitIconSelector(string iconText)
	{
		if (iconText != null && iconText.StartsWith("txt_"))
		{
			string text = iconText.Replace("txt_", string.Empty);
			OnIconSelected(null, text, ViewModel.IconBackground, EmojiHelper.StartWithEmoji(text) ? IconType.Emoji : IconType.Text);
		}
	}

	private async Task InitializeControls()
	{
		bool flag = true;
		switch (ViewModel.Type)
		{
		case CountDownType.Birthday:
			flag = false;
			break;
		case CountDownType.Holiday:
			if (Utils.IsTickPackage())
			{
				flag = false;
			}
			break;
		}
		RecommendIcon.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		if (!ViewModel.IsAdd)
		{
			CountdownTitleBox.Text = ViewModel.Name;
			SaveButton.Content = Utils.GetString("PublicSave");
			RecommendIcon.Visibility = Visibility.Collapsed;
		}
		else
		{
			SaveButton.Content = Utils.GetString("NextStep1");
			await Task.Delay(200);
			CountdownTitleBox.Focus();
		}
	}

	private void DatePickerControl_SelectionCanceled(object sender, EventArgs e)
	{
		SetDatePopup.IsOpen = false;
	}

	private void DatePickerControl_DateSelected(object sender, DateSelectedEventArgs e)
	{
		SetDatePopup.IsOpen = false;
		bool num = ViewModel.IgnoreYear != _datePickerControl.IgnoreYear;
		ViewModel.IgnoreYear = _datePickerControl.IgnoreYear;
		ViewModel.IsGregorianCalendar = _datePickerControl.IsGregorianCalendar;
		ViewModel.Date = e.Date;
		ViewModel.UpdateRepeatFlagByDate();
		if (num)
		{
			ViewModel.ManualIgnoreYear = _datePickerControl.IgnoreYear;
		}
	}

	private void CloseButton_Click(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void IconContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		string iconBackground = null;
		if (ViewModel.IconBackground is SolidColorBrush solidColorBrush && solidColorBrush.Color != Colors.Transparent)
		{
			iconBackground = ColorToHex(solidColorBrush.Color);
		}
		IconSelectionPopup iconSelectionPopup = new IconSelectionPopup(ViewModel.Name, ViewModel.IconName, ViewModel.Type, iconBackground);
		iconSelectionPopup.WindowStartupLocation = WindowStartupLocation.Manual;
		iconSelectionPopup.Owner = this;
		double num = 0.0;
		double num2 = 0.0;
		num = iconSelectionPopup.Owner.Left + (iconSelectionPopup.Owner.Width - iconSelectionPopup.Width) / 2.0;
		num2 = iconSelectionPopup.Owner.Top + 105.0;
		iconSelectionPopup.Left = num;
		iconSelectionPopup.Top = num2;
		iconSelectionPopup.IconSelected += OnIconSelected;
		iconSelectionPopup.Show();
	}

	private void OnIconSelected(string iconName, string customText, Brush iconBackground, IconType type)
	{
		switch (type)
		{
		case IconType.Icon:
			ViewModel.IconName = iconName;
			DefaultEmoji.Visibility = Visibility.Collapsed;
			DefaultText.Visibility = Visibility.Collapsed;
			DefaultIcon.Visibility = Visibility.Visible;
			SelectedText.Visibility = Visibility.Collapsed;
			break;
		case IconType.Emoji:
			ViewModel.IconName = "txt_" + customText;
			DefaultText.Visibility = Visibility.Visible;
			DefaultIcon.Visibility = Visibility.Collapsed;
			SelectedText.Visibility = Visibility.Visible;
			if (!string.IsNullOrEmpty(customText) && EmojiHelper.StartWithEmoji(customText))
			{
				bool flag2 = iconBackground == null || (iconBackground as SolidColorBrush)?.Color == Colors.Transparent;
				Icon.Background = (flag2 ? ThemeUtil.GetPrimaryColor(0.1) : iconBackground);
				SelectedText.Visibility = Visibility.Visible;
				SelectedText.Text = customText.Replace("txt_", string.Empty);
				DefaultEmoji.Visibility = Visibility.Collapsed;
			}
			else
			{
				Icon.Background = ThemeUtil.GetColor("BaseColorOpacity5");
				SelectedText.Visibility = Visibility.Collapsed;
				DefaultEmoji.Visibility = Visibility.Visible;
			}
			break;
		case IconType.Text:
			ViewModel.IconName = "txt_" + customText;
			DefaultText.Visibility = Visibility.Visible;
			DefaultIcon.Visibility = Visibility.Collapsed;
			DefaultEmoji.Visibility = Visibility.Collapsed;
			SelectedText.Visibility = Visibility.Visible;
			if (!string.IsNullOrEmpty(customText))
			{
				bool flag = iconBackground == null || (iconBackground as SolidColorBrush)?.Color == Colors.Transparent;
				Icon.Background = (flag ? ThemeUtil.GetPrimaryColor(0.1) : iconBackground);
				SelectedText.Visibility = Visibility.Visible;
				SelectedText.Foreground = (flag ? ThemeUtil.GetPrimaryColor(1.0) : new SolidColorBrush(Colors.White));
				SelectedText.Text = customText.Replace("txt_", string.Empty);
			}
			else
			{
				Icon.Background = ThemeUtil.GetColor("BaseColorOpacity5");
				SelectedText.Visibility = Visibility.Visible;
				SelectedText.Foreground = ThemeUtil.GetColor("BaseColorOpacity20");
				SelectedText.Text = (Utils.IsCn() ? "文" : "A");
			}
			break;
		}
		ViewModel.IconBackground = iconBackground;
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private string ColorToHex(Color color)
	{
		return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	private async void OkButton_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(ViewModel.Name))
		{
			return;
		}
		if (!ViewModel.CheckIfCanSaveRepeatFlag())
		{
			new CustomerDialog(null, Utils.GetString("LunarUnsupportRepeatHint"), MessageBoxButton.OK, this).ShowDialog();
			return;
		}
		Confirm = true;
		CountdownModel result = ViewModel.ToModel();
		if (!ViewModel.IsAdd)
		{
			result.TypeOfSmartList = ViewModel.TypeOfSmartList;
			if (result.Type != ViewModel.originalType)
			{
				ChangedType = result.Type;
			}
			IsChangeDate = ViewModel.IsChangeDate;
			await CountdownDao.UpdateCountdown(result);
			ViewModel.SetRepeatCollect();
			FinalModel = result;
			Close();
		}
		else
		{
			if (result == null)
			{
				return;
			}
			CountdownModel newCountdown = CountdownModel.GetDefaultCountdown(ViewModel.Type);
			newCountdown.Name = result.Name;
			newCountdown.Date = result.Date;
			newCountdown.Type = result.Type;
			newCountdown.Color = result.Color;
			if (!string.IsNullOrEmpty(result.IconRes))
			{
				newCountdown.IconRes = result.IconRes.Replace(".png", "");
			}
			if (!string.IsNullOrEmpty(result.Color))
			{
				newCountdown.Color = result.Color;
			}
			newCountdown.TypeOfSmartList = ViewModel.TypeOfSmartList;
			newCountdown.ShowCalendarType = result.ShowCalendarType;
			newCountdown.IgnoreYear = result.IgnoreYear;
			newCountdown.Reminders = result.Reminders;
			newCountdown.RepeatFlag = result.RepeatFlag;
			newCountdown.AnnoyingAlert = result.AnnoyingAlert;
			newCountdown.TimerMode = result.TimerMode;
			newCountdown.ShowAge = result.ShowAge;
			newCountdown.DaysOption = result.DaysOption;
			result.Id = newCountdown.Id;
			CountdownStyleDialog countdownStyleDialog = new CountdownStyleDialog(result, fromAdd: true);
			countdownStyleDialog.Owner = this;
			countdownStyleDialog.StyleSelected += async delegate(object s, StyleSelectionResult styleResult)
			{
				UserActCollectUtils.AddClickEvent("countdown", "add", "add_success");
				UserActCollectUtils.AddClickEvent("countdown", "add_type", ViewModel.Type.ToString().ToLower());
				if (styleResult != null)
				{
					UserActCollectUtils.AddClickEvent("countdown", "style", GetActionStyle(styleResult.Style));
					CountdownDao.SetCoundownStyle(newCountdown, styleResult);
				}
				ChangedType = result.Type;
				Close();
				await CountdownDao.CreateCountdown(newCountdown);
				ViewModel.SetRepeatCollect();
				FinalModel = newCountdown;
			};
			countdownStyleDialog.Show();
		}
	}

	private string GetActionStyle(string style)
	{
		string result = style;
		if (style == "normal")
		{
			result = "default";
		}
		if (style == "perpetual")
		{
			result = "chinese_calendar";
		}
		if (style == "two_color_card")
		{
			result = "colour_card";
		}
		if (style == "fullscreen_image")
		{
			result = "full_picture";
		}
		return result;
	}

	public void OnCancel()
	{
		Close();
	}

	public void Ok()
	{
		if (SaveButton.IsEnabled && !SetDatePopup.IsOpen)
		{
			OkButton_Click(this, null);
		}
	}

	private void OpenPopup(object sender, RoutedEventArgs e)
	{
		_datePickerControl = new DatePickerPopup
		{
			SelectedDate = ((!ViewModel.Date.HasValue) ? DateTime.Today : ViewModel.Date.Value),
			CountdownType = ViewModel.Type,
			IsGregorianCalendar = ViewModel.IsGregorianCalendar,
			IgnoreYear = ViewModel.IgnoreYear
		};
		if (!ViewModel.IgnoreYear)
		{
			ViewModel.IsShowAgeBeforeIgnoreYear = ViewModel.IsShowAge;
		}
		_datePickerControl.DateSelected += DatePickerControl_DateSelected;
		_datePickerControl.SelectionCanceled += DatePickerControl_SelectionCanceled;
		SetDatePopup.Child = _datePickerControl;
		SetDatePopup.IsOpen = true;
	}

	private void OpenReminderPopup(object sender, RoutedEventArgs e)
	{
		IEnumerable<TaskReminderModel> reminders = new List<TaskReminderModel>();
		if (ViewModel.Reminders != null && ViewModel.Reminders.Any())
		{
			reminders = ViewModel.Reminders.Select((string it) => new TaskReminderModel
			{
				trigger = it
			});
		}
		SetReminderControl setReminderControl = new SetReminderControl(isAllDay: true, reminders, (!ViewModel.Date.HasValue) ? DateTime.Today : ViewModel.Date.Value, withNull: true, 0, isSpan: false, ViewModel.AnnoyingAlert, DisplayModule.CountDown);
		setReminderControl.OnCancel += delegate
		{
			SetReminderPopup.IsOpen = false;
		};
		setReminderControl.OnSelected += OnReminderSelect;
		SetReminderPopup.Child = setReminderControl;
		SetReminderPopup.IsOpen = true;
	}

	private void OnReminderSelect(object sender, ReminderSelectedEventArgs e)
	{
		ViewModel.Reminders = e.Reminders;
		ViewModel.AnnoyingAlert = e.AnnoyingAlert;
		SetReminderPopup.IsOpen = false;
	}

	private void OpenRepeatPopup(object sender, RoutedEventArgs e)
	{
		RepeatExtra repeat = new RepeatExtra
		{
			DefaultDate = ((!ViewModel.Date.HasValue) ? DateTime.Today : ViewModel.Date.Value),
			RepeatFrom = repeatFrom,
			RepeatFlag = ViewModel.RepeatFlag
		};
		new SetRepeatDialog(SetRepeatPopup, repeat, calenderMode: false, countdownMode: true, !ViewModel.IsGregorianCalendar).RepeatSelect += OnRepeatSelect;
		SetRepeatPopup.IsOpen = true;
	}

	private void OnRepeatSelect(object sender, RepeatExtra e)
	{
		repeatFrom = e.RepeatFrom;
		ViewModel.UpdateRepeatFlag(e.RepeatFlag);
		SetRepeatPopup.IsOpen = false;
	}

	private void OpenDayOptionPopup(object sender, RoutedEventArgs e)
	{
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)DayStartOption.Default, Utils.GetString("DayCalculationStandard"), (DrawingImage)null, "")
			{
				Selected = (ViewModel.DaysOption == DayStartOption.Default)
			},
			new CustomMenuItemViewModel((object)DayStartOption.AddOneDay, Utils.GetString("DayCalculationStandard1"), (DrawingImage)null, "")
			{
				Selected = (ViewModel.DaysOption == DayStartOption.AddOneDay)
			}
		}, SetDayOptionPopup);
		customMenuList.Operated += OnDayOptionSelected;
		customMenuList.Show();
	}

	private void OnDayOptionSelected(object sender, object e)
	{
		DayStartOption daysOption = (DayStartOption)e;
		ViewModel.DaysOption = daysOption;
	}

	private void OpenTypePopup(object sender, RoutedEventArgs e)
	{
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
		List<CountDownType> list2 = (Utils.IsTickPackage() ? new List<CountDownType>
		{
			CountDownType.Countdown,
			CountDownType.Anniversary,
			CountDownType.Birthday,
			CountDownType.Holiday
		} : new List<CountDownType>
		{
			CountDownType.Anniversary,
			CountDownType.Countdown,
			CountDownType.Birthday,
			CountDownType.Holiday
		});
		foreach (CountDownType item in list2)
		{
			CustomMenuItemViewModel customMenuItemViewModel = new CustomMenuItemViewModel((object)item, CountdownModel.GetTypeText(item), (DrawingImage)null, "");
			customMenuItemViewModel.Selected = item == ViewModel.Type;
			list.Add(customMenuItemViewModel);
		}
		CustomMenuList customMenuList = new CustomMenuList(list, SetTypePopup);
		customMenuList.Operated += OnTypeSelected;
		customMenuList.Show();
	}

	private void OnTypeSelected(object sender, object e)
	{
		CountDownType type = (CountDownType)e;
		ViewModel.UpdateType(type);
		InitializeControls();
	}

	private void OnRecommendClick(object sender, RoutedEventArgs routedEventArgs)
	{
		if (ViewModel.Type == CountDownType.Holiday)
		{
			HolidaySelectionWindow holidaySelectionWindow = new HolidaySelectionWindow();
			holidaySelectionWindow.Owner = this;
			holidaySelectionWindow.Show();
		}
		else
		{
			CountDownDropdown countDownDropdown = new CountDownDropdown(ViewModel.Type, ViewModel.IconName, ViewModel.Name)
			{
				Width = CountdownTitleBox.ActualWidth + 12.0
			};
			countDownDropdown.CountDownSelected += OnItemSelected;
			SetRecommendPopup.Child = countDownDropdown;
			SetRecommendPopup.IsOpen = true;
		}
	}

	private void OnItemSelected(object sender, CountDownSelectedEventArgs e)
	{
		OnIconSelected(e.SelectedItem.IconKey, null, null, IconType.Icon);
		if (e.SelectedItem != null)
		{
			ViewModel.Name = e.SelectedItem.Text;
			ViewModel.IconName = e.SelectedItem.IconKey;
			CountdownTitleBox.Text = e.SelectedItem.Text;
		}
		SetRecommendPopup.IsOpen = false;
	}

	private void OnSettingsItemSelected(object sender, object e)
	{
		if (e is int typeOfSmartList)
		{
			ViewModel.TypeOfSmartList = typeOfSmartList;
		}
	}

	private void OpenDisplayPopup(object sender, RoutedEventArgs e)
	{
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
		list.Add(new CustomMenuItemViewModel((object)1000, Utils.GetString("in_smart_list"), (DrawingImage)null, "")
		{
			IsMessage = true
		});
		list.Add(new CustomMenuItemViewModel((object)0, Utils.GetString("smart_list_on_the_day"), (DrawingImage)null, ""));
		list.Add(new CustomMenuItemViewModel((object)(-3), string.Format(Utils.GetString("smart_list_xx_days_early"), 3), (DrawingImage)null, ""));
		list.Add(new CustomMenuItemViewModel((object)(-7), string.Format(Utils.GetString("smart_list_xx_days_early"), 7), (DrawingImage)null, ""));
		list.Add(new CustomMenuItemViewModel((object)(-9999), Utils.GetString("smart_list_always_show"), (DrawingImage)null, ""));
		list.Add(new CustomMenuItemViewModel((object)9999, Utils.GetString("EditProjectDoNotDisplay"), (DrawingImage)null, ""));
		list.ForEach(delegate(CustomMenuItemViewModel it)
		{
			it.Selected = (it.Value as int?).GetValueOrDefault() == ViewModel.TypeOfSmartList;
		});
		CustomMenuList customMenuList = new CustomMenuList(list, SettingsPopup);
		customMenuList.Operated += OnSettingsItemSelected;
		customMenuList.Show();
	}

	private void OpenCalModePopup(object sender, RoutedEventArgs e)
	{
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
		foreach (DisplayTimerMode value in Enum.GetValues(typeof(DisplayTimerMode)))
		{
			CustomMenuItemViewModel customMenuItemViewModel = new CustomMenuItemViewModel((object)value, CountdownModel.GetTimerModeText(value), (DrawingImage)null, "");
			customMenuItemViewModel.Selected = value == ViewModel.DisplayTimerMode;
			list.Add(customMenuItemViewModel);
		}
		CustomMenuList customMenuList = new CustomMenuList(list, CalModePopup);
		customMenuList.Operated += OnCalModeSelected;
		customMenuList.Show();
	}

	private void OnCalModeSelected(object sender, object e)
	{
		DisplayTimerMode displayTimerMode = (DisplayTimerMode)e;
		ViewModel.DisplayTimerMode = displayTimerMode;
	}

	private void ShowAgeCheckBox_Checked(object sender, RoutedEventArgs e)
	{
		if (ViewModel.IgnoreYear && ViewModel.IsShowAgeOption)
		{
			WindowToastHelper.ToastString(ToastGrid, Utils.GetString("PleaseSelectYear"));
			ViewModel.IsShowAge = false;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/editcountdowndialog.xaml", UriKind.Relative);
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
			((Border)target).MouseLeftButtonDown += IconContainer_MouseLeftButtonDown;
			break;
		case 2:
			Icon = (Border)target;
			break;
		case 3:
			DefaultIcon = (ContentPresenter)target;
			break;
		case 4:
			DefaultEmoji = (Path)target;
			break;
		case 5:
			DefaultText = (Grid)target;
			break;
		case 6:
			SelectedText = (EmjTextBlock)target;
			break;
		case 7:
			CountdownTitleBox = (TextBox)target;
			break;
		case 8:
			RecommendIcon = (Button)target;
			RecommendIcon.Click += OnRecommendClick;
			break;
		case 9:
			SetRecommendPopup = (EscPopup)target;
			break;
		case 10:
			((Button)target).Click += OpenPopup;
			break;
		case 11:
			DateSelectBorder = (Border)target;
			break;
		case 12:
			DateText = (TextBlock)target;
			break;
		case 13:
			SetDatePopup = (EscPopup)target;
			break;
		case 14:
			((Button)target).Click += OpenReminderPopup;
			break;
		case 15:
			ReminderSelectBorder = (Border)target;
			break;
		case 16:
			RemindText = (TextBlock)target;
			break;
		case 17:
			SetReminderPopup = (EscPopup)target;
			break;
		case 18:
			((Button)target).Click += OpenRepeatPopup;
			break;
		case 19:
			RepeatBorder = (Border)target;
			break;
		case 20:
			RepeatText = (TextBlock)target;
			break;
		case 21:
			RepeatDropdown = (Path)target;
			break;
		case 22:
			SetRepeatPopup = (EscPopup)target;
			break;
		case 23:
			((Button)target).Click += OpenTypePopup;
			break;
		case 24:
			TypeBorder = (Border)target;
			break;
		case 25:
			SetTypePopup = (EscPopup)target;
			break;
		case 26:
			((Button)target).Click += OpenCalModePopup;
			break;
		case 27:
			CalModeBorder = (Border)target;
			break;
		case 28:
			CalModePopup = (EscPopup)target;
			break;
		case 29:
			((Button)target).Click += OpenDayOptionPopup;
			break;
		case 30:
			DayOptionBorder = (Border)target;
			break;
		case 31:
			SetDayOptionPopup = (EscPopup)target;
			break;
		case 32:
			((Button)target).Click += OpenDisplayPopup;
			break;
		case 33:
			DisplayBorder = (Border)target;
			break;
		case 34:
			SettingsPopup = (EscPopup)target;
			break;
		case 35:
			((CheckBox)target).Checked += ShowAgeCheckBox_Checked;
			break;
		case 36:
			SaveButton = (Button)target;
			SaveButton.Click += OkButton_Click;
			break;
		case 37:
			((Button)target).Click += CancelButton_Click;
			break;
		case 38:
			ToastGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
