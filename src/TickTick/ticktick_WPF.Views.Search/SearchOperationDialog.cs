using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Search;

public class SearchOperationDialog : MyWindow, IOkCancelWindow, IComponentConnector
{
	internal SearchOperationControl SearchControl;

	private bool _contentLoaded;

	public bool Showing { get; set; }

	public SearchOperationDialog()
	{
		InitializeComponent();
		base.Loaded += delegate
		{
			Showing = true;
			SearchControl.FocusInput();
		};
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		Showing = false;
		base.OnClosing(e);
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
	}

	public void OnCancel()
	{
		Close();
	}

	public void Ok()
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/search/searchoperationdialog.xaml", UriKind.Relative);
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
		if (connectionId == 1)
		{
			SearchControl = (SearchOperationControl)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
