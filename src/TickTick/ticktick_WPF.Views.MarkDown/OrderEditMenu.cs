using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public class OrderEditMenu : UserControl, IComponentConnector
{
	private bool _startNewEnabled = true;

	private bool _continueEnabledEnabled = true;

	internal Grid ContinueOrder;

	internal Grid StartNew;

	internal Border EditOrderPopup;

	internal TextBox OrderText;

	private bool _contentLoaded;

	public int Indent { get; set; }

	public bool StartNewEnabled
	{
		get
		{
			return _startNewEnabled;
		}
		set
		{
			_startNewEnabled = value;
			NotifyShowStartNewChanged();
		}
	}

	public bool ContinueEnabled
	{
		get
		{
			return _continueEnabledEnabled;
		}
		set
		{
			_continueEnabledEnabled = value;
			NotifyShowContinueChanged();
		}
	}

	public string CurrentOrder
	{
		get
		{
			return OrderText.Text;
		}
		set
		{
			OrderText.Text = value;
		}
	}

	public event EventHandler<string> OptionSelected;

	public OrderEditMenu()
	{
		InitializeComponent();
	}

	private void NotifyShowContinueChanged()
	{
		ContinueOrder.Opacity = ((!ContinueEnabled) ? 0.6 : 1.0);
		ContinueOrder.Cursor = ((!ContinueEnabled) ? Cursors.No : Cursors.Hand);
	}

	private void NotifyShowStartNewChanged()
	{
		StartNew.Opacity = ((!StartNewEnabled) ? 0.6 : 1.0);
		StartNew.Cursor = ((!StartNewEnabled) ? Cursors.No : Cursors.Hand);
	}

	private void OnStyleClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is Grid { Tag: not null } grid)
		{
			string text = grid.Tag.ToString();
			if (text == "Edit")
			{
				EditOrderPopup.Visibility = Visibility.Visible;
				OrderText.SelectAll();
				OrderText.Focus();
			}
			else if (ContinueEnabled || !(text == "Continue"))
			{
				this.OptionSelected?.Invoke(this, grid.Tag.ToString());
			}
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		this.OptionSelected?.Invoke(this, "Cancel");
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		this.OptionSelected?.Invoke(this, "Edit_" + CurrentOrder);
	}

	private void OnPlusClick(object sender, MouseButtonEventArgs e)
	{
		Utils.TryParseOrderListIndex(OrderText.Text, Indent, out var num);
		num++;
		CurrentOrder = Utils.GetDisplayText(Indent, num);
		OrderText.SelectAll();
		OrderText.Focus();
	}

	private void OnMinusClick(object sender, MouseButtonEventArgs e)
	{
		Utils.TryParseOrderListIndex(OrderText.Text, Indent, out var num);
		num--;
		if (num >= 1)
		{
			CurrentOrder = Utils.GetDisplayText(Indent, num);
			OrderText.SelectAll();
			OrderText.Focus();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/markdown/ordereditmenu.xaml", UriKind.Relative);
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
			ContinueOrder = (Grid)target;
			ContinueOrder.MouseLeftButtonUp += OnStyleClick;
			break;
		case 2:
			StartNew = (Grid)target;
			StartNew.MouseLeftButtonUp += OnStyleClick;
			break;
		case 3:
			((Grid)target).MouseLeftButtonUp += OnStyleClick;
			break;
		case 4:
			EditOrderPopup = (Border)target;
			break;
		case 5:
			OrderText = (TextBox)target;
			break;
		case 6:
			((Grid)target).PreviewMouseLeftButtonUp += OnPlusClick;
			break;
		case 7:
			((Grid)target).PreviewMouseLeftButtonUp += OnMinusClick;
			break;
		case 8:
			((Button)target).Click += OnSaveClick;
			break;
		case 9:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
