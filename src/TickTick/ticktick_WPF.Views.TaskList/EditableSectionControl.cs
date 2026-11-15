using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.TaskList;

public class EditableSectionControl : UserControl, IComponentConnector
{
	private TaskListView _parent;

	private bool _startDrag;

	public static readonly DependencyProperty InOperateProperty = DependencyProperty.Register("InOperate", typeof(bool), typeof(EditableSectionControl), new PropertyMetadata(false));

	internal EditableSectionControl Root;

	internal Grid Container;

	internal StackPanel NormalGrid;

	internal Path OpenIndicator;

	internal StackPanel OptionPanel;

	internal Border AddButton;

	internal Border MoreButton;

	internal Path MoreImage;

	internal EscPopup MorePopup;

	internal TextBlock RightText;

	internal TextBox EditBox;

	internal Popup ErrorPopup;

	private bool _contentLoaded;

	private DisplayItemModel Model => base.DataContext as DisplayItemModel;

	protected bool Editing { get; set; }

	public bool InOperate
	{
		get
		{
			return (bool)GetValue(InOperateProperty);
		}
		set
		{
			SetValue(InOperateProperty, value);
		}
	}

	public EditableSectionControl()
	{
		InitializeComponent();
		EditBox.PreviewKeyDown += OnKeyDown;
		base.DataContextChanged += OnDataContextChanged;
		Container.MouseLeftButtonUp += OnSectionClick;
		base.Unloaded += OnUnloaded;
	}

	protected EditableSectionControl(bool inherit)
	{
	}

	private void OnSectionClick(object sender, MouseButtonEventArgs e)
	{
		if (!InOperate && !TaskDragHelpModel.DragHelp.IsDragging)
		{
			ChangeOpenStatus();
		}
	}

	private void ChangeOpenStatus()
	{
		DisplayItemModel model = Model;
		if (model?.Section?.Children == null)
		{
			return;
		}
		model.Num = model.Section.Children.Count;
		foreach (DisplayItemModel child in model.Section.Children)
		{
			model.Num += child.GetChildrenModels(getAll: true).Count;
		}
		model.IsOpen = !model.IsOpen;
		Utils.FindParent<ISectionList>(this)?.OnSectionStatusChanged(new SectionStatus
		{
			SectionId = model.Section.SectionId,
			IsOpen = model.IsOpen
		});
	}

	private TaskListView GetParent()
	{
		_parent = _parent ?? Utils.FindParent<TaskListView>(this);
		return _parent;
	}

	private async void OnAddTaskClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (!InOperate)
		{
			AddButtonClick();
		}
	}

	protected virtual void AddButtonClick()
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			if (!model.IsOpen)
			{
				ChangeOpenStatus();
				GetParent()?.UpdateLayout();
			}
			GetParent()?.AddTaskInSection(model);
		}
	}

	private async void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		if (!InOperate)
		{
			e.Handled = true;
			Mouse.Capture(null);
			await Task.Delay(30);
			CustomSectionOption customSectionOption = new CustomSectionOption(MorePopup);
			SetOptionAction(customSectionOption);
			DisplayItemModel model = Model;
			customSectionOption.Show(model?.Section.SectionId, model?.Section.ProjectId);
			InOperate = true;
		}
	}

	protected virtual void SetOptionAction(CustomSectionOption option)
	{
		DisplayItemModel model = base.DataContext as DisplayItemModel;
		bool flag = false;
		if (model != null)
		{
			flag = model.Enable && ((model.Section as CustomizedSection)?.CanDelete ?? false);
		}
		option.SetAction(AddSectionUpper, AddSectionUnder, delegate
		{
			Rename(model);
		}, flag ? new Action(Delete) : null, OnMoveColumnItemSelected);
	}

	private async void OnPopupClosed(object sender, EventArgs e)
	{
		await Task.Delay(200);
		InOperate = false;
	}

	private void OnMoveColumnItemSelected(string columnId, string originProjectId, SelectableItemViewModel e, bool isSection)
	{
		string id = e.Id;
		if (id != originProjectId)
		{
			TaskService.MoveColumnAsync(columnId, originProjectId, id, null, isSection, alwaysIgnorePin: true);
		}
	}

	private void AddSectionUpper()
	{
		MorePopup.IsOpen = false;
		GetParent()?.AddNewSection(Model, below: false);
	}

	private void AddSectionUnder()
	{
		MorePopup.IsOpen = false;
		GetParent()?.AddNewSection(Model, below: true);
	}

	private async void Rename(DisplayItemModel model)
	{
		if (model != null && Model == model)
		{
			MorePopup.IsOpen = false;
			SetEditing(edit: true);
			EditBox.Text = model.Title;
			await Task.Delay(50);
			if (!base.IsLoaded || Model != model)
			{
				SetEditing(edit: false);
				return;
			}
			EditBox.Focus();
			EditBox.SelectAll();
			EditBox.LostFocus -= OnLostFocus;
			EditBox.LostFocus += OnLostFocus;
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (EditBox.Visibility == Visibility.Visible)
		{
			SetEditing(edit: false);
		}
	}

	private void OnLostFocus(object sender, RoutedEventArgs e)
	{
		string text = EditBox.Text.Trim();
		if (!Editing)
		{
			SetEditing(edit: false);
			return;
		}
		SetEditing(edit: false);
		if (!string.IsNullOrEmpty(text) && NameUtils.IsValidColumnName(text))
		{
			SaveSectionName(text);
		}
		else
		{
			if (string.IsNullOrEmpty(text))
			{
				Utils.Toast(Utils.GetString("SectionNameCannotBeEmpty"));
			}
			DisplayItemModel model = Model;
			if (model != null && model.NewAdd)
			{
				model.NewAdd = false;
				GetParent()?.RemoveSection(model.Id);
				return;
			}
		}
		ErrorPopup.IsOpen = false;
	}

	private async Task SaveSectionName(string saveName)
	{
		DisplayItemModel model = Model;
		if (string.IsNullOrEmpty(saveName) || model?.Section == null || saveName == model.Title)
		{
			return;
		}
		if (await CheckIfColumnNameExisted(saveName))
		{
			Utils.Toast(Utils.GetString("SectionNameExisted"));
			if (model.NewAdd)
			{
				model.NewAdd = false;
				GetParent()?.RemoveSection(model.Id);
			}
			return;
		}
		if (model.NewAdd)
		{
			ColumnModel columnModel = await ColumnDao.AddColumn(saveName, model.ProjectId, model.SortOrder, model.Id);
			model.NewAdd = false;
			model.Section.SectionId = columnModel.id;
			model.SourceViewModel.Id = columnModel.id;
			GetParent()?.LoadAsync(forceRefresh: true);
		}
		await ColumnDao.SaveColumnName(saveName, model.Section.SectionId);
		model.SourceViewModel.Title = saveName;
		model.NotifyPropertyChanged("Title");
		SyncManager.TryDelaySync();
	}

	private async Task<bool> CheckIfColumnNameExisted(string name)
	{
		if (name == Model.Title)
		{
			return false;
		}
		return (await ColumnDao.GetColumnsByProjectId(Model.ProjectId))?.Exists((ColumnModel c) => c.name == name) ?? false;
	}

	private async void Delete()
	{
		MorePopup.IsOpen = false;
		DisplayItemModel model = Model;
		if (model == null)
		{
			return;
		}
		string targetColumnId = null;
		bool removeAll = false;
		bool flag = (await TaskDao.GetTasksInProjectOrColumn(model.ProjectId, model.Id, first: false)).Any((TaskModel x) => x.pinnedTimeStamp <= 0);
		if (flag)
		{
			DeleteColumnWindow deleteColumnWindow = new DeleteColumnWindow(model.ProjectId, model.Id, fromList: true)
			{
				Owner = Window.GetWindow(this)
			};
			bool? flag2 = deleteColumnWindow.ShowDialog();
			App.Window.Activate();
			if (flag2 != true)
			{
				return;
			}
			targetColumnId = deleteColumnWindow.TargetColumnId;
			removeAll = deleteColumnWindow.removeAll == true;
		}
		await PerformDeleteColumn(model.Id, targetColumnId, removeAll, !flag);
	}

	private async Task PerformDeleteColumn(string columnId, string targetColumnId = null, bool removeAll = false, bool ignoreDefaultColumnFallback = false)
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			await ColumnDao.DeleteColumn(model.Id);
			string primaryColumnId = await ColumnDao.GetProjectDefaultColumnId(model.ProjectId);
			UtilLog.Info("SectionControl.Delete : " + model.Id);
			if (targetColumnId != null)
			{
				await TaskService.MoveColumnAsync(columnId, model.ProjectId, model.ProjectId, targetColumnId);
			}
			else
			{
				await TaskService.BatchDeleteTaskInCustomSection(model.ProjectId, model.Id, primaryColumnId, !removeAll, ignoreDefaultColumnFallback);
			}
			Utils.Toast(Utils.GetString("Deleted"));
			SyncManager.TryDelaySync();
			GetParent()?.LoadAsync(forceRefresh: true);
		}
	}

	private void OnNameTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!NameUtils.IsValidColumnName(EditBox.Text.Trim()))
		{
			ErrorPopup.IsOpen = true;
			EditBox.SelectAll();
		}
		else
		{
			ErrorPopup.IsOpen = false;
		}
	}

	public async void ShowEditBox()
	{
		SetEditing(edit: true);
		EditBox.Text = "";
		await Task.Delay(50);
		EditBox.Focus();
		EditBox.SelectAll();
		EditBox.LostFocus -= OnLostFocus;
		EditBox.LostFocus += OnLostFocus;
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		if (Model == null)
		{
			return;
		}
		switch (e.Key)
		{
		case Key.Escape:
			SetEditing(edit: false);
			if (Model.NewAdd)
			{
				Model.NewAdd = false;
				GetParent()?.RemoveSection(Model.Id);
			}
			break;
		case Key.Return:
		{
			string text = EditBox.Text.Trim();
			if (!string.IsNullOrEmpty(text) && NameUtils.IsValidColumnName(text))
			{
				SetEditing(edit: false);
				SaveSectionName(text);
			}
			else if (!Model.NewAdd)
			{
				SetEditing(edit: false);
			}
			if (string.IsNullOrEmpty(text))
			{
				Utils.Toast(Utils.GetString("SectionNameCannotBeEmpty"));
			}
			break;
		}
		}
	}

	public void SetEditing(bool edit)
	{
		TaskListView taskListView = Utils.FindParent<TaskListView>(this);
		if (taskListView != null)
		{
			taskListView.SectionEditing = edit;
		}
		Editing = edit;
		if (edit)
		{
			EditBox.Visibility = Visibility.Visible;
			NormalGrid.Visibility = Visibility.Collapsed;
		}
		else
		{
			EditBox.Visibility = Visibility.Collapsed;
			EditBox.Text = "";
			NormalGrid.Visibility = Visibility.Visible;
		}
	}

	private void OnDragMouseDown(object sender, MouseButtonEventArgs e)
	{
		_startDrag = true;
	}

	private void OnDragMouseMove(object sender, MouseEventArgs e)
	{
		if (!TickDragHelper.Dragging)
		{
			if (_startDrag && e.LeftButton == MouseButtonState.Pressed)
			{
				StartDrag(e);
			}
			_startDrag = false;
		}
	}

	protected virtual void StartDrag(MouseEventArgs e)
	{
		GetParent()?.StartDragSection(Model);
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!(e.NewValue is DisplayItemModel { IsSection: not false } displayItemModel))
		{
			return;
		}
		displayItemModel.Num = displayItemModel.Section.Children.Count;
		foreach (DisplayItemModel child in displayItemModel.Section.Children)
		{
			displayItemModel.Num += child.GetChildrenModels(getAll: true).Count;
		}
		EditBox.Tag = (displayItemModel.NewAdd ? Utils.GetString("AddSectionHint") : "");
		if (displayItemModel.NewAdd)
		{
			ShowEditBox();
		}
	}

	private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		TaskListView taskListView = Utils.FindParent<TaskListView>(this);
		if (taskListView != null)
		{
			taskListView.SectionEditing = EditBox.Visibility == Visibility.Visible;
		}
	}

	private void RightTextMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			bool flag = displayItemModel.SectionRightActionText == Utils.GetString("DeselectAll");
			e.Handled = true;
			if (!displayItemModel.IsOpen)
			{
				displayItemModel.IsOpen = true;
				Utils.FindParent<ISectionList>(this)?.OnSectionStatusChanged(new SectionStatus
				{
					SectionId = displayItemModel.Section.SectionId,
					IsOpen = displayItemModel.IsOpen
				});
			}
			Utils.FindParent<ISectionList>(this)?.SelectOrDeselectAll(displayItemModel, !flag);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tasklist/editablesectioncontrol.xaml", UriKind.Relative);
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
			Root = (EditableSectionControl)target;
			break;
		case 2:
			Container = (Grid)target;
			break;
		case 3:
			((Border)target).MouseLeftButtonDown += OnDragMouseDown;
			((Border)target).MouseMove += OnDragMouseMove;
			break;
		case 4:
			NormalGrid = (StackPanel)target;
			break;
		case 5:
			OpenIndicator = (Path)target;
			break;
		case 6:
			OptionPanel = (StackPanel)target;
			break;
		case 7:
			AddButton = (Border)target;
			AddButton.MouseLeftButtonUp += OnAddTaskClick;
			break;
		case 8:
			MoreButton = (Border)target;
			MoreButton.MouseLeftButtonUp += OnMoreClick;
			break;
		case 9:
			MoreImage = (Path)target;
			break;
		case 10:
			MorePopup = (EscPopup)target;
			break;
		case 11:
			((Border)target).MouseLeftButtonUp += RightTextMouseUp;
			break;
		case 12:
			RightText = (TextBlock)target;
			break;
		case 13:
			EditBox = (TextBox)target;
			EditBox.IsVisibleChanged += OnVisibleChanged;
			EditBox.TextChanged += OnNameTextChanged;
			break;
		case 14:
			ErrorPopup = (Popup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
