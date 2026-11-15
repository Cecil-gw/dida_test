using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views;

public class CountSpinner : UserControl, IComponentConnector
{
	public static readonly DependencyProperty CountDependencyProperty = DependencyProperty.Register("Count", typeof(int), typeof(CountSpinner), new PropertyMetadata(0, null));

	internal CountSpinner Root;

	internal TextBox CountTextBox;

	private bool _contentLoaded;

	public int Count
	{
		get
		{
			return (int)GetValue(CountDependencyProperty);
		}
		set
		{
			SetValue(CountDependencyProperty, value);
		}
	}

	private int Min { get; } = 1;

	private int Max { get; } = 60;

	public CountSpinner()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		CountTextBox.TextChanged += OnCountTextChanged;
		base.Loaded -= OnLoaded;
	}

	private void OnUpClick(object sender, MouseButtonEventArgs e)
	{
		CountUp();
	}

	private void CountUp()
	{
		int val = Count + 1;
		Count = Math.Min(val, Max);
	}

	private void OnDownClick(object sender, MouseButtonEventArgs e)
	{
		CountDown();
	}

	private void CountDown()
	{
		int val = Count - 1;
		Count = Math.Max(val, Min);
	}

	private void OnCountTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!int.TryParse(CountTextBox.Text, out var result) || result < Min || result > Max)
		{
			CountTextBox.Text = "5";
			Count = 5;
		}
		else
		{
			Count = result;
		}
	}

	private void OnTextKeyUp(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Up:
			CountUp();
			break;
		case Key.Down:
			CountDown();
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countspinner.xaml", UriKind.Relative);
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
			Root = (CountSpinner)target;
			break;
		case 2:
			CountTextBox = (TextBox)target;
			CountTextBox.KeyUp += OnTextKeyUp;
			break;
		case 3:
			((Grid)target).MouseLeftButtonUp += OnUpClick;
			break;
		case 4:
			((Grid)target).MouseLeftButtonUp += OnDownClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
