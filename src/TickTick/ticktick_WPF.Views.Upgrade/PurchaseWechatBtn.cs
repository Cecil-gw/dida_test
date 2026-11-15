using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Upgrade;

public class PurchaseWechatBtn : UserControl, IComponentConnector
{
	public static readonly DependencyProperty ClickableProperty = DependencyProperty.Register("Selected", typeof(bool), typeof(PurchaseWechatBtn));

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

	public PurchaseWechatBtn()
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
			Uri resourceLocator = new Uri("/TickTick;component/views/upgrade/purchasewechatbtn.xaml", UriKind.Relative);
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
