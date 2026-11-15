using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace ticktick_WPF.Views.QuickAdd;

public class AssigneeTokenControl : UserControl, IComponentConnector
{
	internal Button TokenText;

	private bool _contentLoaded;

	public event EventHandler TokenClick;

	public AssigneeTokenControl()
	{
		InitializeComponent();
	}

	private void OnTokenClick(object sender, RoutedEventArgs routedEventArgs)
	{
		this.TokenClick?.Invoke(sender, null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/quickadd/assigneetokencontrol.xaml", UriKind.Relative);
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
			((AssigneeTokenControl)target).PreviewMouseLeftButtonUp += OnTokenClick;
			break;
		case 2:
			TokenText = (Button)target;
			TokenText.Click += OnTokenClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
