using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Habit;

public class SetHabitGoalControl : UserControl, IComponentConnector
{
	private HabitGoalModel _goal;

	private Timer _resetTimer = new Timer(1500.0);

	internal CheckBox BoolTypeCheckBox;

	internal CheckBox RealTypeCheckBox;

	internal Border GoalBorder;

	internal TextBlock DailyGoalText;

	internal EscPopup SetTimesPopup;

	internal TextBox Times;

	internal TextBox Unit;

	internal CustomComboBox CheckTypeComboBox;

	internal Grid AutoRecordGrid;

	internal TextBlock EachTimeText;

	internal TextBox StepText;

	internal Button SaveButton;

	private bool _contentLoaded;

	public event EventHandler<HabitGoalModel> OnGoalSaved;

	public event EventHandler Closed;

	public SetHabitGoalControl()
	{
		InitializeComponent();
		_resetTimer.Elapsed += ResetStepOrTimes;
	}

	public void Init(HabitModel habit)
	{
		BoolTypeCheckBox.IsChecked = habit.IsBoolHabit();
		RealTypeCheckBox.IsChecked = !habit.IsBoolHabit();
		DailyGoalText.Text = string.Format(Utils.GetString("DailyTimes"), habit.Goal, habit.Unit);
		EachTimeText.Text = string.Format(Utils.GetString("RecordAmountEachTime"), habit.Unit);
		_goal = new HabitGoalModel
		{
			Type = habit.Type,
			Goal = habit.Goal,
			Step = habit.Step,
			Unit = habit.Unit
		};
		ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel(0, Utils.GetString("AutoRecord"), 28.0),
			new ComboBoxViewModel(1, Utils.GetString("ManualRecord"), 28.0),
			new ComboBoxViewModel(2, Utils.GetString("CompleteAll"), 28.0)
		};
		CheckTypeComboBox.Init(observableCollection, observableCollection[0]);
	}

	private async void OnBoolTypeCheckChanged(object sender, MouseButtonEventArgs e)
	{
		await Task.Delay(50);
		RealTypeCheckBox.IsChecked = !BoolTypeCheckBox.IsChecked;
		UserActCollectUtils.AddClickEvent("habit", "add_edit_habit", "goal_achieve");
	}

	private async void OnRealTypeCheckChanged(object sender, MouseButtonEventArgs e)
	{
		await Task.Delay(50);
		BoolTypeCheckBox.IsChecked = !RealTypeCheckBox.IsChecked;
		UserActCollectUtils.AddClickEvent("habit", "add_edit_habit", "goal_amount");
	}

	private void OpenTimesPopup(object sender, MouseButtonEventArgs e)
	{
		Times.Text = _goal.Goal.ToString(CultureInfo.InvariantCulture);
		Unit.TextChanged -= OnUnitChanged;
		Unit.TextChanged += OnUnitChanged;
		Unit.Text = _goal.Unit;
		AutoRecordGrid.Visibility = Visibility.Collapsed;
		if (_goal.Step > 0.0)
		{
			CheckTypeComboBox.SetSelected(0);
			AutoRecordGrid.Visibility = Visibility.Visible;
			StepText.Text = ((_goal.Step <= 0.0) ? 1.0 : _goal.Step).ToString(CultureInfo.InvariantCulture);
		}
		else if (Math.Abs(_goal.Step) < 0.0001)
		{
			CheckTypeComboBox.SetSelected(2);
		}
		else if (Math.Abs(_goal.Step + 1.0) < 0.0001)
		{
			CheckTypeComboBox.SetSelected(1);
		}
		SetTimesPopup.IsOpen = true;
		HwndHelper.SetFocus(SetTimesPopup);
	}

	private void OnBoolTypeTextClick(object sender, MouseButtonEventArgs e)
	{
		BoolTypeCheckBox.IsChecked = true;
		RealTypeCheckBox.IsChecked = false;
	}

	private void OnRealTypeTextClick(object sender, MouseButtonEventArgs e)
	{
		BoolTypeCheckBox.IsChecked = false;
		RealTypeCheckBox.IsChecked = true;
	}

	private void OnSaveTimes(object sender, RoutedEventArgs e)
	{
		if (RealTypeCheckBox.IsChecked == true)
		{
			object value = CheckTypeComboBox.SelectedItem.Value;
			if (value is int)
			{
				switch ((int)value)
				{
				case 0:
				{
					if (MathUtil.TryParseString2Double(StepText.Text, out var db))
					{
						_goal.Step = db;
					}
					break;
				}
				case 1:
					_goal.Step = -1.0;
					break;
				case 2:
					_goal.Step = 0.0;
					break;
				}
			}
			if (MathUtil.TryParseString2Double(Times.Text, out var db2))
			{
				_goal.Goal = db2;
			}
			if (!string.IsNullOrEmpty(Unit.Text))
			{
				_goal.Unit = Unit.Text;
			}
			_goal.Type = HabitType.Real.ToString();
		}
		else
		{
			_goal.Type = HabitType.Boolean.ToString();
		}
		DailyGoalText.Text = string.Format(Utils.GetString("DailyTimes"), _goal.Goal, _goal.Unit);
		SetTimesPopup.IsOpen = false;
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		this.Closed?.Invoke(this, null);
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		_goal.Type = ((RealTypeCheckBox.IsChecked == true) ? HabitType.Real.ToString() : HabitType.Boolean.ToString());
		this.OnGoalSaved?.Invoke(this, _goal);
	}

	private void OnSetTimesCancel(object sender, RoutedEventArgs e)
	{
		SetTimesPopup.IsOpen = false;
	}

	private void CheckInputValid(object sender, TextCompositionEventArgs e)
	{
		if (e.Text.Length >= 1 && !char.IsDigit(e.Text, e.Text.Length - 1) && e.Text != ".")
		{
			e.Handled = true;
		}
		if (!(sender is TextBox textBox))
		{
			return;
		}
		if (textBox.Text.Contains("."))
		{
			if (e.Text == ".")
			{
				e.Handled = true;
			}
			int num = textBox.Text.IndexOf(".", StringComparison.Ordinal);
			if (textBox.CaretIndex > num)
			{
				if (textBox.Text.Substring(num + 1, textBox.Text.Length - num - 1).Length >= 2)
				{
					e.Handled = true;
				}
			}
			else if (textBox.Text.Substring(0, num).Length >= 5)
			{
				e.Handled = true;
			}
		}
		else
		{
			if (textBox.Text.Length >= 5 && e.Text != ".")
			{
				e.Handled = true;
			}
			if (e.Text == "." && textBox.CaretIndex == 0)
			{
				e.Handled = true;
			}
		}
	}

	private void ResetStepOrTimes(object sender, ElapsedEventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, ResetTextBox);
		_resetTimer.Stop();
	}

	private void ResetTextBox()
	{
		if (MathUtil.TryParseString2Double(Times.Text, out var db))
		{
			if (Math.Abs(db) < 0.0001)
			{
				Times.Text = "1";
				Times.SelectAll();
			}
		}
		else
		{
			Times.Text = "1";
			Times.SelectAll();
		}
		if (MathUtil.TryParseString2Double(StepText.Text, out var db2))
		{
			if (Math.Abs(db2) < 0.0001)
			{
				StepText.Text = "1";
				StepText.SelectAll();
			}
		}
		else
		{
			StepText.Text = "1";
			StepText.SelectAll();
		}
	}

	private void OnTimesOrStepTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!(sender is TextBox textBox))
		{
			return;
		}
		if (MathUtil.TryParseString2Double(textBox.Text, out var db))
		{
			if (Math.Abs(db) < 0.0001)
			{
				_resetTimer.Stop();
				_resetTimer.Start();
			}
		}
		else
		{
			_resetTimer.Stop();
			_resetTimer.Start();
		}
		textBox.Text = textBox.Text.Replace(" ", "");
	}

	private void OnTextLostFocus(object sender, RoutedEventArgs e)
	{
		if (sender is TextBox textBox && MathUtil.TryParseString2Double(textBox.Text, out var db))
		{
			_resetTimer.Stop();
			textBox.Text = ((Math.Abs(db) < 0.0001) ? "1" : db.ToString(CultureInfo.InvariantCulture));
		}
	}

	private void OnUnitTextLostFocus(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(Unit.Text))
		{
			Unit.Text = _goal.Unit;
		}
	}

	private void OnUnitChanged(object sender, TextChangedEventArgs e)
	{
		EachTimeText.Text = string.Format(Utils.GetString("RecordAmountEachTime"), string.IsNullOrEmpty(Unit.Text) ? Utils.GetString("Count") : Unit.Text);
	}

	private void OnCheckTypeChanged(object sender, ComboBoxViewModel e)
	{
		AutoRecordGrid.Visibility = ((!e.Value.Equals(0)) ? Visibility.Collapsed : Visibility.Visible);
	}

	public void OnEnter()
	{
		if (SetTimesPopup.IsOpen)
		{
			OnSaveTimes(null, null);
		}
		else
		{
			OnSaveClick(null, null);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/sethabitgoalcontrol.xaml", UriKind.Relative);
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
			BoolTypeCheckBox = (CheckBox)target;
			BoolTypeCheckBox.PreviewMouseLeftButtonUp += OnBoolTypeCheckChanged;
			break;
		case 2:
			((TextBlock)target).MouseLeftButtonUp += OnBoolTypeTextClick;
			break;
		case 3:
			RealTypeCheckBox = (CheckBox)target;
			RealTypeCheckBox.PreviewMouseLeftButtonUp += OnRealTypeCheckChanged;
			break;
		case 4:
			((TextBlock)target).MouseLeftButtonUp += OnRealTypeTextClick;
			break;
		case 5:
			GoalBorder = (Border)target;
			GoalBorder.MouseLeftButtonUp += OpenTimesPopup;
			break;
		case 6:
			DailyGoalText = (TextBlock)target;
			break;
		case 7:
			SetTimesPopup = (EscPopup)target;
			break;
		case 8:
			Times = (TextBox)target;
			Times.PreviewTextInput += CheckInputValid;
			Times.TextChanged += OnTimesOrStepTextChanged;
			Times.LostFocus += OnTextLostFocus;
			break;
		case 9:
			Unit = (TextBox)target;
			Unit.LostFocus += OnUnitTextLostFocus;
			break;
		case 10:
			CheckTypeComboBox = (CustomComboBox)target;
			break;
		case 11:
			AutoRecordGrid = (Grid)target;
			break;
		case 12:
			EachTimeText = (TextBlock)target;
			break;
		case 13:
			StepText = (TextBox)target;
			StepText.PreviewTextInput += CheckInputValid;
			StepText.TextChanged += OnTimesOrStepTextChanged;
			StepText.LostFocus += OnTextLostFocus;
			break;
		case 14:
			((Button)target).Click += OnSaveTimes;
			break;
		case 15:
			((Button)target).Click += OnSetTimesCancel;
			break;
		case 16:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 17:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
