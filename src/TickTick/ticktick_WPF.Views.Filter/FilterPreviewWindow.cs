using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Filter;

public class FilterPreviewWindow : Window, ISectionList, IOkCancelWindow, IComponentConnector
{
	private readonly FilterModel _filterModel;

	internal ListView ListView;

	internal StackPanel NoTaskGrid;

	internal Button SaveButton;

	private bool _contentLoaded;

	public FilterPreviewWindow(FilterModel filterModel)
	{
		_filterModel = filterModel;
		InitializeComponent();
		base.Loaded += OnLoaded;
		InitEmptyView();
	}

	private void InitEmptyView()
	{
		EmptyView emptyView = new EmptyView(160.0);
		emptyView.SetIconsSource(new EmptyIconsSourceModel("FilterEmpty"));
		NoTaskGrid.Children.Add(emptyView);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		FilterPreviewIdentity projectIdentity = new FilterPreviewIdentity(_filterModel);
		Load(projectIdentity);
	}

	private async Task Load(ProjectIdentity projectIdentity)
	{
		List<DisplayItemModel> revisedItems = (await TaskListRefreshHelper.GetSortedDisplayItems(projectIdentity, await TaskListRefreshHelper.InitData(projectIdentity))) ?? new List<DisplayItemModel>();
		if (revisedItems.Count > 0)
		{
			List<DisplayItemModel> items = new List<DisplayItemModel>();
			foreach (DisplayItemModel item in revisedItems)
			{
				items.Add(item);
				item.HitVisible = false;
				if (item.IsOpen)
				{
					continue;
				}
				List<DisplayItemModel> children = item.Children;
				if (children != null && children.Count > 0)
				{
					item.GetChildrenModels(getAll: true).ForEach(delegate(DisplayItemModel c)
					{
						c.HitVisible = false;
						items.Add(c);
					});
				}
			}
			await TaskItemLoadHelper.LoadIndicator(items, loadPomo: false);
			ItemsSourceHelper.SetItemsSource(ListView, revisedItems);
		}
		else
		{
			NoTaskGrid.Visibility = Visibility.Visible;
		}
	}

	private void SaveBtnClick(object sender, RoutedEventArgs e)
	{
		Close();
		StopPreview(save: true);
	}

	private void CancelBtnClick(object sender, RoutedEventArgs e)
	{
		Close();
		StopPreview();
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	public async void StopPreview(bool save = false)
	{
		FilterModel filter = _filterModel;
		bool needToast = save;
		if (save)
		{
			if (FilterDao.CheckNameValid(filter.name, filter.id))
			{
				await FilterDao.SaveFilterFromPreview(filter);
				SyncManager.TryDelaySync();
				((base.Owner as IListViewParent) ?? App.Window)?.SaveSelectedProject("filter:" + filter.id);
			}
			else
			{
				save = false;
			}
		}
		if (!save)
		{
			FilterEditDialog filterEditDialog = new FilterEditDialog(filter, needToast);
			filterEditDialog.Owner = base.Owner;
			filterEditDialog.ShowDialog();
		}
	}

	public void OnSectionStatusChanged(SectionStatus status)
	{
		if (!(ListView.ItemsSource is ObservableCollection<DisplayItemModel> source))
		{
			return;
		}
		List<DisplayItemModel> models = source.ToList();
		DisplayItemModel displayItemModel = models.FirstOrDefault((DisplayItemModel item) => item.Type == DisplayType.Section && item.Section.SectionId == status.SectionId);
		int num = models.IndexOf(displayItemModel);
		if (displayItemModel != null && num >= 0)
		{
			if (status.IsOpen)
			{
				if (num >= 0)
				{
					for (int num2 = 0; num2 < displayItemModel.Children.Count; num2++)
					{
						models.Insert(num + 1 + num2, displayItemModel.Children[num2]);
					}
				}
			}
			else
			{
				List<DisplayItemModel> list = new List<DisplayItemModel>();
				for (int num3 = num + 1; num3 < models.Count; num3++)
				{
					DisplayItemModel displayItemModel2 = models[num3];
					if (displayItemModel2.IsSection)
					{
						break;
					}
					list.Add(displayItemModel2);
				}
				displayItemModel.Children = list;
				list.ForEach(delegate(DisplayItemModel child)
				{
					models.Remove(child);
				});
			}
		}
		ItemsSourceHelper.SetItemsSource(ListView, models);
	}

	public void SelectOrDeselectAll(DisplayItemModel model, bool selectAll)
	{
	}

	public async Task OnTaskOpenClick(DisplayItemModel model)
	{
		if (!(ListView.ItemsSource is ObservableCollection<DisplayItemModel> source))
		{
			return;
		}
		List<DisplayItemModel> models = source.ToList();
		if (model.IsOpen)
		{
			model.GetChildrenModels(getAll: true).ForEach(delegate(DisplayItemModel m)
			{
				models.Remove(m);
			});
		}
		else
		{
			HashSet<string> openedTaskIds = null;
			List<DisplayItemModel> childrenModels = model.GetChildrenModels(getAll: false, openedTaskIds);
			int num = models.IndexOf(model);
			if (num >= 0)
			{
				for (int num2 = childrenModels.Count - 1; num2 >= 0; num2--)
				{
					models.Insert(num + 1, childrenModels[num2]);
				}
			}
		}
		model.IsOpen = !model.IsOpen;
		ItemsSourceHelper.SetItemsSource(ListView, models);
	}

	public async Task OnAddTaskInSectionClick(DisplayItemModel model)
	{
	}

	public void OnCancel()
	{
		Close();
		StopPreview();
	}

	public void Ok()
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/filter/filterpreviewwindow.xaml", UriKind.Relative);
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
			ListView = (ListView)target;
			break;
		case 2:
			NoTaskGrid = (StackPanel)target;
			break;
		case 3:
			SaveButton = (Button)target;
			SaveButton.Click += SaveBtnClick;
			break;
		case 4:
			((Button)target).Click += CancelBtnClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
