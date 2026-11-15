using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo;

public class TaskItemPomoSummaryControl : UserControl, IComponentConnector
{
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

	public TaskItemPomoSummaryControl()
	{
		InitializeComponent();
	}

	public void SetData(PomodoroSummaryModel pomoSummary)
	{
		if (pomoSummary == null)
		{
			base.Visibility = Visibility.Collapsed;
			return;
		}
		base.Visibility = Visibility.Visible;
		(int, long) focusSummary = pomoSummary.GetFocusSummary();
		int item = focusSummary.Item1;
		long item2 = focusSummary.Item2;
		bool flag = pomoSummary.estimatedPomo > 0;
		bool flag2 = pomoSummary.estimatedPomo <= 0 && pomoSummary.EstimatedDuration > 0;
		PomoCountText.Visibility = ((!(item > 0 || flag)) ? Visibility.Collapsed : Visibility.Visible);
		if (PomoCountText.Visibility == Visibility.Visible)
		{
			PomoCountRun.Text = item.ToString() ?? "";
			PomoSplitRun.Text = ((pomoSummary.estimatedPomo > 0) ? "/" : "");
			EstimatePomoRun.Text = ((pomoSummary.estimatedPomo > 0) ? (pomoSummary.estimatedPomo.ToString() ?? "") : "");
			TimerImage.Margin = new Thickness(0.0, 1.0, 2.0, 2.0);
		}
		else
		{
			TimerImage.Margin = new Thickness(2.0, 1.0, 2.0, 2.0);
		}
		DurationText.Visibility = ((!(item2 >= 30 || flag2)) ? Visibility.Collapsed : Visibility.Visible);
		if (DurationText.Visibility == Visibility.Visible)
		{
			DurationRun.Text = Utils.GetDurationString(item2, isShort: true, flag2) ?? "";
			DurationSplitRun.Text = (flag2 ? "/" : "");
			EstimateDurationRun.Text = (flag2 ? Utils.GetDurationString(pomoSummary.EstimatedDuration, isShort: true) : "");
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/taskitempomosummarycontrol.xaml", UriKind.Relative);
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
			PomoImage = (Path)target;
			break;
		case 2:
			PomoCountText = (TextBlock)target;
			break;
		case 3:
			PomoCountRun = (Run)target;
			break;
		case 4:
			PomoSplitRun = (Run)target;
			break;
		case 5:
			EstimatePomoRun = (Run)target;
			break;
		case 6:
			TimerImage = (Path)target;
			break;
		case 7:
			DurationText = (TextBlock)target;
			break;
		case 8:
			DurationRun = (Run)target;
			break;
		case 9:
			DurationSplitRun = (Run)target;
			break;
		case 10:
			EstimateDurationRun = (Run)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
