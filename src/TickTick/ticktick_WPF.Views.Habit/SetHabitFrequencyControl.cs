using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Ical.Net;
using Ical.Net.DataTypes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.Habit;

public class SetHabitFrequencyControl : UserControl, IComponentConnector
{
	private bool _needCorrectIntervalText;

	private static readonly string ByDay = Utils.GetString("ByDay");

	private static readonly string ByWeek = Utils.GetString("ByWeek");

	private static readonly string ByTimeSpan = Utils.GetString("ByTimeSpan");

	private ComboBoxViewModel _byDayModel = new ComboBoxViewModel(ByDay, ByDay, 32.0);

	private ComboBoxViewModel _byWeekModel = new ComboBoxViewModel(ByWeek, ByWeek, 32.0);

	private ComboBoxViewModel _byTimeSpanModel = new ComboBoxViewModel(ByTimeSpan, ByTimeSpan, 32.0);

	private ObservableCollection<ComboBoxViewModel> _freqTypeComboBoxModels = new ObservableCollection<ComboBoxViewModel>();

	private ObservableCollection<ComboBoxViewModel> _weekTimesComboBoxModels = new ObservableCollection<ComboBoxViewModel>();

	internal CustomComboBox FreqTypeComboBox;

	internal StackPanel ByDayPanel;

	internal TextBlock DayText;

	internal WeekdaySelector WeekDaySelector;

	internal StackPanel ByWeekPanel;

	internal CustomComboBox WeekTimesComboBox;

	internal TextBlock WeekDayText;

	internal StackPanel ByTimeSpanPanel;

	internal TextBox IntervalText;

	private bool _contentLoaded;

	public event EventHandler<string> OnFreqRuleSaved;

	public event EventHandler Closed;

	public SetHabitFrequencyControl()
	{
		InitializeComponent();
		WeekDaySelector.SelectedDaysChanged += OnSelectedDaysChanged;
		WeekTimesComboBox.ItemSelected += OnWeekTimesComboBoxItemSelected;
		_freqTypeComboBoxModels = new ObservableCollection<ComboBoxViewModel> { _byDayModel, _byWeekModel, _byTimeSpanModel };
		for (int i = 1; i <= 7; i++)
		{
			_weekTimesComboBoxModels.Add(new ComboBoxViewModel(i, i.ToString(), 24.0));
		}
	}

	public void Init(string habitRule)
	{
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(habitRule);
		_freqTypeComboBoxModels.ToList().ForEach(delegate(ComboBoxViewModel item)
		{
			item.Selected = false;
		});
		_weekTimesComboBoxModels.ToList().ForEach(delegate(ComboBoxViewModel item)
		{
			item.Selected = false;
		});
		switch (recurrenceModel.Frequency)
		{
		case FrequencyType.Weekly:
			if (habitRule.Contains("TT_TIMES"))
			{
				_byWeekModel.Selected = true;
				Match match = new Regex("TT_TIMES=(\\d{0,})").Match(habitRule);
				if (match.Success && int.TryParse(match.Groups[1].ToString(), out var result))
				{
					_weekTimesComboBoxModels[Math.Min(result - 1, 5)].Selected = true;
				}
			}
			else
			{
				_byDayModel.Selected = true;
			}
			break;
		case FrequencyType.Daily:
			((recurrenceModel.Interval > 1) ? _byTimeSpanModel : _byDayModel).Selected = true;
			IntervalText.Text = Math.Max(2, recurrenceModel.Interval) + string.Empty;
			break;
		default:
			_byDayModel.Selected = true;
			break;
		}
		List<DayOfWeek> list = null;
		if (_byDayModel.Selected)
		{
			list = ((recurrenceModel.Frequency != FrequencyType.Weekly) ? null : recurrenceModel.ByDay?.Select((WeekDay d) => d.DayOfWeek).ToList());
			SetDayRepeatText(recurrenceModel.Frequency == FrequencyType.Daily, habitRule);
		}
		if (list == null)
		{
			DayOfWeek[] array = (DayOfWeek[])Enum.GetValues(typeof(DayOfWeek));
			int count = Array.IndexOf(array, LocalSettings.Settings.WeekStartFromType);
			list = array.Skip(count).Concat(array.Take(count)).ToList();
		}
		WeekDaySelector.SelectedDays = list;
		WeekDaySelector.NotifySelectedChanged();
		FreqTypeComboBox.Init(_freqTypeComboBoxModels, _freqTypeComboBoxModels.FirstOrDefault((ComboBoxViewModel item) => item.Selected) ?? _freqTypeComboBoxModels[0]);
		ChangeFreqTypePanelVisibility(FreqTypeComboBox.SelectedItem.Value as string);
		WeekTimesComboBox.Init(_weekTimesComboBoxModels, _weekTimesComboBoxModels.FirstOrDefault((ComboBoxViewModel item) => item.Selected) ?? _weekTimesComboBoxModels[0]);
	}

	private void ChangeFreqTypePanelVisibility(string freqType)
	{
		ByDayPanel.Visibility = Visibility.Collapsed;
		ByWeekPanel.Visibility = Visibility.Collapsed;
		ByTimeSpanPanel.Visibility = Visibility.Collapsed;
		if (ByDay.Equals(freqType))
		{
			ByDayPanel.Visibility = Visibility.Visible;
		}
		else if (ByWeek.Equals(freqType))
		{
			ByWeekPanel.Visibility = Visibility.Visible;
		}
		else if (ByTimeSpan.Equals(freqType))
		{
			ByTimeSpanPanel.Visibility = Visibility.Visible;
		}
	}

	private void OnSelectedDaysChanged(object sender, EventArgs e)
	{
		SetByDayRule();
	}

	private void SetByDayRule()
	{
		if (_byDayModel.Selected)
		{
			RecurrencePattern byDayPattern = GetByDayPattern();
			SetDayRepeatText(byDayPattern.Frequency == FrequencyType.Daily, RecurrenceModel.Normalize(byDayPattern));
		}
	}

	private void SetDayRepeatText(bool isDaily, string rule)
	{
		DayText.Text = (isDaily ? Utils.GetString("PickDays") : RRuleUtils.RRule2String("2", rule, null, showUntil: false));
	}

	private RecurrencePattern GetByDayPattern()
	{
		RecurrencePattern recurrencePattern = new RecurrencePattern
		{
			Interval = 1
		};
		bool flag = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().All((DayOfWeek value) => WeekDaySelector.SelectedDays.Contains(value));
		recurrencePattern.Frequency = (flag ? FrequencyType.Daily : FrequencyType.Weekly);
		if (!flag)
		{
			List<WeekDay> byDay = WeekDaySelector.SelectedDays.Select((DayOfWeek item) => new WeekDay(item)).ToList();
			recurrencePattern.ByDay = byDay;
		}
		return recurrencePattern;
	}

	private string GetByWeekRule()
	{
		if (WeekTimesComboBox.SelectedItem.Value is int num)
		{
			if (num == 7)
			{
				return "RRULE:FREQ=DAILY";
			}
			return "RRULE:FREQ=WEEKLY;INTERVAL=1;TT_TIMES=" + num;
		}
		return "";
	}

	private string GetBySpanRule()
	{
		RecurrencePattern recurrencePattern = new RecurrencePattern
		{
			Frequency = FrequencyType.Daily
		};
		if (int.TryParse(IntervalText.Text, out var result))
		{
			if (result > 30)
			{
				recurrencePattern.Interval = 30;
				IntervalText.Text = "30";
			}
			else if (result < 1)
			{
				recurrencePattern.Interval = 1;
				IntervalText.Text = "1";
			}
			else
			{
				recurrencePattern.Interval = result;
			}
		}
		else
		{
			recurrencePattern.Interval = 2;
			IntervalText.Text = "2";
		}
		return "RRULE:" + RecurrenceModel.Normalize(recurrencePattern);
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		string rule = "";
		if (FreqTypeComboBox.SelectedItem.Value is string value)
		{
			if (ByDay.Equals(value))
			{
				rule = "RRULE:" + RecurrenceModel.Normalize(GetByDayPattern());
			}
			else if (ByWeek.Equals(value))
			{
				rule = GetByWeekRule();
			}
			else if (ByTimeSpan.Equals(value))
			{
				rule = GetBySpanRule();
			}
		}
		await Task.Delay(100);
		if (!string.IsNullOrEmpty(rule))
		{
			this.OnFreqRuleSaved?.Invoke(this, rule);
		}
		this.Closed?.Invoke(this, null);
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		this.Closed?.Invoke(this, null);
	}

	private async void OnIntervalTextChanged(object sender, TextChangedEventArgs e)
	{
		if (int.TryParse(IntervalText.Text, out var result))
		{
			if (result > 30)
			{
				IntervalText.Text = "30";
				IntervalText.SelectAll();
			}
			else if (result <= 0)
			{
				IntervalText.Text = "1";
				IntervalText.SelectAll();
			}
		}
		else
		{
			IntervalText.Text = "2";
			IntervalText.SelectAll();
		}
	}

	private void HandleNumberInput(object sender, TextCompositionEventArgs e)
	{
		if (e.Text.Length >= 1 && !char.IsDigit(e.Text, e.Text.Length - 1))
		{
			e.Handled = true;
		}
	}

	private void OnWeekTimesComboBoxItemSelected(object sender, ComboBoxViewModel e)
	{
		WeekDayText.Text = Utils.GetString(((int)e.Value == 1) ? "PublicDay" : "PublicDays");
	}

	private void OnFreqTypeChanged(object sender, ComboBoxViewModel e)
	{
		ChangeFreqTypePanelVisibility(e.Value as string);
		if (e.Value is string value)
		{
			if (ByDay.Equals(value))
			{
				SetByDayRule();
				FreqTypeComboBox.Tag = 0;
				UserActCollectUtils.AddClickEvent("habit", "add_edit_habit", "fq_daily");
			}
			else if (ByWeek.Equals(value))
			{
				FreqTypeComboBox.Tag = 1;
				UserActCollectUtils.AddClickEvent("habit", "add_edit_habit", "fq_weekly");
			}
			else if (ByTimeSpan.Equals(value))
			{
				UserActCollectUtils.AddClickEvent("habit", "add_edit_habit", "fq_interval");
				IntervalText.Focus();
				IntervalText.SelectAll();
				FreqTypeComboBox.Tag = 3;
			}
		}
	}

	public void OnEnter()
	{
		OnSaveClick(null, null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/sethabitfrequencycontrol.xaml", UriKind.Relative);
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
			FreqTypeComboBox = (CustomComboBox)target;
			break;
		case 2:
			ByDayPanel = (StackPanel)target;
			break;
		case 3:
			DayText = (TextBlock)target;
			break;
		case 4:
			WeekDaySelector = (WeekdaySelector)target;
			break;
		case 5:
			ByWeekPanel = (StackPanel)target;
			break;
		case 6:
			WeekTimesComboBox = (CustomComboBox)target;
			break;
		case 7:
			WeekDayText = (TextBlock)target;
			break;
		case 8:
			ByTimeSpanPanel = (StackPanel)target;
			break;
		case 9:
			IntervalText = (TextBox)target;
			IntervalText.PreviewTextInput += HandleNumberInput;
			IntervalText.TextChanged += OnIntervalTextChanged;
			break;
		case 10:
			((Button)target).Click += OnSaveClick;
			break;
		case 11:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
