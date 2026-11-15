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
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Time;

public class CustomReminderControl : UserControl, ITabControl, IComponentConnector
{
	private enum TimeUnitType
	{
		Minute,
		Hour,
		Day,
		Week
	}

	private new enum TabIndex
	{
		Textbox,
		OkButton,
		CancelButton
	}

	private DateTime _date;

	private TabIndex _tabIndex;

	private bool loaded;

	internal TextBox RemindTimeTextBox;

	internal CustomComboBox RemindUnitComboBox;

	internal TextBlock ErrorTextBlock;

	internal TextBlock RemindTextBlock;

	internal TextBlock RemindTimeTextBlock;

	internal Button SaveButton;

	internal Button CancelButton;

	internal TextBox EmptyBox;

	private bool _contentLoaded;

	private ObservableCollection<ComboBoxViewModel> comboBoxModels { get; set; } = new ObservableCollection<ComboBoxViewModel>();

	public event EventHandler OnCancel;

	public event EventHandler<string> OnSave;

	public CustomReminderControl(DateTime date)
	{
		InitializeComponent();
		_date = date;
		if (Utils.IsEmptyDate(_date))
		{
			RemindTimeTextBlock.Visibility = Visibility.Collapsed;
		}
		UpdateRemindText();
		base.Loaded += async delegate
		{
			loaded = true;
			await Task.Delay(150);
			RemindTimeTextBox.Focus();
			RemindTimeTextBox.SelectAll();
		};
		comboBoxModels.Add(new ComboBoxViewModel(TimeUnitType.Minute, Utils.GetString("UpMinute"), 32.0)
		{
			Selected = true
		});
		comboBoxModels.Add(new ComboBoxViewModel(TimeUnitType.Hour, Utils.GetString("UpHour"), 32.0));
		comboBoxModels.Add(new ComboBoxViewModel(TimeUnitType.Day, Utils.GetString("PublicUpDay"), 32.0));
		comboBoxModels.Add(new ComboBoxViewModel(TimeUnitType.Week, Utils.GetString("PublicWeek"), 32.0));
		RemindUnitComboBox.Init(comboBoxModels, comboBoxModels.FirstOrDefault((ComboBoxViewModel item) => item.Selected) ?? comboBoxModels[0]);
	}

	private void UpdateRemindText()
	{
		string text = CheckLimit();
		ErrorTextBlock.Text = text;
		ErrorTextBlock.Visibility = (string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible);
		int.TryParse(RemindTimeTextBox.Text, out var result);
		string empty = string.Empty;
		if (result <= 0)
		{
			empty = Utils.GetString("OnTime");
		}
		else
		{
			Dictionary<TimeUnitType, int> dictionary = Calculate();
			empty = string.Format(Utils.GetString("ReminderBefore"), GetHasValue(dictionary[TimeUnitType.Day]), GetUnitText(dictionary[TimeUnitType.Day], TimeUnitType.Day), GetHasValue(dictionary[TimeUnitType.Hour]), GetUnitText(dictionary[TimeUnitType.Hour], TimeUnitType.Hour), GetHasValue(dictionary[TimeUnitType.Minute]), GetUnitText(dictionary[TimeUnitType.Minute], TimeUnitType.Minute));
		}
		RemindTextBlock.Text = empty;
		if (!Utils.IsEmptyDate(_date))
		{
			DateTime date = _date;
			switch ((TimeUnitType)RemindUnitComboBox.SelectedIndex)
			{
			case TimeUnitType.Minute:
				date = date.AddMinutes(-1 * result);
				break;
			case TimeUnitType.Hour:
				date = date.AddHours(-1 * result);
				break;
			case TimeUnitType.Day:
				date = date.AddDays(-1 * result);
				break;
			case TimeUnitType.Week:
				date = date.AddDays(-7 * result);
				break;
			}
			string arg;
			try
			{
				arg = DateUtils.FormatYearMonthDay(date, DateFormatLen.Long) + date.ToString(" " + DateUtils.GetTimeFormatString(), App.Ci);
			}
			catch (FormatException)
			{
				arg = date.ToString("MM/dd/yyyy HH:mm:ss", App.Ci);
			}
			RemindTimeTextBlock.Text = string.Format(Utils.GetString("ReminderTime"), arg);
		}
	}

	private Dictionary<TimeUnitType, int> Calculate()
	{
		int value = 0;
		int value2 = 0;
		int value3 = 0;
		TimeUnitType selectedIndex = (TimeUnitType)RemindUnitComboBox.SelectedIndex;
		int.TryParse(RemindTimeTextBox.Text, out var result);
		switch (selectedIndex)
		{
		case TimeUnitType.Minute:
			value = result / 1440;
			value2 = result % 1440 / 60;
			value3 = result % 1440 % 60;
			break;
		case TimeUnitType.Hour:
			value = result / 24;
			value2 = result % 24;
			break;
		case TimeUnitType.Day:
			value = result;
			break;
		case TimeUnitType.Week:
			value = result * 7;
			break;
		}
		return new Dictionary<TimeUnitType, int>
		{
			{
				TimeUnitType.Minute,
				value3
			},
			{
				TimeUnitType.Hour,
				value2
			},
			{
				TimeUnitType.Day,
				value
			}
		};
	}

	public string CheckLimit()
	{
		int.TryParse(RemindTimeTextBox.Text, out var result);
		TimeUnitType selectedIndex = (TimeUnitType)RemindUnitComboBox.SelectedIndex;
		string result2 = string.Empty;
		switch (selectedIndex)
		{
		case TimeUnitType.Minute:
			if (result > 86400)
			{
				result2 = string.Format("0~{0} {1}", 86400, Utils.GetString("PublicMinutes"));
			}
			break;
		case TimeUnitType.Hour:
			if (result > 1440)
			{
				result2 = string.Format("0~{0} {1}", 1440, Utils.GetString("PublicHours"));
			}
			break;
		case TimeUnitType.Day:
			if (result > 60)
			{
				result2 = "0~60 " + Utils.GetString("PublicDays");
			}
			break;
		case TimeUnitType.Week:
			if (result > 8)
			{
				result2 = "0~8 " + Utils.GetString("PublicWeeks");
			}
			break;
		}
		return result2;
	}

	private string GetHasValue(int value)
	{
		if (value <= 0)
		{
			return string.Empty;
		}
		return value.ToString();
	}

	private string GetUnitText(int unitValue, TimeUnitType type)
	{
		string text = (Utils.IsCn() ? "" : " ");
		if (unitValue <= 0)
		{
			return string.Empty;
		}
		return type switch
		{
			TimeUnitType.Minute => Utils.GetString((unitValue > 1) ? "PublicMinutes" : "PublicMinute") + text, 
			TimeUnitType.Hour => Utils.GetString((unitValue > 1) ? "PublicHours" : "PublicHour") + text, 
			TimeUnitType.Day => Utils.GetString((unitValue > 1) ? "PublicDays" : "PublicDay") + text, 
			TimeUnitType.Week => Utils.GetString((unitValue > 1) ? "PublicWeeks" : "PublicWeek") + text, 
			_ => string.Empty, 
		};
	}

	private void RemindTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (loaded)
		{
			UpdateRemindText();
		}
	}

	private void RemindUnitComboBox_ItemSelected(object sender, ComboBoxViewModel e)
	{
		if (loaded)
		{
			UpdateRemindText();
		}
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(CheckLimit()))
		{
			Dictionary<TimeUnitType, int> dictionary = Calculate();
			UtilLog.Info($"CustomReminder m {dictionary[TimeUnitType.Minute]}, h {dictionary[TimeUnitType.Hour]}, d{dictionary[TimeUnitType.Day]}");
			string triggerValue = Utils.GetTriggerValue(new Dictionary<string, int>
			{
				{
					"M",
					dictionary[TimeUnitType.Minute]
				},
				{
					"H",
					dictionary[TimeUnitType.Hour]
				},
				{
					"D",
					dictionary[TimeUnitType.Day]
				}
			});
			this.OnSave?.Invoke(this, triggerValue);
		}
	}

	private void OnCancel_Click(object sender, RoutedEventArgs e)
	{
		this.OnCancel?.Invoke(this, null);
	}

	private void OnlyNumberInput(object sender, KeyEventArgs e)
	{
		if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
		{
			e.Handled = true;
		}
		else if ((e.Key < Key.D0 || e.Key > Key.D9) && (e.Key < Key.NumPad0 || e.Key > Key.NumPad9) && e.Key != Key.Back && e.Key != Key.Delete && e.Key != Key.Left && e.Key != Key.Right && e.Key != Key.Up && e.Key != Key.Down)
		{
			e.Handled = true;
		}
	}

	private void TextBox_LostFocus(object sender, RoutedEventArgs e)
	{
		if (sender is TextBox { Text: "" } textBox)
		{
			textBox.Text = "0";
		}
	}

	public bool HandleTab(bool shift)
	{
		_tabIndex += 3 + ((!shift) ? 1 : (-1));
		_tabIndex = (TabIndex)((int)_tabIndex % 3);
		switch (_tabIndex)
		{
		case TabIndex.Textbox:
			RemindTimeTextBox.Focus();
			RemindTimeTextBox.SelectAll();
			break;
		case TabIndex.OkButton:
		case TabIndex.CancelButton:
			EmptyBox.Focus();
			break;
		}
		UiUtils.SetSaveButtonTabSelected(SaveButton, _tabIndex == TabIndex.OkButton);
		UiUtils.SetCancelButtonTabSelected(CancelButton, _tabIndex == TabIndex.CancelButton);
		return true;
	}

	private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
	{
		if (sender.Equals(RemindTimeTextBox))
		{
			_tabIndex = TabIndex.Textbox;
		}
		UiUtils.SetSaveButtonTabSelected(SaveButton, tabSelected: false);
		UiUtils.SetCancelButtonTabSelected(CancelButton, tabSelected: false);
	}

	public bool HandleEnter()
	{
		switch (_tabIndex)
		{
		case TabIndex.OkButton:
			OnSaveClick(null, null);
			return true;
		case TabIndex.CancelButton:
			this.OnCancel?.Invoke(this, null);
			return true;
		default:
			return false;
		}
	}

	public bool HandleEsc()
	{
		this.OnCancel?.Invoke(this, null);
		return true;
	}

	public bool UpDownSelect(bool isUp)
	{
		return false;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		return false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/customremindercontrol.xaml", UriKind.Relative);
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
			RemindTimeTextBox = (TextBox)target;
			RemindTimeTextBox.PreviewKeyDown += OnlyNumberInput;
			RemindTimeTextBox.GotFocus += OnTextBoxGotFocus;
			RemindTimeTextBox.LostFocus += TextBox_LostFocus;
			RemindTimeTextBox.TextChanged += RemindTimeTextBox_TextChanged;
			break;
		case 2:
			RemindUnitComboBox = (CustomComboBox)target;
			break;
		case 3:
			ErrorTextBlock = (TextBlock)target;
			break;
		case 4:
			RemindTextBlock = (TextBlock)target;
			break;
		case 5:
			RemindTimeTextBlock = (TextBlock)target;
			break;
		case 6:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 7:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancel_Click;
			break;
		case 8:
			EmptyBox = (TextBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
