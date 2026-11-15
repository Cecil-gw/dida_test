using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views.Setting;

public class MoreSettingsConfig : UserControl, IComponentConnector
{
	internal SmoothScrollViewer ScrollViewer;

	internal Border ContainerBorder;

	internal NewTaskDefault NewTaskDefault;

	internal LockSettings LockSettings;

	internal TextBlock SetPasswordButton;

	internal TemplateControl TemplateControl;

	private bool _contentLoaded;

	public MoreSettingsConfig()
	{
		InitializeComponent();
		TemplateControl.SetSettingMode();
		TemplateControl.Init(TemplateKind.Task);
		base.Loaded += delegate
		{
			ScrollViewer.ScrollToTop();
		};
	}

	public MoreSettingsConfig(TemplateKind kind, AddTaskViewModel addModel)
	{
		InitializeComponent();
		TemplateControl.SetSettingMode();
		TemplateControl.Init(kind, addModel);
	}

	public void ResetTaskDefault()
	{
		NewTaskDefault.InitData();
	}

	private void ProxyManageClick(object sender, MouseButtonEventArgs e)
	{
		ProxyWindow proxyWindow = new ProxyWindow();
		proxyWindow.Owner = Window.GetWindow(this);
		proxyWindow.ShowDialog();
	}

	public async Task DelayScrollToTemplate()
	{
		await Task.Delay(200);
		Point point = TemplateControl.TranslatePoint(new Point(0.0, 0.0), ContainerBorder);
		if (point.Y > 0.0)
		{
			ScrollViewer.ScrollToVerticalOffset(point.Y);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/setting/moresettingsconfig.xaml", UriKind.Relative);
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
			ScrollViewer = (SmoothScrollViewer)target;
			break;
		case 2:
			ContainerBorder = (Border)target;
			break;
		case 3:
			NewTaskDefault = (NewTaskDefault)target;
			break;
		case 4:
			LockSettings = (LockSettings)target;
			break;
		case 5:
			SetPasswordButton = (TextBlock)target;
			SetPasswordButton.MouseLeftButtonUp += ProxyManageClick;
			break;
		case 6:
			TemplateControl = (TemplateControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
