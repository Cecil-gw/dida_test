using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.Views.TaskList;

public class CustomSectionOption : ContentControl, IComponentConnector
{
	private Action _addSectionUpper;

	private Action _addSectionUnder;

	private Action _rename;

	private Action _delete;

	private Action<string, string, SelectableItemViewModel, bool> _moveColumn;

	private EscPopup _popup;

	private bool _contentLoaded;

	public CustomSectionOption(EscPopup popup)
	{
		_popup = popup;
		InitializeComponent();
	}

	public void Show(string columnId = null, string projectId = null)
	{
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("rename", Utils.GetString("Rename"), Utils.GetGeometry("Ic.TaskOM.Edit"))
			{
				ShowSelected = false
			}
		};
		int num = 1;
		if (_addSectionUpper != null)
		{
			num++;
			list.Add(new CustomMenuItemViewModel("addUp", Utils.GetString("AddSectionUpper"), Utils.GetGeometry("Ic.TaskOM.AddGroupTop"))
			{
				ShowSelected = false
			});
		}
		if (_addSectionUnder != null)
		{
			num++;
			list.Add(new CustomMenuItemViewModel("addUnder", Utils.GetString("AddSectionBelow"), Utils.GetGeometry("Ic.TaskOM.AddGroupBottom"))
			{
				ShowSelected = false
			});
		}
		if (_delete != null)
		{
			list.Add(new CustomMenuItemViewModel("delete", Utils.GetString("Delete"), Utils.GetGeometry("Ic.ProjectList.Delete"))
			{
				ShowSelected = false,
				IsDelete = true
			});
		}
		CustomMenuList customMenuList = new CustomMenuList(list, _popup);
		if (!string.IsNullOrWhiteSpace(columnId))
		{
			ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(customMenuList.SubPopup, new ProjectExtra
			{
				ProjectIds = new List<string> { projectId }
			}, new ProjectSelectorExtra
			{
				showAll = false,
				batchMode = false,
				canSelectGroup = false,
				CanSearch = true,
				onlyShowPermission = true,
				ShowColumn = false
			});
			list.Insert(num, new CustomMenuItemViewModel("move", Utils.GetString("MoveTo"), Utils.GetGeometry("Ic.ProjectList.MoveTo"))
			{
				SubControl = projectOrGroupPopup,
				NeedSubContentStyle = false,
				ShowSelected = false
			});
			projectOrGroupPopup.ItemSelect += async delegate(object o, SelectableItemViewModel viewModel)
			{
				_popup.IsOpen = false;
				await Task.Delay(100);
				bool moveUncomplete = true;
				List<TaskBaseViewModel> list2 = await TaskCache.GetModelsInProjectColumn(projectId, columnId);
				if (list2.Any((TaskBaseViewModel x) => x.IsTask && x.Status != 0 && !x.IsPinned))
				{
					MoveColumnWindow moveColumnWindow = new MoveColumnWindow(list2);
					moveColumnWindow.Owner = App.Window;
					moveColumnWindow.ShowDialog();
					App.Window.Activate();
					bool? isMoveAll = moveColumnWindow.IsMoveAll;
					if (!isMoveAll.HasValue)
					{
						return;
					}
					moveUncomplete = !isMoveAll.Value;
				}
				_moveColumn(columnId, projectId, viewModel, moveUncomplete);
			};
		}
		customMenuList.Operated += OnOptionSelected;
		customMenuList.Show();
	}

	private async void OnOptionSelected(object sender, object e)
	{
		switch (e as string)
		{
		case "rename":
			_rename();
			break;
		case "addUp":
			_addSectionUpper();
			break;
		case "addUnder":
			_addSectionUnder();
			break;
		case "delete":
			await Task.Delay(120);
			_delete();
			break;
		}
	}

	public void SetAction(Action addSectionUpper, Action addSectionUnder, Action rename, Action delete, Action<string, string, SelectableItemViewModel, bool> moveColumn = null)
	{
		_addSectionUpper = addSectionUpper;
		_addSectionUnder = addSectionUnder;
		_rename = rename;
		_delete = delete;
		_moveColumn = moveColumn;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tasklist/customsectionoption.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		_contentLoaded = true;
	}
}
