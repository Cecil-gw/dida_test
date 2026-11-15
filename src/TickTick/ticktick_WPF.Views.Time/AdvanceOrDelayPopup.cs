using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Time;

public class AdvanceOrDelayPopup : UserControl, IComponentConnector
{
	private readonly Popup _popup;

	private readonly bool _canPostPoneTime;

	internal Border ContainerBorder;

	internal Grid Container;

	internal CustomComboBox TypeComboBox;

	internal TextBox NumTextBox;

	internal CustomComboBox UnitComboBox;

	internal TextBlock DescTextBlock;

	internal Button CancelButton;

	internal Button SaveButton;

	private bool _contentLoaded;

	public event EventHandler<long> DelayOrAdvanced;

	public AdvanceOrDelayPopup(Popup popup, bool canPostPoneTime)
	{
		InitializeComponent();
		_canPostPoneTime = canPostPoneTime;
		_popup = popup;
		_popup.Child = this;
		InitTypeComboBox();
		InitUnitComboBox();
		TypeComboBox.BackBorder.CornerRadius = new CornerRadius(4.0);
		UnitComboBox.BackBorder.CornerRadius = new CornerRadius(4.0);
		string format = Utils.GetString("PostponeTaskDesc");
		format = string.Format(format, Utils.GetString("Delay"), 1 + " " + Utils.GetString("PublicUpDay").ToLower());
		format = format.Substring(0, 1).ToUpper() + format.Substring(1);
		DescTextBlock.Text = format;
		ContainerBorder.Width = (Utils.IsCn() ? 200 : 220);
		Container.Width = (Utils.IsCn() ? 168 : 188);
		base.MouseLeftButtonUp += OnLeftClick;
		base.Loaded += async delegate
		{
			Keyboard.Focus(null);
			await Task.Delay(150);
			Keyboard.Focus(NumTextBox);
			NumTextBox.Focus();
			NumTextBox.SelectAll();
			NumTextBox.TextChanged += OnTextChanged;
		};
	}

	private void OnLeftClick(object sender, MouseButtonEventArgs e)
	{
		if (!NumTextBox.IsMouseOver)
		{
			Keyboard.Focus(null);
		}
	}

	private void InitUnitComboBox()
	{
		string text = Utils.GetString("PublicUpDay");
		string text2 = Utils.GetString("Week");
		ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel(text, text, 32.0)
			{
				Selected = true
			},
			new ComboBoxViewModel(text2, text2, 32.0)
		};
		if (_canPostPoneTime)
		{
			string text3 = Utils.GetString("UpMinute");
			string text4 = Utils.GetString("UpHour");
			observableCollection.Insert(0, new ComboBoxViewModel(text4, text4, 32.0));
			observableCollection.Insert(0, new ComboBoxViewModel(text3, text3, 32.0));
		}
		UnitComboBox.Init(observableCollection, null);
	}

	private void InitTypeComboBox()
	{
		string text = Utils.GetString("Delay");
		string text2 = Utils.GetString("Advance");
		ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel(text, text, 32.0)
			{
				Selected = true
			},
			new ComboBoxViewModel(text2, text2, 32.0)
		};
		TypeComboBox.Init(observableCollection, observableCollection[0]);
	}

	private void CancelClick(object sender, RoutedEventArgs e)
	{
		_popup.IsOpen = false;
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		_popup.IsOpen = false;
		int.TryParse(NumTextBox.Text, out var result);
		int num = ((UnitComboBox.SelectedIndex == 0 && _canPostPoneTime) ? result : ((UnitComboBox.SelectedIndex == 1 && _canPostPoneTime) ? (result * 60) : (((UnitComboBox.SelectedIndex == 2 && _canPostPoneTime) || (!_canPostPoneTime && UnitComboBox.SelectedIndex == 0)) ? (result * 24 * 60) : (result * 24 * 60 * 7))));
		this.DelayOrAdvanced?.Invoke(this, ((TypeComboBox.SelectedIndex == 0) ? 1 : (-1)) * num);
	}

	private void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (sender is TextBox textBox)
		{
			if (int.TryParse(textBox.Text, out var result))
			{
				if (result == 0)
				{
					textBox.Text = "1";
					textBox.SelectAll();
				}
				if (UnitComboBox.SelectedIndex == 0 && _canPostPoneTime && result >= 59)
				{
					textBox.Text = "59";
					textBox.SelectAll();
				}
				if (UnitComboBox.SelectedIndex == 1 && _canPostPoneTime && result >= 23)
				{
					textBox.Text = "23";
					textBox.SelectAll();
				}
				if (((UnitComboBox.SelectedIndex == 2 && _canPostPoneTime) || (!_canPostPoneTime && UnitComboBox.SelectedIndex == 0)) && result >= 30)
				{
					textBox.Text = "30";
					textBox.SelectAll();
				}
				if (((UnitComboBox.SelectedIndex == 3 && _canPostPoneTime) || (!_canPostPoneTime && UnitComboBox.SelectedIndex == 1)) && result >= 8)
				{
					textBox.Text = "8";
					textBox.SelectAll();
				}
			}
			else
			{
				textBox.Text = "1";
				textBox.SelectAll();
			}
		}
		SetPostponeMessage();
	}

	private void SetPostponeMessage()
	{
		if (!int.TryParse(NumTextBox.Text, out var result))
		{
			return;
		}
		string text = result.ToString() ?? "";
		switch (UnitComboBox.SelectedIndex)
		{
		case 0:
			if (_canPostPoneTime)
			{
				text = text + " " + Utils.GetString((result > 1) ? "Minutes" : "Minute");
				break;
			}
			if (_canPostPoneTime)
			{
				break;
			}
			goto IL_00be;
		case 1:
			if (_canPostPoneTime)
			{
				text = text + " " + Utils.GetString((result > 1) ? "Hours" : "UpHour");
				break;
			}
			if (_canPostPoneTime)
			{
				break;
			}
			goto IL_00f4;
		case 2:
			if (!_canPostPoneTime)
			{
				break;
			}
			goto IL_00be;
		case 3:
			{
				if (!_canPostPoneTime)
				{
					break;
				}
				goto IL_00f4;
			}
			IL_00f4:
			text = text + " " + Utils.GetString((result > 1) ? "PublicWeeks" : "PublicWeek");
			break;
			IL_00be:
			text = text + " " + Utils.GetString((result > 1) ? "PublicDays" : "PublicUpDay");
			break;
		}
		string arg = TypeComboBox.SelectedItem.Value.ToString().ToLower();
		string format = Utils.GetString("PostponeTaskDesc");
		format = string.Format(format, arg, text.ToLower());
		format = format.Substring(0, 1).ToUpper() + format.Substring(1);
		DescTextBlock.Text = format;
	}

	private void TextBox_LostFocus(object sender, RoutedEventArgs e)
	{
		if (sender is TextBox { Text: "" } textBox)
		{
			textBox.Text = "1";
		}
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

	private void OnComboBoxSelectedChanged(object sender, ComboBoxViewModel e)
	{
		if (int.TryParse(NumTextBox.Text, out var result))
		{
			if (UnitComboBox.SelectedIndex == 0 && _canPostPoneTime && result >= 59)
			{
				NumTextBox.Text = "59";
				NumTextBox.SelectAll();
			}
			if (UnitComboBox.SelectedIndex == 1 && _canPostPoneTime && result >= 23)
			{
				NumTextBox.Text = "23";
				NumTextBox.SelectAll();
			}
			if (((UnitComboBox.SelectedIndex == 2 && _canPostPoneTime) || (!_canPostPoneTime && UnitComboBox.SelectedIndex == 0)) && result >= 30)
			{
				NumTextBox.Text = "30";
				NumTextBox.SelectAll();
			}
			if (((UnitComboBox.SelectedIndex == 3 && _canPostPoneTime) || (!_canPostPoneTime && UnitComboBox.SelectedIndex == 1)) && result >= 8)
			{
				NumTextBox.Text = "8";
				NumTextBox.SelectAll();
			}
		}
		SetPostponeMessage();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/advanceordelaypopup.xaml", UriKind.Relative);
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
			ContainerBorder = (Border)target;
			break;
		case 2:
			Container = (Grid)target;
			break;
		case 3:
			TypeComboBox = (CustomComboBox)target;
			break;
		case 4:
			NumTextBox = (TextBox)target;
			NumTextBox.LostFocus += TextBox_LostFocus;
			NumTextBox.PreviewKeyDown += OnlyNumberInput;
			break;
		case 5:
			UnitComboBox = (CustomComboBox)target;
			break;
		case 6:
			DescTextBlock = (TextBlock)target;
			break;
		case 7:
			CancelButton = (Button)target;
			CancelButton.Click += CancelClick;
			break;
		case 8:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
