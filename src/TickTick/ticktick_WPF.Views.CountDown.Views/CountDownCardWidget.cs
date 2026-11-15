using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.CountDown.Views;

public class CountDownCardWidget : UserControl, IComponentConnector
{
	internal ContentControl TemplateControl;

	private bool _contentLoaded;

	private double _lastWidth { get; set; }

	private double _lastHeight { get; set; }

	private CountDownViewModel viewModel { get; set; }

	public CountDownCardWidget()
	{
		InitializeComponent();
	}

	private double GetTargetHeight()
	{
		if (viewModel != null)
		{
			if (viewModel.DayNumLength < 4)
			{
				return base.ActualHeight;
			}
			if (viewModel.DayNumLength < 6)
			{
				return Math.Min(base.ActualHeight, base.ActualWidth / 8.0 * 7.0);
			}
			return Math.Min(base.ActualHeight, base.ActualWidth / 9.0 * 7.0);
		}
		return base.ActualHeight;
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		_lastWidth = base.ActualWidth;
		_lastHeight = GetTargetHeight();
	}

	private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		viewModel = base.DataContext as CountDownViewModel;
		UpdateRadioScale();
	}

	private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateRadioScale(checkHeightChange: true);
	}

	private void UpdateRadioScale(bool checkHeightChange = false)
	{
		double targetHeight = GetTargetHeight();
		if (checkHeightChange && (!(targetHeight > 0.0) || (!(Math.Abs(targetHeight - _lastHeight) > 1.0) && !(Math.Abs(base.ActualWidth - _lastWidth) > 1.0))))
		{
			return;
		}
		_lastHeight = targetHeight;
		_lastWidth = base.ActualWidth;
		if (viewModel == null || viewModel.IgnoreRatio)
		{
			return;
		}
		double num = targetHeight / 191.0;
		if (base.ActualWidth / base.ActualHeight > 1.1666666666666667)
		{
			double num2 = 310.0;
			Size? maxSize = WidgetConstants.GetMaxSize(WidgetType.CountdownItem);
			if (maxSize.HasValue)
			{
				num2 = maxSize.Value.Width;
			}
			double num3 = base.ActualHeight * 7.0 / 6.0;
			if (num3 != num2)
			{
				double num4 = 1.0 + (base.ActualWidth - num3) / (num2 - num3) * 0.2;
				num *= num4;
				viewModel.ScaleRatioExtra = num4;
			}
		}
		viewModel.ScaleRatio = num;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/countdowncardwidget.xaml", UriKind.Relative);
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
			((CountDownCardWidget)target).SizeChanged += UserControl_SizeChanged;
			((CountDownCardWidget)target).Loaded += UserControl_Loaded;
			((CountDownCardWidget)target).DataContextChanged += UserControl_DataContextChanged;
			break;
		case 2:
			TemplateControl = (ContentControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
