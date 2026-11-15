using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.CustomControl;

public class UpDownSelectListView : ListView, ITabControl, IComponentConnector, IStyleConnector
{
	private Point _movePoint;

	private DateTime _lastMoveTime;

	private UpDownSelectViewModel _mouseDownItem;

	private bool _contentLoaded;

	public bool CanBatchSelected { get; set; }

	public bool CanUpDownOverLimit { get; set; }

	public bool CanTabSelect { get; set; } = true;

	public bool NeedHandleItemEnter { get; set; } = true;

	public event UpDownSelectListViewItemSelect ItemSelected;

	public event EventHandler<bool> LeftRightKeyDown;

	public UpDownSelectListView()
	{
		InitializeComponent();
	}

	public bool HandleTab(bool shift)
	{
		if (CanTabSelect)
		{
			return UpDownSelect(shift);
		}
		return false;
	}

	public bool HandleEsc()
	{
		return false;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		if (base.ItemsSource is IEnumerable<UpDownSelectViewModel> source && source.ToList().ToList().FirstOrDefault((UpDownSelectViewModel m) => m.HoverSelected) != null)
		{
			this.LeftRightKeyDown?.Invoke(this, isLeft);
			return true;
		}
		return false;
	}

	public bool Focused()
	{
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		if (base.ItemsSource is IEnumerable<UpDownSelectViewModel> source)
		{
			List<UpDownSelectViewModel> list = source.ToList();
			if (!list.Any((UpDownSelectViewModel item) => item.IsEnable))
			{
				return true;
			}
			int num = list.ToList().FindIndex((UpDownSelectViewModel m) => m.HoverSelected);
			if (num < 0)
			{
				UpDownSelectViewModel upDownSelectViewModel = (isUp ? list.LastOrDefault((UpDownSelectViewModel i) => i.IsEnable) : list.FirstOrDefault((UpDownSelectViewModel i) => i.IsEnable));
				if (upDownSelectViewModel != null)
				{
					upDownSelectViewModel.HoverSelected = true;
					ScrollIntoView(upDownSelectViewModel);
				}
			}
			else
			{
				if (list[num].SubOpened)
				{
					return false;
				}
				list[num].HoverSelected = false;
				do
				{
					num += ((!isUp) ? 1 : (-1));
					if (CanUpDownOverLimit && (num < 0 || num >= list.Count))
					{
						return false;
					}
					num += list.Count;
					num %= list.Count;
				}
				while (!list[num].IsEnable);
				list[num].HoverSelected = true;
				ScrollIntoView(list[num]);
			}
		}
		return true;
	}

	public bool HandleSpace()
	{
		if (base.ItemsSource is IEnumerable<UpDownSelectViewModel> source)
		{
			List<UpDownSelectViewModel> list = source.ToList();
			if (list.Count == 1)
			{
				UpDownSelectViewModel upDownSelectViewModel = list[0];
				if (!upDownSelectViewModel.HasChildren)
				{
					SelectItem(list, upDownSelectViewModel, onEnter: true);
					return true;
				}
			}
		}
		return false;
	}

	public bool HandleEnter()
	{
		if (base.ItemsSource is IEnumerable<UpDownSelectViewModel> source)
		{
			List<UpDownSelectViewModel> list = source.ToList();
			int num = list.ToList().FindIndex((UpDownSelectViewModel m) => m.HoverSelected);
			if (num >= 0)
			{
				UpDownSelectViewModel upDownSelectViewModel = list[num];
				if (!upDownSelectViewModel.IsEnable)
				{
					return true;
				}
				if (upDownSelectViewModel.HasChildren)
				{
					this.LeftRightKeyDown?.Invoke(this, e: false);
					return false;
				}
				SelectItem(list, upDownSelectViewModel, onEnter: true);
			}
		}
		return true;
	}

	private void SelectItem(IEnumerable<UpDownSelectViewModel> items, UpDownSelectViewModel item, bool onEnter = false)
	{
		if (item.Selectable)
		{
			if (!CanBatchSelected)
			{
				foreach (UpDownSelectViewModel item2 in items)
				{
					item2.Selected = false;
				}
				item.Selected = true;
			}
			else
			{
				item.Selected = !item.Selected;
			}
		}
		this.ItemSelected?.Invoke(onEnter, item);
	}

	private async void OnItemMouseEnter(object sender, MouseEventArgs e)
	{
		if (NeedHandleItemEnter && (Utils.IsEmptyDate(_lastMoveTime) || !((DateTime.Now - _lastMoveTime).TotalMilliseconds > 200.0)))
		{
			ClearHover();
			if (sender is FrameworkElement { DataContext: UpDownSelectViewModel { IsEnable: not false } dataContext })
			{
				dataContext.HoverSelected = true;
			}
		}
	}

	private async void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if ((sender as FrameworkElement)?.IsMouseOver ?? false)
		{
			await Task.Delay(50);
			if (!e.Handled && sender is FrameworkElement { DataContext: UpDownSelectViewModel dataContext } && dataContext == _mouseDownItem && dataContext.IsEnable && base.ItemsSource is IEnumerable<UpDownSelectViewModel> items)
			{
				_mouseDownItem = null;
				SelectItem(items, dataContext);
				e.Handled = true;
				_ = dataContext.Selectable;
			}
		}
	}

	public ListViewItem GetHoverSelectedItem()
	{
		if (base.ItemsSource is IEnumerable<UpDownSelectViewModel> source)
		{
			UpDownSelectViewModel upDownSelectViewModel = source.ToList().ToList().FirstOrDefault((UpDownSelectViewModel m) => m.HoverSelected);
			if (upDownSelectViewModel != null)
			{
				return base.ItemContainerGenerator.ContainerFromItem(upDownSelectViewModel) as ListViewItem;
			}
		}
		return null;
	}

	public void ClearHover()
	{
		if (!(base.ItemsSource is IEnumerable<UpDownSelectViewModel> enumerable))
		{
			return;
		}
		foreach (UpDownSelectViewModel item in enumerable)
		{
			item.HoverSelected = false;
		}
	}

	private void OnViewMouseMove(object sender, MouseEventArgs e)
	{
		Point position = e.GetPosition(this);
		if (Math.Abs(position.X - _movePoint.X) > 2.0 || Math.Abs(position.Y - _movePoint.Y) > 2.0)
		{
			_lastMoveTime = DateTime.Now;
			_movePoint = position;
		}
	}

	public void HoverFirst()
	{
		if (!(base.ItemsSource is IEnumerable<UpDownSelectViewModel> enumerable))
		{
			return;
		}
		int num = 0;
		foreach (UpDownSelectViewModel item in enumerable)
		{
			item.HoverSelected = num == 0;
			num++;
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		_lastMoveTime = default(DateTime);
		if (!(base.ItemsSource is IEnumerable<UpDownSelectViewModel> enumerable))
		{
			return;
		}
		foreach (UpDownSelectViewModel item in enumerable)
		{
			if (item.HoverSelected && !item.SubOpened)
			{
				item.HoverSelected = false;
			}
		}
	}

	private void OnItemDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: UpDownSelectViewModel { IsEnable: not false } dataContext })
		{
			_mouseDownItem = dataContext;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/customcontrol/updownselectlistview.xaml", UriKind.Relative);
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
			((UpDownSelectListView)target).MouseMove += OnViewMouseMove;
			((UpDownSelectListView)target).MouseLeave += OnMouseLeave;
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
			((ContentPresenter)target).PreviewMouseLeftButtonDown += OnItemDown;
			((ContentPresenter)target).PreviewMouseLeftButtonUp += OnItemClick;
			((ContentPresenter)target).MouseEnter += OnItemMouseEnter;
		}
	}
}
