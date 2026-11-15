using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Misc;

public class NumInputTextBox : Grid, IComponentConnector
{
	public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(double), typeof(NumInputTextBox), new PropertyMetadata(12.0));

	internal NumInputTextBox Root;

	internal TextBox InputText;

	internal Border UpBorder;

	private bool _contentLoaded;

	public double FontSize
	{
		get
		{
			return (double)GetValue(FontSizeProperty);
		}
		set
		{
			SetValue(FontSizeProperty, value);
		}
	}

	public int MaxNum { get; set; } = 100000;

	public int MinNum { get; set; }

	public string Text
	{
		get
		{
			return InputText.Text;
		}
		set
		{
			InputText.Text = value;
		}
	}

	public int Value
	{
		get
		{
			int.TryParse(Text, out var result);
			return result;
		}
	}

	public event TextChangedEventHandler ValueChanged;

	public NumInputTextBox()
	{
		InitializeComponent();
	}

	private void OnNumPreviewInput(object sender, TextCompositionEventArgs e)
	{
		if (e.Text.Length >= 1 && !char.IsDigit(e.Text, e.Text.Length - 1) && e.Text != ".")
		{
			e.Handled = true;
		}
	}

	private async void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		int.TryParse(InputText.Text, out var result);
		if (result > MaxNum)
		{
			InputText.Text = MaxNum.ToString() ?? "";
			InputText.SelectAll();
		}
		if (result < MinNum)
		{
			await Task.Delay(1200);
			int.TryParse(InputText.Text, out var result2);
			if (result2 < MinNum)
			{
				InputText.Text = MinNum.ToString() ?? "";
				InputText.SelectAll();
			}
		}
		this.ValueChanged?.Invoke(this, e);
	}

	private void OnUpClick(object sender, MouseButtonEventArgs e)
	{
		int.TryParse(InputText.Text, out var result);
		result++;
		if (result <= MaxNum)
		{
			InputText.Text = result.ToString() ?? "";
		}
		else
		{
			InputText.SelectAll();
		}
	}

	private void OnDownClick(object sender, MouseButtonEventArgs e)
	{
		int.TryParse(InputText.Text, out var result);
		result--;
		if (result >= MinNum)
		{
			InputText.Text = result.ToString() ?? "";
		}
		else
		{
			InputText.SelectAll();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/numinputtextbox.xaml", UriKind.Relative);
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
			Root = (NumInputTextBox)target;
			break;
		case 2:
			InputText = (TextBox)target;
			InputText.PreviewTextInput += OnNumPreviewInput;
			InputText.TextChanged += OnTextChanged;
			break;
		case 3:
			UpBorder = (Border)target;
			UpBorder.MouseLeftButtonUp += OnUpClick;
			break;
		case 4:
			((Border)target).MouseLeftButtonUp += OnDownClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
