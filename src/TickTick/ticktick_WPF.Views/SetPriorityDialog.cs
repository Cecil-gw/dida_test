using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class SetPriorityDialog : UserControl, ITabControl, IComponentConnector
{
	public static readonly DependencyProperty PriorityProperty = DependencyProperty.Register("Priority", typeof(int), typeof(SetPriorityDialog), new PropertyMetadata(-1));

	public static readonly DependencyProperty HoverPriorityProperty = DependencyProperty.Register("HoverPriority", typeof(int), typeof(SetPriorityDialog), new PropertyMetadata(-1));

	public static readonly DependencyProperty LineIconStyleProperty = DependencyProperty.Register("LineIconStyle", typeof(bool), typeof(SetPriorityDialog), new PropertyMetadata(false));

	private readonly Popup _popup;

	internal SetPriorityDialog Root;

	private bool _contentLoaded;

	public int Priority
	{
		get
		{
			return (int)GetValue(PriorityProperty);
		}
		set
		{
			SetValue(PriorityProperty, value);
		}
	}

	public int HoverPriority
	{
		get
		{
			return (int)GetValue(HoverPriorityProperty);
		}
		set
		{
			SetValue(HoverPriorityProperty, value);
		}
	}

	private bool LineIconStyle
	{
		get
		{
			return (bool)GetValue(LineIconStyleProperty);
		}
		set
		{
			SetValue(LineIconStyleProperty, value);
		}
	}

	public event EventHandler<int> PrioritySelect;

	public SetPriorityDialog()
	{
		InitializeComponent();
	}

	public SetPriorityDialog(int priority, bool lineStyle = false)
	{
		InitializeComponent();
		Priority = priority;
		HoverPriority = priority;
		LineIconStyle = lineStyle;
	}

	public SetPriorityDialog(Popup popup, int priority = 0)
	{
		InitializeComponent();
		_popup = popup;
		Priority = priority;
		HoverPriority = priority;
	}

	public void Show()
	{
		if (_popup != null)
		{
			_popup.Child = this;
			_popup.IsOpen = true;
		}
	}

	private async void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_popup != null)
		{
			_popup.IsOpen = false;
		}
		if (sender is FrameworkElement frameworkElement)
		{
			int e2 = int.Parse(frameworkElement.Tag.ToString());
			this.PrioritySelect?.Invoke(this, e2);
			this.PrioritySelect = null;
		}
	}

	private void OnItemMouseEnter(object sender, MouseEventArgs e)
	{
		if (sender is FrameworkElement frameworkElement)
		{
			int hoverPriority = int.Parse(frameworkElement.Tag.ToString());
			HoverPriority = hoverPriority;
		}
	}

	public void MoveHover(bool isUp)
	{
		switch (HoverPriority)
		{
		case 5:
			HoverPriority = ((!isUp) ? 3 : 0);
			break;
		case 3:
			HoverPriority = ((!isUp) ? 1 : 5);
			break;
		case 1:
			HoverPriority = (isUp ? 3 : 0);
			break;
		case 0:
			HoverPriority = (isUp ? 1 : 5);
			break;
		default:
			HoverPriority = ((!isUp) ? 5 : 0);
			break;
		}
	}

	public void EnterSelect()
	{
		Priority = HoverPriority;
		this.PrioritySelect?.Invoke(this, Priority);
	}

	public bool HandleTab(bool shift)
	{
		MoveHover(shift);
		return true;
	}

	public bool HandleEnter()
	{
		EnterSelect();
		return true;
	}

	public bool HandleEsc()
	{
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		MoveHover(isUp);
		return true;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		return false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/setprioritydialog.xaml", UriKind.Relative);
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
			Root = (SetPriorityDialog)target;
			break;
		case 2:
			((Border)target).MouseEnter += OnItemMouseEnter;
			break;
		case 3:
			((DockPanel)target).MouseLeftButtonUp += OnMouseLeftButtonUp;
			break;
		case 4:
			((Border)target).MouseEnter += OnItemMouseEnter;
			break;
		case 5:
			((DockPanel)target).MouseLeftButtonUp += OnMouseLeftButtonUp;
			break;
		case 6:
			((Border)target).MouseEnter += OnItemMouseEnter;
			break;
		case 7:
			((DockPanel)target).MouseLeftButtonUp += OnMouseLeftButtonUp;
			break;
		case 8:
			((Border)target).MouseEnter += OnItemMouseEnter;
			break;
		case 9:
			((DockPanel)target).MouseLeftButtonUp += OnMouseLeftButtonUp;
			((DockPanel)target).MouseEnter += OnItemMouseEnter;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
