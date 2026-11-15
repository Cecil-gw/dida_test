using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo.MainFocus;

public class MainFocusControl : Grid
{
	private AnimateGroupTitle _focusSwitch;

	private StackPanel _focusTitlePanel;

	private FocusOptionButtons _buttonsPanel;

	private FocusTimeClock _timeClock;

	public FocusBreakControl _relaxPanel;

	private EmjTextBlock _focusTitle;

	private double _timeWidth = 268.0;

	private PomoFilterControl _pomoFilter;

	private List<TimerModel> _timers;

	public bool IsShowClockPanel;

	private HoverIconButton _soundButton;

	private HoverIconButton _addButton;

	private TextBlock _title;

	private Border _foldClockButton;

	public Grid ToastGrid;

	private static bool _showArchiveList;

	private AnimateGroupTitle _archiveSwitch;

	private FocusTimerListView _timerListView;

	private SmallFocusView _smallFocusView;

	private string _selectedId;

	private EscPopup taskPopup { get; set; }

	private bool isOpenPopup { get; set; }

	private EscPopup MorePopup { get; set; }

	private EscPopup SoundPopup { get; set; }

	public bool IsListMouseOver => _timerListView?.IsMouseOver ?? false;

	private void SetClockPanel()
	{
		SetFocusTimerSwitch(TickFocusManager.Status == PomoStatus.WaitingWork);
		_addButton.Visibility = ((TickFocusManager.Status != PomoStatus.WaitingWork || (_timers != null && _timers.Count != 0)) ? Visibility.Collapsed : Visibility.Visible);
		bool flag = TickFocusManager.Status == PomoStatus.WaitingRelax;
		SetFocusTitleAndClock(!flag);
		SetBreakPanel(flag);
		_soundButton.Visibility = ((_addButton.Visibility == Visibility.Visible || flag) ? Visibility.Collapsed : Visibility.Visible);
		SetButtonContainer(show: true);
		_buttonsPanel.Margin = new Thickness(0.0, (!flag) ? (-14) : 0, 0.0, 0.0);
		_buttonsPanel.SetButtons();
		_timeClock?.OnStatusChanged();
	}

	private void SetFocusTimerSwitch(bool show)
	{
		if (show && _focusSwitch == null)
		{
			_focusSwitch = new AnimateGroupTitle
			{
				Titles = "PomoTimer2|Timing",
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Center,
				Margin = new Thickness(0.0, 30.0, 0.0, 0.0),
				Height = 34.0
			};
			_focusSwitch.SetSelectedIndex((!TickFocusManager.IsPomo) ? 1 : 0);
			base.Children.Add(_focusSwitch);
			_focusSwitch.SelectedTitleChanged += OnTypeSelected;
		}
		else if (!show && _focusSwitch != null)
		{
			_focusSwitch.SelectedTitleChanged -= OnTypeSelected;
			base.Children.Remove(_focusSwitch);
			_focusSwitch = null;
		}
	}

	private void InitFocusTitle()
	{
		_focusTitlePanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Bottom,
			Background = Brushes.Transparent,
			Margin = new Thickness(4.0, 0.0, 0.0, 40.0),
			Cursor = Cursors.Hand
		};
		_focusTitle = new EmjTextBlock
		{
			FontSize = 14.0,
			TextTrimming = TextTrimming.CharacterEllipsis,
			TextWrapping = TextWrapping.Wrap,
			MaxHeight = 18.0,
			ClipToBounds = true
		};
		double val = Math.Min(360.0, Math.Max(240.0, base.ActualWidth / 2.1));
		double val2 = Math.Min(360.0, Math.Max(240.0, base.ActualHeight - 460.0));
		val = Math.Min(val, val2);
		_focusTitle.MaxWidth = Math.Max(val - 20.0, 200.0);
		_focusTitle.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		_focusTitlePanel.Children.Add(_focusTitle);
		_focusTitle.Text = (TickFocusManager.NoFocus ? Utils.GetString("Focus") : TickFocusManager.Config.FocusVModel.Title);
		_focusTitle.Opacity = (TickFocusManager.NoFocus ? 0.6 : 1.0);
		Path path = new Path
		{
			Data = Utils.GetGeometry("Ic.Common.DownArrow"),
			Margin = new Thickness(4.0, 0.0, 0.0, 0.0),
			Height = 14.0,
			Width = 14.0,
			Stretch = Stretch.Fill,
			RenderTransformOrigin = new Point(0.5, 0.5)
		};
		path.SetResourceReference(Shape.FillProperty, "BaseColorOpacity40");
		path.RenderTransform = new RotateTransform
		{
			Angle = 270.0
		};
		_focusTitlePanel.Children.Add(path);
		_focusTitlePanel.SetValue(Grid.RowProperty, 1);
		base.Children.Add(_focusTitlePanel);
		_focusTitlePanel.Margin = _timeClock.Margin;
		_focusTitlePanel.MouseLeftButtonUp += OnTitleClick;
	}

	private void SetFocusTitle(bool show)
	{
		if (_focusTitlePanel == null && show)
		{
			InitFocusTitle();
			PropertyChangedEventManager.AddHandler(TickFocusManager.Config.FocusVModel, OnFocusTitleChanged, "Title");
		}
		else if (_focusTitlePanel != null && !show)
		{
			_focusTitlePanel.MouseLeftButtonUp -= OnTitleClick;
			base.Children.Remove(_focusTitlePanel);
			_focusTitlePanel.Children.Clear();
			_focusTitle = null;
			_focusTitlePanel = null;
			PropertyChangedEventManager.RemoveHandler(TickFocusManager.Config.FocusVModel, OnFocusTitleChanged, "Title");
		}
	}

	private void SetTimePanel(bool show)
	{
		if (_timeClock == null && show)
		{
			_timeClock = new FocusTimeClock();
			_timeClock.SetSize(_timeWidth);
			double bottom = Math.Min(100.0, Math.Max(36.0, (base.ActualHeight - _timeClock.Height - 380.0) / 2.0));
			_timeClock.Margin = new Thickness(0.0, 0.0, 0.0, bottom);
			_timeClock.SetValue(Grid.RowProperty, 2);
			base.Children.Add(_timeClock);
		}
		else if (_timeClock != null && !show)
		{
			base.Children.Remove(_timeClock);
			_timeClock = null;
		}
	}

	private void SetFocusTitleAndClock(bool show)
	{
		SetTimePanel(show);
		SetFocusTitle(show);
	}

	private void SetBreakPanel(bool show)
	{
		if (_relaxPanel == null)
		{
			_relaxPanel = new FocusBreakControl
			{
				Margin = new Thickness(0.0, -100.0, 0.0, 0.0)
			};
			base.Children.Add(_relaxPanel);
			_relaxPanel.SetValue(Grid.RowProperty, 2);
		}
		if (show)
		{
			_relaxPanel.Visibility = Visibility.Visible;
			_relaxPanel.SetRelaxText();
		}
		else
		{
			_relaxPanel.Visibility = Visibility.Collapsed;
		}
	}

	private void SetButtonContainer(bool show)
	{
		if (_buttonsPanel == null && show)
		{
			_buttonsPanel = new FocusOptionButtons
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, -14.0, 0.0, 0.0)
			};
			_buttonsPanel.SetValue(Grid.RowProperty, 3);
			_buttonsPanel.SetValue(Grid.RowSpanProperty, 2);
			double num = 140.0 + (_timeWidth - 300.0) / 2.0;
			_buttonsPanel.SetButtonWidth(num, Math.Min(48.0, 48.0 + (num - 140.0) / 2.5));
			base.Children.Add(_buttonsPanel);
			_buttonsPanel.SetButtons();
		}
		else if (_buttonsPanel != null && !show)
		{
			base.Children.Remove(_buttonsPanel);
			_buttonsPanel = null;
		}
	}

	private void OnTypeSelected(object sender, GroupTitleViewModel e)
	{
		TickFocusManager.SetFocusType(e.Index);
		UserActCollectUtils.AddClickEvent("focus", "focus_tab", (e.Index == 0) ? "tab_pomo" : "tab_stopwatch");
	}

	private void OnTitleClick(object sender, MouseButtonEventArgs e)
	{
		if (isOpenPopup)
		{
			return;
		}
		if (_pomoFilter == null)
		{
			taskPopup = new EscPopup
			{
				StaysOpen = false,
				PlacementTarget = (sender as UIElement),
				Placement = PlacementMode.Center,
				HorizontalOffset = -3.0,
				VerticalOffset = 208.0
			};
			taskPopup.Closed += async delegate
			{
				await Task.Delay(100);
				isOpenPopup = false;
			};
			_pomoFilter = new PomoFilterControl(taskPopup)
			{
				DataContext = TickFocusManager.Config.FocusVModel
			};
		}
		Popup parentPopup = _pomoFilter.GetParentPopup();
		parentPopup.PlacementTarget = sender as UIElement;
		parentPopup.Placement = PlacementMode.Center;
		isOpenPopup = true;
		_pomoFilter.Open();
	}

	private void HideClockPanel()
	{
		SetFocusTimerSwitch(show: false);
		SetFocusTitleAndClock(show: false);
		SetBreakPanel(show: false);
		SetButtonContainer(show: false);
	}

	public MainFocusControl(List<TimerModel> timers, bool isShowClockPanel)
	{
		IsShowClockPanel = isShowClockPanel;
		_timers = timers;
		InitChildren();
		InitEvent();
		OnFocusSecondChanged();
		base.UseLayoutRounding = true;
		base.SizeChanged += OnSizeChanged;
		base.Unloaded += OnUnloaded;
	}

	private void InitChildren()
	{
		InitMainContainer();
		InitTitle();
		InitTopOption();
		SetPanel();
		ToastGrid = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 0.0, 70.0),
			VerticalAlignment = VerticalAlignment.Bottom
		};
		Panel.SetZIndex(ToastGrid, 100);
		ToastGrid.SetValue(Grid.RowSpanProperty, 6);
		base.Children.Add(ToastGrid);
	}

	private void SetPanel()
	{
		List<TimerModel> timers = _timers;
		if (timers != null && timers.Count > 0 && !IsShowClockPanel)
		{
			HideClockPanel();
			_addButton.Visibility = Visibility.Visible;
			_soundButton.Visibility = Visibility.Collapsed;
			_title.Visibility = Visibility.Visible;
			_foldClockButton.Visibility = Visibility.Collapsed;
			SetTimerPanels();
		}
		else
		{
			TextBlock title = _title;
			List<TimerModel> timers2 = _timers;
			title.Visibility = ((timers2 != null && timers2.Count > 0) ? Visibility.Collapsed : Visibility.Visible);
			Border foldClockButton = _foldClockButton;
			List<TimerModel> timers3 = _timers;
			foldClockButton.Visibility = ((timers3 == null || timers3.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
			HideTimerPanel();
			SetClockPanel();
		}
	}

	private void InitMainContainer()
	{
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star),
			MinHeight = 60.0
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(40.0)
		});
	}

	private void InitTitle()
	{
		_title = new TextBlock
		{
			Text = Utils.GetString("PomoFocus"),
			Margin = new Thickness(24.0, 33.0, 0.0, 0.0),
			FontWeight = FontWeights.Bold,
			FontSize = 20.0
		};
		_title.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		base.Children.Add(_title);
		_foldClockButton = new Border
		{
			Cursor = Cursors.Hand,
			Width = 24.0,
			Height = 24.0,
			Margin = new Thickness(24.0, 33.0, 0.0, 0.0),
			Visibility = Visibility.Collapsed,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_foldClockButton.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle");
		_foldClockButton.Child = UiUtils.GetArrow(18.0, 0.0, "BaseColorOpacity100");
		_foldClockButton.MouseLeftButtonUp += FoldClockPanel;
		base.Children.Add(_foldClockButton);
	}

	private void InitTopOption()
	{
		HoverIconButton hoverIconButton = new HoverIconButton
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 30.0, 16.0, 0.0),
			ImageWidth = 20.0
		};
		hoverIconButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.ProjectList.More");
		hoverIconButton.IsImage = false;
		hoverIconButton.MouseLeftButtonUp += OnMoreClick;
		base.Children.Add(hoverIconButton);
		_soundButton = new HoverIconButton
		{
			IconData = TickFocusManager.GetSoundIcon(),
			IsImage = false,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 30.0, 46.0, 0.0),
			Visibility = Visibility.Collapsed,
			ImageWidth = 20.0
		};
		_soundButton.MouseLeftButtonUp += OnSoundClick;
		base.Children.Add(_soundButton);
		_addButton = new HoverIconButton
		{
			IconData = Utils.GetGeometry("Ic.ProjectList.Add"),
			IsImage = false,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 30.0, 46.0, 0.0),
			ImageWidth = 20.0
		};
		_addButton.MouseLeftButtonUp += OnAddTimerClick;
		base.Children.Add(_addButton);
	}

	private void InitEvent()
	{
		TickFocusManager.StatusChanged += OnFocusStatusChanged;
		TickFocusManager.TypeChanged += OnFocusTypeChanged;
		TickFocusManager.CurrentSecondChanged += OnFocusSecondChanged;
		PomoNotifier.TimerChanged += OnTimerChanged;
		PomoNotifier.Changed += OnPomoChanged;
		PomoNotifier.ServiceChanged += OnPomoChanged;
		PomoNotifier.LinkChanged += OnPomoLinkChanged;
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnSoundChanged, "PomoSound");
		PropertyChangedEventManager.AddHandler(TickFocusManager.Config.FocusVModel, OnFocusChanged, "NoTask");
	}

	private void OnPomoLinkChanged(object sender, PomoLinkArgs e)
	{
		if (!string.IsNullOrEmpty(e.NewTimerId) || !string.IsNullOrEmpty(e.PreviewTimerId))
		{
			base.Dispatcher.Invoke(SetPanel);
		}
	}

	private void OnPomoChanged(object sender, object e)
	{
		base.Dispatcher.Invoke(SetPanel);
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		double val = Math.Min(360.0, Math.Max(240.0, e.NewSize.Width / 2.1));
		double val2 = Math.Min(360.0, Math.Max(240.0, e.NewSize.Height - 460.0));
		_timeWidth = Math.Min(val, val2);
		double num = 140.0 + (_timeWidth - 300.0) / 2.0;
		_buttonsPanel?.SetButtonWidth(num, Math.Min(48.0, 48.0 + (num - 140.0) / 2.5));
		if (_timeClock != null)
		{
			_timeClock.SetSize(_timeWidth);
			double bottom = Math.Min(100.0, Math.Max(36.0, (e.NewSize.Height - _timeClock.Height - 380.0) / 2.0));
			_timeClock.Margin = new Thickness(0.0, 0.0, 0.0, bottom);
			if (_focusTitlePanel != null)
			{
				_focusTitlePanel.Margin = new Thickness(0.0, 0.0, 0.0, bottom);
			}
			if (_focusTitle != null)
			{
				_focusTitle.MaxWidth = _timeWidth - 20.0;
			}
		}
	}

	private void OnFocusTypeChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			_timeClock?.SetProgressMode();
			_focusSwitch?.SetSelectedIndex((!TickFocusManager.IsPomo) ? 1 : 0);
			_timeClock?.SetTime();
			_smallFocusView?.OnFocusTypeChanged();
		});
	}

	private void OnFocusSecondChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			_timeClock?.SetTime();
			if (TickFocusManager.Status == PomoStatus.WaitingRelax)
			{
				_relaxPanel?.SetRelaxText();
			}
		});
	}

	private void OnFocusStatusChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			List<TimerModel> timers = _timers;
			if (timers != null && timers.Count > 0 && !IsShowClockPanel)
			{
				_smallFocusView?.OnStatusChanged();
				_timerListView?.SetItemFocusing();
			}
			else
			{
				SetClockPanel();
			}
		});
	}

	private void OnFocusTitleChanged(object sender, PropertyChangedEventArgs e)
	{
		base.Dispatcher.Invoke(delegate
		{
			if (_focusTitle != null)
			{
				_focusTitle.Text = (TickFocusManager.NoFocus ? Utils.GetString("Focus") : TickFocusManager.Config.FocusVModel.Title);
				_focusTitle.Opacity = (TickFocusManager.NoFocus ? 0.6 : 1.0);
			}
		});
	}

	private void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		if (MorePopup != null)
		{
			return;
		}
		MorePopup = new EscPopup
		{
			PlacementTarget = (sender as UIElement),
			StaysOpen = false,
			Placement = PlacementMode.Bottom
		};
		MorePopup.Closed += MorePopup_Closed;
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("immerse", Utils.GetString("ImmerseMode"), Utils.GetGeometry("Ic.MarkdownEditor.FullScreen")),
			new CustomMenuItemViewModel("minimode", Utils.GetString("MiniMode"), Utils.GetGeometry("Ic.Focus.PopupWindow")),
			new CustomMenuItemViewModel("statistics", Utils.GetString("Statistics"), Utils.GetGeometry("Ic.ProjectList.Statistics")),
			new CustomMenuItemViewModel("settings", Utils.GetString("FocusSetting"), Utils.GetGeometry("Ic.Focus.Setting"))
		};
		if (FocusTimer.Config.Status == PomoStatus.WaitingRelax && !FocusTimer.Config.FocusVModel.IsHabit)
		{
			list.Insert(2, new CustomMenuItemViewModel("editFocusTime", Utils.GetString("EditFocusDuration"), Utils.GetGeometry("Ic.ProjectList.Edit")));
		}
		CustomMenuList customMenuList = new CustomMenuList(list, MorePopup);
		customMenuList.Operated += delegate(object o, object val)
		{
			switch (val as string)
			{
			case "immerse":
				UserActCollectUtils.AddClickEvent("focus", "om", "full_screen_mode");
				TickFocusManager.ShowImmerseWindow(Window.GetWindow(this));
				break;
			case "minimode":
				UserActCollectUtils.AddClickEvent("focus", "om", "mini_mode");
				TickFocusManager.HideOrShowFocusWidget(force: true);
				break;
			case "editFocusTime":
				UserActCollectUtils.AddClickEvent("focus", "om", "edit_focus_duration");
				_relaxPanel?.EditDuration();
				break;
			case "settings":
				SettingsHelper.PullRemoteSettings();
				UserActCollectUtils.AddClickEvent("focus", "om", "focus-settings");
				PomoSettings.ShowInstance(Window.GetWindow(this));
				break;
			case "statistics":
				Utils.TryProcessStartUrlWithToken("/webapp/#statistics/pomo?enablePomo=true");
				UserActCollectUtils.AddClickEvent("focus", "om", "statistics");
				break;
			}
		};
		customMenuList.Show();
	}

	private async void MorePopup_Closed(object sender, EventArgs e)
	{
		await Task.Delay(100);
		MorePopup.Closed -= MorePopup_Closed;
		MorePopup = null;
	}

	private void OnAddTimerClick(object sender, MouseButtonEventArgs e)
	{
		AddTimerWindow.ShowAddTimerWindow(Window.GetWindow(this));
		UserActCollectUtils.AddClickEvent("focus", "focus_tab", "add_timer");
	}

	private void OnSoundClick(object sender, MouseButtonEventArgs e)
	{
		if (SoundPopup == null)
		{
			SoundPopup = new EscPopup
			{
				StaysOpen = false,
				PlacementTarget = (sender as UIElement),
				Placement = PlacementMode.Bottom
			};
			SelectSoundControl child = new SelectSoundControl
			{
				Popup = SoundPopup
			};
			SoundPopup.Closed += SoundPopup_Closed;
			SoundPopup.Child = child;
			SoundPopup.IsOpen = true;
			string actCType = TickFocusManager.GetActCType();
			UserActCollectUtils.AddClickEvent("focus", actCType, "white_noise");
		}
	}

	private async void SoundPopup_Closed(object sender, EventArgs e)
	{
		PomoSoundPlayer.TryStopSound();
		await Task.Delay(100);
		SoundPopup.Closed -= SoundPopup_Closed;
		SoundPopup = null;
	}

	private void OnSoundChanged(object sender, PropertyChangedEventArgs e)
	{
		_soundButton.IconData = TickFocusManager.GetSoundIcon();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		TickFocusManager.StatusChanged -= OnFocusStatusChanged;
		TickFocusManager.TypeChanged -= OnFocusTypeChanged;
		TickFocusManager.CurrentSecondChanged -= OnFocusSecondChanged;
		PomoNotifier.Changed -= OnPomoChanged;
		PomoNotifier.ServiceChanged -= OnPomoChanged;
		PomoNotifier.TimerChanged -= OnTimerChanged;
		PomoNotifier.LinkChanged -= OnPomoLinkChanged;
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnSoundChanged, "PomoSound");
		PropertyChangedEventManager.RemoveHandler(TickFocusManager.Config.FocusVModel, OnFocusChanged, "NoTask");
		base.Children.Clear();
		_pomoFilter = null;
	}

	public void ShowClockPanel()
	{
		IsShowClockPanel = true;
		SetPanel();
		Utils.FindParent<FocusView>(this)?.OnTimerSelect(null);
	}

	private void FoldClockPanel(object sender, MouseButtonEventArgs e)
	{
		IsShowClockPanel = false;
		SetPanel();
		Utils.FindParent<FocusView>(this)?.OnTimerSelect(_selectedId);
	}

	public void TryFoldClockPanel()
	{
		if (IsShowClockPanel)
		{
			_pomoFilter?.ResetSelectTitle();
			IsShowClockPanel = false;
			SetPanel();
			Utils.FindParent<FocusView>(this)?.OnTimerSelect(_selectedId);
		}
	}

	public bool ClockPanelShow()
	{
		if (_timers != null && _timers.Count != 0)
		{
			return IsShowClockPanel;
		}
		return true;
	}

	public bool ExistTimer()
	{
		List<TimerModel> timers = _timers;
		if (timers == null)
		{
			return false;
		}
		return timers.Count > 0;
	}

	private async void OnTimerChanged(object sender, EventArgs e)
	{
		_timers = await TimerDao.GetDisplayTimersAsync();
		base.Dispatcher.Invoke(delegate
		{
			SetPanel();
			if (!string.IsNullOrEmpty(_selectedId))
			{
				if (_timers.Count == 0)
				{
					_selectedId = null;
				}
				OnTimerSelect(_selectedId);
			}
		});
	}

	private void SetTimerPanels()
	{
		List<TimerModel> list = _timers.Where((TimerModel t) => t.Status == 0).ToList();
		List<TimerModel> list2 = _timers.Where((TimerModel t) => t.Status == 1).ToList();
		SetArchiveSwitch(show: true);
		SetTimerList(show: true);
		SetTimerItems(_showArchiveList ? list2 : list);
		SetSmallFocus(show: true);
	}

	private void SetTimerItems(List<TimerModel> timers)
	{
		if (!string.IsNullOrEmpty(_selectedId) && (timers == null || timers.All((TimerModel t) => t.Id != _selectedId)))
		{
			OnTimerSelect(null);
		}
		_timerListView?.SetItems(timers, _showArchiveList, _selectedId);
	}

	private void SetTimerList(bool show)
	{
		if (show && _timerListView == null)
		{
			_timerListView = new FocusTimerListView
			{
				VerticalAlignment = VerticalAlignment.Stretch,
				Margin = new Thickness(0.0, 76.0, 0.0, 71.0)
			};
			_timerListView.SetValue(Grid.RowProperty, 0);
			_timerListView.SetValue(Grid.RowSpanProperty, 6);
			base.Children.Add(_timerListView);
		}
		else if (!show && _timerListView != null)
		{
			base.Children.Remove(_timerListView);
			_timerListView = null;
		}
	}

	private void SetArchiveSwitch(bool show)
	{
		if (show && _archiveSwitch == null)
		{
			if (_archiveSwitch == null)
			{
				_archiveSwitch = new AnimateGroupTitle
				{
					Titles = "Keeping|Archived",
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Center,
					Margin = new Thickness(0.0, 30.0, 0.0, 0.0),
					Height = 34.0
				};
				base.Children.Add(_archiveSwitch);
				_archiveSwitch.SelectedTitleChanged += OnArchiveTitleSelected;
			}
			_archiveSwitch.SetSelectedIndex(_showArchiveList ? 1 : 0);
		}
		else if (!show && _archiveSwitch != null)
		{
			_archiveSwitch.SelectedTitleChanged -= OnArchiveTitleSelected;
			base.Children.Remove(_archiveSwitch);
			_archiveSwitch = null;
		}
	}

	private void OnArchiveTitleSelected(object sender, GroupTitleViewModel e)
	{
		_showArchiveList = e.Index == 1;
		List<TimerModel> items = _timers.Where((TimerModel t) => t.Status == e.Index).ToList();
		OnTimerSelect(null);
		_timerListView?.SetItems(items, _showArchiveList, _selectedId);
	}

	private void SetSmallFocus(bool show)
	{
		if (show)
		{
			if (_smallFocusView == null)
			{
				_smallFocusView = new SmallFocusView
				{
					VerticalAlignment = VerticalAlignment.Bottom,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					Margin = new Thickness(0.0, 0.0, 0.0, 0.0)
				};
				_smallFocusView.SetValue(Grid.RowProperty, 0);
				_smallFocusView.SetValue(Grid.RowSpanProperty, 6);
				base.Children.Add(_smallFocusView);
				_smallFocusView.MouseLeftButtonUp += SmallFocusClick;
			}
			_smallFocusView.ReloadIcon();
		}
		else if (!show && _smallFocusView != null)
		{
			base.Children.Remove(_smallFocusView);
			_smallFocusView = null;
		}
	}

	private void SmallFocusClick(object sender, MouseButtonEventArgs e)
	{
		ShowClockPanel();
		UserActCollectUtils.AddClickEvent("timer", "action_bar", "expand");
	}

	private void OnFocusChanged(object sender, PropertyChangedEventArgs e)
	{
		_timerListView?.SetItemFocusing();
	}

	private void HideTimerPanel()
	{
		SetArchiveSwitch(show: false);
		SetTimerList(show: false);
		SetSmallFocus(show: false);
	}

	public void OnTimerSelect(string timerId)
	{
		_selectedId = timerId;
		Utils.FindParent<FocusView>(this)?.OnTimerSelect(timerId);
	}

	public void ClearSelect()
	{
		_selectedId = null;
		_timerListView?.ClearSelect();
	}

	public void UpdateTimerDuration(string timerId, long duration)
	{
		_timerListView?.UpdateTimerDuration(timerId, duration);
	}
}
