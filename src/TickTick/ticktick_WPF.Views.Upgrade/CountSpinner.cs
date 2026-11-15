using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Upgrade;

public class CountSpinner : UserControl, IComponentConnector
{
	public static readonly DependencyProperty AddEnabledProperty = DependencyProperty.Register("AddEnabled", typeof(bool), typeof(CountSpinner));

	public static readonly DependencyProperty MinusEnabledProperty = DependencyProperty.Register("MinusEnabled", typeof(bool), typeof(CountSpinner));

	public static readonly DependencyProperty CountProperty = DependencyProperty.Register("Count", typeof(int), typeof(CountSpinner));

	public static readonly DependencyProperty MiniCountProperty = DependencyProperty.Register("MiniCount", typeof(int), typeof(CountSpinner));

	public static readonly DependencyProperty MaxCountProperty = DependencyProperty.Register("MaxCount", typeof(int), typeof(CountSpinner));

	private bool _contentLoaded;

	public int Count
	{
		get
		{
			return (int)GetValue(CountProperty);
		}
		set
		{
			SetValue(CountProperty, value);
			AddEnabled = value < MaxCount;
			MinusEnabled = value > MiniCount;
		}
	}

	public int MiniCount
	{
		get
		{
			return (int)GetValue(MiniCountProperty);
		}
		set
		{
			SetValue(MiniCountProperty, value);
		}
	}

	public int MaxCount
	{
		get
		{
			return (int)GetValue(MaxCountProperty);
		}
		set
		{
			SetValue(MaxCountProperty, value);
		}
	}

	public bool AddEnabled
	{
		get
		{
			return (bool)GetValue(AddEnabledProperty);
		}
		set
		{
			SetValue(AddEnabledProperty, value);
		}
	}

	public bool MinusEnabled
	{
		get
		{
			return (bool)GetValue(MinusEnabledProperty);
		}
		set
		{
			SetValue(MinusEnabledProperty, value);
		}
	}

	public event EventHandler<int> OnCountChanged;

	public CountSpinner()
	{
		InitializeComponent();
		base.DataContext = this;
	}

	private void OnAddClick(object sender, MouseButtonEventArgs e)
	{
		int num = Count + 1;
		if (num <= MaxCount)
		{
			Count = num;
			this.OnCountChanged?.Invoke(this, Count);
		}
	}

	private void OnMinusClick(object sender, MouseButtonEventArgs e)
	{
		int num = Count - 1;
		if (num >= MiniCount)
		{
			Count = num;
			this.OnCountChanged?.Invoke(this, Count);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/upgrade/countspinner.xaml", UriKind.Relative);
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
			((Border)target).PreviewMouseLeftButtonUp += OnMinusClick;
			break;
		case 2:
			((Border)target).PreviewMouseLeftButtonUp += OnAddClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
