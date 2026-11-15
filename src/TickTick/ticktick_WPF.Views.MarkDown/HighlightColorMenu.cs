using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.MarkDown;

public class HighlightColorMenu : UserControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register("SelectedColor", typeof(string), typeof(HighlightColorMenu), new PropertyMetadata(null, OnSelectedColorChanged));

	internal HighlightColorMenu Root;

	private bool _contentLoaded;

	public ObservableCollection<HighlightColorOption> Options { get; } = new ObservableCollection<HighlightColorOption>();

	public string SelectedColor
	{
		get
		{
			return (string)GetValue(SelectedColorProperty);
		}
		set
		{
			SetValue(SelectedColorProperty, HighlightColorMenuState.NormalizeColor(value));
		}
	}

	public event EventHandler<string> HighlightColorSelected;

	public HighlightColorMenu()
	{
		InitializeComponent();
		Options.Add(HighlightColorOption.Clear());
		foreach (HighlightColorData highlightColor in HighlightColorMenuState.HighlightColors)
		{
			Options.Add(HighlightColorOption.FromColor(highlightColor));
		}
		UpdateSelectedColor();
	}

	private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((HighlightColorMenu)d).UpdateSelectedColor();
	}

	private void OnOptionClick(object sender, MouseButtonEventArgs e)
	{
		if (((FrameworkElement)sender).DataContext is HighlightColorOption highlightColorOption)
		{
			e.Handled = true;
			string text = HighlightColorMenuState.NormalizeColor(highlightColorOption.Color);
			if (!highlightColorOption.IsClear && string.Equals(SelectedColor, text, StringComparison.OrdinalIgnoreCase))
			{
				text = null;
			}
			SelectedColor = text;
			this.HighlightColorSelected?.Invoke(this, text);
		}
	}

	private void UpdateSelectedColor()
	{
		if (Options == null)
		{
			return;
		}
		string optionColor = HighlightColorMenuState.GetOptionColor(SelectedColor);
		foreach (HighlightColorOption option in Options)
		{
			option.IsSelected = (option.IsClear ? string.IsNullOrEmpty(optionColor) : string.Equals(option.Color, optionColor, StringComparison.OrdinalIgnoreCase));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/markdown/highlightcolormenu.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			Root = (HighlightColorMenu)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			((Border)target).PreviewMouseLeftButtonUp += OnOptionClick;
		}
	}
}
