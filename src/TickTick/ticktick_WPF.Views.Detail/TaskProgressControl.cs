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

namespace ticktick_WPF.Views.Detail;

public class TaskProgressControl : UserControl, IComponentConnector
{
	private TaskProgress _progress;

	internal Grid Container;

	private bool _contentLoaded;

	public event TaskProgressDelegate ProgressChanged;

	public TaskProgressControl()
	{
		InitializeComponent();
		InitViewModel();
	}

	private void InitViewModel()
	{
		_progress = new TaskProgress
		{
			Percent = 0,
			ShowPointer = false,
			Width = base.ActualWidth
		};
		Container.DataContext = _progress;
	}

	public async Task SetProgress(int percent, bool withAnim = false)
	{
		if (!withAnim)
		{
			_progress.Percent = percent;
			return;
		}
		int diff = percent - _progress.Percent;
		if (diff == 0)
		{
			return;
		}
		int step = Math.Abs(diff) / 20 + 1;
		for (int i = 0; i < 100; i++)
		{
			await Task.Delay(1);
			int num = _progress.Percent + ((diff > 0) ? step : (step * -1));
			if (diff > 0)
			{
				if (num >= percent)
				{
					_progress.Percent = percent;
					break;
				}
				_progress.Percent = num;
			}
			else if (diff < 0)
			{
				if (num <= percent)
				{
					_progress.Percent = percent;
					break;
				}
				_progress.Percent = num;
			}
		}
	}

	public void SetPointerProgress(int percent)
	{
		_progress.PreviewPercent = percent;
	}

	public void ShowPointer()
	{
		_progress.ShowPointer = true;
	}

	public void HidePointer()
	{
		_progress.ShowPointer = false;
	}

	public int GetProgress()
	{
		return _progress.Percent;
	}

	public int GetPointerProgress()
	{
		return _progress.PreviewPercent;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		_progress.Width = e.NewSize.Width;
	}

	private async void ProgressPreviewMouseMove(object sender, MouseEventArgs e)
	{
		int num = (int)(e.GetPosition(this).X / base.ActualWidth * 100.0);
		int num2 = -1;
		if ((double)Math.Abs(GetProgress() - num) < 2.001)
		{
			num2 = GetProgress();
		}
		else
		{
			int num3 = num % 10;
			if (num3 >= 0 && num3 <= 5)
			{
				num2 = num - num3;
			}
			else if (num3 >= 6 && num3 <= 9)
			{
				num2 = num + (10 - num3);
			}
		}
		if (num2 != -1)
		{
			await TryShowProgressPointer(num2);
		}
		else
		{
			HidePointer();
		}
	}

	private async Task TryShowProgressPointer(int previewPercent)
	{
		SetPointerProgress(previewPercent);
		DelayActionHandlerCenter.TryDoAction("DetailShowProgressPointer", delegate
		{
			ShowPointer();
		}, 200);
	}

	private void ProgressMouseLeave(object sender, MouseEventArgs e)
	{
		DelayActionHandlerCenter.RemoveAction("DetailShowProgressPointer");
		HidePointer();
	}

	private void ProgressClick(object sender, MouseButtonEventArgs e)
	{
		int pointerProgress = GetPointerProgress();
		int progress = GetProgress();
		this.ProgressChanged?.Invoke(pointerProgress, progress);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/taskprogresscontrol.xaml", UriKind.Relative);
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
			((TaskProgressControl)target).SizeChanged += OnSizeChanged;
			break;
		case 2:
			Container = (Grid)target;
			break;
		case 3:
			((Border)target).PreviewMouseMove += ProgressPreviewMouseMove;
			((Border)target).MouseLeave += ProgressMouseLeave;
			((Border)target).MouseLeftButtonDown += ProgressClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
