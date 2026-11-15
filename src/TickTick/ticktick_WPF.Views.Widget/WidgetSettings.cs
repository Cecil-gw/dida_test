using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.CountDown.Views;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Widget;

public class WidgetSettings : Window, IComponentConnector
{
	private static readonly Dictionary<string, int> DisplayDict = new Dictionary<string, int>
	{
		{ "embed", 0 },
		{ "top", 1 }
	};

	private bool _isSingle;

	private bool _initializingDisplayOptions;

	private CalendarWidgetModel _aiSettings;

	private Action _applyAIAppearance;

	private Dictionary<string, string> _sortNameTypeDict;

	private List<string> _orderDirectionValues = new List<string>();

	private bool _isFirstShow = true;

	internal StackPanel NormalGrid;

	internal Border HabitOptionBorder;

	internal CustomDataComboBox HabitNameCombo;

	internal StackPanel ThemeOpacityPanel;

	internal TextBlock ThemeAndStyleTitle;

	internal CustomSimpleComboBox ThemeComboBox;

	internal Slider OpacitySlider;

	internal TextBlock TaskOptionText;

	internal Border TaskOptionBorder;

	internal Grid GroupOptionPanel;

	internal TextBlock GroupOptionText;

	internal CustomSimpleComboBox GroupTypeComboBox;

	internal Grid SortOptionPanel;

	internal TextBlock SortOptionText;

	internal CustomSimpleComboBox SortTypeComboBox;

	internal Grid OrderDirectionPanel;

	internal TextBlock OrderDirectionText;

	internal CustomSimpleComboBox OrderDirectionComboBox;

	internal TextBlock WidgetDisplayTitle;

	internal CheckBox TopmostCheckBox;

	internal CheckBox AutoHideCheckBox;

	internal CheckBox ShowAddCheckBox;

	internal Grid ShowCompletedPanel;

	internal TextBlock ShowCompletedText;

	internal CheckBox ShowCompletedCheckBox;

	internal CustomSimpleComboBox ShowDateByComboBox;

	internal Grid ShowDayUnitGrid;

	internal CheckBox ShowDayUnitCheckBox;

	internal Grid CountdownSetGrid;

	internal Grid CountdownStyleMainViewGrid;

	internal CountdownStyleMainView CountdownMainPreview;

	internal Border EmptyCoundownView;

	internal Image EmptyImage;

	internal new TextBlock AddText;

	internal CustomDataComboBox CountdownNameCombo;

	internal Border CountdownStyleOperateGrid;

	internal StackPanel CountdownPartGrid;

	internal CountdownStyleTypePreview CountdownTypePreview;

	internal StackPanel CountdownDefaultSetGrid;

	internal CheckBox CountdownAutoHideCheckBox;

	internal CustomDataComboBox CountdownDateDisplayCombo;

	internal Grid ToastGrid;

	private bool _contentLoaded;

	private CountDownStyleSetViewModel countDownStyleVM { get; set; }

	private WidgetViewModel Model => (WidgetViewModel)base.DataContext;

	public string WidgetId => Model.Id;

	public event EventHandler HideCompleteChanged;

	public event EventHandler<string> DisplayOptionChanged;

	public event EventHandler<string> SortTypeChanged;

	public event EventHandler<string> GroupTypeChanged;

	public event EventHandler<string> OrderDirectionChanged;

	public event EventHandler<float> OpacityChanged;

	private void OnCompatibilityShortcut(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.F12 || Keyboard.Modifiers != (ModifierKeys.Control | ModifierKeys.Shift))
		{
			return;
		}
		e.Handled = true;
		MenuItem option = new MenuItem
		{
			Header = (Utils.IsCn() ? "桌面小组件兼容模式（所有小组件）" : "Desktop widget compatibility mode (all widgets)"),
			ToolTip = (Utils.IsCn() ? "立即应用于所有桌面小组件。开启后，非置顶小组件可能随“显示桌面”（Win+D）隐藏。" : "Applies immediately to all desktop widgets. When enabled, widgets that are not always on top may be hidden by Show desktop (Win+D)."),
			IsCheckable = true,
			IsChecked = LocalSettings.Settings.ExtraSettings.DisableWidgetDesktopOwner
		};
		option.Click += async delegate
		{
			int num = WidgetWindow.SetDesktopOwnerCompatibility(option.IsChecked);
			List<string> messages = new List<string>();
			if (num > 0)
			{
				messages.Add(Utils.IsCn() ? $"有 {num} 个小组件未能应用此设置，当前可能仍保持原来的行为。请重新打开小组件后重试，并提交 Windows 日志。" : $"The setting could not be applied to {num} widget(s). They may still behave as before. Reopen the widgets to retry and submit the Windows logs.");
			}
			try
			{
				await LocalSettings.Settings.Save(force: true);
			}
			catch (Exception ex)
			{
				UtilLog.Error("WidgetDesktopOwner save failed: " + ex);
				messages.Add(Utils.IsCn() ? "设置保存失败，重启应用后可能恢复为之前的设置。" : "The setting could not be saved and may revert after restarting the app.");
			}
			if (messages.Count > 0 && base.IsLoaded)
			{
				CustomerDialog customerDialog = new CustomerDialog(Utils.IsCn() ? "小组件兼容设置" : "Widget compatibility setting", string.Join("\n\n", messages), MessageBoxButton.OK);
				customerDialog.Owner = this;
				customerDialog.ShowDialog();
			}
		};
		new ContextMenu
		{
			PlacementTarget = this,
			Items = { (object)option },
			IsOpen = true
		};
	}

	public WidgetSettings(WidgetViewModel model, bool isSingle = false)
	{
		InitializeComponent();
		base.PreviewKeyDown += OnCompatibilityShortcut;
		base.DataContext = model;
		_initializingDisplayOptions = true;
		InitDisplayOption();
		_initializingDisplayOptions = false;
		_isSingle = isSingle;
		countDownStyleVM = new CountDownStyleSetViewModel
		{
			IsWidgetCard = true
		};
		CountdownStyleMainViewGrid.DataContext = (CountdownStyleOperateGrid.DataContext = countDownStyleVM);
		switch (model.WidgetType)
		{
		case WidgetType.CountdownItem:
		{
			CountdownSetGrid.Visibility = Visibility.Visible;
			NormalGrid.Visibility = Visibility.Collapsed;
			countDownStyleVM.SetParentWindow(this);
			CountDownStyleSetViewModel countDownStyleSetViewModel2 = countDownStyleVM;
			countDownStyleSetViewModel2.StyleSelected = (Action<StyleSelectionResult>)Delegate.Combine(countDownStyleSetViewModel2.StyleSelected, new Action<StyleSelectionResult>(StyleControl_StyleSelected));
			CountdownDateDisplayCombo.Init(new List<DataComboBoxViewModel>
			{
				new DataComboBoxViewModel(CountdownWidgetModel.DisplayDescType.UntilOrSince, Utils.GetString("DaysLeftOrSince")),
				new DataComboBoxViewModel(CountdownWidgetModel.DisplayDescType.Detail, Utils.GetString("date"))
			});
			CountdownDateDisplayCombo.SelectedIndex = ((model.CountdownDisplayDescType != CountdownWidgetModel.DisplayDescType.UntilOrSince) ? 1 : 0);
			InitCountdownNames();
			ThemeUtil.SetTheme(model.ThemeId, CountdownMainPreview.StylePreviewCard);
			CountdownDefaultSetGrid.Visibility = ((!(model.CountdownStyle == "normal") && !(model.CountdownStyle == "calendar")) ? Visibility.Collapsed : Visibility.Visible);
			break;
		}
		case WidgetType.Calendar:
			base.Title = Utils.GetString("ConfigCalendarWidget");
			break;
		case WidgetType.Matrix:
			base.Title = Utils.GetString("ConfigMatrixWidget");
			break;
		case WidgetType.CountdownList:
			ShowDayUnitGrid.Visibility = Visibility.Visible;
			break;
		case WidgetType.HabitItem:
			ThemeOpacityPanel.Visibility = Visibility.Collapsed;
			InitHabitNames();
			break;
		case WidgetType.HabitWeekItem:
			InitHabitNames();
			break;
		}
		if (model.WidgetType != WidgetType.List)
		{
			TaskOptionBorder.Visibility = Visibility.Collapsed;
			TaskOptionText.Visibility = Visibility.Collapsed;
			if (ShowDayUnitGrid.Visibility == Visibility.Collapsed)
			{
				AutoHideCheckBox.Margin = new Thickness(0.0, 0.0, 0.0, 12.0);
			}
		}
		if (model.WidgetType != WidgetType.HabitItem && model.WidgetType != WidgetType.HabitWeekItem)
		{
			HabitOptionBorder.Visibility = Visibility.Collapsed;
		}
		if (model.WidgetType == WidgetType.CountdownItem)
		{
			base.Visibility = Visibility.Collapsed;
			base.ShowActivated = false;
		}
	}

	public WidgetSettings(CalendarWidgetModel settings, Action applyAppearance)
		: this(new WidgetViewModel(settings), isSingle: true)
	{
		_aiSettings = settings;
		_applyAIAppearance = applyAppearance;
		base.Title = Utils.GetString("ConfigAIWidget");
		ThemeAndStyleTitle.Visibility = Visibility.Visible;
		WidgetDisplayTitle.Visibility = Visibility.Visible;
		OpacitySlider.Minimum = 2.0;
		Model.PropertyChanged += OnAIAppearancePropertyChanged;
		base.Closed += delegate
		{
			Model.PropertyChanged -= OnAIAppearancePropertyChanged;
		};
		base.PreviewKeyDown += delegate(object sender, KeyEventArgs args)
		{
			if (args.Key == Key.Escape && !ThemeComboBox.IsOpen)
			{
				args.Handled = true;
				Close();
			}
		};
	}

	private void OnAIAppearancePropertyChanged(object sender, PropertyChangedEventArgs args)
	{
		if (args.PropertyName == "AutoHide")
		{
			SaveAppearance();
		}
	}

	private void SaveAppearance()
	{
		if (_aiSettings == null)
		{
			Model.Save();
			return;
		}
		_aiSettings.themeId = Model.ThemeId;
		_aiSettings.opacity = Model.Opacity;
		_aiSettings.displayOption = Model.DisplayOption;
		_aiSettings.autoHide = Model.AutoHide;
		_applyAIAppearance?.Invoke();
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		if (Model.WidgetType == WidgetType.CountdownItem && _isFirstShow)
		{
			base.Dispatcher.Invoke(delegate
			{
				UpdateLayout();
			}, DispatcherPriority.Loaded);
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				base.Visibility = Visibility.Visible;
				base.ShowActivated = true;
				Activate();
				_isFirstShow = false;
			}, DispatcherPriority.ContextIdle);
		}
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is WidgetViewModel { WidgetType: WidgetType.List })
		{
			InitSortType();
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	public void Rebind(WidgetViewModel model)
	{
		if (model != null)
		{
			base.DataContext = model;
			UtilLog.Info("WidgetRebind " + model.GroupType + "  " + model.SortType);
			if (!model.IsSingleMode)
			{
				InitDisplayOption();
			}
			if (model.WidgetType == WidgetType.List)
			{
				InitSortType();
			}
		}
	}

	private async Task InitCountdownNames()
	{
		List<CountdownModel> countdowns = await CountdownDao.GetActiveCountdowns();
		IEnumerable<CountdownModel> obj = await Task.Run(() => MainCountDownViewModel.SortAllModels(countdowns, isArchivedOrder: false));
		List<DataComboBoxViewModel> list = new List<DataComboBoxViewModel>
		{
			new DataComboBoxViewModel(string.Empty, Utils.GetString("UpcomingDays"))
		};
		foreach (CountdownModel item2 in obj)
		{
			DataComboBoxViewModel item = new DataComboBoxViewModel(item2.Id, item2.Name);
			list.Add(item);
		}
		CountdownNameCombo.Init(list);
		await ReloadCountdown(setIndex: true);
	}

	public async Task ReloadCountdown(bool setIndex = false)
	{
		CountdownModel countdownModel = ((!string.IsNullOrEmpty(Model.CountdownId)) ? (await CountdownDao.GetCountdownById(Model.CountdownId)) : (await CountdownDao.GetNearestCountdown()));
		if (countdownModel == null)
		{
			DateTime today = DateTime.Today;
			DateTime dateTime = ((today.Month == 1 && today.Day == 1) ? today : new DateTime(today.AddYears(1).Year, 1, 1));
			countdownModel = new CountdownModel
			{
				Name = Utils.GetString("new_years_day"),
				Date = int.Parse(dateTime.ToString("yyyyMMdd")),
				IconRes = "countdown_new_year_day"
			};
			CountdownMainPreview.Visibility = Visibility.Collapsed;
			EmptyCoundownView.Visibility = Visibility.Visible;
		}
		else
		{
			CountdownMainPreview.Visibility = Visibility.Visible;
			EmptyCoundownView.Visibility = Visibility.Collapsed;
		}
		Model.UpdateCountdownModel(countdownModel);
		countDownStyleVM?.Init(countdownModel);
		if (setIndex)
		{
			CountdownNameCombo.SelectedIndex = CountdownNameCombo.Items.IndexOf(CountdownNameCombo.Items.FirstOrDefault((DataComboBoxViewModel x) => x.Data.ToString() == Model.CountdownId));
		}
	}

	private async Task InitHabitNames()
	{
		List<HabitModel> source = (await HabitDao.GetAllHabits()).Where((HabitModel it) => it.Status == 0).ToList();
		List<DataComboBoxViewModel> list = new List<DataComboBoxViewModel>();
		if (!source.Any())
		{
			list.Add(new DataComboBoxViewModel(null, Utils.GetString("Choose")));
		}
		else
		{
			foreach (HabitModel item2 in source.OrderBy((HabitModel x) => x.SortOrder))
			{
				DataComboBoxViewModel item = new DataComboBoxViewModel(item2.Id, item2.Name);
				list.Add(item);
			}
		}
		HabitNameCombo.Init(list);
		HabitNameCombo.SelectedIndex = list.IndexOf(list.FirstOrDefault((DataComboBoxViewModel x) => x.Data?.ToString() == Model.HabitId));
	}

	public async void InitSortType()
	{
		UtilLog.Info("InitSortType " + Model.GroupType + "  " + Model.SortType);
		_sortNameTypeDict = new Dictionary<string, string>();
		Dictionary<string, string> dictionary;
		switch (Model.Type)
		{
		case "normal":
		{
			ProjectModel projectModel = await ProjectDao.GetProjectById(Model.Identity);
			dictionary = ((projectModel != null) ? SortOptionHelper.GetSortOptionDict(normalProject: true, projectModel.IsShareList(), isTag: false, isTagParent: false, projectModel.IsNote ? TaskType.Note : TaskType.Task) : SortOptionHelper.GetSortOptionDict(normalProject: false, withShare: false));
			break;
		}
		case "tag":
			dictionary = SortOptionHelper.GetSortOptionDict(normalProject: false, withShare: false, isTag: true, TagDao.IsParentTag(Model.Identity));
			break;
		case "group":
		{
			ObservableCollection<ProjectModel> observableCollection = await ProjectDao.GetProjectsInGroup(Model.Identity);
			TaskType kind = TaskType.Task;
			if (observableCollection != null && observableCollection.Count > 0)
			{
				bool flag = false;
				bool flag2 = false;
				foreach (ProjectModel item in observableCollection)
				{
					if (item.IsNote)
					{
						flag2 = true;
					}
					else
					{
						flag = true;
					}
				}
				kind = (flag2 ? ((!flag) ? TaskType.Note : TaskType.TaskAndNote) : TaskType.Task);
			}
			dictionary = SortOptionHelper.GetSortOptionDictInGroup(kind);
			break;
		}
		default:
			dictionary = SortOptionHelper.GetSortOptionDict(normalProject: false, withShare: false);
			break;
		}
		if (dictionary == null)
		{
			return;
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		List<string> list4 = new List<string>();
		foreach (KeyValuePair<string, string> item2 in dictionary)
		{
			if (item2.Key != "project")
			{
				list.Add(item2.Key);
				list3.Add(item2.Value);
			}
			if (item2.Key != "title" && item2.Key != "modifiedTime")
			{
				list2.Add(item2.Key);
				list4.Add(item2.Value);
			}
			_sortNameTypeDict[item2.Value] = item2.Key;
		}
		list2.Add("none");
		list4.Add(Utils.GetString("none"));
		_sortNameTypeDict[Utils.GetString("none")] = "none";
		SortTypeComboBox.Init(list3);
		GroupTypeComboBox.Init(list4);
		int num = list.IndexOf(Model.SortType);
		if (num >= 0)
		{
			SortTypeComboBox.SelectedIndex = num;
		}
		else
		{
			SortTypeComboBox.SelectedIndex = 0;
			Model.SortType = list[0];
		}
		int num2 = list2.IndexOf(Model.GroupType);
		if (num2 >= 0)
		{
			GroupTypeComboBox.SelectedIndex = num2;
		}
		else
		{
			GroupTypeComboBox.SelectedIndex = 0;
			Model.GroupType = list2[0];
		}
		InitOrderDirectionOptions();
	}

	private void OnGroupTypeChanged(object sender, SimpleComboBoxViewModel model)
	{
		string title = model.Title;
		if (_sortNameTypeDict != null && _sortNameTypeDict.TryGetValue(title, out var value) && !string.IsNullOrEmpty(value))
		{
			Model.GroupType = value;
			Model.Save();
			this.GroupTypeChanged?.Invoke(this, value);
		}
	}

	private void OnSortTypeChanged(object sender, SimpleComboBoxViewModel model)
	{
		string title = model.Title;
		if (_sortNameTypeDict != null && _sortNameTypeDict.TryGetValue(title, out var value) && !string.IsNullOrEmpty(value))
		{
			Model.SortType = value;
			Model.Save();
			this.SortTypeChanged?.Invoke(this, value);
			InitOrderDirectionOptions();
		}
	}

	private void InitOrderDirectionOptions()
	{
		string sortType = Model.SortType;
		if (string.IsNullOrEmpty(sortType) || !Enum.TryParse<Constants.SortType>(sortType, ignoreCase: true, out var _))
		{
			OrderDirectionPanel.Visibility = Visibility.Collapsed;
			return;
		}
		SortOption sortOption = new SortOption
		{
			orderBy = sortType
		};
		if (!sortOption.SupportsDirection)
		{
			OrderDirectionPanel.Visibility = Visibility.Collapsed;
			return;
		}
		List<string> list = new List<string>();
		_orderDirectionValues.Clear();
		string directionName = SortTypeViewModel.GetDirectionName(sortType, Constants.OrderDirection.Asc);
		if (!string.IsNullOrEmpty(directionName))
		{
			list.Add(directionName);
			_orderDirectionValues.Add("asc");
		}
		string directionName2 = SortTypeViewModel.GetDirectionName(sortType, Constants.OrderDirection.Desc);
		if (!string.IsNullOrEmpty(directionName2))
		{
			list.Add(directionName2);
			_orderDirectionValues.Add("desc");
		}
		if (list.Count == 0)
		{
			OrderDirectionPanel.Visibility = Visibility.Collapsed;
			return;
		}
		OrderDirectionPanel.Visibility = Visibility.Visible;
		OrderDirectionComboBox.Init(list);
		string order = Model.Order;
		Constants.OrderDirection? defaultDirection = sortOption.GetDefaultDirection();
		int num = ((order == "desc" || (string.IsNullOrEmpty(order) && defaultDirection == Constants.OrderDirection.Desc)) ? 1 : 0);
		OrderDirectionComboBox.SelectedIndex = num;
		if (string.IsNullOrEmpty(order))
		{
			Model.Order = _orderDirectionValues[num];
		}
	}

	private void OnOrderDirectionChanged(object sender, SimpleComboBoxViewModel model)
	{
		int selectedIndex = OrderDirectionComboBox.SelectedIndex;
		if (selectedIndex >= 0 && selectedIndex < _orderDirectionValues.Count)
		{
			string text = _orderDirectionValues[selectedIndex];
			Model.Order = text;
			Model.Save();
			this.OrderDirectionChanged?.Invoke(this, text);
		}
	}

	private void InitDisplayOption()
	{
		InitThemeComboBox();
		TopmostCheckBox.Checked -= OnTopmostCheckedChanged;
		TopmostCheckBox.Unchecked -= OnTopmostCheckedChanged;
		string displayOption = Model.DisplayOption;
		TopmostCheckBox.IsChecked = displayOption == "top";
		TopmostCheckBox.Checked += OnTopmostCheckedChanged;
		TopmostCheckBox.Unchecked += OnTopmostCheckedChanged;
		ShowAddCheckBox.IsChecked = LocalSettings.Settings.ExtraSettings.PwShowAdd;
		ShowDayUnitCheckBox.IsChecked = Model.IsShowUnit;
		ShowDateByComboBox.Init(new List<string>
		{
			Utils.GetString("task_time"),
			Utils.GetString("Countdown")
		});
		ShowDateByComboBox.SelectedIndex = (Model.ShowCountDown ? 1 : 0);
		if (Model.Opacity >= 0f && Model.Opacity <= 1f)
		{
			OpacitySlider.ValueChanged -= OnSlideValueChanged;
			OpacitySlider.Value = Model.Opacity * 10f;
			OpacitySlider.ToolTip = (int)(OpacitySlider.Value * 10.0) + "%";
			OpacitySlider.ValueChanged += OnSlideValueChanged;
		}
	}

	private async void OnTopmostCheckedChanged(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		string option = ((TopmostCheckBox.IsChecked == true) ? "top" : "embed");
		if (!string.IsNullOrEmpty(option))
		{
			Model.DisplayOption = option;
			SaveAppearance();
			await Task.Delay(100);
			this.DisplayOptionChanged?.Invoke(this, option);
		}
	}

	private void OnSlideValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		Model.Opacity = (float)e.NewValue / 10f;
		OpacitySlider.ToolTip = (int)(e.NewValue * 10.0) + "%";
		SaveAppearance();
		this.OpacityChanged?.Invoke(this, Model.Opacity);
	}

	private void OnCompletedCheckedChanged(object sender, RoutedEventArgs e)
	{
		Model.Save();
		this.HideCompleteChanged?.Invoke(this, null);
	}

	private void OnShowDateBySelected(object sender, SimpleComboBoxViewModel e)
	{
		bool flag = ShowDateByComboBox.SelectedIndex == 1;
		if (Model.ShowCountDown != flag)
		{
			Model.ShowCountDown = flag;
			Model.Save();
		}
	}

	private void OnThemeChecked(object sender, RoutedEventArgs e)
	{
		if (sender is RadioButton { Tag: string tag } && tag != Model.ThemeId)
		{
			Model.ThemeId = ((Model.ThemeId == "light") ? "dark" : "light");
			Model.Save();
			this.DisplayOptionChanged?.Invoke(this, Model.ThemeId);
		}
	}

	private void InitThemeComboBox()
	{
		List<string> list = new List<string>
		{
			Utils.GetString("DarkColor"),
			Utils.GetString("LightColor")
		};
		if (Model.WidgetType != WidgetType.AI)
		{
			list.Add(Utils.GetString("FollowSystem"));
		}
		ThemeComboBox.ItemsSource = list;
		switch (Model.ThemeId)
		{
		default:
			ThemeComboBox.SelectedIndex = 0;
			break;
		case "light":
			ThemeComboBox.SelectedIndex = 1;
			break;
		case "auto":
			ThemeComboBox.SelectedIndex = 2;
			break;
		}
	}

	private void OnThemeChanged(object sender, SimpleComboBoxViewModel e)
	{
		if (!_initializingDisplayOptions)
		{
			switch (ThemeComboBox.SelectedIndex)
			{
			default:
				Model.ThemeId = "dark";
				break;
			case 1:
				Model.ThemeId = "light";
				break;
			case 2:
				Model.ThemeId = "auto";
				break;
			}
			SaveAppearance();
			this.DisplayOptionChanged?.Invoke(this, Model.ThemeId);
			if (Model.WidgetType == WidgetType.CountdownItem)
			{
				ThemeUtil.SetTheme(Model.ThemeId, CountdownMainPreview.StylePreviewCard);
			}
		}
	}

	private void OnAddCheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		LocalSettings.Settings.ExtraSettings.PwShowAdd = !LocalSettings.Settings.ExtraSettings.PwShowAdd;
		ShowAddCheckBox.IsChecked = LocalSettings.Settings.ExtraSettings.PwShowAdd;
		ProjectWidgetsHelper.OnShowAddChanged();
	}

	private void OnShowDayUnitCheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		Model.IsShowUnit = !Model.IsShowUnit;
		Model.Save();
		CountdownWidgetsHelper.OnShowListUnitChange();
	}

	private void CountdownNameCombo_ItemSelected(object sender, DataComboBoxViewModel e)
	{
		string countdownId = e.Data.ToString();
		Model.CountdownId = countdownId;
		ReloadCountdown();
		Model.Save();
		CountdownWidgetsHelper.OnStyleChange(Model.Id);
	}

	private void StyleControl_StyleSelected(StyleSelectionResult e)
	{
		Model.CountdownLocalImagePath = e.LocalImagePath;
		Model.CountdownStyle = e.Style;
		Model.CountdownStyleColors = e.StyleColors;
		string attachmentId = e.AttachmentId;
		string text = attachmentId;
		if (!string.IsNullOrEmpty(attachmentId) && attachmentId.Length > 1)
		{
			text = attachmentId;
		}
		else if (!string.IsNullOrEmpty(e.PresetImage))
		{
			text = "preset_" + e.PresetImage;
		}
		if (!string.IsNullOrEmpty(text))
		{
			BackgroundModel backgroundModel = Model.CountdownBackground ?? new BackgroundModel();
			backgroundModel.Id = text;
			if (e.ClipRect.Width > 0.0)
			{
				backgroundModel.Landscape = new CropModel
				{
					Top = e.ClipRect.Top,
					Right = e.ClipRect.Right,
					Bottom = e.ClipRect.Bottom,
					Left = e.ClipRect.Left
				};
			}
			else
			{
				backgroundModel.Landscape = null;
			}
			Model.CountdownBackground = backgroundModel;
		}
		Model.Save();
		CountdownDefaultSetGrid.Visibility = ((!(e.Style == "normal") && !(e.Style == "calendar")) ? Visibility.Collapsed : Visibility.Visible);
		CountdownWidgetsHelper.OnStyleChange(Model.Id);
	}

	private void CountdownDateDisplayCombo_ItemSelected(object sender, DataComboBoxViewModel e)
	{
		CountdownWidgetModel.DisplayDescType countdownDisplayDescType = (CountdownWidgetModel.DisplayDescType)e.Data;
		Model.CountdownDisplayDescType = countdownDisplayDescType;
		ReloadCountdown();
		Model.Save();
		CountdownWidgetsHelper.OnStyleChange(Model.Id);
	}

	public void OnCountdownSetGridClick(object sender, MouseButtonEventArgs e)
	{
		countDownStyleVM?.CloseSelectImageWindow();
	}

	private void HabitNameCombo_ItemSelected(object sender, DataComboBoxViewModel e)
	{
		if (e.Data != null)
		{
			string habitId = e.Data.ToString();
			Model.HabitId = habitId;
			Model.Save();
			HabitWidgetHelper.OnHabitChanged(Model.Id);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/widgetsettings.xaml", UriKind.Relative);
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
			((WidgetSettings)target).Loaded += OnWindowLoaded;
			break;
		case 2:
			NormalGrid = (StackPanel)target;
			break;
		case 3:
			HabitOptionBorder = (Border)target;
			break;
		case 4:
			HabitNameCombo = (CustomDataComboBox)target;
			break;
		case 5:
			ThemeOpacityPanel = (StackPanel)target;
			break;
		case 6:
			ThemeAndStyleTitle = (TextBlock)target;
			break;
		case 7:
			ThemeComboBox = (CustomSimpleComboBox)target;
			break;
		case 8:
			OpacitySlider = (Slider)target;
			break;
		case 9:
			TaskOptionText = (TextBlock)target;
			break;
		case 10:
			TaskOptionBorder = (Border)target;
			break;
		case 11:
			GroupOptionPanel = (Grid)target;
			break;
		case 12:
			GroupOptionText = (TextBlock)target;
			break;
		case 13:
			GroupTypeComboBox = (CustomSimpleComboBox)target;
			break;
		case 14:
			SortOptionPanel = (Grid)target;
			break;
		case 15:
			SortOptionText = (TextBlock)target;
			break;
		case 16:
			SortTypeComboBox = (CustomSimpleComboBox)target;
			break;
		case 17:
			OrderDirectionPanel = (Grid)target;
			break;
		case 18:
			OrderDirectionText = (TextBlock)target;
			break;
		case 19:
			OrderDirectionComboBox = (CustomSimpleComboBox)target;
			break;
		case 20:
			WidgetDisplayTitle = (TextBlock)target;
			break;
		case 21:
			TopmostCheckBox = (CheckBox)target;
			break;
		case 22:
			AutoHideCheckBox = (CheckBox)target;
			break;
		case 23:
			ShowAddCheckBox = (CheckBox)target;
			ShowAddCheckBox.PreviewMouseLeftButtonUp += OnAddCheckBoxClick;
			break;
		case 24:
			ShowCompletedPanel = (Grid)target;
			break;
		case 25:
			ShowCompletedText = (TextBlock)target;
			break;
		case 26:
			ShowCompletedCheckBox = (CheckBox)target;
			ShowCompletedCheckBox.Checked += OnCompletedCheckedChanged;
			ShowCompletedCheckBox.Unchecked += OnCompletedCheckedChanged;
			break;
		case 27:
			ShowDateByComboBox = (CustomSimpleComboBox)target;
			break;
		case 28:
			ShowDayUnitGrid = (Grid)target;
			break;
		case 29:
			ShowDayUnitCheckBox = (CheckBox)target;
			ShowDayUnitCheckBox.PreviewMouseLeftButtonUp += OnShowDayUnitCheckBoxClick;
			break;
		case 30:
			CountdownSetGrid = (Grid)target;
			CountdownSetGrid.MouseLeftButtonUp += OnCountdownSetGridClick;
			break;
		case 31:
			CountdownStyleMainViewGrid = (Grid)target;
			break;
		case 32:
			CountdownMainPreview = (CountdownStyleMainView)target;
			break;
		case 33:
			EmptyCoundownView = (Border)target;
			break;
		case 34:
			EmptyImage = (Image)target;
			break;
		case 35:
			AddText = (TextBlock)target;
			break;
		case 36:
			CountdownNameCombo = (CustomDataComboBox)target;
			break;
		case 37:
			CountdownStyleOperateGrid = (Border)target;
			break;
		case 38:
			CountdownPartGrid = (StackPanel)target;
			break;
		case 39:
			CountdownTypePreview = (CountdownStyleTypePreview)target;
			break;
		case 40:
			CountdownDefaultSetGrid = (StackPanel)target;
			break;
		case 41:
			CountdownAutoHideCheckBox = (CheckBox)target;
			break;
		case 42:
			CountdownDateDisplayCombo = (CustomDataComboBox)target;
			break;
		case 43:
			ToastGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
