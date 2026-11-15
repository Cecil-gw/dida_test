using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Widget;

public class WidgetParentWindow : Window, IComponentConnector
{
	private bool _topMost;

	private bool _contentLoaded;

	public WidgetParentWindow(string modelOption)
	{
		_topMost = modelOption == "top";
		InitializeComponent();
		base.SourceInitialized += OnWindowSourceInitialized;
		base.Title = Utils.GetAppName();
	}

	private void OnWindowSourceInitialized(object sender, EventArgs e)
	{
		SetWindowTopStyle();
	}

	private async void SetWindowTopStyle()
	{
		base.Topmost = true;
		Activate();
		IntPtr handle = new WindowInteropHelper(this).Handle;
		int windowLong = NativeUtils.GetWindowLong(handle, -16);
		NativeUtils.SetWindowLong(handle, WinParameter.GWL_STYLE, windowLong & -65537 & -131073);
		if (!_topMost)
		{
			NativeUtils.SetWindowLong(handle, WinParameter.GWL_HWNDPARENT, NativeUtils.GetDesktopPtr().ToInt32());
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/widgetparentwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		_contentLoaded = true;
	}
}
