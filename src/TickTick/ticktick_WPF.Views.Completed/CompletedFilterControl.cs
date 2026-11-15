using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Views.Completed;

public class CompletedFilterControl : UserControl, IComponentConnector
{
	private bool _isEditingTime;

	internal PopupPlacementBorder DateFilterText;

	internal EscPopup DateSelectPopup;

	internal PopupPlacementBorder ProjectOrGroupFilterText;

	internal EscPopup ProjectOrGroupFilterPopup;

	internal PopupPlacementBorder PersonalFilterText;

	internal EscPopup PersonalFilterPopup;

	private bool _contentLoaded;

	private ClosedFilterViewModel ViewModel => base.DataContext as ClosedFilterViewModel;

	public event EventHandler<ClosedFilterViewModel> FilterChanged;

	public CompletedFilterControl(bool isAbandon = false)
	{
		base.DataContext = (isAbandon ? AbandonedProjectIdentity.Filter : CompletedProjectIdentity.Filter);
		ViewModel.IsCompleted = !isAbandon;
		InitializeComponent();
	}

	private void DateFilterClick(object sender, MouseButtonEventArgs e)
	{
		TimeFilterDialog timeFilterDialog = new TimeFilterDialog
		{
			ItemsSource = new List<ListItemData>
			{
				new ListItemData(DateFilter.All, Utils.GetString("AllDate")),
				new ListItemData(DateFilter.ThisWeek, Utils.GetString("ThisWeek")),
				new ListItemData(DateFilter.LastWeek, Utils.GetString("LastWeek")),
				new ListItemData(DateFilter.ThisMonth, Utils.GetString("ThisMonth")),
				new ListItemData(DateFilter.Custom, Utils.GetString("Custom"))
			}
		};
		timeFilterDialog.SetStartDate(ViewModel.StartDate);
		timeFilterDialog.SetEndDate(ViewModel.EndDate);
		timeFilterDialog.ItemsSource.ForEach(delegate(ListItemData item)
		{
			if ((DateFilter)item.Key == ViewModel.DateFilter)
			{
				item.Selected = true;
			}
		});
		timeFilterDialog.OnFilterSelect += async delegate(object obj, DateFilterData item)
		{
			ViewModel.DateFilter = item.Type;
			ViewModel.StartDate = item.StartDate;
			ViewModel.EndDate = item.EndDate;
			DateSelectPopup.IsOpen = false;
			ViewModel.Changed = true;
			if (ViewModel.EndDate.HasValue)
			{
				await CompletionLoadStatusDao.SetStatusPulledDateByEntityId("all", ViewModel.EndDate.Value);
			}
			this.FilterChanged?.Invoke(this, ViewModel);
			UserActCollectUtils.AddClickEvent("tasklist", "completed_list", "filter_date");
		};
		timeFilterDialog.OnEndEditDate += async delegate
		{
			_isEditingTime = true;
		};
		timeFilterDialog.OnCancel += delegate
		{
			DateSelectPopup.IsOpen = false;
		};
		DateSelectPopup.Child = timeFilterDialog;
		DateSelectPopup.IsOpen = true;
		DateSelectPopup.Closed += async delegate
		{
			if (_isEditingTime)
			{
				await Task.Delay(1);
				_isEditingTime = false;
			}
		};
	}

	private void ProjectOrGroupFilterClick(object sender, MouseButtonEventArgs e)
	{
		ProjectExtra data = new ProjectExtra
		{
			ProjectIds = ViewModel.SelectedProjectIds,
			GroupIds = ViewModel.SelectedProjectGroupIds
		};
		ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(ProjectOrGroupFilterPopup, data, new ProjectSelectorExtra());
		projectOrGroupPopup.Save += OnProjectSelect;
		projectOrGroupPopup.Show();
	}

	private async void OnProjectSelect(object sender, ProjectExtra data)
	{
		if (!data.IsAll)
		{
			ViewModel.SelectedProjectIds = data.ProjectIds;
			data.ProjectIds.ForEach(delegate(string id)
			{
				CompletionLoadStatusDao.DeleteStatusByEntityId(id);
			});
			ViewModel.SelectedProjectGroupIds = data.GroupIds;
			ViewModel.SelectedProjectDisplayText = ProjectExtra.FormatDisplayText(ProjectExtra.Serialize(data));
			ViewModel.ProjectOrGroupFilter = ProjectOrGroupFilter.Custom;
			UserActCollectUtils.AddClickEvent("tasklist", "completed_list", "filter_list");
		}
		else
		{
			ViewModel.SelectedProjectIds = new List<string>();
			ViewModel.SelectedProjectGroupIds = new List<string>();
			ViewModel.SelectedProjectDisplayText = Utils.GetString("AllList");
			ViewModel.ProjectOrGroupFilter = ProjectOrGroupFilter.All;
		}
		ViewModel.Changed = true;
		this.FilterChanged?.Invoke(this, ViewModel);
	}

	private void PersonalOrAllFilterClick(object sender, MouseButtonEventArgs e)
	{
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"personal", Utils.GetString(ViewModel.IsCompleted ? "CompletedbyMe" : "WontDobyMe"), (Geometry)null, "")
			{
				Selected = !ViewModel.ShowAll
			},
			new CustomMenuItemViewModel((object)"all", Utils.GetString(ViewModel.IsCompleted ? "AllCompleted" : "AllWontDo"), (Geometry)null, "")
			{
				Selected = ViewModel.ShowAll
			}
		}, PersonalFilterPopup);
		customMenuList.Operated += OnShowAllSelected;
		customMenuList.Show();
	}

	private void OnShowAllSelected(object sender, object e)
	{
		PersonalFilterPopup.IsOpen = false;
		ViewModel.ShowAll = e == "all";
		this.FilterChanged?.Invoke(this, ViewModel);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/completed/completedfiltercontrol.xaml", UriKind.Relative);
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
			DateFilterText = (PopupPlacementBorder)target;
			break;
		case 2:
			DateSelectPopup = (EscPopup)target;
			break;
		case 3:
			ProjectOrGroupFilterText = (PopupPlacementBorder)target;
			break;
		case 4:
			ProjectOrGroupFilterPopup = (EscPopup)target;
			break;
		case 5:
			PersonalFilterText = (PopupPlacementBorder)target;
			break;
		case 6:
			PersonalFilterPopup = (EscPopup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
