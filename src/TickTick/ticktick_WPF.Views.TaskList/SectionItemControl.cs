using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.TaskList;

public class SectionItemControl : UserControl, IComponentConnector
{
	private bool _mouseDown;

	private bool _addMouseDown;

	internal SectionItemControl Root;

	internal DockPanel Container;

	internal Path OpenIndicator;

	internal Path StarPath;

	internal HoverIconButton AddButton;

	internal TextBlock PostponeText;

	private bool _contentLoaded;

	private DisplayItemModel Model => base.DataContext as DisplayItemModel;

	public SectionItemControl()
	{
		InitializeComponent();
	}

	private void OnSectionClick(object sender, MouseButtonEventArgs e)
	{
		if (_mouseDown)
		{
			_mouseDown = false;
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

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!(e.NewValue is DisplayItemModel { IsSection: not false } displayItemModel))
		{
			return;
		}
		StarPath.Visibility = ((!(displayItemModel.Section is PinnedSection)) ? Visibility.Collapsed : Visibility.Visible);
		if (displayItemModel.InKanban)
		{
			Container.Margin = new Thickness(15.0, displayItemModel.UnderTaskItem ? 6 : 0, 12.0, 0.0);
			SetMouseEnterEvent();
		}
		displayItemModel.Num = displayItemModel.Section.Children.Count;
		foreach (DisplayItemModel child in displayItemModel.Section.Children)
		{
			displayItemModel.Num += child.GetChildrenModels(getAll: true).Count;
		}
		if (displayItemModel.Section is AssigneeSection assigneeSection && string.IsNullOrEmpty(assigneeSection.Name))
		{
			TaskItemLoadHelper.LoadAssigneeName(displayItemModel);
		}
	}

	private void SelectAllMouseUp(DisplayItemModel model)
	{
		if (model != null)
		{
			bool flag = model.SectionRightActionText == Utils.GetString("DeselectAll");
			if (!model.IsOpen)
			{
				model.IsOpen = true;
				Utils.FindParent<ISectionList>(this)?.OnSectionStatusChanged(new SectionStatus
				{
					SectionId = model.Section.SectionId,
					IsOpen = model.IsOpen
				});
			}
			Utils.FindParent<ISectionList>(this)?.SelectOrDeselectAll(model, !flag);
		}
	}

	private void RightTextMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			if (displayItemModel.InBatchSelected && !(displayItemModel.Section is HabitSection) && !(displayItemModel.Section is TimetableSection) && !(displayItemModel.Section.SectionId == "8ac3038d93c54b80a67321b6a03df066"))
			{
				SelectAllMouseUp(displayItemModel);
			}
			else if (displayItemModel.Section is OutdatedSection { ShowPostpone: not false } && new CustomerDialog(Utils.GetString("PostponeToTodayTitle"), Utils.GetString("PostponeToTodayMsg"), Utils.GetString("Postpone"), Utils.GetString("Cancel"), App.Window).ShowDialog() == true)
			{
				TaskUtils.PostPoneTasks(Utils.FindParent<TaskListView>(this));
			}
		}
	}

	private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDown = true;
	}

	private void SetMouseEnterEvent()
	{
		base.MouseEnter -= OnMouseEnter;
		base.MouseEnter += OnMouseEnter;
		base.MouseLeave -= OnMouseLeave;
		base.MouseLeave += OnMouseLeave;
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		if (base.DataContext is DisplayItemModel { IsSection: not false })
		{
			AddButton.Visibility = Visibility.Collapsed;
			PostponeText.Visibility = Visibility.Collapsed;
		}
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		KanbanColumnView kanbanColumnView = Utils.FindParent<KanbanColumnView>(this);
		if (kanbanColumnView == null)
		{
			return;
		}
		ColumnViewModel model = kanbanColumnView.GetModel();
		if (model == null || !model.CanAdd || !(base.DataContext is DisplayItemModel { IsSection: not false } displayItemModel) || displayItemModel.Section is HabitSection || !(displayItemModel.Section?.SectionId != "8ac3038d93c54b80a67321b6a03df066") || displayItemModel.Section is TimetableSection)
		{
			return;
		}
		if (displayItemModel.Enable)
		{
			PostponeText.Visibility = ((!(displayItemModel.Section is OutdatedSection { ShowPostpone: not false })) ? Visibility.Collapsed : Visibility.Visible);
			if (PostponeText.Visibility == Visibility.Visible)
			{
				PostponeText.Text = Utils.GetString("Postpone");
			}
		}
		if (displayItemModel.Section is OutdatedSection || displayItemModel.Enable)
		{
			AddButton.Visibility = Visibility.Visible;
		}
	}

	private void OnAddTaskMouseDown(object sender, MouseButtonEventArgs e)
	{
		KanbanContainer kanbanContainer = Utils.FindParent<KanbanContainer>(this);
		if (kanbanContainer != null && kanbanContainer.AddingModel == null)
		{
			_addMouseDown = true;
		}
	}

	private void OnAddTaskMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (base.DataContext is DisplayItemModel displayItemModel && _addMouseDown)
		{
			if (!displayItemModel.IsOpen)
			{
				ChangeOpenStatus();
			}
			Utils.FindParent<ISectionList>(this)?.OnAddTaskInSectionClick(displayItemModel);
		}
		_addMouseDown = false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tasklist/sectionitemcontrol.xaml", UriKind.Relative);
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
			Root = (SectionItemControl)target;
			Root.MouseLeftButtonDown += OnItemMouseDown;
			Root.MouseLeftButtonUp += OnSectionClick;
			Root.DataContextChanged += OnDataContextChanged;
			break;
		case 2:
			Container = (DockPanel)target;
			break;
		case 3:
			OpenIndicator = (Path)target;
			break;
		case 4:
			StarPath = (Path)target;
			break;
		case 5:
			AddButton = (HoverIconButton)target;
			break;
		case 6:
			PostponeText = (TextBlock)target;
			PostponeText.MouseLeftButtonUp += RightTextMouseUp;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
