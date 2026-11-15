using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Time;

public class TimeInputControl : UserControl, IComponentConnector
{
	public new static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(TimeInputControl), new PropertyMetadata(new SolidColorBrush(Colors.Black), OnForegroundChangedCallback));

	public static readonly DependencyProperty ShowTimeZoneProperty = DependencyProperty.Register("ShowTimeZone", typeof(bool), typeof(TimeInputControl), new PropertyMetadata(false, null));

	public static readonly DependencyProperty ForceShowTimeZoneProperty = DependencyProperty.Register("ForceShowTimeZone", typeof(bool), typeof(TimeInputControl), new PropertyMetadata(false, null));

	public static readonly DependencyProperty DropWidthProperty = DependencyProperty.Register("DropWidth", typeof(double), typeof(TimeInputControl), new PropertyMetadata(110.0, null));

	public static readonly DependencyProperty TabSelectedProperty = DependencyProperty.Register("TabSelected", typeof(bool), typeof(TimeInputControl), new PropertyMetadata(false, null));

	public static readonly DependencyProperty BorderThickProperty = DependencyProperty.Register("BorderThick", typeof(Thickness), typeof(TimeInputControl), new PropertyMetadata(new Thickness(1.0), null));

	public static readonly DependencyProperty ContentPaddingProperty = DependencyProperty.Register("ContentPadding", typeof(Thickness), typeof(TimeInputControl), new PropertyMetadata(new Thickness(0.0), null));

	public static readonly DependencyProperty PopupOpenProperty = DependencyProperty.Register("PopupOpen", typeof(bool), typeof(TimeInputControl), new PropertyMetadata(false, null));

	private SelectTimeDropDialog _droplist;

	private DateTime? _lastToggleTime;

	private DateTime _selectedTime = DateTime.Today;

	private bool _showPopupMouseDown;

	internal TimeInputControl Root;

	internal TextBox EmptyBox;

	internal Border InputGrid;

	internal TextBox HourText;

	internal TextBlock SplitText;

	internal TextBox MinuteText;

	internal TextBox AmOrPmText;

	internal Popup ReminderPopup;

	private bool _contentLoaded;

	public bool PopupOpen
	{
		get
		{
			return (bool)GetValue(PopupOpenProperty);
		}
		set
		{
			SetValue(PopupOpenProperty, value);
		}
	}

	public Thickness BorderThick
	{
		get
		{
			return (Thickness)GetValue(BorderThickProperty);
		}
		set
		{
			SetValue(BorderThickProperty, value);
		}
	}

	public Thickness ContentPadding
	{
		get
		{
			return (Thickness)GetValue(ContentPaddingProperty);
		}
		set
		{
			SetValue(ContentPaddingProperty, value);
		}
	}

	public bool ShowTimeZone
	{
		get
		{
			return (bool)GetValue(ShowTimeZoneProperty);
		}
		set
		{
			SetValue(ShowTimeZoneProperty, value);
		}
	}

	public bool ForceShowTimeZone
	{
		get
		{
			return (bool)GetValue(ForceShowTimeZoneProperty);
		}
		set
		{
			SetValue(ForceShowTimeZoneProperty, value);
		}
	}

	public bool DirectSelectTimeZone { get; set; }

	public bool TabSelected
	{
		get
		{
			return (bool)GetValue(TabSelectedProperty);
		}
		set
		{
			SetValue(TabSelectedProperty, value);
		}
	}

	public double DropWidth
	{
		get
		{
			return (double)GetValue(DropWidthProperty);
		}
		set
		{
			SetValue(DropWidthProperty, value);
		}
	}

	public bool EnableEditTimeZone { get; set; } = true;

	public TimeZoneViewModel TimeZone { get; set; }

	public DateTime SelectedTime
	{
		get
		{
			return _selectedTime;
		}
		set
		{
			_selectedTime = value;
			SetTimeText(_selectedTime);
		}
	}

	public new Brush Foreground
	{
		get
		{
			return (Brush)GetValue(ForegroundProperty);
		}
		set
		{
			SetValue(ForegroundProperty, value);
		}
	}

	public Popup DropdownPopup => ReminderPopup;

	public DateTime? BeginTime { get; set; }

	public event EventHandler<TimeZoneViewModel> TimeZoneChanged;

	public event EventHandler<DateTime> SelectedTimeChanged;

	public TimeInputControl()
	{
		InitializeComponent();
		base.Cursor = Cursors.Hand;
		InputGrid.PreviewMouseLeftButtonDown += OnLeftMouseDown;
		GlobalEventManager.TimeFormatChanged += OnTimeFormatChanged;
	}

	private void OnTimeFormatChanged(object sender, EventArgs e)
	{
		DateUtils.TimeDisplayFormat timeDisplayFormat = DateUtils.GetTimeDisplayFormat();
		AmOrPmText.Visibility = ((timeDisplayFormat == DateUtils.TimeDisplayFormat.Hour24) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnInputLoaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is DateTime selectedTime)
		{
			SelectedTime = selectedTime;
		}
		InitEvents();
		base.Loaded -= OnInputLoaded;
	}

	private void InitEvents()
	{
		HourText.TextChanged -= HourTextChanged;
		HourText.TextChanged += HourTextChanged;
		MinuteText.TextChanged -= MinuteTextChanged;
		MinuteText.TextChanged += MinuteTextChanged;
	}

	private static void OnForegroundChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is TimeInputControl timeInputControl && e.NewValue != null)
		{
			timeInputControl.HourText.Foreground = (Brush)e.NewValue;
			timeInputControl.SplitText.Foreground = (Brush)e.NewValue;
			timeInputControl.MinuteText.Foreground = (Brush)e.NewValue;
			timeInputControl.AmOrPmText.Foreground = (Brush)e.NewValue;
		}
	}

	private void OnLeftMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (PopupStateManager.CanOpenTimePopup)
		{
			_showPopupMouseDown = true;
		}
		else
		{
			PopupStateManager.SetCanOpenTimePopup(canOpen: true);
		}
	}

	private async void OnClick(object sender, MouseButtonEventArgs e)
	{
		await Task.Delay(50);
		if (!AmOrPmText.IsMouseOver)
		{
			ToggleReminderPopup();
			if (MinuteText.IsFocused)
			{
				MinuteText.Focus();
				MinuteText.SelectAll();
			}
			else
			{
				HourText.Focus();
				HourText.SelectAll();
			}
		}
		_showPopupMouseDown = false;
	}

	private void OnInitialized(object sender, EventArgs e)
	{
		if (DateUtils.GetTimeDisplayFormat() == DateUtils.TimeDisplayFormat.Hour24)
		{
			AmOrPmText.Visibility = Visibility.Collapsed;
		}
	}

	public void SetTimeText(DateTime time)
	{
		ClearHourTextChangeHandler();
		ClearMinuteTextChangeHandler();
		ClearAmOrPmTextChangeHandler();
		HourText.Text = DateUtils.GetHourText(time);
		MinuteText.Text = DateUtils.GetMinuteText(time);
		AmOrPmText.Text = DateUtils.GetAmOrPm(time);
		HourText.TextChanged += HourTextChanged;
		MinuteText.TextChanged += MinuteTextChanged;
		AmOrPmText.TextChanged += AmOrPmTextChanged;
	}

	public void ClearSelection()
	{
		HourText.Select(0, 0);
		MinuteText.Select(0, 0);
		AmOrPmText.Select(0, 0);
	}

	public void CloseReminderPopup()
	{
		PopupOpen = false;
		if (ReminderPopup.IsOpen)
		{
			ReminderPopup.IsOpen = false;
		}
	}

	public void ToggleReminderPopup()
	{
		if (!ReminderPopup.IsOpen)
		{
			_droplist = new SelectTimeDropDialog(_selectedTime, ShowTimeZone && (LocalSettings.Settings.EnableTimeZone || ForceShowTimeZone), EnableEditTimeZone, TimeZone, BeginTime, DirectSelectTimeZone)
			{
				MinWidth = DropWidth
			};
			_droplist.Select += delegate(object arg, DateTime selected)
			{
				ReminderPopup.IsOpen = false;
				_selectedTime = DateUtils.SetHourAndMinuteOnly(selected, selected.Hour, selected.Minute);
				SetTimeText(_selectedTime);
				NotifyChanged();
			};
			_droplist.TimeZoneChanged += delegate(object obj, TimeZoneViewModel model)
			{
				TimeZone = model;
				this.TimeZoneChanged?.Invoke(obj, model);
			};
			ReminderPopup.Child = _droplist;
			ReminderPopup.IsOpen = true;
		}
		else
		{
			ReminderPopup.IsOpen = false;
		}
	}

	private void OnPopupOpened(object sender, EventArgs e)
	{
		PopupOpen = true;
		PopupStateManager.SetCanOpenTimePopup(canOpen: false);
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		PopupOpen = false;
		PopupStateManager.SetCanOpenTimePopup(canOpen: true, delay: true);
	}

	private async void HourTextClick(object sender, MouseButtonEventArgs e)
	{
		await Task.Delay(150);
		HourText.SelectAll();
	}

	private async void MinuteTextClick(object sender, MouseButtonEventArgs e)
	{
		await Task.Delay(150);
		MinuteText.SelectAll();
	}

	private async void HourTextKeyUp(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Return:
			ToggleReminderPopup();
			e.Handled = true;
			break;
		case Key.Up:
			AddHour(-1);
			break;
		case Key.Down:
			AddHour(1);
			break;
		case Key.Right:
			MinuteText.Focus();
			MinuteText.SelectAll();
			break;
		case Key.Left:
			if (AmOrPmText.IsVisible)
			{
				AmOrPmText.Focus();
				AmOrPmText.SelectAll();
			}
			else
			{
				MinuteText.Focus();
				MinuteText.SelectAll();
			}
			break;
		}
	}

	private async void MinuteTextKeyUp(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Return:
			ToggleReminderPopup();
			e.Handled = true;
			break;
		case Key.Up:
			AddMinute(-15);
			break;
		case Key.Down:
			AddMinute(15);
			break;
		case Key.Left:
			HourText.Focus();
			HourText.SelectAll();
			break;
		case Key.Right:
			if (AmOrPmText.IsVisible)
			{
				AmOrPmText.Focus();
				AmOrPmText.SelectAll();
			}
			else
			{
				HourText.Focus();
				HourText.SelectAll();
			}
			break;
		}
	}

	private void AmOrPmKeyUp(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Up:
		case Key.Down:
		case Key.A:
		case Key.P:
			AmOrPmText.Text = ((AmOrPmText.Text == "AM") ? "PM" : "AM");
			break;
		case Key.Left:
			MinuteText.Focus();
			MinuteText.SelectAll();
			break;
		case Key.Right:
			HourText.Focus();
			HourText.SelectAll();
			break;
		}
	}

	private void AddHour(int unit)
	{
		ClearHourTextChangeHandler();
		int.TryParse(HourText.Text, out var result);
		result += unit;
		HourText.Text = PaddingLeftZero(result);
		_selectedTime = _selectedTime.AddHours(unit);
		SetTimeText(_selectedTime);
		NotifyChanged();
		HourText.SelectAll();
		HourText.TextChanged += HourTextChanged;
	}

	private void ClearHourTextChangeHandler()
	{
		HourText.TextChanged -= HourTextChanged;
		HourText.TextChanged -= HourTextChanged;
	}

	private void ClearMinuteTextChangeHandler()
	{
		MinuteText.TextChanged -= MinuteTextChanged;
		MinuteText.TextChanged -= MinuteTextChanged;
	}

	private void ClearAmOrPmTextChangeHandler()
	{
		AmOrPmText.TextChanged -= AmOrPmTextChanged;
		AmOrPmText.TextChanged -= AmOrPmTextChanged;
	}

	private static string PaddingLeftZero(int num)
	{
		if (num >= 0 && num < 10)
		{
			return "0" + num;
		}
		return num.ToString();
	}

	private void AddMinute(int unit)
	{
		ClearMinuteTextChangeHandler();
		int.TryParse(MinuteText.Text, out var result);
		result += unit;
		if (result >= 60)
		{
			result %= 60;
			AddHour(1);
		}
		else if (result < 0)
		{
			result += 60;
			AddHour(-1);
		}
		if (!Utils.IsEmptyDate(_selectedTime))
		{
			_selectedTime = DateUtils.SetMinuteOnly(_selectedTime, result);
			SetTimeText(_selectedTime);
		}
		MinuteText.SelectAll();
		NotifyChanged();
		MinuteText.TextChanged += MinuteTextChanged;
	}

	private void HandleNumberInput(object sender, TextCompositionEventArgs e)
	{
		if (e.Text.Length >= 1 && !char.IsDigit(e.Text, e.Text.Length - 1))
		{
			e.Handled = true;
		}
	}

	private async void HourTextChanged(object sender, TextChangedEventArgs e)
	{
		int.TryParse(HourText.Text, out var result);
		if (HourText.Text.Length <= 1)
		{
			return;
		}
		MinuteText.Focus();
		MinuteText.SelectAll();
		if (DateUtils.GetTimeDisplayFormat() == DateUtils.TimeDisplayFormat.Hour12)
		{
			if (result < 0 || result > 12)
			{
				result %= 12;
				HourText.Text = ((result >= 10) ? result.ToString() : ("0" + result));
			}
		}
		else if (result < 0 || result > 23)
		{
			result %= 12;
			HourText.Text = ((result >= 10) ? result.ToString() : ("0" + result));
		}
		NotifyChanged();
		await Task.Delay(100);
		FocusManager.GetFocusedElement(this);
	}

	private void MinuteTextChanged(object sender, TextChangedEventArgs e)
	{
		int.TryParse(MinuteText.Text, out var result);
		if (MinuteText.Text.Length > 1)
		{
			if (AmOrPmText.IsVisible)
			{
				AmOrPmText.Focus();
				AmOrPmText.SelectAll();
			}
			else
			{
				HourText.Focus();
				HourText.SelectAll();
			}
			if (result < 0 || result > 59)
			{
				MinuteText.Text = "59";
			}
			NotifyChanged();
		}
	}

	private void AmOrPmTextChanged(object sender, TextChangedEventArgs e)
	{
		AmOrPmText.SelectAll();
		NotifyChanged();
	}

	private void AmOrPmTextPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		AmOrPmText.Text = ((AmOrPmText.Text == "AM") ? "PM" : "AM");
	}

	private async void NotifyChanged()
	{
		DateTime? selectedDate = GetSelectedDate();
		if (selectedDate.HasValue)
		{
			_selectedTime = selectedDate.Value;
			this.SelectedTimeChanged?.Invoke(this, _selectedTime);
		}
	}

	private DateTime? GetSelectedDate()
	{
		if (HourText != null && MinuteText != null && AmOrPmText != null)
		{
			string text = (Utils.IsEmptyDate(_selectedTime) ? DateTime.Today : _selectedTime).ToString("yyyy-MM-dd ", CultureInfo.InvariantCulture);
			string format = string.Empty;
			if (!string.IsNullOrEmpty(HourText.Text) && !string.IsNullOrEmpty(MinuteText.Text))
			{
				string text2 = MinuteText.Text;
				string text3 = HourText.Text;
				if (text2.Length == 1)
				{
					text2 = "0" + text2;
				}
				if (text3.Length == 1)
				{
					text3 = "0" + text3;
				}
				if (DateUtils.GetTimeDisplayFormat() == DateUtils.TimeDisplayFormat.Hour12)
				{
					if (!string.IsNullOrEmpty(AmOrPmText.Text))
					{
						string text4 = text3 + ":" + text2 + " " + AmOrPmText.Text;
						text += text4;
						format = "yyyy-MM-dd hh:mm tt";
					}
				}
				else
				{
					string text5 = text3 + ":" + text2;
					text += text5;
					format = "yyyy-MM-dd HH:mm";
				}
				DateTime? dateTime = DateUtils.TryParseTime(text, format);
				if (dateTime.HasValue)
				{
					_selectedTime = dateTime.Value;
					return dateTime.Value;
				}
			}
		}
		return null;
	}

	public void HidePopup()
	{
		ReminderPopup.IsOpen = false;
	}

	private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
	}

	public async void FoucusHourText()
	{
		await Task.Delay(50);
		FocusEmpty();
		HourText.Focus();
		HourText.SelectAll();
	}

	private void OnLostFocus(object sender, RoutedEventArgs e)
	{
		if (!(sender is TextBox textBox))
		{
			return;
		}
		textBox.TextChanged -= HourTextChanged;
		textBox.TextChanged -= MinuteTextChanged;
		bool flag = true;
		if (LocalSettings.Settings.SettingsModel.TimeFormat == "System")
		{
			flag = DateUtils.CheckSystemFormatPadZero();
		}
		if (textBox.Text.Length == 1)
		{
			if (flag)
			{
				textBox.Text = "0" + textBox.Text;
			}
		}
		else if (string.IsNullOrEmpty(textBox.Text))
		{
			if (flag)
			{
				textBox.Text = "00";
			}
			else
			{
				textBox.Text = "0";
			}
		}
		textBox.SelectAll();
		if (textBox == HourText)
		{
			textBox.TextChanged += HourTextChanged;
		}
		else
		{
			textBox.TextChanged += MinuteTextChanged;
		}
		NotifyChanged();
	}

	public void FocusEmpty()
	{
		EmptyBox.Focus();
	}

	public void FocusHour()
	{
		HourText.Focus();
		HourText.SelectAll();
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		if (ReminderPopup.IsOpen && e.Key == Key.Escape)
		{
			SelectTimeDropDialog droplist = _droplist;
			if (droplist != null && droplist.TryCloseTimezonePopup())
			{
				ReminderPopup.IsOpen = false;
				e.Handled = true;
			}
		}
	}

	public bool TryCloseTimezonePopup()
	{
		return _droplist?.TryCloseTimezonePopup() ?? false;
	}

	public DateTime? GetTime()
	{
		return GetSelectedDate();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/timeinputcontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (TimeInputControl)target;
			Root.Initialized += OnInitialized;
			Root.IsVisibleChanged += OnVisibleChanged;
			Root.Loaded += OnInputLoaded;
			break;
		case 2:
			EmptyBox = (TextBox)target;
			break;
		case 3:
			InputGrid = (Border)target;
			InputGrid.PreviewMouseLeftButtonUp += OnClick;
			break;
		case 4:
			HourText = (TextBox)target;
			HourText.PreviewTextInput += HandleNumberInput;
			HourText.KeyUp += HourTextKeyUp;
			HourText.PreviewKeyDown += OnKeyDown;
			HourText.LostFocus += OnLostFocus;
			break;
		case 5:
			SplitText = (TextBlock)target;
			break;
		case 6:
			MinuteText = (TextBox)target;
			MinuteText.PreviewTextInput += HandleNumberInput;
			MinuteText.KeyUp += MinuteTextKeyUp;
			MinuteText.PreviewKeyDown += OnKeyDown;
			MinuteText.LostFocus += OnLostFocus;
			break;
		case 7:
			AmOrPmText = (TextBox)target;
			AmOrPmText.KeyUp += AmOrPmKeyUp;
			AmOrPmText.PreviewKeyDown += OnKeyDown;
			AmOrPmText.TextChanged += AmOrPmTextChanged;
			AmOrPmText.PreviewMouseDown += AmOrPmTextPreviewMouseDown;
			break;
		case 8:
			ReminderPopup = (Popup)target;
			ReminderPopup.Closed += OnPopupClosed;
			ReminderPopup.Opened += OnPopupOpened;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
