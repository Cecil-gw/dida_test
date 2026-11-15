using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Ical.Net;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Time;

public class SetRepeatEndControl : UserControl, ITabControl, IComponentConnector
{
	private int _repeatUntilIndex;

	internal StackPanel RepeatEndSelectPanel;

	internal OptionItemWithImageIcon NoRepeat;

	internal OptionItemWithImageIcon RepeatUntil;

	internal OptionItemWithImageIcon RepeatCount;

	internal StackPanel SelectRepeatUntilPanel;

	internal TextBox RepeatCountText;

	internal Button SaveButton;

	internal Button CancelButton;

	internal TickDatePicker SelectRepeatEndDatePicker;

	internal TextBox EmptyBox;

	private bool _contentLoaded;

	private TimeData TimeData => (TimeData)base.DataContext;

	public event RepeatEndHandler RepeatEndChange;

	public event EventHandler Cancel;

	public SetRepeatEndControl()
	{
		InitializeComponent();
		SelectRepeatEndDatePicker.SetMinDate(DateTime.Today);
	}

	public void Init(bool enter)
	{
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(TimeData.RepeatFlag);
		if (recurrenceModel.Count > 1)
		{
			ShowSelectCountDialog();
		}
		else if (Utils.IsEmptyDate(recurrenceModel.Until))
		{
			RepeatEndSelectPanel.Visibility = Visibility.Visible;
			SelectRepeatEndDatePicker.Visibility = Visibility.Collapsed;
			SelectRepeatUntilPanel.Visibility = Visibility.Collapsed;
			if (enter)
			{
				NoRepeat.HoverSelected = true;
			}
		}
		else
		{
			ShowSelectEndPicker();
			if (enter)
			{
				SelectRepeatEndDatePicker.TabSelectCurrent();
			}
		}
	}

	private void OnSetNoRepeatEndClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ClearRepeatEnd();
	}

	private void OnSelectRepeatClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ShowSelectEndPicker();
	}

	private void ShowSelectEndPicker()
	{
		RepeatEndSelectPanel.Visibility = Visibility.Collapsed;
		SelectRepeatEndDatePicker.Visibility = Visibility.Visible;
		SelectRepeatUntilPanel.Visibility = Visibility.Collapsed;
		bool flag = false;
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(TimeData.RepeatFlag);
		DateTime? dateTime = TimeData.StartDate;
		if (!dateTime.HasValue || dateTime.Value.Date <= DateTime.Today)
		{
			dateTime = DateTime.Today;
		}
		DateTime dateTime2 = dateTime.Value.Date.AddDays(7.0);
		if (!Utils.IsEmptyDate(recurrenceModel.Until))
		{
			dateTime2 = recurrenceModel.Until;
		}
		else
		{
			switch (recurrenceModel.Frequency)
			{
			case FrequencyType.Daily:
				dateTime2 = dateTime.Value.AddDays(7.0);
				break;
			case FrequencyType.Weekly:
				dateTime2 = dateTime.Value.AddMonths(1);
				break;
			case FrequencyType.Monthly:
				dateTime2 = dateTime.Value.AddYears(1);
				break;
			case FrequencyType.Yearly:
				dateTime2 = dateTime.Value.AddYears(5);
				break;
			}
			flag = true;
		}
		SelectRepeatEndDatePicker.ClearTabSelected();
		TickDatePicker selectRepeatEndDatePicker = SelectRepeatEndDatePicker;
		DateTime? selectedDate = dateTime2;
		DateTime? minDate = DateTime.Today;
		selectRepeatEndDatePicker.SetData(selectedDate, null, null, isStart: true, isMonthMode: false, null, minDate);
		if (flag)
		{
			TimeData.RepeatFlag = RepeatUtils.GetRepeatFlag(TimeData.RepeatFlag, dateTime2);
		}
	}

	private void OnRepeatEndSelected(object sender, DateTime until)
	{
		DateTime? startDate = TimeData.StartDate;
		if (!(until < startDate))
		{
			this.RepeatEndChange?.Invoke(-1, until);
		}
	}

	private void ClearRepeatEnd()
	{
		this.RepeatEndChange?.Invoke(-1, default(DateTime));
	}

	private void OnSelectCountClick(object sender, MouseButtonEventArgs e)
	{
		ShowSelectCountDialog();
	}

	private void ShowSelectCountDialog()
	{
		RepeatEndSelectPanel.Visibility = Visibility.Collapsed;
		SelectRepeatEndDatePicker.Visibility = Visibility.Collapsed;
		SelectRepeatUntilPanel.Visibility = Visibility.Visible;
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(TimeData.RepeatFlag);
		RepeatCountText.Text = ((recurrenceModel.Count > 0) ? recurrenceModel.Count.ToString() : "2");
		RepeatCountText.SelectAll();
		RepeatCountText.Focus();
	}

	private void OnRepeatCountCancelClick(object sender, RoutedEventArgs e)
	{
		this.Cancel?.Invoke(this, null);
	}

	private void OnRepeatCountSaveClick(object sender, RoutedEventArgs e)
	{
		int count = int.Parse(RepeatCountText.Text);
		this.RepeatEndChange?.Invoke(count, default(DateTime));
	}

	private void OnRepeatCountPreviewInput(object sender, TextCompositionEventArgs e)
	{
		if (sender != null && sender is TextBox textBox)
		{
			int result;
			bool flag = int.TryParse(textBox.Text.Insert(textBox.SelectionStart, e.Text), out result);
			e.Handled = !flag;
		}
	}

	private void OnRepeatCountTextChanged(object sender, TextChangedEventArgs e)
	{
		DelayActionHandlerCenter.TryDoAction("CheckRepeatCountText", CheckTextValid, 400);
	}

	private void CheckTextValid(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, CheckInputValid);
	}

	private void CheckInputValid()
	{
		int.TryParse(RepeatCountText.Text, out var result);
		if (result > 200)
		{
			RepeatCountText.Text = "200";
			RepeatCountText.SelectAll();
		}
		if (result <= 1)
		{
			RepeatCountText.Text = "2";
			RepeatCountText.SelectAll();
		}
	}

	public bool HandleTab(bool shift)
	{
		if (SelectRepeatUntilPanel.Visibility == Visibility.Visible)
		{
			_repeatUntilIndex += 3 + ((!shift) ? 1 : (-1));
			_repeatUntilIndex %= 3;
			if (_repeatUntilIndex == 0)
			{
				RepeatCountText.Focus();
			}
			else
			{
				EmptyBox.Focus();
			}
			UiUtils.SetSaveButtonTabSelected(SaveButton, _repeatUntilIndex == 1);
			UiUtils.SetCancelButtonTabSelected(CancelButton, _repeatUntilIndex == 2);
		}
		return true;
	}

	public bool HandleEnter()
	{
		if (RepeatEndSelectPanel.Visibility == Visibility.Visible)
		{
			if (NoRepeat.HoverSelected)
			{
				ClearRepeatEnd();
			}
			else if (RepeatUntil.HoverSelected)
			{
				ShowSelectEndPicker();
				SelectRepeatEndDatePicker.TabSelectCurrent();
			}
			else if (RepeatCount.HoverSelected)
			{
				ShowSelectCountDialog();
			}
			return true;
		}
		if (SelectRepeatUntilPanel.Visibility == Visibility.Visible)
		{
			switch (_repeatUntilIndex)
			{
			case 1:
			{
				int count = int.Parse(RepeatCountText.Text);
				this.RepeatEndChange?.Invoke(count, default(DateTime));
				break;
			}
			case 2:
				this.Cancel?.Invoke(this, null);
				break;
			}
		}
		if (SelectRepeatEndDatePicker.Visibility == Visibility.Visible)
		{
			SelectRepeatEndDatePicker.SelectTabItem();
		}
		return true;
	}

	public bool HandleEsc()
	{
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		if (RepeatEndSelectPanel.Visibility == Visibility.Visible)
		{
			if (!NoRepeat.HoverSelected && !RepeatUntil.HoverSelected && !RepeatCount.HoverSelected)
			{
				NoRepeat.HoverSelected = true;
			}
			else if (NoRepeat.HoverSelected)
			{
				NoRepeat.HoverSelected = false;
				RepeatUntil.HoverSelected = !isUp;
				RepeatCount.HoverSelected = isUp;
			}
			else if (RepeatUntil.HoverSelected)
			{
				NoRepeat.HoverSelected = isUp;
				RepeatUntil.HoverSelected = false;
				RepeatCount.HoverSelected = !isUp;
			}
			else
			{
				NoRepeat.HoverSelected = !isUp;
				RepeatUntil.HoverSelected = isUp;
				RepeatCount.HoverSelected = false;
			}
		}
		else if (SelectRepeatEndDatePicker.Visibility == Visibility.Visible)
		{
			SelectRepeatEndDatePicker.MoveTabSelectDate(isUp ? (-7) : 7);
		}
		return true;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		if (SelectRepeatEndDatePicker.Visibility == Visibility.Visible)
		{
			SelectRepeatEndDatePicker.MoveTabSelectDate((!isLeft) ? 1 : (-1));
		}
		return true;
	}

	private void OnItemMouseEnter(object sender, MouseEventArgs e)
	{
		if (sender is OptionItemWithImageIcon optionItemWithImageIcon)
		{
			NoRepeat.HoverSelected = false;
			RepeatUntil.HoverSelected = false;
			RepeatCount.HoverSelected = false;
			optionItemWithImageIcon.HoverSelected = true;
		}
	}

	private void OnRepeatCountFocused(object sender, RoutedEventArgs e)
	{
		_repeatUntilIndex = 0;
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
			Uri resourceLocator = new Uri("/TickTick;component/views/time/setrepeatendcontrol.xaml", UriKind.Relative);
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
			RepeatEndSelectPanel = (StackPanel)target;
			break;
		case 2:
			NoRepeat = (OptionItemWithImageIcon)target;
			break;
		case 3:
			RepeatUntil = (OptionItemWithImageIcon)target;
			break;
		case 4:
			RepeatCount = (OptionItemWithImageIcon)target;
			break;
		case 5:
			SelectRepeatUntilPanel = (StackPanel)target;
			break;
		case 6:
			RepeatCountText = (TextBox)target;
			RepeatCountText.PreviewTextInput += OnRepeatCountPreviewInput;
			RepeatCountText.GotFocus += OnRepeatCountFocused;
			RepeatCountText.TextChanged += OnRepeatCountTextChanged;
			break;
		case 7:
			SaveButton = (Button)target;
			SaveButton.Click += OnRepeatCountSaveClick;
			break;
		case 8:
			CancelButton = (Button)target;
			CancelButton.Click += OnRepeatCountCancelClick;
			break;
		case 9:
			SelectRepeatEndDatePicker = (TickDatePicker)target;
			break;
		case 10:
			EmptyBox = (TextBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
