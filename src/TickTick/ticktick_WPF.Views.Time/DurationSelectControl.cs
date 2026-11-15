using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Time;

public class DurationSelectControl : UserControl, IComponentConnector
{
	public int MinMinutes = 5;

	internal TextBox RemindHourTextBox;

	internal TextBlock RemindHourTextBlock;

	internal TextBox RemindMinuteTextBox;

	internal TextBlock RemindMinuteTextBlock;

	internal Button CancelButton;

	internal Button SaveButton;

	internal TextBox EmptyBox;

	private bool _contentLoaded;

	public event EventHandler OnCancel;

	public event EventHandler<int> OnSave;

	public DurationSelectControl(int hour, int minute)
	{
		InitializeComponent();
		InitializeComponent();
		InitChangEvent();
		RemindHourTextBox.Text = hour.ToString();
		RemindMinuteTextBox.Text = minute.ToString();
		base.Loaded += async delegate
		{
			await Task.Delay(150);
			RemindMinuteTextBox.Focus();
			RemindMinuteTextBox.SelectAll();
		};
	}

	private void InitChangEvent()
	{
		RemindHourTextBox.TextChanged -= RemindHourTextBox_TextChanged;
		RemindHourTextBox.TextChanged += RemindHourTextBox_TextChanged;
		RemindMinuteTextBox.TextChanged -= RemindMinuteTextBox_TextChanged;
		RemindMinuteTextBox.TextChanged += RemindMinuteTextBox_TextChanged;
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		if (RemindHourTextBox.Text == "0")
		{
			if (int.TryParse(RemindMinuteTextBox.Text, out var result) && result < MinMinutes)
			{
				RemindMinuteTextBox.Text = MinMinutes.ToString();
			}
			await Task.Delay(300);
		}
		int.TryParse(RemindMinuteTextBox.Text, out var result2);
		int.TryParse(RemindHourTextBox.Text, out var result3);
		this.OnSave?.Invoke(this, result2 + result3 * 60);
	}

	private void RemindHourTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (!(RemindHourTextBox.Text == ""))
		{
			RemindHourTextBox.TextChanged -= RemindHourTextBox_TextChanged;
			int.TryParse(RemindHourTextBox.Text, out var result);
			RemindHourTextBox.Text = result.ToString();
			if (result > 23)
			{
				RemindHourTextBox.Text = "23";
				RemindHourTextBox.SelectAll();
			}
			if (result <= 0)
			{
				RemindHourTextBox.Text = "0";
				RemindHourTextBox.SelectAll();
			}
			if (RemindHourTextBox.Text == "0" && int.TryParse(RemindMinuteTextBox.Text, out var result2) && result2 < MinMinutes)
			{
				RemindMinuteTextBox.Text = result2.ToString();
			}
			RemindHourTextBlock.Text = Utils.GetString((result > 1) ? "PublicHours" : "PublicHour");
			RemindHourTextBox.TextChanged += RemindHourTextBox_TextChanged;
		}
	}

	private void RemindMinuteTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (!(RemindMinuteTextBox.Text == ""))
		{
			RemindMinuteTextBox.TextChanged -= RemindMinuteTextBox_TextChanged;
			int.TryParse(RemindMinuteTextBox.Text, out var result);
			RemindMinuteTextBox.Text = result.ToString();
			if (result > 59)
			{
				RemindMinuteTextBox.Text = "59";
				RemindMinuteTextBox.SelectAll();
			}
			if (result <= 0)
			{
				RemindMinuteTextBox.Text = "0";
				RemindMinuteTextBox.SelectAll();
			}
			RemindMinuteTextBlock.Text = Utils.GetString((result > 1) ? "PublicMinutes" : "PublicMinute");
			RemindMinuteTextBox.TextChanged += RemindMinuteTextBox_TextChanged;
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

	private void TextBox_LostFocus(object sender, RoutedEventArgs e)
	{
		if (sender is TextBox { Text: "" } textBox)
		{
			textBox.Text = "0";
		}
		if (RemindHourTextBox.Text == "0" && int.TryParse(RemindMinuteTextBox.Text, out var result) && result < MinMinutes)
		{
			RemindMinuteTextBox.Text = MinMinutes.ToString();
		}
	}

	private void CancelClick(object sender, RoutedEventArgs e)
	{
		this.OnCancel?.Invoke(this, null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/durationselectcontrol.xaml", UriKind.Relative);
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
			RemindHourTextBox = (TextBox)target;
			RemindHourTextBox.LostFocus += TextBox_LostFocus;
			RemindHourTextBox.PreviewKeyDown += OnlyNumberInput;
			break;
		case 2:
			RemindHourTextBlock = (TextBlock)target;
			break;
		case 3:
			RemindMinuteTextBox = (TextBox)target;
			RemindMinuteTextBox.LostFocus += TextBox_LostFocus;
			RemindMinuteTextBox.PreviewKeyDown += OnlyNumberInput;
			break;
		case 4:
			RemindMinuteTextBlock = (TextBlock)target;
			break;
		case 5:
			CancelButton = (Button)target;
			CancelButton.Click += CancelClick;
			break;
		case 6:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 7:
			EmptyBox = (TextBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
