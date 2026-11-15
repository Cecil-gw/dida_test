using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo.MiniFocus;

public class MiniFocusMoreControl : UserControl, IComponentConnector
{
	private Point _position;

	private bool _focusPopupShow;

	private bool _displayPopupShow;

	private Popup _popup;

	private Window _window;

	private PomoFilterControl _focusTaskFilter;

	private FocusWindowType _type;

	internal ContentControl Container;

	internal MiniFocusStatisticsView StatisticsView;

	internal ContentControl FocusOnItem;

	internal TextBlock BindText;

	internal ContentControl EditPomoDuratinoItem;

	internal ContentControl FocusPopupTarget;

	internal ContentControl ModifyFocusItem;

	internal EscPopup ModifyFocusPopup;

	internal ContentControl SetDurationBtn;

	internal ContentControl SwitchTiming;

	internal ContentControl SwitchPomo;

	internal Path PinImage;

	internal TextBlock PinText;

	private bool _contentLoaded;

	public MiniFocusMoreControl(Popup popup, Window window, FocusWindowType type)
	{
		_type = type;
		_popup = popup;
		_window = window;
		InitializeComponent();
		if (type == FocusWindowType.Normal)
		{
			Container.Margin = new Thickness(0.0, 4.0, 0.0, 4.0);
			StatisticsView.Visibility = Visibility.Collapsed;
		}
		if (!TickFocusManager.IsPomo)
		{
			ModifyFocusItem.Visibility = Visibility.Collapsed;
		}
		if (!TickFocusManager.InRelax)
		{
			EditPomoDuratinoItem.Visibility = Visibility.Collapsed;
		}
		PinImage.SetResourceReference(Path.DataProperty, LocalSettings.Settings.PomoTopMost ? "Ic.ProjectList.CancelTopmost" : "Ic.ProjectList.Topmost");
		PinText.Text = Utils.GetString(LocalSettings.Settings.PomoTopMost ? "Unpin" : "Pin");
		if (TickFocusManager.Working)
		{
			SwitchPomo.IsEnabled = false;
			SwitchPomo.Opacity = 0.6;
			SwitchTiming.IsEnabled = false;
			SwitchTiming.Opacity = 0.6;
		}
		SetDurationBtn.IsEnabled = TickFocusManager.Status == PomoStatus.WaitingWork;
		SetDurationBtn.Opacity = ((TickFocusManager.Status == PomoStatus.WaitingWork) ? 1.0 : 0.6);
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		BindText.Text = TickFocusManager.Config.FocusVModel.Title;
	}

	private void ShowPopup(object sender, MouseEventArgs e)
	{
		Point position = e.GetPosition(this);
		if (Math.Abs(_position.Y - position.Y) > 4.0 || Math.Abs(_position.X - position.X) > 4.0)
		{
			_position = position;
			TryShowModifyPomoPopup();
		}
	}

	private void TryShowModifyPomoPopup()
	{
		if (!ModifyFocusItem.IsMouseOver && !ModifyFocusPopup.IsMouseOver)
		{
			TryHideFocusPopup();
		}
		else if (!_focusPopupShow && !ModifyFocusPopup.IsOpen)
		{
			_focusPopupShow = true;
			DelayShowFocusPopup();
		}
	}

	private async void DelayShowFocusPopup()
	{
		await Task.Delay(150);
		if (ModifyFocusPopup.IsOpen)
		{
			_focusPopupShow = false;
		}
		else if (_focusPopupShow)
		{
			ModifyFocusPopup.IsOpen = true;
		}
	}

	private async Task TryHideFocusPopup()
	{
		_focusPopupShow = false;
		if (ModifyFocusPopup.IsOpen)
		{
			await Task.Delay(100);
			if (!ModifyFocusItem.IsMouseOver && !ModifyFocusPopup.IsMouseOver)
			{
				ModifyFocusPopup.IsOpen = false;
			}
			else
			{
				_focusPopupShow = ModifyFocusPopup.IsOpen;
			}
		}
	}

	private void OnSoundClick(object sender, MouseButtonEventArgs e)
	{
		EscPopup escPopup = new EscPopup
		{
			PlacementTarget = (UIElement)sender,
			Placement = PlacementMode.Bottom,
			StaysOpen = false,
			HorizontalOffset = -20.0
		};
		SelectSoundControl selectSoundControl = new SelectSoundControl
		{
			Popup = escPopup
		};
		selectSoundControl.ItemSelect += delegate
		{
			AddClickEvent("white_noise");
		};
		escPopup.Child = selectSoundControl;
		escPopup.Closed += delegate
		{
			PomoSoundPlayer.TryStopSound();
		};
		escPopup.IsOpen = true;
	}

	private void OnStatisticsClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
	}

	private void OnPinClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		LocalSettings.Settings.PomoTopMost = !LocalSettings.Settings.PomoTopMost;
		AddClickEvent("pin");
	}

	private void OnSettingsClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		PomoSettings.ShowInstance(_window, scrollToMini: true, centerScreen: true);
		AddClickEvent("mini_style");
	}

	private void ImmerseClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		TickFocusManager.ShowImmerseWindow(_window);
		AddClickEvent("full_screen_mode");
	}

	private void OnFocusOnClick(object sender, MouseButtonEventArgs e)
	{
		if (_focusTaskFilter == null)
		{
			EscPopup escPopup = new EscPopup
			{
				PlacementTarget = (UIElement)sender,
				Placement = PlacementMode.Bottom,
				StaysOpen = false,
				HorizontalOffset = -20.0
			};
			escPopup.Closed += delegate
			{
				BindText.Text = TickFocusManager.Config.FocusVModel.Title;
			};
			_focusTaskFilter = new PomoFilterControl(escPopup, inFocus: true, showDetail: true)
			{
				DataContext = TickFocusManager.Config.FocusVModel
			};
			_focusTaskFilter.SetTheme(LocalSettings.Settings.PomoWindowTheme);
			_focusTaskFilter.ItemSelected += delegate
			{
				AddClickEvent("focus_on");
			};
		}
		_focusTaskFilter.Show();
	}

	private async void OnSetDurationClick(object sender, MouseButtonEventArgs e)
	{
		ModifyFocusPopup.IsOpen = false;
		_popup.IsOpen = false;
		await Task.Delay(150);
		if (!TickFocusManager.Working)
		{
			EscPopup obj = new EscPopup
			{
				PlacementTarget = _popup.PlacementTarget,
				Placement = _popup.Placement,
				StaysOpen = false,
				HorizontalOffset = _popup.HorizontalOffset,
				VerticalOffset = _popup.VerticalOffset
			};
			obj.Child = new PomoModitySpanConfig(obj, collectEvent: true);
			obj.IsOpen = true;
		}
	}

	private async void OnSwitchTimingClick(object sender, MouseButtonEventArgs e)
	{
		TickFocusManager.SetFocusType(1);
		ModifyFocusPopup.IsOpen = false;
		_popup.IsOpen = false;
		AddClickEvent("switch_stopwatch");
	}

	private void OnSwitchPomoClick(object sender, MouseButtonEventArgs e)
	{
		TickFocusManager.SetFocusType(0);
		_popup.IsOpen = false;
		AddClickEvent("switch_pomo");
	}

	private void OnOpenMainWindowClick(object sender, MouseButtonEventArgs e)
	{
		App.Window.SwitchModule(DisplayModule.Pomo);
		App.Window.TryShowMainWindow();
		_popup.IsOpen = false;
		AddClickEvent("open_main_window");
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		if (_window is MiniFocusWindow miniFocusWindow)
		{
			miniFocusWindow.CloseWithReason("MoreMenuClose");
		}
		else
		{
			_window?.Close();
		}
		AddClickEvent("close");
	}

	private void AddClickEvent(string label)
	{
		UserActCollectUtils.AddClickEvent("focus_mini", "om", label);
	}

	private void OnEditPomoDurationClick(object sender, MouseButtonEventArgs e)
	{
		Point value = new Point(_window.Left + _window.ActualWidth - 10.0, _window.Top - 10.0);
		if (SystemParameters.PrimaryScreenWidth - value.X < 240.0)
		{
			value.X = _window.Left - 230.0;
		}
		TickFocusManager.EditDuration(value);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/minifocus/minifocusmorecontrol.xaml", UriKind.Relative);
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
			Container = (ContentControl)target;
			break;
		case 2:
			((StackPanel)target).MouseMove += ShowPopup;
			break;
		case 3:
			StatisticsView = (MiniFocusStatisticsView)target;
			break;
		case 4:
			FocusOnItem = (ContentControl)target;
			FocusOnItem.MouseLeftButtonUp += OnFocusOnClick;
			break;
		case 5:
			BindText = (TextBlock)target;
			break;
		case 6:
			EditPomoDuratinoItem = (ContentControl)target;
			EditPomoDuratinoItem.MouseLeftButtonUp += OnEditPomoDurationClick;
			break;
		case 7:
			FocusPopupTarget = (ContentControl)target;
			break;
		case 8:
			ModifyFocusItem = (ContentControl)target;
			break;
		case 9:
			ModifyFocusPopup = (EscPopup)target;
			break;
		case 10:
			SetDurationBtn = (ContentControl)target;
			SetDurationBtn.MouseLeftButtonUp += OnSetDurationClick;
			break;
		case 11:
			SwitchTiming = (ContentControl)target;
			SwitchTiming.MouseLeftButtonUp += OnSwitchTimingClick;
			break;
		case 12:
			SwitchPomo = (ContentControl)target;
			SwitchPomo.MouseLeftButtonUp += OnSwitchPomoClick;
			break;
		case 13:
			((ContentControl)target).MouseLeftButtonUp += OnSettingsClick;
			break;
		case 14:
			((ContentControl)target).MouseLeftButtonUp += OnPinClick;
			break;
		case 15:
			PinImage = (Path)target;
			break;
		case 16:
			PinText = (TextBlock)target;
			break;
		case 17:
			((ContentControl)target).MouseLeftButtonUp += OnSoundClick;
			break;
		case 18:
			((ContentControl)target).MouseLeftButtonUp += ImmerseClick;
			break;
		case 19:
			((ContentControl)target).MouseLeftButtonUp += OnOpenMainWindowClick;
			break;
		case 20:
			((ContentControl)target).MouseLeftButtonUp += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
