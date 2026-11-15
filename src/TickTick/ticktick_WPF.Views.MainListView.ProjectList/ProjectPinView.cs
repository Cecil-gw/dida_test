using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.MainListView.ProjectList;

public class ProjectPinView : StackPanel
{
	private List<ProjectPinItemViewModel> _data;

	private int _countInLine;

	private readonly ProjectPinSortOrderModel _selectedData = new ProjectPinSortOrderModel();

	private ProjectPinItemViewModel _moreItem;

	private double _itemWidth;

	private readonly ListView _listView = new ListView
	{
		MaxHeight = (LocalSettings.Settings.ProjectPinFolded ? 120 : 240),
		HorizontalAlignment = HorizontalAlignment.Left
	};

	private readonly Border _bottomBorder = new Border();

	private Popup _projectDragPopup;

	private int _startDragIndex;

	private ProjectPinItemViewModel _mouseDownModel;

	private List<ProjectPinItemViewModel> _models;

	public ProjectPinView()
	{
		InitUiChildren();
		base.SizeChanged += OnSizeChanged;
		base.Unloaded += OnUnloaded;
		base.MouseLeftButtonUp += DragMouseUp;
		base.MouseMove += OnMouseMove;
		base.Loaded += delegate
		{
			if (_models != null)
			{
				ItemsSourceHelper.SetItemsSource(_listView, _models);
			}
		};
	}

	private void InitUiChildren()
	{
		_listView.SetResourceReference(FrameworkElement.StyleProperty, "ListViewStyle");
		_listView.SetResourceReference(ItemsControl.ItemContainerStyleProperty, "ListViewItemContainerStyle");
		_listView.ItemTemplate = (DataTemplate)FindResource("ProjectPinItemTemplate");
		_listView.ItemsPanel = (ItemsPanelTemplate)FindResource("WrapPanelTemplate");
		base.Children.Add(_listView);
		Grid grid = new Grid
		{
			MinHeight = 11.0,
			Margin = new Thickness(0.0, 0.0, 12.0, 6.0)
		};
		Line line = new Line
		{
			X1 = 0.0,
			X2 = 1.0,
			Stretch = Stretch.Fill,
			VerticalAlignment = VerticalAlignment.Bottom,
			StrokeThickness = 1.0
		};
		line.SetResourceReference(Shape.StrokeProperty, "ProjectMenuColorOpacity5");
		grid.Children.Add(_bottomBorder);
		grid.Children.Add(line);
		base.Children.Add(grid);
		_projectDragPopup = new EscPopup
		{
			PlacementTarget = this,
			StaysOpen = true,
			Placement = PlacementMode.Relative
		};
	}

	private void ShowOrHideCollapse(bool show)
	{
		if (show)
		{
			if (_bottomBorder.Child == null)
			{
				Border border = new Border
				{
					Cursor = Cursors.Hand,
					Height = 18.0,
					Margin = new Thickness(0.0, -2.0, 0.0, 2.0)
				};
				border.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle60_100");
				border.MouseLeftButtonUp += OnCollapseClick;
				Path path = UiUtils.CreatePath("Ic.Common.DownArrow", "ProjectMenuColorOpacity100_80", "Path01", -1.0, geoKeyIsGeo: true);
				path.Width = 12.0;
				path.Height = 12.0;
				path.RenderTransformOrigin = new Point(0.5, 0.5);
				path.RenderTransform = new RotateTransform
				{
					Angle = 180.0
				};
				border.Child = path;
				_bottomBorder.Child = border;
			}
		}
		else
		{
			_bottomBorder.Child = null;
		}
	}

	public void SetData(List<ProjectPinItemViewModel> data)
	{
		if (_projectDragPopup.IsOpen)
		{
			return;
		}
		_data = data;
		foreach (ProjectPinItemViewModel datum in data)
		{
			datum.Selected = datum.EntityId == _selectedData.EntityId && datum.Type == _selectedData.Type;
		}
		SetModels();
	}

	private void SetModels()
	{
		if (_countInLine == 0)
		{
			_countInLine = ((int)base.ActualWidth - 14) / 46;
		}
		List<ProjectPinItemViewModel> list = _data.ToList();
		if (LocalSettings.Settings.ProjectPinFolded && _data.Count() > _countInLine * 2)
		{
			list = _data.Take(_countInLine * 2 - 1).ToList();
			_moreItem = new ProjectPinItemViewModel
			{
				IsMore = true,
				ItemWidth = ((_data.Count > _countInLine) ? _itemWidth : 46.0)
			};
			list.Add(_moreItem);
		}
		else
		{
			_moreItem = null;
		}
		foreach (ProjectPinItemViewModel datum in _data)
		{
			datum.ItemWidth = ((_data.Count > _countInLine) ? _itemWidth : 46.0);
		}
		ShowOrHideCollapse(!LocalSettings.Settings.ProjectPinFolded && _data.Count > _countInLine * 2);
		_models = list;
		ItemsSourceHelper.SetItemsSource(_listView, list);
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_listView.ItemsSource = null;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		_countInLine = (int)((base.ActualWidth - 14.0) / 46.0);
		_itemWidth = (int)((e.NewSize.Width - (double)(_countInLine * 2) - 14.0) / (double)_countInLine);
		if (_itemWidth * (double)_countInLine > e.NewSize.Width)
		{
			_itemWidth -= 1.0;
		}
		if (_data == null || _data.Count == 0)
		{
			return;
		}
		if (_countInLine != (int)Math.Floor((e.PreviousSize.Width - 14.0) / 46.0))
		{
			SetModels();
			return;
		}
		foreach (ProjectPinItemViewModel datum in _data)
		{
			datum.ItemWidth = ((_data.Count > _countInLine) ? _itemWidth : 46.0);
		}
		if (_moreItem != null)
		{
			_moreItem.ItemWidth = ((_data.Count > _countInLine) ? _itemWidth : 46.0);
		}
	}

	private void OnCollapseClick(object sender, MouseButtonEventArgs e)
	{
		SetCollapseStatus(collapse: true);
		e.Handled = true;
	}

	private void SetCollapseStatus(bool collapse)
	{
		LocalSettings.Settings.ProjectPinFolded = collapse;
		_listView.MaxHeight = (LocalSettings.Settings.ProjectPinFolded ? 120 : 240);
		SetModels();
	}

	private void DragMouseUp(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		_data.ForEach(delegate(ProjectPinItemViewModel m)
		{
			m.Dragging = false;
		});
		if (!_projectDragPopup.IsOpen || !(_projectDragPopup.DataContext is ProjectPinItemViewModel projectPinItemViewModel))
		{
			return;
		}
		_projectDragPopup.DataContext = null;
		_projectDragPopup.IsOpen = false;
		_mouseDownModel = null;
		int num = _data.IndexOf(projectPinItemViewModel);
		if (_startDragIndex == num)
		{
			return;
		}
		ProjectPinItemViewModel projectPinItemViewModel2 = ((num > 0) ? _data[num - 1] : null);
		ProjectPinItemViewModel projectPinItemViewModel3 = ((num < _data.Count - 1) ? _data[num + 1] : null);
		if (projectPinItemViewModel2 != null || projectPinItemViewModel3 != null)
		{
			if (projectPinItemViewModel2 == null)
			{
				projectPinItemViewModel.SortOrder = projectPinItemViewModel3.SortOrder - 268435456;
			}
			else if (projectPinItemViewModel3 == null)
			{
				projectPinItemViewModel.SortOrder = projectPinItemViewModel2.SortOrder + 268435456;
			}
			else
			{
				projectPinItemViewModel.SortOrder = projectPinItemViewModel2.SortOrder + (projectPinItemViewModel3.SortOrder - projectPinItemViewModel2.SortOrder) / 2;
			}
			ProjectPinSortOrderService.UpdateProjectPinModel(projectPinItemViewModel.SyncEntityId, projectPinItemViewModel.Type, projectPinItemViewModel.SortOrder);
		}
	}

	public void OnItemMouseDown(ProjectPinItemViewModel model)
	{
		ProjectListView projectListView = Utils.FindParent<ProjectListView>(this);
		if (projectListView == null || !projectListView.InPopup)
		{
			_mouseDownModel = model;
		}
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (!_projectDragPopup.IsOpen && e.LeftButton == MouseButtonState.Pressed)
		{
			if (_mouseDownModel != null && !_mouseDownModel.IsMore)
			{
				_mouseDownModel.Dragging = true;
				_projectDragPopup.DataContext = _mouseDownModel;
				_projectDragPopup.Child = new ProjectPinItemView
				{
					InPopup = true
				};
				_projectDragPopup.IsOpen = true;
				Point position = e.GetPosition(this);
				_projectDragPopup.HorizontalOffset = position.X - 18.0;
				_projectDragPopup.VerticalOffset = position.Y - 24.0;
				_startDragIndex = _data.IndexOf(_mouseDownModel);
				Mouse.Capture(this);
			}
		}
		else if (_projectDragPopup.IsOpen && e.LeftButton == MouseButtonState.Pressed)
		{
			Point position2 = e.GetPosition(this);
			_projectDragPopup.HorizontalOffset = position2.X - 28.0;
			_projectDragPopup.VerticalOffset = position2.Y - 34.0;
			ListBoxItem mousePointElement = Utils.GetMousePointElement<ListBoxItem>(e, this);
			if (mousePointElement != null && mousePointElement.DataContext is ProjectPinItemViewModel { IsMore: false } projectPinItemViewModel && _projectDragPopup.DataContext is ProjectPinItemViewModel projectPinItemViewModel2 && !projectPinItemViewModel.Equals(projectPinItemViewModel2) && _listView.ItemsSource is ObservableCollection<ProjectPinItemViewModel> observableCollection)
			{
				Point position3 = e.GetPosition(mousePointElement);
				observableCollection.Remove(projectPinItemViewModel2);
				_data.Remove(projectPinItemViewModel2);
				int num = observableCollection.IndexOf(projectPinItemViewModel);
				if (num >= 0)
				{
					observableCollection.Insert((position3.X > 23.0) ? (num + 1) : num, projectPinItemViewModel2);
					_data.Insert((position3.X > 23.0) ? (num + 1) : num, projectPinItemViewModel2);
				}
				else
				{
					observableCollection.Insert(0, projectPinItemViewModel2);
					_data.Insert(0, projectPinItemViewModel2);
				}
			}
		}
		else
		{
			_mouseDownModel = null;
			if (_projectDragPopup.IsOpen)
			{
				_projectDragPopup.IsOpen = false;
			}
		}
	}

	public void SetSelectedProject(ProjectItemViewModel vm)
	{
		if (vm == null || _selectedData == null)
		{
			return;
		}
		_selectedData.EntityId = vm.Id;
		switch (vm.ListType)
		{
		case ProjectListType.Project:
			_selectedData.Type = 5;
			break;
		case ProjectListType.Group:
			_selectedData.Type = 6;
			break;
		case ProjectListType.Tag:
			_selectedData.Type = 7;
			break;
		case ProjectListType.Filter:
			_selectedData.Type = 8;
			break;
		case ProjectListType.Calendar:
			_selectedData.Type = 9;
			break;
		case ProjectListType.Smart:
			_selectedData.Type = 11;
			break;
		}
		if (_data == null)
		{
			return;
		}
		foreach (ProjectPinItemViewModel item in _data.Where((ProjectPinItemViewModel model) => model != null))
		{
			if (item.EntityId == _selectedData.EntityId && item.Type == _selectedData.Type)
			{
				item.Selected = true;
			}
			else if (item.Selected)
			{
				item.Selected = false;
			}
		}
	}

	public void OnItemSelect(ProjectPinItemViewModel model)
	{
		if (model.IsMore)
		{
			SetCollapseStatus(collapse: false);
			return;
		}
		string type = "project";
		string text = "project";
		switch (model.Type)
		{
		case 6:
			text = "group";
			type = "group";
			break;
		case 7:
			text = "tag";
			type = "tag";
			break;
		case 8:
			text = "filter";
			type = "filter";
			break;
		case 9:
			text = (model.IsBindAccount ? "bind_account" : "subscribe_calendar");
			type = "calendar";
			break;
		case 11:
			text = "smart";
			type = "smart";
			break;
		}
		ProjectListView projectListView = Utils.FindParent<ProjectListView>(this);
		if (projectListView != null)
		{
			projectListView.NavigateProject(type, model.EntityId, scrollToItem: false);
			Utils.FindParent<IListViewParent>(projectListView.GetListViewContainer())?.SaveSelectedProject(text + ":" + model.EntityId);
		}
		foreach (ProjectPinItemViewModel datum in _data)
		{
			if (datum.Selected)
			{
				datum.Selected = false;
			}
		}
		model.Selected = true;
	}

	public void OnItemUnpin(ProjectPinItemViewModel model)
	{
		_data.Remove(model);
		SetModels();
		Utils.FindParent<ProjectListView>(this)?.LoadPinModels();
	}

	public bool IsEmpty()
	{
		if (_data != null)
		{
			return _data.Count == 0;
		}
		return true;
	}
}
