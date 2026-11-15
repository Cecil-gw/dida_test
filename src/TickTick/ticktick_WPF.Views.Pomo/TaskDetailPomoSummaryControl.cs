using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo;

public class TaskDetailPomoSummaryControl : UserControl, IComponentConnector
{
	private bool _inList;

	private bool _mouseDown;

	internal TextBlock FocusedText;

	internal Path PomoImage;

	internal TextBlock PomoCountText;

	internal Run PomoCountRun;

	internal Run PomoSplitRun;

	internal Run EstimatePomoRun;

	internal Path TimerImage;

	internal TextBlock DurationText;

	internal Run DurationRun;

	internal Run DurationSplitRun;

	internal Run EstimateDurationRun;

	private bool _contentLoaded;

	public new event EventHandler<MouseButtonEventArgs> MouseUp;

	public TaskDetailPomoSummaryControl()
	{
		InitializeComponent();
		base.PreviewMouseLeftButtonDown += OnMouseDown;
		base.MouseLeftButtonUp += OnMouseUp;
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (_mouseDown)
		{
			this.MouseUp?.Invoke(this, e);
		}
		_mouseDown = false;
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDown = true;
	}

	public void SetData(PomodoroSummaryModel pomoSummary)
	{
		if (pomoSummary != null)
		{
			(int, long) focusSummary = pomoSummary.GetFocusSummary();
			int item = focusSummary.Item1;
			long item2 = focusSummary.Item2;
			bool flag = pomoSummary.estimatedPomo > 0;
			bool flag2 = pomoSummary.estimatedPomo <= 0 && pomoSummary.EstimatedDuration > 0;
			PomoCountText.Visibility = ((!(item > 0 || flag) || (_inList && flag2)) ? Visibility.Collapsed : Visibility.Visible);
			if (PomoCountText.Visibility == Visibility.Visible)
			{
				PomoCountRun.Text = item.ToString() ?? "";
				PomoSplitRun.Text = ((pomoSummary.estimatedPomo > 0) ? "/" : "");
				EstimatePomoRun.Text = ((pomoSummary.estimatedPomo > 0) ? (pomoSummary.estimatedPomo.ToString() ?? "") : "");
			}
			DurationText.Visibility = ((!(item2 >= 30 || flag2) || (_inList && flag)) ? Visibility.Collapsed : Visibility.Visible);
			if (DurationText.Visibility == Visibility.Visible)
			{
				DurationRun.Text = Utils.GetDurationString(item2, isShort: true, flag2) ?? "";
				DurationSplitRun.Text = (flag2 ? "/" : "");
				EstimateDurationRun.Text = (flag2 ? Utils.GetDurationString(pomoSummary.EstimatedDuration, isShort: true) : "");
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
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/taskdetailpomosummarycontrol.xaml", UriKind.Relative);
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
			FocusedText = (TextBlock)target;
			break;
		case 2:
			PomoImage = (Path)target;
			break;
		case 3:
			PomoCountText = (TextBlock)target;
			break;
		case 4:
			PomoCountRun = (Run)target;
			break;
		case 5:
			PomoSplitRun = (Run)target;
			break;
		case 6:
			EstimatePomoRun = (Run)target;
			break;
		case 7:
			TimerImage = (Path)target;
			break;
		case 8:
			DurationText = (TextBlock)target;
			break;
		case 9:
			DurationRun = (Run)target;
			break;
		case 10:
			DurationSplitRun = (Run)target;
			break;
		case 11:
			EstimateDurationRun = (Run)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
