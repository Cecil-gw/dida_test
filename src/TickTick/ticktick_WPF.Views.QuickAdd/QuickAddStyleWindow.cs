using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.QuickAdd;

public class QuickAddStyleWindow : Window, IComponentConnector
{
	public static readonly DependencyProperty UseDetailStyleProperty = DependencyProperty.Register("UseDetailStyle", typeof(bool), typeof(QuickAddStyleWindow));

	internal Border MiniGrid;

	internal Border DetailGrid;

	private bool _contentLoaded;

	public bool UseDetailStyle
	{
		get
		{
			return (bool)GetValue(UseDetailStyleProperty);
		}
		set
		{
			SetValue(UseDetailStyleProperty, value);
		}
	}

	public QuickAddStyleWindow()
	{
		InitializeComponent();
		base.InputBindings.Add(new KeyBinding(WindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		base.DataContext = this;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async void OnMinimalClick(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("add_task", "inputbox_style", "minimalist");
		UseDetailStyle = false;
		SaveStyle(0);
	}

	private void OnDetailClick(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("add_task", "inputbox_style", "detailed");
		UseDetailStyle = true;
		SaveStyle(1);
	}

	private static async Task SaveStyle(int style)
	{
		LocalSettings.Settings.UserPreference.desktopQuickAdd = new DesktopQuickAdd
		{
			style = style,
			mtime = Utils.GetNowTimeStampInMills()
		};
		LocalSettings.Settings.UserPreference.mtime = Utils.GetNowTimeStampInMills();
		LocalSettings.Settings.Save();
		GlobalEventManager.NotifyQuickAddStyleChanged(style);
		await SettingsHelper.PushLocalPreference();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/quickadd/quickaddstylewindow.xaml", UriKind.Relative);
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
			((Grid)target).MouseLeftButtonUp += OnMinimalClick;
			break;
		case 2:
			MiniGrid = (Border)target;
			break;
		case 3:
			((Grid)target).MouseLeftButtonUp += OnDetailClick;
			break;
		case 4:
			DetailGrid = (Border)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
