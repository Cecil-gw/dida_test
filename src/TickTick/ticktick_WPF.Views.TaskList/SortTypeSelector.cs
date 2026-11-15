using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.TaskList;

public class SortTypeSelector
{
	private readonly bool _resetDate;

	private readonly bool _resetProject;

	private readonly bool _resetTag;

	private ObservableCollection<CustomMenuItemViewModel> _menuItems;

	private EscPopup _popup;

	private List<SortTypeViewModel> _sortTypes;

	private CustomMenuItemViewModel _groupItem;

	private CustomMenuItemViewModel _orderItem;

	private CustomMenuItemViewModel _directionItem;

	private SortOption _option;

	private SortOption _originOption;

	private ProjectIdentity _catId;

	private bool _inKanban;

	private bool _inTimeline;

	private bool _isGroup;

	public event EventHandler<int> ResetSortOrder;

	public event EventHandler<string> OptionSelect;

	public event EventHandler<SortOption> SortOptionSelect;

	public SortTypeSelector(ProjectIdentity identity, List<SortTypeViewModel> sortTypes, SortOption option, bool inKanban = false, EscPopup popup = null, List<CustomMenuItemViewModel> extraItem = null)
	{
		_sortTypes = sortTypes;
		_originOption = option?.Copy() ?? new SortOption
		{
			groupBy = "dueDate",
			orderBy = "dueDate"
		};
		_option = option?.Copy() ?? new SortOption
		{
			groupBy = "dueDate",
			orderBy = "dueDate"
		};
		_catId = identity;
		_inKanban = inKanban;
		_isGroup = identity is GroupProjectIdentity;
		SortTypeViewModel sortTypeViewModel = _sortTypes.FirstOrDefault((SortTypeViewModel s) => s.Id == _option.groupBy);
		SortTypeViewModel sortTypeViewModel2 = _sortTypes.FirstOrDefault((SortTypeViewModel s) => s.Id == _option.orderBy) ?? _sortTypes[0];
		if (sortTypeViewModel == null)
		{
			sortTypeViewModel = (inKanban ? _sortTypes[0] : new SortTypeViewModel("none"));
		}
		ObservableCollection<CustomMenuItemViewModel> observableCollection = new ObservableCollection<CustomMenuItemViewModel>();
		_groupItem = new CustomMenuItemViewModel("group", Utils.GetString("GroupBy"), SortTypeViewModel.GetGroupIconById(sortTypeViewModel?.Id ?? "none"))
		{
			RightText = (SortTypeViewModel.GetTitleById(_option.groupBy, inTimeline: false) ?? Utils.GetString("none")),
			ExtraIcon = Utils.GetGeometry("Ic.Common.DownArrow"),
			ExtraIconAngle = -90,
			ExtraIconSize = 14,
			ExtraIconColor = ThemeUtil.GetColor("BaseColorOpacity40", popup),
			ShowSelected = false
		};
		_orderItem = new CustomMenuItemViewModel("sort", Utils.GetString("SortBy"), SortTypeViewModel.GetSortIconById(sortTypeViewModel2.Id, inTimeline: false))
		{
			RightText = SortTypeViewModel.GetTitleById(_option.orderBy, inTimeline: false),
			ExtraIcon = Utils.GetGeometry("Ic.Common.DownArrow"),
			ExtraIconAngle = -90,
			ExtraIconSize = 14,
			ExtraIconColor = ThemeUtil.GetColor("BaseColorOpacity40", popup),
			ShowSelected = false
		};
		_directionItem = new CustomMenuItemViewModel("direction", Utils.GetString("Order"), Utils.GetGeometry("Ic.TaskOM.Sort"))
		{
			RightText = SortTypeViewModel.GetDirectionName(_option.orderBy, _option.orderDirection),
			ExtraIcon = Utils.GetGeometry("Ic.Common.DownArrow"),
			ExtraIconAngle = -90,
			ExtraIconSize = 14,
			ExtraIconColor = ThemeUtil.GetColor("BaseColorOpacity40", popup),
			ShowSelected = false
		};
		extraItem?.ForEach(observableCollection.Add);
		observableCollection.Add(_groupItem);
		observableCollection.Add(_orderItem);
		if (_option.orderBy != "sortOrder")
		{
			observableCollection.Add(_directionItem);
		}
		_menuItems = observableCollection;
		_popup = popup;
		if (_popup != null)
		{
			_popup.Closed -= OnClosed;
			_popup.Closed += OnClosed;
		}
	}

	private void OnClosed(object sender, EventArgs e)
	{
		if (_popup != null)
		{
			_popup.Closed -= OnClosed;
		}
		if (_option.groupBy != _originOption.groupBy || _option.orderBy != _originOption.orderBy)
		{
			UserActCollectUtils.AddSortOptionEvent("list_group_order", "sort_switch", _option.groupBy, _option.orderBy);
		}
	}

	public SortTypeSelector(ProjectIdentity catId, List<SortTypeViewModel> sortTypes, SortOption option, EscPopup popup = null)
	{
		_sortTypes = sortTypes;
		_originOption = option?.Copy() ?? new SortOption
		{
			groupBy = "none",
			orderBy = "dueDate"
		};
		_option = option?.Copy() ?? new SortOption
		{
			groupBy = "none",
			orderBy = "dueDate"
		};
		_catId = catId;
		_inKanban = false;
		_inTimeline = true;
		SortTypeViewModel sortTypeViewModel = _sortTypes.FirstOrDefault((SortTypeViewModel s) => s.Id == _option.groupBy);
		ObservableCollection<CustomMenuItemViewModel> observableCollection = new ObservableCollection<CustomMenuItemViewModel>();
		_groupItem = new CustomMenuItemViewModel("group", Utils.GetString("GroupBy"), SortTypeViewModel.GetGroupIconById(sortTypeViewModel?.Id ?? "none"))
		{
			RightText = SortTypeViewModel.GetTitleById(_option.groupBy ?? "none", inTimeline: false),
			ExtraIcon = Utils.GetGeometry("Ic.Common.DownArrow"),
			ExtraIconAngle = -90,
			ExtraIconSize = 14,
			ExtraIconColor = ThemeUtil.GetColor("BaseColorOpacity40"),
			ShowSelected = false
		};
		_orderItem = new CustomMenuItemViewModel("sort", Utils.GetString("SortBy"), SortTypeViewModel.GetSortIconById(_option.orderBy, inTimeline: false))
		{
			RightText = SortTypeViewModel.GetTitleById(_option.orderBy, inTimeline: true),
			ExtraIcon = Utils.GetGeometry("Ic.Common.DownArrow"),
			ExtraIconAngle = -90,
			ExtraIconSize = 14,
			ExtraIconColor = ThemeUtil.GetColor("BaseColorOpacity40"),
			ShowSelected = false
		};
		observableCollection.Add(_groupItem);
		observableCollection.Add(_orderItem);
		_menuItems = observableCollection;
		_popup = popup;
		if (_popup != null)
		{
			_popup.Closed -= OnClosed;
			_popup.Closed += OnClosed;
		}
	}

	private async Task SetReset()
	{
		if (_inTimeline)
		{
			return;
		}
		bool num = await SyncSortOrderDao.ExistTaskListSortOrder(_option, _catId.GetSortProjectId());
		string text = string.Empty;
		if (num)
		{
			switch (_option.orderBy)
			{
			case "dueDate":
				text = Utils.GetString("CenterOrderComboBoxByDefaultDuedate");
				break;
			case "project":
				text = Utils.GetString("ResetProjectOrder");
				break;
			case "priority":
				text = Utils.GetString("ResetPriorityOrder");
				break;
			case "tag":
				text = Utils.GetString("ResetTagOrder");
				break;
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			CustomMenuItemViewModel customMenuItemViewModel = _menuItems.FirstOrDefault((CustomMenuItemViewModel m) => m.Value == "reset");
			if (customMenuItemViewModel != null)
			{
				customMenuItemViewModel.Text = text;
				return;
			}
			_menuItems.Add(new CustomMenuItemViewModel(""));
			_menuItems.Add(new CustomMenuItemViewModel((object)"reset", text, (Geometry)null, "")
			{
				IsCenterText = true
			});
			return;
		}
		CustomMenuItemViewModel customMenuItemViewModel2 = _menuItems.FirstOrDefault((CustomMenuItemViewModel x) => x.Value == "" || x.Value == null);
		if (customMenuItemViewModel2 != null)
		{
			_menuItems.Remove(customMenuItemViewModel2);
		}
		CustomMenuItemViewModel customMenuItemViewModel3 = _menuItems.FirstOrDefault((CustomMenuItemViewModel x) => x.Value == "reset");
		if (customMenuItemViewModel3 != null)
		{
			_menuItems.Remove(customMenuItemViewModel3);
		}
	}

	public async void Show()
	{
		await SetReset();
		CustomMenuList customMenuList = new CustomMenuList(_menuItems, _popup);
		customMenuList.MinWidth = 212.0;
		customMenuList.AutoClose = false;
		customMenuList.Operated += OnItemSelected;
		customMenuList.Closed += OnControlUnloaded;
		customMenuList.Show();
	}

	private void OnItemSelected(object sender, object e)
	{
		if (!(e is string text) || string.IsNullOrEmpty(text))
		{
			return;
		}
		switch (text)
		{
		case "reset":
			_popup.IsOpen = false;
			this.ResetSortOrder?.Invoke(this, 0);
			if (_option != null)
			{
				UserActCollectUtils.AddSortOptionEvent("list_group_order", "resume", _option.groupBy, _option.orderBy);
			}
			break;
		case "group":
		case "sort":
		case "direction":
			_groupItem.Selected = false;
			_orderItem.Selected = false;
			OnItemClick(sender, text);
			break;
		default:
			this.OptionSelect?.Invoke(this, text);
			_popup.IsOpen = false;
			break;
		}
	}

	private void OnControlUnloaded(object sender, EventArgs e)
	{
		this.ResetSortOrder = null;
		this.OptionSelect = null;
		this.SortOptionSelect = null;
	}

	private void OnItemClick(object sender, string type)
	{
		EscPopup escPopup = new EscPopup
		{
			PlacementTarget = (sender as FrameworkElement),
			Placement = PlacementMode.Left,
			HorizontalOffset = 212.0,
			StaysOpen = false
		};
		int num = _menuItems.IndexOf(_groupItem);
		double num2 = ((double?)Application.Current?.FindResource("Height36")) ?? 36.0;
		switch (type)
		{
		case "group":
		{
			List<SortTypeViewModel> list2 = (_inTimeline ? _sortTypes.Where((SortTypeViewModel t) => t.Id != "dueDate").ToList() : _sortTypes.Where((SortTypeViewModel t) => t.CanGroup()).ToList());
			if (!_inKanban)
			{
				list2.Add(new SortTypeViewModel("none"));
			}
			List<CustomMenuItemViewModel> list3 = new List<CustomMenuItemViewModel>();
			list3.AddRange(list2.Select((SortTypeViewModel sortType) => new CustomMenuItemViewModel((object)sortType.Id, SortTypeViewModel.GetTitleById(sortType.Id, inTimeline: false), (Geometry)null, "")
			{
				Selected = (sortType.Id == _option.groupBy)
			}));
			escPopup.Closed += delegate
			{
				_groupItem.SetExtraIconAngle(-90);
			};
			escPopup.VerticalOffset = ((num >= 0) ? ((double)(num + 1) * num2 + 8.0) : 0.0);
			_groupItem.SetExtraIconAngle(0);
			CustomMenuList customMenuList3 = new CustomMenuList(list3, escPopup);
			customMenuList3.MinWidth = 130.0;
			customMenuList3.Operated += OnGroupSelected;
			customMenuList3.Show();
			break;
		}
		case "sort":
		{
			List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
			if (!_inTimeline)
			{
				List<SortTypeViewModel> source = _sortTypes.ToList();
				list.AddRange(from sortType in source
					where sortType.Id != "project" && (!_isGroup || !(sortType.Id == "assignee"))
					select new CustomMenuItemViewModel((object)sortType.Id, sortType.Title, (Geometry)null, "")
					{
						Selected = (sortType.Id == _option.orderBy)
					});
			}
			else
			{
				list.Add(new CustomMenuItemViewModel((object)"sortOrder", Utils.GetString("Default"), (Geometry)null, "")
				{
					Selected = ("sortOrder" == _option.orderBy)
				});
				list.Add(new CustomMenuItemViewModel((object)"dueDate", Utils.GetString("TimeToDate"), (Geometry)null, "")
				{
					Selected = ("dueDate" == _option.orderBy)
				});
			}
			escPopup.Closed += delegate
			{
				_orderItem.SetExtraIconAngle(-90);
			};
			escPopup.VerticalOffset = ((num >= 0) ? ((double)(num + 2) * num2 + 8.0) : 0.0);
			_orderItem.SetExtraIconAngle(0);
			CustomMenuList customMenuList2 = new CustomMenuList(list, escPopup);
			customMenuList2.MinWidth = 130.0;
			customMenuList2.Operated += OnSortSelected;
			customMenuList2.Show();
			break;
		}
		case "direction":
		{
			List<CustomMenuItemViewModel> types = new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel((object)Constants.OrderDirection.Asc, SortTypeViewModel.GetDirectionName(_option.orderBy, Constants.OrderDirection.Asc), (Geometry)null, "")
				{
					Selected = (_option.orderDirection == Constants.OrderDirection.Asc)
				},
				new CustomMenuItemViewModel((object)Constants.OrderDirection.Desc, SortTypeViewModel.GetDirectionName(_option.orderBy, Constants.OrderDirection.Desc), (Geometry)null, "")
				{
					Selected = (_option.orderDirection == Constants.OrderDirection.Desc)
				}
			};
			escPopup.Closed += delegate
			{
				_directionItem.SetExtraIconAngle(-90);
			};
			escPopup.VerticalOffset = ((num >= 0) ? ((double)(num + 3) * num2 + 8.0) : 0.0);
			_directionItem.SetExtraIconAngle(0);
			CustomMenuList customMenuList = new CustomMenuList(types, escPopup);
			customMenuList.MinWidth = 130.0;
			customMenuList.Operated += OnDirectionSelected;
			customMenuList.Show();
			break;
		}
		}
	}

	private void OnGroupSelected(object sender, object e)
	{
		if (e is string text)
		{
			_option.groupBy = text;
			_groupItem.Icon = SortTypeViewModel.GetGroupIconById(text);
			_groupItem.RightText = SortTypeViewModel.GetTitleById(text, inTimeline: false);
			this.SortOptionSelect?.Invoke(this, _option.Copy());
			SetReset();
		}
	}

	private void OnSortSelected(object sender, object e)
	{
		if (!(e is string text))
		{
			return;
		}
		_option.orderBy = text;
		_orderItem.Icon = SortTypeViewModel.GetSortIconById(text, inTimeline: false);
		_orderItem.RightText = SortTypeViewModel.GetTitleById(text, _inTimeline);
		if (!_inTimeline)
		{
			_directionItem.RightText = SortTypeViewModel.GetDirectionName(_option.orderBy, _option.orderDirection);
			if (string.IsNullOrEmpty(_directionItem.RightText))
			{
				if (_menuItems.Contains(_directionItem))
				{
					_menuItems.Remove(_directionItem);
				}
			}
			else if (!_menuItems.Contains(_directionItem))
			{
				_menuItems.Add(_directionItem);
			}
		}
		this.SortOptionSelect?.Invoke(this, _option.Copy());
		SetReset();
	}

	private void OnDirectionSelected(object sender, object e)
	{
		if (e is Constants.OrderDirection value)
		{
			_option.orderDirection = value;
			if (!_inTimeline)
			{
				_directionItem.RightText = SortTypeViewModel.GetDirectionName(_option.orderBy, value);
			}
			this.SortOptionSelect?.Invoke(this, _option.Copy());
			SetReset();
		}
	}
}
