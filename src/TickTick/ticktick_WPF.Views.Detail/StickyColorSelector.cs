using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Detail;

public class StickyColorSelector : ContentControl, IComponentConnector, IStyleConnector
{
	private EscPopup _popup;

	internal ItemsControl ColorItems;

	private bool _contentLoaded;

	public bool IsOpen
	{
		get
		{
			return _popup?.IsOpen ?? false;
		}
		set
		{
			if (_popup != null)
			{
				_popup.IsOpen = value;
			}
		}
	}

	public event EventHandler<string> ColorSelect;

	public event EventHandler Closed;

	public StickyColorSelector()
	{
		InitializeComponent();
		List<StickyColorViewModel> colorModels = StickyColorViewModel.GetColorModels();
		ColorItems.ItemsSource = colorModels;
		base.Style = null;
	}

	public StickyColorSelector(string key, bool inPopup = true)
	{
		InitializeComponent();
		if (!Enum.TryParse<StickyColorKey>(key, ignoreCase: true, out var _))
		{
			key = StickyColorKey.Default.ToString();
		}
		List<StickyColorViewModel> colorModels = StickyColorViewModel.GetColorModels();
		colorModels.RemoveAt(colorModels.Count - 1);
		foreach (StickyColorViewModel item in colorModels)
		{
			if (item.Key == key)
			{
				item.Selected = true;
			}
		}
		ColorItems.ItemsSource = colorModels;
		ColorItems.Width = 272.0;
		if (inPopup)
		{
			_popup = new EscPopup
			{
				Child = this,
				StaysOpen = false,
				PopupAnimation = PopupAnimation.Fade,
				Placement = PlacementMode.Bottom,
				HorizontalOffset = -144.0
			};
			_popup.Closed += delegate(object o, EventArgs e)
			{
				this.Closed?.Invoke(o, e);
			};
		}
	}

	private void OnItemSelect(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: StickyColorViewModel { Selected: false } dataContext }))
		{
			return;
		}
		IsOpen = false;
		if (dataContext.NeedPro && !ProChecker.CheckPro(ProType.StickyColor))
		{
			return;
		}
		this.ColorSelect?.Invoke(this, dataContext.Key);
		if (!(ColorItems.ItemsSource is List<StickyColorViewModel> list))
		{
			return;
		}
		foreach (StickyColorViewModel item in list)
		{
			item.Selected = item.Key == dataContext.Key;
		}
	}

	public void SetPlacementTarget(UIElement element)
	{
		if (_popup != null)
		{
			_popup.PlacementTarget = element;
		}
	}

	public void SetSelectedColor(string color)
	{
		if (!Enum.TryParse<StickyColorKey>(color, ignoreCase: true, out var _))
		{
			color = StickyColorKey.Default.ToString();
		}
		if (!(ColorItems.ItemsSource is List<StickyColorViewModel> list))
		{
			return;
		}
		foreach (StickyColorViewModel item in list)
		{
			item.Selected = item.Key == color;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/stickycolorselector.xaml", UriKind.Relative);
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
			ColorItems = (ItemsControl)target;
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
			((Rectangle)target).MouseLeftButtonUp += OnItemSelect;
		}
	}
}
