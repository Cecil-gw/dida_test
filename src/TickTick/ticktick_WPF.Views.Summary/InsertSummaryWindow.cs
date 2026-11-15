using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Summary;

public class InsertSummaryWindow : MyWindow, IComponentConnector
{
	internal SummaryControl SummaryControl;

	private bool _contentLoaded;

	public event EventHandler<string> InsertSummary;

	public InsertSummaryWindow()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		SummaryControl.SetInsertMode();
		SummaryControl.LoadData();
		SummaryControl.InsertSummary += OnInsertSummary;
	}

	private void OnInsertSummary(object sender, string e)
	{
		this.InsertSummary?.Invoke(this, e + "\r\n");
		Close();
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/summary/insertsummarywindow.xaml", UriKind.Relative);
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
			((Border)target).MouseLeftButtonUp += OnCloseClick;
			break;
		case 2:
			SummaryControl = (SummaryControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
