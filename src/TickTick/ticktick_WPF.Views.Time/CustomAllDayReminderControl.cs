using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Time;

public class CustomAllDayReminderControl : UserControl, ITabControl, IComponentConnector
{
	private string _currentType;

	private EscPopup _popup;

	private ObservableCollection<ComboBoxViewModel> _dayOrWeekItems;

	private string _advanceByDay;

	private string _advanceByWeek;

	private int _tabIndex = -1;

	internal CustomComboBox DayOrWeekComboBox;

	internal TextBlock AdvanceText;

	internal TextBox DayOrWeekText;

	internal TimeInputControl TimeInput;

	internal TextBlock ReminderText;

	internal Button CancelButton;

	internal Button SaveButton;

	internal TextBox EmptyWidth;

	private bool _contentLoaded;

	public event EventHandler<string> Save;

	public CustomAllDayReminderControl(AdvanceDateModel model, EscPopup popup, bool inTab)
	{
		InitializeComponent();
		base.DataContext = model;
		InitTimeInput(model);
		_popup = popup;
		_popup.Child = this;
		_tabIndex = ((!inTab) ? 1 : 0);
		DayOrWeekText.TextChanged -= OnDayOrWeekTextChanged;
		DayOrWeekText.TextChanged += OnDayOrWeekTextChanged;
		_currentType = "ByDay";
		InitDayOrWeekComboBox();
	}

	private void InitDayOrWeekComboBox()
	{
		_advanceByDay = Utils.GetString("AdvanceByDay");
		_advanceByWeek = Utils.GetString("AdvanceByWeek");
		_dayOrWeekItems = new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel(_advanceByDay, _advanceByDay, 32.0)
			{
				Selected = true
			},
			new ComboBoxViewModel(_advanceByWeek, _advanceByWeek, 32.0)
		};
		DayOrWeekComboBox.Init(_dayOrWeekItems, _dayOrWeekItems[0]);
	}

	private void InitTimeInput(AdvanceDateModel model)
	{
		TimeInput.SelectedTime = DateUtils.SetHourAndMinuteOnly(DateTime.Today, model.Hour, model.Minute);
		TimeInput.SelectedTimeChanged -= OnTimeChanged;
		TimeInput.SelectedTimeChanged += OnTimeChanged;
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		if (_popup != null)
		{
			_popup.IsOpen = false;
		}
	}

	private void OnDayOrWeekPreviewInput(object sender, TextCompositionEventArgs e)
	{
		if (sender != null && sender is TextBox textBox)
		{
			int result;
			bool flag = int.TryParse(textBox.Text.Insert(textBox.SelectionStart, e.Text), out result);
			e.Handled = !flag;
		}
	}

	private async void OnWeekTextChanged()
	{
		int.TryParse(DayOrWeekText.Text, out var result);
		if (result > 12)
		{
			DayOrWeekText.Text = "12";
			FocusDayOrWeekText();
			return;
		}
		if (result < 0)
		{
			DayOrWeekText.Text = "0";
			FocusDayOrWeekText();
			return;
		}
		DayOrWeekText.Text = result.ToString();
		AdvanceText.Text = Utils.GetString((result > 1) ? "WeeksInAdvance" : "WeekInAdvance");
		if (result == 0)
		{
			FocusDayOrWeekText();
		}
		if (base.DataContext != null && base.DataContext is AdvanceDateModel advanceDateModel)
		{
			advanceDateModel.AdvanceDays = result * 7;
			advanceDateModel.Trigger = advanceDateModel.ToRule();
		}
	}

	private void FocusDayOrWeekText()
	{
		if (_tabIndex == 1)
		{
			DayOrWeekText.Focus();
			DayOrWeekText.SelectAll();
		}
	}

	private async Task OnDayTextChanged()
	{
		int.TryParse(DayOrWeekText.Text, out var result);
		if (result > 60)
		{
			DayOrWeekText.Text = "60";
			DayOrWeekText.SelectAll();
			return;
		}
		if (result < 0)
		{
			DayOrWeekText.Text = "0";
			DayOrWeekText.SelectAll();
			return;
		}
		DayOrWeekText.Text = result.ToString();
		AdvanceText.Text = Utils.GetString((result > 1) ? "DaysInAdvance" : "DayInAdvance");
		if (result == 0)
		{
			DayOrWeekText.SelectAll();
		}
		if (base.DataContext != null && base.DataContext is AdvanceDateModel advanceDateModel)
		{
			advanceDateModel.AdvanceDays = result;
			advanceDateModel.Trigger = advanceDateModel.ToRule();
		}
	}

	private async void OnDayOrWeekTextChanged(object sender, TextChangedEventArgs e)
	{
		if (DayOrWeekComboBox.SelectedItem.Value == _advanceByDay)
		{
			OnDayTextChanged();
		}
		else
		{
			OnWeekTextChanged();
		}
	}

	private void TrySaveAndClose()
	{
		if (base.DataContext != null && base.DataContext is AdvanceDateModel advanceDateModel)
		{
			if (_popup != null)
			{
				_popup.IsOpen = false;
			}
			this.Save?.Invoke(this, advanceDateModel.ToRule());
		}
	}

	private async void OnInit(object sender, EventArgs e)
	{
		await Task.Delay(100);
		if (_tabIndex >= 0)
		{
			DayOrWeekComboBox.TabSelected = true;
		}
		FocusDayOrWeekText();
	}

	private void TimeClick(object sender, MouseButtonEventArgs e)
	{
		DayOrWeekComboBox.TabSelected = false;
		TimeInput.TabSelected = false;
		UiUtils.SetSaveButtonTabSelected(SaveButton, tabSelected: false);
		UiUtils.SetCancelButtonTabSelected(CancelButton, tabSelected: false);
	}

	private void OnTimeChanged(object sender, DateTime date)
	{
		if (base.DataContext != null && base.DataContext is AdvanceDateModel advanceDateModel)
		{
			advanceDateModel.Hour = date.Hour;
			advanceDateModel.Minute = date.Minute;
			advanceDateModel.Trigger = advanceDateModel.ToRule();
		}
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		TrySaveAndClose();
	}

	private void DayOrWeekComboBox_SelectionChanged(object sender, ComboBoxViewModel model)
	{
		TimeInput.HourText.Text = "09";
		TimeInput.MinuteText.Text = "00";
		TimeInput.AmOrPmText.Text = "AM";
		bool flag = model.Value == _advanceByDay;
		if (DayOrWeekText.Text == "1")
		{
			if (flag)
			{
				OnDayTextChanged();
			}
			else
			{
				OnWeekTextChanged();
			}
		}
		else
		{
			DayOrWeekText.Text = "1";
			AdvanceText.Text = Utils.GetString(flag ? "DayInAdvance" : "WeekInAdvance");
		}
	}

	public async void Show()
	{
		_popup.IsOpen = true;
	}

	public bool HandleEsc()
	{
		if (TimeInput.ReminderPopup.IsOpen)
		{
			TimeInput.ReminderPopup.IsOpen = false;
			return true;
		}
		return false;
	}

	public bool HandleTab(bool shift)
	{
		if (DayOrWeekComboBox.IsOpen || TimeInput.ReminderPopup.IsOpen)
		{
			return true;
		}
		_tabIndex += 5 + ((!shift) ? 1 : (-1));
		_tabIndex %= 5;
		if (_tabIndex == 1)
		{
			DayOrWeekText.Focus();
			DayOrWeekText.SelectAll();
		}
		else
		{
			EmptyWidth.Focus();
		}
		DayOrWeekComboBox.TabSelected = _tabIndex == 0;
		TimeInput.TabSelected = _tabIndex == 2;
		UiUtils.SetSaveButtonTabSelected(SaveButton, _tabIndex == 3);
		UiUtils.SetCancelButtonTabSelected(CancelButton, _tabIndex == 4);
		return true;
	}

	public bool UpDownSelect(bool isUp)
	{
		if (DayOrWeekComboBox.IsOpen)
		{
			DayOrWeekComboBox.UpDownSelect(isUp);
		}
		return DayOrWeekComboBox.IsOpen;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		return false;
	}

	public bool HandleEnter()
	{
		switch (_tabIndex)
		{
		case 0:
			if (DayOrWeekComboBox.TabSelected)
			{
				return DayOrWeekComboBox.HandleEnter();
			}
			break;
		case 2:
			TimeInput.ToggleReminderPopup();
			TimeInput.FocusHour();
			break;
		case 3:
			TrySaveAndClose();
			break;
		case 4:
			OnCancelClick(null, null);
			break;
		}
		return true;
	}

	private void OnTextGotFocus(object sender, RoutedEventArgs e)
	{
		_tabIndex = 1;
		DayOrWeekText.Focus();
		DayOrWeekText.SelectAll();
		DayOrWeekComboBox.TabSelected = false;
		TimeInput.TabSelected = false;
		UiUtils.SetSaveButtonTabSelected(SaveButton, tabSelected: false);
		UiUtils.SetCancelButtonTabSelected(CancelButton, tabSelected: false);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/customalldayremindercontrol.xaml", UriKind.Relative);
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
			((CustomAllDayReminderControl)target).Initialized += OnInit;
			break;
		case 2:
			DayOrWeekComboBox = (CustomComboBox)target;
			break;
		case 3:
			AdvanceText = (TextBlock)target;
			break;
		case 4:
			DayOrWeekText = (TextBox)target;
			DayOrWeekText.PreviewTextInput += OnDayOrWeekPreviewInput;
			DayOrWeekText.GotFocus += OnTextGotFocus;
			break;
		case 5:
			((Grid)target).PreviewMouseLeftButtonUp += TimeClick;
			break;
		case 6:
			TimeInput = (TimeInputControl)target;
			break;
		case 7:
			ReminderText = (TextBlock)target;
			break;
		case 8:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancelClick;
			break;
		case 9:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 10:
			EmptyWidth = (TextBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
