using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo;

public class EnsureTimingDialog : MyWindow, IComponentConnector
{
	private long _time;

	private long _hour;

	private long _minute;

	private long _second;

	internal StackPanel EstimatePanel;

	internal TextBox HourCount;

	internal TextBox MinuteCount;

	internal TextBlock DescText;

	internal Button CancelButton;

	internal Button SaveButton;

	private bool _contentLoaded;

	private int MinFocusMinutes => Constants.MinFocusDuration / 60;

	private bool CanCancel => _second < 43200;

	public event EventHandler<long> OnSaveClick;

	public EnsureTimingDialog(long second = 0L)
	{
		_second = second;
		_time = Utils.SecondToMinute(second);
		_hour = _time / 60;
		_minute = _time % 60;
		InitializeComponent();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		HourCount.Text = _hour.ToString() ?? "";
		MinuteCount.Text = _minute.ToString() ?? "";
		if (!CanCancel)
		{
			DescText.Text = Utils.GetString("EnsureTimingText2");
			CancelButton.Visibility = Visibility.Collapsed;
			Grid.SetColumn(SaveButton, 0);
			Grid.SetColumnSpan(SaveButton, 3);
		}
	}

	private void OnTextPreviewInput(object sender, TextCompositionEventArgs e)
	{
		if (sender != null && sender is TextBox textBox)
		{
			long result;
			bool flag = long.TryParse(textBox.Text.Insert(textBox.SelectionStart, e.Text), out result);
			e.Handled = !flag;
		}
	}

	private void OnTextKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			TrySaveAndClose();
		}
	}

	private void TrySaveAndClose()
	{
		this.OnSaveClick?.Invoke(this, (_hour * 60 + _minute == _time) ? _second : (_hour * 3600 + _minute * 60));
		Close();
	}

	private void OnSaveButtonClick(object sender, RoutedEventArgs e)
	{
		TrySaveAndClose();
	}

	private void OnCancelButtonClick(object sender, RoutedEventArgs e)
	{
		if (CanCancel)
		{
			Close();
		}
	}

	private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape && CanCancel)
		{
			e.Handled = true;
			Close();
		}
	}

	private async void OnHourTextChanged(object sender, TextChangedEventArgs e)
	{
		await Task.Delay(10);
		int.TryParse(HourCount.Text, out var result);
		if (result >= 12)
		{
			HourCount.Text = 12.ToString();
			MinuteCount.Text = "0";
			HourCount.SelectAll();
			_hour = 12L;
			_minute = 0L;
		}
		else if (result <= 0)
		{
			HourCount.Text = "0";
			HourCount.SelectAll();
			_hour = 0L;
			if (_minute < MinFocusMinutes)
			{
				MinuteCount.Text = MinFocusMinutes.ToString();
				_minute = MinFocusMinutes;
			}
		}
		else
		{
			_hour = result;
		}
		CheckTimeNum();
	}

	private void CheckTimeNum()
	{
		if (_hour * 60 + _minute > _time)
		{
			DescText.Text = string.Format(Utils.GetString("EnsureTimingText3"), Utils.GetDurationString(_time * 60));
			SaveButton.IsEnabled = false;
			return;
		}
		if (_time < 720)
		{
			DescText.Text = Utils.GetString("EnsureTimingText1");
		}
		SaveButton.IsEnabled = true;
	}

	private async void OnMinuteTextChanged(object sender, TextChangedEventArgs e)
	{
		await Task.Delay(10);
		int.TryParse(MinuteCount.Text, out var result);
		if (result > 59)
		{
			MinuteCount.Text = "59";
			MinuteCount.SelectAll();
			_minute = result;
		}
		else if (_hour == 0L && result < MinFocusMinutes)
		{
			await Task.Delay(1000);
			int.TryParse(MinuteCount.Text, out var result2);
			if (result2 < MinFocusMinutes)
			{
				MinuteCount.Text = MinFocusMinutes.ToString();
				MinuteCount.SelectAll();
				_minute = MinFocusMinutes;
			}
		}
		else if (result < 0)
		{
			MinuteCount.Text = "0";
			MinuteCount.SelectAll();
			_minute = 0L;
		}
		else
		{
			_minute = result;
		}
		CheckTimeNum();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/ensuretimingdialog.xaml", UriKind.Relative);
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
			EstimatePanel = (StackPanel)target;
			break;
		case 2:
			HourCount = (TextBox)target;
			HourCount.PreviewTextInput += OnTextPreviewInput;
			HourCount.TextChanged += OnHourTextChanged;
			HourCount.KeyDown += OnTextKeyDown;
			break;
		case 3:
			MinuteCount = (TextBox)target;
			MinuteCount.PreviewTextInput += OnTextPreviewInput;
			MinuteCount.TextChanged += OnMinuteTextChanged;
			MinuteCount.KeyDown += OnTextKeyDown;
			break;
		case 4:
			DescText = (TextBlock)target;
			break;
		case 5:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancelButtonClick;
			break;
		case 6:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveButtonClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
