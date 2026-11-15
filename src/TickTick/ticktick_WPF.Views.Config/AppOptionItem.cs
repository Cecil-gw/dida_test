using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ticktick_WPF.Views.Config;

public class AppOptionItem : MenuItem, IComponentConnector
{
	public static readonly DependencyProperty IconDependencyProperty = DependencyProperty.Register("Icon", typeof(Geometry), typeof(AppOptionItem), new PropertyMetadata(null, OnDataChangeCallback));

	public static readonly DependencyProperty TitleDependencyProperty = DependencyProperty.Register("Title", typeof(string), typeof(AppOptionItem), new PropertyMetadata(string.Empty, null));

	public static readonly DependencyProperty ShortcutDependencyProperty = DependencyProperty.Register("Shortcut", typeof(string), typeof(AppOptionItem), new PropertyMetadata(string.Empty, OnShortCutChanged));

	internal AppOptionItem Root;

	internal Grid IconGrid;

	internal Path IconPath;

	internal TextBlock ShortcutText;

	private bool _contentLoaded;

	public new Geometry Icon
	{
		get
		{
			return (Geometry)GetValue(IconDependencyProperty);
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
		if (sender is AppOptionItem appOptionItem && e.NewValue != null)
		{
			appOptionItem.ShortcutText.Visibility = Visibility.Visible;
		}
	}

	public AppOptionItem()
	{
		InitializeComponent();
	}

	private static void OnDataChangeCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is AppOptionItem appOptionItem && e.NewValue != null)
		{
			appOptionItem.IconPath.Data = (Geometry)e.NewValue;
		}
	}

	public void SetIcon(string iconKey)
	{
		if (string.IsNullOrEmpty(iconKey))
		{
			IconGrid.Width = 0.0;
			IconGrid.Margin = new Thickness(4.0, 0.0, 4.0, 0.0);
			IconPath.Data = null;
		}
		else
		{
			IconPath.SetResourceReference(Path.DataProperty, iconKey);
			IconGrid.Margin = new Thickness(8.0, 0.0, 8.0, 0.0);
			IconGrid.Width = 24.0;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/appoptionitem.xaml", UriKind.Relative);
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
			Root = (AppOptionItem)target;
			break;
		case 2:
			IconGrid = (Grid)target;
			break;
		case 3:
			IconPath = (Path)target;
			break;
		case 4:
			ShortcutText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
