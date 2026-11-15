using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class CustomSnoozePopupControl : UserControl, IComponentConnector
{
	internal NumInputTextBox HourInput;

	internal NumInputTextBox MinuteInput;

	internal TextBlock PreviewText;

	private bool _contentLoaded;

	public event EventHandler<CustomSnoozeConfirmedEventArgs> Confirmed;

	public event EventHandler Canceled;

	public CustomSnoozePopupControl()
	{
		InitializeComponent();
		Reset();
	}

	public void Reset()
	{
		HourInput.Text = "0";
		MinuteInput.Text = "5";
		UpdatePreview();
	}

	private int GetSnoozeMinutes()
	{
		int num = Math.Max(0, HourInput.Value);
		int num2 = Math.Max(0, MinuteInput.Value);
		return Math.Max(1, num * 60 + num2);
	}

	private void CustomSnoozeInputChanged(object sender, TextChangedEventArgs e)
	{
		UpdatePreview();
	}

	private void UpdatePreview()
	{
		if (PreviewText != null)
		{
			DateTime date = DateTime.Now.AddMinutes(GetSnoozeMinutes());
			string text = (Utils.IsCn() ? "" : " ");
			string arg = DateUtils.FormatMonthDay(date, DateFormatLen.Long) + text + DateUtils.FormatHourMinute(date);
			PreviewText.Text = string.Format(Utils.GetString("ReminderTime"), arg);
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		this.Canceled?.Invoke(this, EventArgs.Empty);
	}

	private void OnConfirmClick(object sender, RoutedEventArgs e)
	{
		this.Confirmed?.Invoke(this, new CustomSnoozeConfirmedEventArgs(GetSnoozeMinutes()));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/customsnoozepopupcontrol.xaml", UriKind.Relative);
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
			HourInput = (NumInputTextBox)target;
			break;
		case 2:
			MinuteInput = (NumInputTextBox)target;
			break;
		case 3:
			PreviewText = (TextBlock)target;
			break;
		case 4:
			((Button)target).Click += OnConfirmClick;
			break;
		case 5:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
