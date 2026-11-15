using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo;

public class PomoModitySpanConfig : ContentControl, IComponentConnector
{
	private Popup _popup;

	private bool _needResetPomoDuraion;

	private bool _collectEvent;

	internal TextBox PomoDurationText;

	internal TextBlock DurationHintText;

	private bool _contentLoaded;

	public PomoModitySpanConfig(Popup popup, bool collectEvent = false)
	{
		InitializeComponent();
		_collectEvent = collectEvent;
		_popup = popup;
		base.Loaded += OnLoaded;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		PomoDurationText.TextChanged -= OnSettingsTextChanged;
		switch (TickFocusManager.Status)
		{
		case PomoStatus.WaitingWork:
			PomoDurationText.Tag = "PomoDuration";
			DurationHintText.Text = string.Format(Utils.GetString("DurationSpan"), "5~180");
			PomoDurationText.Text = LocalSettings.Settings.PomoDuration.ToString();
			break;
		case PomoStatus.WaitingRelax:
			if (LocalSettings.Settings.LongBreakEvery <= 1 || (TickFocusManager.Config.PomoCount >= 1 && TickFocusManager.Config.PomoCount % LocalSettings.Settings.LongBreakEvery == 0))
			{
				PomoDurationText.Tag = "LongBreakDuration";
				DurationHintText.Text = string.Format(Utils.GetString("LongBreakSpan"), "1~60");
				PomoDurationText.Text = LocalSettings.Settings.LongBreakDuration.ToString();
			}
			else
			{
				PomoDurationText.Tag = "ShortBreakDuration";
				DurationHintText.Text = string.Format(Utils.GetString("ShortBreakSpan"), "1~60");
				PomoDurationText.Text = LocalSettings.Settings.ShortBreakDuration.ToString();
			}
			break;
		}
		PomoDurationText.TextChanged += OnSettingsTextChanged;
		await Task.Delay(100);
		PomoDurationText.Focus();
		PomoDurationText.SelectAll();
	}

	private void OnTextInput(object sender, TextCompositionEventArgs e)
	{
		if (e.Text.Length >= 1 && !char.IsDigit(e.Text, e.Text.Length - 1))
		{
			e.Handled = true;
		}
	}

	private void OnSettingsKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			OnSaveClick(null, null);
		}
		if (e.Key != Key.Up && e.Key != Key.Down)
		{
			return;
		}
		string key = PomoDurationText.Tag.ToString();
		if (!PomoSettings.PomoSettingsData.ContainsKey(key))
		{
			return;
		}
		Tuple<int, int, int> tuple = PomoSettings.PomoSettingsData[key];
		PomoDurationText.TextChanged -= OnSettingsTextChanged;
		if (int.TryParse(PomoDurationText.Text, out var result))
		{
			int item = tuple.Item2;
			int item2 = tuple.Item3;
			int num = ((e.Key == Key.Up) ? (result + 1) : (result - 1));
			if (num >= item && num <= item2)
			{
				PomoDurationText.Text = num.ToString();
				PomoDurationText.SelectAll();
			}
		}
		PomoDurationText.TextChanged += OnSettingsTextChanged;
	}

	private async void OnSettingsTextChanged(object sender, TextChangedEventArgs e)
	{
		Dictionary<string, Tuple<int, int, int>> pomoSettingsData = PomoSettings.PomoSettingsData;
		string key = PomoDurationText.Tag.ToString();
		if (pomoSettingsData.ContainsKey(key))
		{
			Tuple<int, int, int> tuple = pomoSettingsData[key];
			int.TryParse(PomoDurationText.Text, out var result);
			if (result > tuple.Item3)
			{
				PomoDurationText.Text = tuple.Item3.ToString();
				PomoDurationText.SelectAll();
			}
			else if (result < tuple.Item2)
			{
				_needResetPomoDuraion = true;
				TryResetPomoDuration(tuple.Item2.ToString());
			}
			else
			{
				_needResetPomoDuraion = false;
			}
		}
	}

	private async void TryResetPomoDuration(string text)
	{
		await Task.Delay(1500);
		if (_needResetPomoDuraion)
		{
			PomoDurationText.Text = text;
			PomoDurationText.SelectAll();
		}
	}

	private void OnSaveClick(object sender, RoutedEventArgs routedEventArgs)
	{
		if (int.TryParse(PomoDurationText.Text, out var result))
		{
			Dictionary<string, Tuple<int, int, int>> pomoSettingsData = PomoSettings.PomoSettingsData;
			string text = PomoDurationText.Tag.ToString();
			if (result != (int)LocalSettings.Settings[text] && result < pomoSettingsData[text].Item2)
			{
				result = pomoSettingsData[text].Item2;
			}
			LocalSettings.Settings[PomoDurationText.Tag.ToString()] = result;
		}
		if (TickFocusManager.Status == PomoStatus.WaitingWork)
		{
			TickFocusManager.Config.SetPomoSeconds();
		}
		if (_popup != null)
		{
			_popup.IsOpen = false;
		}
		_popup = null;
		PomoSettings.SyncRemoteSettings();
		if (_collectEvent)
		{
			UserActCollectUtils.AddClickEvent("focus_mini", "om", "modify_pomo");
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs routedEventArgs)
	{
		_popup.IsOpen = false;
		_popup = null;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/pomomodityspanconfig.xaml", UriKind.Relative);
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
			PomoDurationText = (TextBox)target;
			PomoDurationText.PreviewTextInput += OnTextInput;
			PomoDurationText.KeyUp += OnSettingsKeyUp;
			PomoDurationText.TextChanged += OnSettingsTextChanged;
			break;
		case 2:
			DurationHintText = (TextBlock)target;
			break;
		case 3:
			((Button)target).Click += OnSaveClick;
			break;
		case 4:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
