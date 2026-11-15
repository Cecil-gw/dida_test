using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Calendar;

public class OperationDialog : UserControl, ITabControl, IComponentConnector
{
	protected readonly string _id;

	private readonly EscPopup _popup;

	private OperationItemViewModel _hoverButton;

	private bool _isChild;

	internal UpDownSelectListView OptionItems;

	internal EscPopup SubPopup;

	private bool _contentLoaded;

	public event EventHandler<KeyValuePair<string, ActionType>> Operated;

	public event EventHandler Closed;

	public OperationDialog(string habitKey, OperationItemViewModel type)
		: this(habitKey, new List<OperationItemViewModel> { type })
	{
	}

	public OperationDialog(string id, List<OperationItemViewModel> types, EscPopup popup = null)
	{
		_id = id;
		_popup = popup ?? new EscPopup
		{
			StaysOpen = false,
			Placement = PlacementMode.MousePoint,
			VerticalOffset = -5.0,
			HorizontalOffset = -5.0,
			MinWidth = 162.0
		};
		InitializeComponent();
		_popup.Closed += OnPopupClosed;
		OptionItems.ItemsSource = types;
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		PopupStateManager.OnViewPopupClosed();
		this.Closed?.Invoke(this, e);
		this.Operated = null;
		this.Closed = null;
	}

	protected virtual async void OnActionClick(OperationItemViewModel model)
	{
		if (!model.StayOpen)
		{
			_popup.IsOpen = false;
		}
		this.Operated?.Invoke(this, new KeyValuePair<string, ActionType>(_id, model.Type));
	}

	public void Show()
	{
		PopupStateManager.OnViewPopupOpened();
		_popup.Child = this;
		_popup.IsOpen = true;
	}

	private static bool IsInTargetShowPopupArea(object sender)
	{
		if (sender is OptionItemWithImageIcon { DataContext: OperationItemViewModel { SubActions: not null } dataContext })
		{
			return dataContext.SubActions.Any();
		}
		return false;
	}

	private void OnActionMouseEnter(object sender, MouseEventArgs e)
	{
		if (IsInTargetShowPopupArea(sender))
		{
			ShowSubPopup((sender as OptionItemWithImageIcon)?.DataContext as OperationItemViewModel);
		}
		else
		{
			SubPopup.IsOpen = false;
		}
	}

	private void ShowSubPopup(OperationItemViewModel model, bool onEnter = false)
	{
		if (model.SubActions == null || !model.SubActions.Any() || model.SubOpened)
		{
			return;
		}
		int? num = (OptionItems.ItemsSource as List<OperationItemViewModel>)?.IndexOf(model);
		if (num.HasValue && num >= 0)
		{
			ListViewItem listViewItem = Utils.GetListViewItem(OptionItems, num.Value);
			SubPopup.PlacementTarget = listViewItem;
		}
		model.HoverSelected = true;
		if (_hoverButton != null)
		{
			_hoverButton.SubOpened = false;
		}
		_hoverButton = model;
		_hoverButton.SubOpened = true;
		SubPopup.StaysOpen = true;
		SubPopup.Placement = PlacementMode.Right;
		SubPopup.IsOpen = false;
		OperationDialog operationDialog = new OperationDialog(_id, model.SubActions, SubPopup)
		{
			_isChild = true
		};
		operationDialog.Operated += delegate(object s, KeyValuePair<string, ActionType> t)
		{
			if (!model.StayOpen)
			{
				_popup.IsOpen = false;
				this.Closed?.Invoke(s, null);
			}
			this.Operated?.Invoke(s, t);
		};
		operationDialog.Show();
		if (onEnter)
		{
			operationDialog.OptionItems.HoverFirst();
		}
	}

	private void SubpopupClosed(object sender, EventArgs e)
	{
		if (_hoverButton != null)
		{
			_hoverButton.SubOpened = false;
			_hoverButton = null;
		}
	}

	private void OnItemSelect(bool onenter, UpDownSelectViewModel e)
	{
		if (e is OperationItemViewModel { Enable: not false } operationItemViewModel)
		{
			if (operationItemViewModel.SubActions == null || operationItemViewModel.SubActions.Count == 0)
			{
				OnActionClick(operationItemViewModel);
			}
			else if (!SubPopup.IsOpen)
			{
				ShowSubPopup(operationItemViewModel, onEnter: true);
			}
		}
	}

	private void OnLeftRightKeyDown(object sender, bool isLeft)
	{
		if (OptionItems.ItemsSource is List<OperationItemViewModel> source)
		{
			OperationItemViewModel operationItemViewModel = source.FirstOrDefault((OperationItemViewModel m) => m.HoverSelected);
			if (operationItemViewModel != null)
			{
				if (_isChild && isLeft)
				{
					_popup.IsOpen = false;
				}
				if (!_isChild && !isLeft && !SubPopup.IsOpen)
				{
					ShowSubPopup(operationItemViewModel, onEnter: true);
				}
			}
		}
		if (isLeft && SubPopup.IsOpen)
		{
			SubPopup.IsOpen = false;
		}
	}

	public bool HandleTab(bool shift)
	{
		UpDownSelect(shift);
		return true;
	}

	public bool HandleEnter()
	{
		if (SubPopup.IsOpen)
		{
			SubPopup.HandleEnter();
			return true;
		}
		return OptionItems.HandleEnter();
	}

	public bool HandleEsc()
	{
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		if (SubPopup.IsOpen)
		{
			SubPopup.HandleUpDown(isUp);
			return true;
		}
		return OptionItems.UpDownSelect(isUp);
	}

	public bool LeftRightSelect(bool isLeft)
	{
		if (SubPopup.IsOpen && isLeft)
		{
			SubPopup.IsOpen = false;
			return true;
		}
		return OptionItems.LeftRightSelect(isLeft);
	}

	public void SetPlaceTarget(UIElement ele)
	{
		if (_popup != null)
		{
			_popup.PlacementTarget = ele;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/operationdialog.xaml", UriKind.Relative);
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
		case 1:
			OptionItems = (UpDownSelectListView)target;
			break;
		case 2:
			SubPopup = (EscPopup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
