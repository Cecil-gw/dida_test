using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.CustomControl;

public class CustomMenuList : UserControl, ITabControl, IComponentConnector, IStyleConnector
{
	private const int PopupClosedEventCleanupDelayMilliseconds = 250;

	private readonly Popup _popup;

	private CustomMenuItemViewModel _hoverButton;

	private bool _isChild;

	private ITabControl _topTabControl;

	private ITabControl _currentFocus;

	public bool CanSelectGroup;

	internal ContentControl Container;

	internal ContentControl TopItem;

	internal UpDownSelectListView Items;

	internal EscPopup SubPopup;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	private bool _contentLoaded;

	public bool AutoClose { get; set; } = true;

	public event EventHandler<object> Operated;

	public event EventHandler Closed;

	public CustomMenuList()
	{
		InitializeComponent();
		Container.Style = null;
	}

	public void SetMinWidth(double minWidth)
	{
		Container.MinWidth = minWidth;
	}

	public void SetFixedWidth(double width)
	{
		Container.Width = width;
		Container.MinWidth = width;
		Container.MaxWidth = width;
	}

	public CustomMenuList(IEnumerable<CustomMenuItemViewModel> types, Popup popup = null, ITabControl topTabControl = null)
	{
		InitializeComponent();
		_popup = popup ?? new EscPopup
		{
			StaysOpen = false,
			Placement = PlacementMode.MousePoint,
			VerticalOffset = -5.0,
			HorizontalOffset = -5.0
		};
		_popup.Closed += OnPopupClosed;
		Items.ItemsSource = types;
		Items.CanTabSelect = true;
		if (topTabControl is UIElement content)
		{
			TopItem.Content = content;
			_topTabControl = topTabControl;
			_currentFocus = _topTabControl;
			Items.CanUpDownOverLimit = true;
		}
		else
		{
			_currentFocus = Items;
		}
	}

	public void InitItems(IEnumerable<CustomMenuItemViewModel> types)
	{
		Items.ItemsSource = types;
		Items.CanTabSelect = true;
		_currentFocus = Items;
	}

	private async void OnPopupClosed(object sender, EventArgs e)
	{
		_popup.Closed -= OnPopupClosed;
		this.Closed?.Invoke(this, e);
		_popup.Child = null;
		await Task.Delay(250);
		this.Closed = null;
		this.Operated = null;
	}

	private void OnActionClick(CustomMenuItemViewModel model)
	{
		if (model.IsMessage || (!CanSelectGroup && model.HasChildren))
		{
			return;
		}
		if (!model.Selectable)
		{
			if (!string.IsNullOrEmpty(model.UnableText))
			{
				Toast(model.UnableText);
			}
			return;
		}
		EventHandler<object> eventHandler = this.Operated;
		object value = model.Value;
		if (AutoClose && _popup != null)
		{
			_popup.IsOpen = false;
		}
		eventHandler?.Invoke(this, value);
	}

	public void Show()
	{
		_popup.Child = this;
		_popup.IsOpen = true;
	}

	public void Toast(string toastText)
	{
		ToastBorder.MaxWidth = Container.ActualWidth - 20.0;
		ToastBorder.Visibility = Visibility.Visible;
		ToastText.Text = toastText;
		Storyboard storyboard = (Storyboard)FindResource("ShowToast");
		ToastBorder.BeginStoryboard(storyboard);
	}

	private void OnToasted(object sender, EventArgs e)
	{
		ToastBorder.Visibility = Visibility.Collapsed;
	}

	private void OnActionMouseEnter(object sender, MouseEventArgs e)
	{
		if (!(sender is FrameworkElement frameworkElement) || !(frameworkElement.DataContext is CustomMenuItemViewModel))
		{
			SubPopup.IsOpen = false;
		}
	}

	private async Task ShowSubPopup(CustomMenuItemViewModel model, bool onEnter = false)
	{
		if (!model.HasChildren)
		{
			return;
		}
		int? num = (Items.ItemsSource as List<CustomMenuItemViewModel>)?.IndexOf(model);
		if (num.HasValue && num >= 0)
		{
			ListViewItem listViewItem = Utils.GetListViewItem(Items, num.Value);
			SubPopup.PlacementTarget = listViewItem;
		}
		model.HoverSelected = true;
		_hoverButton = model;
		_hoverButton.SubOpened = true;
		SubPopup.StaysOpen = true;
		SubPopup.Placement = PlacementMode.Right;
		if (model.SubContent != null || !double.IsNaN(model.SubPopupWidth))
		{
			SubPopup.Width = model.SubPopupWidth;
		}
		if (model.SubContent != null)
		{
			ContentControl contentControl = new ContentControl
			{
				Content = model.SubContent
			};
			contentControl.SetResourceReference(FrameworkElement.StyleProperty, "PopupContentStyle");
			SubPopup.Child = contentControl;
			SubPopup.IsOpen = true;
			return;
		}
		CustomMenuList customMenuList = new CustomMenuList(model.SubActions, SubPopup, model.SubControl)
		{
			_isChild = true
		};
		if (!model.NeedSubContentStyle)
		{
			customMenuList.Container.Style = null;
		}
		customMenuList.Operated += async delegate(object s, object t)
		{
			SubPopup.StaysOpen = false;
			this.Closed?.Invoke(s, null);
			this.Operated?.Invoke(s, t);
			await Task.Delay(50);
			if (_popup != null)
			{
				_popup.IsOpen = false;
			}
		};
		customMenuList.Show();
		if (onEnter)
		{
			customMenuList.Items.HoverFirst();
		}
	}

	private void SubpopupClosed(object sender, EventArgs e)
	{
		SubPopup.Child = null;
		if (_hoverButton != null)
		{
			_hoverButton.SubOpened = false;
			_hoverButton = null;
		}
	}

	private void OnItemSelect(bool onenter, UpDownSelectViewModel e)
	{
		if (e is CustomMenuItemViewModel customMenuItemViewModel)
		{
			if (!customMenuItemViewModel.HasChildren)
			{
				OnActionClick(customMenuItemViewModel);
			}
			else if (!SubPopup.IsOpen)
			{
				ShowSubPopup(customMenuItemViewModel, onEnter: true);
			}
		}
	}

	private void OnLeftRightKeyDown(object sender, bool isLeft)
	{
		if (!(Items.ItemsSource is List<CustomMenuItemViewModel> source))
		{
			return;
		}
		CustomMenuItemViewModel customMenuItemViewModel = source.FirstOrDefault((CustomMenuItemViewModel m) => m.HoverSelected);
		if (customMenuItemViewModel != null)
		{
			if (_isChild && isLeft && _popup != null)
			{
				_popup.IsOpen = false;
			}
			if (!_isChild && !isLeft && !SubPopup.IsOpen)
			{
				ShowSubPopup(customMenuItemViewModel, onEnter: true);
			}
		}
	}

	public void Close()
	{
		if (_popup != null)
		{
			_popup.IsOpen = false;
		}
	}

	public bool HandleTab(bool shift)
	{
		if (SubPopup.IsOpen)
		{
			SubPopup.HandleUpDown(shift);
			return true;
		}
		if (_currentFocus.HandleTab(shift))
		{
			return true;
		}
		ITabControl currentFocus;
		if (!object.Equals(_currentFocus, Items) || _topTabControl == null)
		{
			ITabControl items = Items;
			currentFocus = items;
		}
		else
		{
			currentFocus = _topTabControl;
		}
		_currentFocus = currentFocus;
		return _currentFocus.HandleTab(shift);
	}

	public bool HandleEnter()
	{
		if (_currentFocus.HandleEnter())
		{
			if (AutoClose && _popup != null)
			{
				_popup.IsOpen = false;
			}
			return true;
		}
		return false;
	}

	public bool HandleEsc()
	{
		return _currentFocus.HandleEsc();
	}

	public bool UpDownSelect(bool isUp)
	{
		if (SubPopup.IsOpen)
		{
			SubPopup.HandleUpDown(isUp);
			return true;
		}
		if (_currentFocus.UpDownSelect(isUp))
		{
			return true;
		}
		ITabControl currentFocus;
		if (!object.Equals(_currentFocus, Items) || _topTabControl == null)
		{
			ITabControl items = Items;
			currentFocus = items;
		}
		else
		{
			currentFocus = _topTabControl;
		}
		_currentFocus = currentFocus;
		return _currentFocus.UpDownSelect(isUp);
	}

	public bool LeftRightSelect(bool isLeft)
	{
		if (isLeft && SubPopup.IsOpen)
		{
			SubPopup.IsOpen = false;
			return true;
		}
		return _currentFocus.LeftRightSelect(isLeft);
	}

	private void OnItemLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: CustomMenuItemViewModel dataContext } frameworkElement)
		{
			if (dataContext.FontSize == 12)
			{
				frameworkElement.MaxHeight = 30.0;
			}
			if (dataContext.FontSize == 11)
			{
				frameworkElement.MaxHeight = 28.0;
			}
		}
	}

	public void CloseSubPopup(object sender, bool e)
	{
		SubPopup.IsOpen = false;
		if (_popup != null)
		{
			_popup.IsOpen = false;
		}
	}

	private void OnActionMouseMove(object sender, MouseEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: CustomMenuItemViewModel dataContext } && (!dataContext.SubOpened || !object.Equals(_hoverButton, dataContext)))
		{
			SubPopup.IsOpen = false;
			ShowSubPopup(dataContext);
		}
	}

	public ListViewItem GetItemView(string val)
	{
		if (Items.ItemsSource is List<CustomMenuItemViewModel> source)
		{
			CustomMenuItemViewModel item = source.FirstOrDefault((CustomMenuItemViewModel m) => m.Value == val);
			return Items.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
		}
		return null;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/customcontrol/custommenulist.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 3:
			((Storyboard)target).Completed += OnToasted;
			break;
		case 4:
			Container = (ContentControl)target;
			break;
		case 5:
			TopItem = (ContentControl)target;
			break;
		case 6:
			Items = (UpDownSelectListView)target;
			break;
		case 7:
			SubPopup = (EscPopup)target;
			break;
		case 8:
			ToastBorder = (Border)target;
			break;
		case 9:
			ToastText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((Border)target).MouseEnter += OnActionMouseEnter;
			((Border)target).MouseMove += OnActionMouseMove;
			((Border)target).Loaded += OnItemLoaded;
			break;
		case 2:
			((Border)target).MouseEnter += OnActionMouseEnter;
			((Border)target).MouseMove += OnActionMouseMove;
			((Border)target).Loaded += OnItemLoaded;
			break;
		}
	}
}
