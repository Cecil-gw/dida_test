using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace ticktick_WPF.Views.Upgrade;

public class ProItemLabel : UserControl, IComponentConnector
{
	public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register("IconSize", typeof(double), typeof(ProItemLabel), new PropertyMetadata(16.0, null));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("ProText", typeof(string), typeof(ProItemLabel));

	public static readonly DependencyProperty ClickableProperty = DependencyProperty.Register("Clickable", typeof(bool), typeof(ProItemLabel));

	internal Path CheckIcon;

	internal TextBlock TitleText;

	private bool _contentLoaded;

	public string ProText
	{
		get
		{
			return (string)GetValue(TextProperty);
		}
		set
		{
			SetValue(TextProperty, value);
		}
	}

	public bool Clickable
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

	public double IconSize
	{
		get
		{
			return (double)GetValue(IconSizeProperty);
		}
		set
		{
			SetValue(IconSizeProperty, value);
		}
	}

	public event EventHandler OnClick;

	public ProItemLabel()
	{
		InitializeComponent();
		base.DataContext = this;
		base.MouseLeftButtonUp += delegate
		{
			if (Clickable)
			{
				this.OnClick?.Invoke(this, EventArgs.Empty);
			}
		};
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/upgrade/proitemlabel.xaml", UriKind.Relative);
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
			CheckIcon = (Path)target;
			break;
		case 2:
			TitleText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
