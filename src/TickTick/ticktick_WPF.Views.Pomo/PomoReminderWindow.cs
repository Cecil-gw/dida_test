using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Remind;

namespace ticktick_WPF.Views.Pomo;

public class PomoReminderWindow : MyWindow, IRemindPop, IComponentConnector
{
	private int _currentIndex;

	internal Border ContainerBorder;

	internal Image WindowIcon;

	internal TextBlock TitleText;

	internal Grid OperationPanel;

	internal Button PositiveButton;

	internal Button CloseButton;

	private bool _contentLoaded;

	public string ReminderKey => string.Empty;

	public bool IsRelax
	{
		set
		{
			if (!value)
			{
				TitleText.SetResourceReference(TextBlock.TextProperty, "TimeToFocus");
				PositiveButton.SetResourceReference(ContentControl.ContentProperty, "StartFocus");
			}
			else
			{
				TitleText.SetResourceReference(TextBlock.TextProperty, "TimeToRelax");
				PositiveButton.SetResourceReference(ContentControl.ContentProperty, "Relax");
			}
			if (PositiveButton.Content is string text)
			{
				UtilLog.Info("RelaxReminderText: " + TitleText.Text + " Button :" + text);
			}
		}
	}

	public bool IsAutomatic
	{
		set
		{
			OperationPanel.Visibility = (value ? Visibility.Collapsed : Visibility.Visible);
			if (value)
			{
				DelayCloseWindow();
			}
		}
	}

	public PomoReminderWindow()
	{
		InitializeComponent();
		WindowIcon.Source = AppIconUtils.GetIconImage();
		ReminderWindowPositionHelper.ResetToBottomRight(this);
	}

	private async void DelayCloseWindow()
	{
		await Task.Delay(3000);
		TryClose();
	}

	private void OnExitClick(object sender, RoutedEventArgs e)
	{
		FocusTimer.Reset();
		base.Visibility = Visibility.Collapsed;
	}

	private void OnStartClick(object sender, RoutedEventArgs e)
	{
		if (TickFocusManager.Status == PomoStatus.WaitingWork)
		{
			UserActCollectUtils.AddClickEvent("focus", "start_from", "reminder");
		}
		if (TickFocusManager.Status == PomoStatus.WaitingWork || TickFocusManager.Status == PomoStatus.WaitingRelax)
		{
			FocusTimer.BeginTimer();
		}
		base.Visibility = Visibility.Collapsed;
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		TryClose();
	}

	public void SetIcon()
	{
		WindowIcon.Source = AppIconUtils.GetIconImage();
	}

	public bool IsSameReminder(ReminderModel reminder)
	{
		return false;
	}

	private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (Mouse.LeftButton == MouseButtonState.Pressed)
		{
			base.LocationChanged -= OnLocationChanged;
			base.LocationChanged += OnLocationChanged;
			DragMove();
		}
	}

	private void OnLocationChanged(object sender, EventArgs e)
	{
		ReminderWindowManager.OnWindowMoved(this);
	}

	public void SetDisplayStyle(int index)
	{
		bool flag = true;
		if (_currentIndex < 0 && index == 2)
		{
			_currentIndex = index - 1;
			flag = false;
		}
		int num = ((_currentIndex >= 0) ? _currentIndex : ((index > 0) ? (index + 1) : index));
		if (ContainerBorder.RenderTransform is ScaleTransform scaleTransform)
		{
			if (flag)
			{
				DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(1.0 - 0.05 * (double)num, 1.0 - 0.05 * (double)index, 180);
				scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, doubleAnimation);
				scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, doubleAnimation);
			}
			else
			{
				scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
				scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
				scaleTransform.ScaleX = 1.0 - 0.05 * (double)index;
				scaleTransform.ScaleY = 1.0 - 0.05 * (double)index;
			}
		}
		double baseTop = ReminderWindowPositionHelper.GetBaseTop(this);
		DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(flag ? (baseTop - (double)(8 * num)) : (baseTop - (double)(8 * index) + 2.0), baseTop - (double)(8 * index), 180);
		BeginAnimation(Window.TopProperty, doubleAnimation2);
		_currentIndex = index;
		base.Opacity = 1.0;
	}

	public void TryHide()
	{
		_currentIndex = -1;
		base.Opacity = 0.0;
		if (ContainerBorder.RenderTransform is ScaleTransform scaleTransform)
		{
			scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
			scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
			scaleTransform.ScaleX = 0.9;
			scaleTransform.ScaleY = 0.9;
			BeginAnimation(Window.TopProperty, null);
			base.Top = ReminderWindowPositionHelper.GetHiddenTop(this);
		}
	}

	public void TryClose()
	{
		Close();
	}

	public void ShowWindow()
	{
		IntPtr handle = new WindowInteropHelper(this).Handle;
		base.Opacity = 1.0;
		base.Visibility = Visibility.Visible;
		if (handle != IntPtr.Zero)
		{
			base.Topmost = false;
			base.Topmost = true;
			NativeUtils.ShowWindow(handle, 4);
		}
		else
		{
			base.ShowActivated = false;
			Show();
		}
	}

	public void UpdateReminder(ReminderModel reminder)
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/pomoreminderwindow.xaml", UriKind.Relative);
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
			ContainerBorder = (Border)target;
			break;
		case 2:
			WindowIcon = (Image)target;
			break;
		case 3:
			((Border)target).PreviewMouseDown += OnPreviewMouseDown;
			break;
		case 4:
			((Border)target).MouseLeftButtonUp += OnCloseClick;
			break;
		case 5:
			TitleText = (TextBlock)target;
			break;
		case 6:
			OperationPanel = (Grid)target;
			break;
		case 7:
			PositiveButton = (Button)target;
			PositiveButton.Click += OnStartClick;
			break;
		case 8:
			CloseButton = (Button)target;
			CloseButton.Click += OnExitClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
