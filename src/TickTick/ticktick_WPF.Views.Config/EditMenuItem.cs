using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace ticktick_WPF.Views.Config;

public class EditMenuItem : MenuItem, IComponentConnector
{
	public static readonly DependencyProperty IconDependencyProperty = DependencyProperty.Register("Icon", typeof(ImageSource), typeof(EditMenuItem), new PropertyMetadata(null, OnDataChangeCallback));

	public static readonly DependencyProperty TitleDependencyProperty = DependencyProperty.Register("Title", typeof(string), typeof(EditMenuItem), new PropertyMetadata(string.Empty, null));

	public static readonly DependencyProperty ShortcutDependencyProperty = DependencyProperty.Register("Shortcut", typeof(string), typeof(EditMenuItem), new PropertyMetadata(string.Empty, OnShortCutChanged));

	internal EditMenuItem Root;

	internal Grid IconGrid;

	internal Image IconImage;

	internal TextBlock TitleText;

	internal TextBlock ShortcutText;

	private bool _contentLoaded;

	public new ImageSource Icon
	{
		get
		{
			return (ImageSource)GetValue(IconDependencyProperty);
		}
		set
		{
			SetValue(IconDependencyProperty, value);
		}
	}

	public string Title
	{
		get
		{
			return (string)GetValue(TitleDependencyProperty);
		}
		set
		{
			SetValue(TitleDependencyProperty, value);
		}
	}

	public string Shortcut
	{
		get
		{
			return (string)GetValue(ShortcutDependencyProperty);
		}
		set
		{
			SetValue(ShortcutDependencyProperty, value);
		}
	}

	private static void OnShortCutChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is EditMenuItem editMenuItem && e.NewValue != null)
		{
			editMenuItem.ShortcutText.Visibility = Visibility.Visible;
		}
	}

	public EditMenuItem(double textWidth = 140.0)
	{
		InitializeComponent();
		TitleText.MaxWidth = textWidth;
	}

	private static void OnDataChangeCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is EditMenuItem editMenuItem)
		{
			if (e.NewValue != null)
			{
				editMenuItem.IconImage.Source = (ImageSource)e.NewValue;
				editMenuItem.IconGrid.Width = 24.0;
			}
			else
			{
				editMenuItem.IconGrid.Width = 0.0;
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/editmenuitem.xaml", UriKind.Relative);
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
			Root = (EditMenuItem)target;
			break;
		case 2:
			IconGrid = (Grid)target;
			break;
		case 3:
			IconImage = (Image)target;
			break;
		case 4:
			TitleText = (TextBlock)target;
			break;
		case 5:
			ShortcutText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
