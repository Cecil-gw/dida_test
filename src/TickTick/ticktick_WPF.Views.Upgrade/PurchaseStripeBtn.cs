using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Upgrade;

public class PurchaseStripeBtn : UserControl, IComponentConnector
{
	public static readonly DependencyProperty ClickableProperty = DependencyProperty.Register("Selected", typeof(bool), typeof(PurchaseStripeBtn));

	private bool _contentLoaded;

	public bool Selected
	{
		get
		{
			return (bool)GetValue(ClickableProperty);
		}
		set
		{
			SetValue(ClickableProperty, value);
		}
	}

	public PurchaseStripeBtn()
	{
		InitializeComponent();
		base.DataContext = this;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/upgrade/purchasestripebtn.xaml", UriKind.Relative);
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
