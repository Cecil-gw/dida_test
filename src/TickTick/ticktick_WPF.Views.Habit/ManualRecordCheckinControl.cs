using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Habit;

public class ManualRecordCheckinControl : UserControl, IComponentConnector
{
	internal TextBlock DateText;

	internal TextBlock HintText;

	internal TextBlock LeftLabel;

	internal TextBox CheckInText;

	internal TextBlock UnitText;

	private bool _contentLoaded;

	public event EventHandler<double> Save;

	public event EventHandler Cancel;

	public ManualRecordCheckinControl()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		CheckInText.SelectAll();
		CheckInText.Focus();
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		SaveAmount(sender, e);
	}

	private void SaveAmount(object sender, EventArgs e)
	{
		if (MathUtil.TryParseString2Double(CheckInText.Text, out var db))
		{
			if (0.0 < db && db <= 99999.99)
			{
				this.Save?.Invoke(sender, db);
			}
			else if (HintText.Visibility == Visibility.Visible && db == 0.0)
			{
				this.Save?.Invoke(sender, db);
			}
		}
		else
		{
			this.Cancel?.Invoke(sender, e);
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		this.Cancel?.Invoke(sender, e);
	}

	public async void Init(string habitUnit)
	{
		UnitText.Text = HabitUtils.GetUnitText(habitUnit);
		CheckInText.Text = "0";
		await Task.Delay(50);
		CheckInText.SelectAll();
		CheckInText.Focus();
	}

	public async void Init(double value, string habitUnit, bool isToday)
	{
		UnitText.Text = HabitUtils.GetUnitText(habitUnit);
		CheckInText.Text = value.ToString(CultureInfo.InvariantCulture);
		HintText.Text = string.Format(Utils.GetString("CompleteHabitCount"), Utils.GetString(isToday ? "Today" : "ThisDay"));
		HintText.Visibility = Visibility.Visible;
		await Task.Delay(50);
		CheckInText.SelectAll();
		CheckInText.Focus();
	}

	private void OnTextPreviewInput(object sender, TextCompositionEventArgs e)
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

	private void OnInputKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			SaveAmount(sender, e);
		}
		else if (e.Key == Key.Escape)
		{
			this.Cancel?.Invoke(sender, e);
		}
	}

	private void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!(sender is TextBox textBox))
		{
			return;
		}
		bool flag = false;
		string text = textBox.Text;
		int caretIndex = textBox.CaretIndex;
		int num = 0;
		if (text.Contains(" "))
		{
			string text2 = text;
			for (int i = 0; i < text2.Length; i++)
			{
				if (text2[i] == ' ')
				{
					num++;
				}
			}
			text = text.Replace(" ", "");
		}
		if (!MathUtil.TryParseString2Double(text, out var db) || Math.Abs(db) < 0.0)
		{
			text = "0";
			flag = true;
		}
		if (textBox.Text != text)
		{
			textBox.Text = text;
			if (flag)
			{
				textBox.SelectAll();
			}
			else
			{
				textBox.CaretIndex = Math.Min(caretIndex - num, text.Length);
			}
		}
	}

	public void ClearSaveHandler()
	{
		if (this.Save == null)
		{
			return;
		}
		Delegate[] invocationList = this.Save.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			if (invocationList[i] is EventHandler<double> value)
			{
				Save -= value;
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/manualrecordcheckincontrol.xaml", UriKind.Relative);
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
			DateText = (TextBlock)target;
			break;
		case 2:
			HintText = (TextBlock)target;
			break;
		case 3:
			LeftLabel = (TextBlock)target;
			break;
		case 4:
			CheckInText = (TextBox)target;
			CheckInText.PreviewTextInput += OnTextPreviewInput;
			CheckInText.KeyUp += OnInputKeyUp;
			CheckInText.TextChanged += OnTextChanged;
			break;
		case 5:
			UnitText = (TextBlock)target;
			break;
		case 6:
			((Button)target).Click += OnSaveClick;
			break;
		case 7:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
