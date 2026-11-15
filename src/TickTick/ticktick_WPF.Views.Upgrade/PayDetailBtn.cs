using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Upgrade;

public class PayDetailBtn : UserControl, IComponentConnector
{
	public static readonly DependencyProperty PriceProperty = DependencyProperty.Register("Price", typeof(string), typeof(PayDetailBtn));

	public static readonly DependencyProperty UnitProperty = DependencyProperty.Register("Unit", typeof(string), typeof(PayDetailBtn));

	public static readonly DependencyProperty CurrencyProperty = DependencyProperty.Register("Currency", typeof(string), typeof(PayDetailBtn));

	public static readonly DependencyProperty AdTextProperty = DependencyProperty.Register("AdText", typeof(string), typeof(PayDetailBtn));

	public static readonly DependencyProperty AdText2Property = DependencyProperty.Register("AdText2", typeof(string), typeof(PayDetailBtn));

	public static readonly DependencyProperty AdText3Property = DependencyProperty.Register("AdText3", typeof(string), typeof(PayDetailBtn));

	public static readonly DependencyProperty ClickableProperty = DependencyProperty.Register("Selected", typeof(bool), typeof(PayDetailBtn));

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

	public string AdText
	{
		get
		{
			return (string)GetValue(AdTextProperty);
		}
		set
		{
			SetValue(AdTextProperty, value);
		}
	}

	public string AdText2
	{
		get
		{
			return (string)GetValue(AdText2Property);
		}
		set
		{
			SetValue(AdText2Property, value);
		}
	}

	public string AdText3
	{
		get
		{
			return (string)GetValue(AdText3Property);
		}
		set
		{
			SetValue(AdText3Property, value);
		}
	}

	public string Currency
	{
		get
		{
			return (string)GetValue(CurrencyProperty);
		}
		set
		{
			SetValue(CurrencyProperty, value);
		}
	}

	public string Unit
	{
		get
		{
			return (string)GetValue(UnitProperty);
		}
		set
		{
			SetValue(UnitProperty, value);
		}
	}

	public string Price
	{
		get
		{
			return (string)GetValue(PriceProperty);
		}
		set
		{
			SetValue(PriceProperty, value);
		}
	}

	public PayDetailBtn()
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
			Uri resourceLocator = new Uri("/TickTick;component/views/upgrade/paydetailbtn.xaml", UriKind.Relative);
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
