using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.MarkDown;

public class EditorMenuLink : UserControl, IComponentConnector
{
	internal new ContentControl Content;

	private bool _contentLoaded;

	public event EventHandler<string> EditorAction;

	public EditorMenuLink()
	{
		InitializeComponent();
	}

	private void OnClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { Tag: string tag })
		{
			e.Handled = true;
			this.EditorAction?.Invoke(sender, tag);
		}
	}

	private void UserControl_MouseLeave(object sender, MouseEventArgs e)
	{
		this.EditorAction?.Invoke(sender, "Leave");
	}

	private void UserControl_LostFocus(object sender, RoutedEventArgs e)
	{
		this.EditorAction?.Invoke(sender, "Leave");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/markdown/editormenulink.xaml", UriKind.Relative);
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
			((EditorMenuLink)target).MouseLeave += UserControl_MouseLeave;
			((EditorMenuLink)target).LostFocus += UserControl_LostFocus;
			break;
		case 2:
			Content = (ContentControl)target;
			break;
		case 3:
			((ContentControl)target).MouseLeftButtonUp += OnClick;
			break;
		case 4:
			((ContentControl)target).MouseLeftButtonUp += OnClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
