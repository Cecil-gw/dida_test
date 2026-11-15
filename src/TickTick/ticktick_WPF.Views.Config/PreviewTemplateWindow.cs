using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Config;

public class PreviewTemplateWindow : Window, IComponentConnector, IStyleConnector
{
	private bool _contentLoaded;

	public event EventHandler<TemplateViewModel> AddTask;

	public PreviewTemplateWindow(TemplateViewModel model)
	{
		base.DataContext = model;
		InitializeComponent();
		base.InputBindings.Add(new KeyBinding(WindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
	}

	private void ApplyTemplateClick(object sender, RoutedEventArgs e)
	{
		this.AddTask?.Invoke(null, base.DataContext as TemplateViewModel);
		Close();
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnTagLoaded(object sender, RoutedEventArgs e)
	{
		if (!(sender is Border border))
		{
			if (sender is TextBlock textBlock)
			{
				textBlock.Text = TagDataHelper.GetTagDisplayName(textBlock.Text);
				SolidColorBrush colorInString = ThemeUtil.GetColorInString(ThemeUtil.IsDark() ? "#B2FFFFFF" : "#CC191919");
				textBlock.Foreground = colorInString;
			}
		}
		else
		{
			SolidColorBrush solidColorBrush = new SolidColorBrush
			{
				Color = ThemeUtil.GetColor("PrimaryColor").Color
			};
			solidColorBrush.Opacity = 0.56;
			border.Background = solidColorBrush;
		}
	}

	private void HowToModifyClick(object sender, MouseButtonEventArgs e)
	{
		CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("HowToModifyTemplate"), Utils.GetString("HowToModifyTemplateContent"), Utils.GetString("GotIt"), "");
		customerDialog.Owner = this;
		customerDialog.ShowDialog();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/previewtemplatewindow.xaml", UriKind.Relative);
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
		case 3:
			((TextBlock)target).MouseLeftButtonUp += HowToModifyClick;
			break;
		case 4:
			((Button)target).Click += ApplyTemplateClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((Border)target).Loaded += OnTagLoaded;
			break;
		case 2:
			((TextBlock)target).Loaded += OnTagLoaded;
			break;
		}
	}
}
