using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.CustomPopup;

public class TaskStatusProgressControl : UserControl, IComponentConnector
{
	internal TextBlock PercentText;

	internal Slider ProgressSlider;

	private bool _contentLoaded;

	public event Action<int> ProgressChangeRequested;

	public TaskStatusProgressControl(int progress)
	{
		InitializeComponent();
		SetProgress(progress);
	}

	public void SetProgress(int progress)
	{
		int num = Math.Max(0, Math.Min(100, progress));
		ProgressSlider.Value = num;
		PercentText.Text = num + "%";
	}

	public void SetProgressEnabled(bool enabled)
	{
		ProgressSlider.IsEnabled = enabled;
	}

	private void OnProgressValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		PercentText.Text = (int)Math.Round(e.NewValue) + "%";
	}

	private void OnProgressSliderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		this.ProgressChangeRequested?.Invoke((int)Math.Round(ProgressSlider.Value));
	}

	private void OnMouseLeftButton(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/custompopup/taskstatusprogresscontrol.xaml", UriKind.Relative);
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
			((TaskStatusProgressControl)target).MouseLeftButtonDown += OnMouseLeftButton;
			((TaskStatusProgressControl)target).MouseLeftButtonUp += OnMouseLeftButton;
			break;
		case 2:
			PercentText = (TextBlock)target;
			break;
		case 3:
			ProgressSlider = (Slider)target;
			ProgressSlider.ValueChanged += OnProgressValueChanged;
			ProgressSlider.PreviewMouseLeftButtonUp += OnProgressSliderMouseLeftButtonUp;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
