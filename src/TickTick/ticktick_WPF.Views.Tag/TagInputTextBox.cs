using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Tag;

public class TagInputTextBox : Grid, IComponentConnector
{
	internal TextBlock HintText;

	internal TextBox InputText;

	private bool _contentLoaded;

	public event EventHandler<KeyEventArgs> TextKeyDown;

	public event EventHandler<KeyEventArgs> TextKeyUp;

	public event EventHandler<string> TextChanged;

	public TagInputTextBox()
	{
		InitializeComponent();
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		this.TextKeyDown?.Invoke(sender, e);
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		this.TextKeyUp?.Invoke(sender, e);
		if (e.Key == Key.Up || e.Key == Key.Down)
		{
			e.Handled = true;
		}
	}

	private void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (string.IsNullOrEmpty(InputText.Text))
		{
			HintText.Text = Utils.GetString("InputTag");
			HintText.Visibility = Visibility.Visible;
		}
		else
		{
			HintText.Text = InputText.Text;
			HintText.Visibility = Visibility.Hidden;
		}
		if (base.DataContext is SelectedTagViewModel { IsAdd: not false } selectedTagViewModel)
		{
			selectedTagViewModel.Tag = InputText.Text;
		}
		this.TextChanged?.Invoke(sender, InputText.Text);
	}

	private async void OnDataBinded(object sender, DependencyPropertyChangedEventArgs e)
	{
		object dataContext = base.DataContext;
		if (dataContext is SelectedTagViewModel { IsAdd: not false } model)
		{
			await Task.Delay(150);
			InputText.Text = model.Tag;
			InputText.Focus();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/taginputtextbox.xaml", UriKind.Relative);
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
			((TagInputTextBox)target).DataContextChanged += OnDataBinded;
			break;
		case 2:
			HintText = (TextBlock)target;
			break;
		case 3:
			InputText = (TextBox)target;
			InputText.PreviewKeyUp += OnKeyUp;
			InputText.PreviewKeyDown += OnKeyDown;
			InputText.TextChanged += OnTextChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
