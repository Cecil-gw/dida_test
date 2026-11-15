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
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Template;

namespace ticktick_WPF.Views.QuickAdd;

public class AddOptionDialog : UserControl, ITabControl, IComponentConnector
{
	public static readonly DependencyProperty PriorityProperty = DependencyProperty.Register("Priority", typeof(int), typeof(AddOptionDialog), new PropertyMetadata(-1, null));

	public static readonly DependencyProperty TabSelectedIndexProperty = DependencyProperty.Register("TabSelectedIndex", typeof(int), typeof(AddOptionDialog), new PropertyMetadata(-1, null));

	private bool _isNote;

	private BatchSetTagControl _batchSetTagCtrl;

	private AddTaskViewModel _model;

	private ProjectOrGroupPopup _projectPopup;

	private SetAssigneeDialog _assignDialog;

	private bool _assignPopupShow;

	private bool _setProjectPopupShow;

	private bool _tagPopupShow;

	private readonly PopupLocationInfo _projectPopupTracker = new PopupLocationInfo();

	private readonly PopupLocationInfo _tagPopupTracker = new PopupLocationInfo();

	private readonly PopupLocationInfo _assignPopupTracker = new PopupLocationInfo();

	private readonly List<PopupLocationInfo> _popupTrackers = new List<PopupLocationInfo>();

	private List<EscPopup> _popups = new List<EscPopup>();

	internal AddOptionDialog Root;

	internal StackPanel EntityPanel;

	internal Grid PriorityGrid;

	internal Grid MoveButton;

	internal OptionCheckBox ProjectButton;

	internal EscPopup SetProjectPopup;

	internal Grid AssignBtn;

	internal EscPopup SetAssigneePopup;

	internal Grid TagGrid;

	internal EscPopup SetTagPopup;

	internal OptionCheckBox AttachmentButton;

	internal OptionCheckBox AddTemplateButton;

	internal OptionCheckBox ConvertToTaskSwitch;

	internal OptionCheckBox ConvertToNoteSwitch;

	internal OptionCheckBox ConvertToEventSwitch;

	internal Grid MoreOptionSeparator;

	internal OptionCheckBox QuickAddStyle;

	internal OptionCheckBox SwitchKind;

	private bool _contentLoaded;

	public int Priority
	{
		get
		{
			return (int)GetValue(PriorityProperty);
		}
		set
		{
			SetValue(PriorityProperty, value);
		}
	}

	public int TabSelectedIndex
	{
		get
		{
			return (int)GetValue(TabSelectedIndexProperty);
		}
		set
		{
			SetValue(TabSelectedIndexProperty, value);
		}
	}

	public QuickAddView QuickAddView { private get; set; }

	public event EventHandler AttachmentClick;

	public event EventHandler SwitchNoteClick;

	public event EventHandler SwitchTaskClick;

	public event EventHandler SwitchEventClick;

	public event EventHandler SwitchChecklistClick;

	public event EventHandler AICompleteClick;

	public AddOptionDialog()
	{
		InitializeComponent();
	}

	private void PriorityGridClick(object sender, MouseButtonEventArgs e)
	{
		QuickAddView.HideOptionDialog(stopOperate: true);
		if (sender is FrameworkElement frameworkElement)
		{
			UserActCollectUtils.AddClickEvent("add_task", "action_btn", "priority");
			int priority = int.Parse(frameworkElement.Tag.ToString());
			QuickAddView.SetPriority(priority);
		}
		e.Handled = true;
	}

	public void Init(AddTaskViewModel model, bool showTab, bool showEntity = true, bool showQuickAddStyle = true, string kind = "TEXT", bool canConvertToEvent = false)
	{
		_model = model;
		InitProject(model.ProjectId, model.IsNote);
		InitPriority(model.Priority);
		TabSelectedIndex = ((!showTab) ? (-1) : ((PriorityGrid.Visibility != Visibility.Visible) ? 4 : 0));
		_popups.Add(SetProjectPopup);
		_popups.Add(SetTagPopup);
		_popups.Add(SetAssigneePopup);
		_popupTrackers.Add(_projectPopupTracker);
		_popupTrackers.Add(_tagPopupTracker);
		_popupTrackers.Add(_assignPopupTracker);
		EntityPanel.Visibility = ((!showEntity) ? Visibility.Collapsed : Visibility.Visible);
		AttachmentButton.Visibility = (model.IsCalendar ? Visibility.Collapsed : Visibility.Visible);
		AddTemplateButton.Visibility = (model.IsCalendar ? Visibility.Collapsed : Visibility.Visible);
		QuickAddStyle.Visibility = ((!showQuickAddStyle) ? Visibility.Collapsed : Visibility.Visible);
		SwitchKind.Visibility = ((showQuickAddStyle || model.IsNoteProject || model.IsCalendar) ? Visibility.Collapsed : Visibility.Visible);
		SwitchKind.Text = ((kind == "TEXT") ? Utils.GetString("switch_checklist") : Utils.GetString("switch_description"));
		SwitchKind.Icon = ((kind == "TEXT") ? Utils.GetGeometry("Ic.TaskDetail.Checkitem") : Utils.GetGeometry("Ic.TaskDetail.Detail"));
		InitCalendarOptions(model, showTab);
		InitConvertSwitches(model, canConvertToEvent);
		UpdateMoreOptionSeparatorVisibility();
	}

	private void InitCalendarOptions(AddTaskViewModel model, bool showTab)
	{
		if (model.IsCalendar)
		{
			PriorityGrid.Visibility = Visibility.Collapsed;
			AssignBtn.Visibility = Visibility.Collapsed;
			TagGrid.Visibility = Visibility.Collapsed;
			ProjectButton.Text = model.CalendarName ?? model.ProjectName;
			ProjectButton.TitleText.MaxWidth = 160.0;
			ProjectButton.Icon = Utils.GetGeometry("Ic.ProjectList.SkipCalendar");
			TabSelectedIndex = (showTab ? 4 : (-1));
		}
	}

	private void TryCloseOtherPopups(EscPopup popup)
	{
		foreach (EscPopup item in _popups.Where((EscPopup escPopup) => escPopup != popup))
		{
			item.Close();
		}
	}

	private void InitProject(string projectId, bool isNote)
	{
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (projectModel != null)
		{
			ProjectButton.Text = projectModel.name;
			ProjectButton.TitleText.MaxWidth = 160.0;
			AssignBtn.Visibility = ((!projectModel.IsShareList()) ? Visibility.Collapsed : Visibility.Visible);
			_isNote = isNote;
			PriorityGrid.Visibility = ((_isNote || _model.IsCalendar) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void InitConvertSwitches(AddTaskViewModel model, bool canConvertToEvent)
	{
		QuickAddConvertOptions quickAddConvertOptions = QuickAddConvertOptionPolicy.Create(model.IsNote, model.IsCalendar, model.IsNoteProject, canConvertToEvent);
		ConvertToTaskSwitch.Visibility = ((!quickAddConvertOptions.ShowConvertToTask) ? Visibility.Collapsed : Visibility.Visible);
		ConvertToNoteSwitch.Visibility = ((!quickAddConvertOptions.ShowConvertToNote) ? Visibility.Collapsed : Visibility.Visible);
		ConvertToEventSwitch.Visibility = ((!quickAddConvertOptions.ShowConvertToEvent) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void UpdateMoreOptionSeparatorVisibility()
	{
		bool flag = EntityPanel.Visibility == Visibility.Visible || AttachmentButton.Visibility == Visibility.Visible || AddTemplateButton.Visibility == Visibility.Visible || ConvertToTaskSwitch.Visibility == Visibility.Visible || ConvertToNoteSwitch.Visibility == Visibility.Visible || ConvertToEventSwitch.Visibility == Visibility.Visible;
		bool flag2 = QuickAddStyle.Visibility == Visibility.Visible || SwitchKind.Visibility == Visibility.Visible;
		MoreOptionSeparator.Visibility = ((!(flag && flag2)) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void InitPriority(int priority)
	{
		Priority = priority;
	}

	private async void OnAddFromTemplateClick(object sender, EventArgs e)
	{
		UserActCollectUtils.AddClickEvent("add_task", "action_btn", "template");
		ShowAddFromTemplateDialog(_isNote ? TemplateKind.Note : TemplateKind.Task);
	}

	private void ShowAddFromTemplateDialog(TemplateKind kind = TemplateKind.Task)
	{
		AddTemplateDialog addTemplateDialog = new AddTemplateDialog(kind, _model, Utils.FindParent<ListViewContainer>(QuickAddView));
		addTemplateDialog.Owner = Window.GetWindow(this);
		addTemplateDialog.TemplateSelected -= OnTemplateSelected;
		addTemplateDialog.TemplateSelected += OnTemplateSelected;
		addTemplateDialog.ShowDialog();
		addTemplateDialog.Activate();
		addTemplateDialog.Topmost = true;
		QuickAddView?.TryFocus();
	}

	private async void OnTemplateSelected(object sender, string templateId)
	{
		if (!(await ProChecker.CheckTaskLimit(QuickAddView.GetTaskAddModel().ProjectId)) && !string.IsNullOrEmpty(templateId) && QuickAddView != null)
		{
			TaskModel task = await TaskService.AddTaskFromTemplate(templateId, QuickAddView.GetTaskAddModel(), QuickAddView.GetExtra());
			if (task != null)
			{
				QuickAddView.NotifyAddTask(task);
				await Task.Delay(200);
				Utils.FindParent<TaskView>(QuickAddView)?.GetTaskDetail()?.TryFocusDetail(focusEnd: true, task.id);
			}
		}
	}

	private void ShowPopup(object sender, MouseEventArgs e)
	{
		if (!_popupTrackers.Any((PopupLocationInfo tracker) => tracker.IsSafeShowing()))
		{
			TryShowAssignPopup();
			TryShowSetProjectPopup();
			TryShowTagPopup();
		}
	}

	private void TryShowTagPopup()
	{
		if (SetTagPopup.IsOpen)
		{
			_tagPopupTracker.Mark();
		}
		if (!TagGrid.IsMouseOver && !SetTagPopup.IsMouseOver)
		{
			_tagPopupShow = false;
			SetTagPopup.IsOpen = false;
		}
		else if (!_tagPopupShow)
		{
			_tagPopupShow = true;
			DelayShowTagPopup();
		}
	}

	private async Task DelayShowTagPopup()
	{
		bool isFirst = _batchSetTagCtrl == null;
		if (isFirst)
		{
			_batchSetTagCtrl = new BatchSetTagControl();
			_batchSetTagCtrl.Close += OnBatchSetTagClosed;
			_batchSetTagCtrl.TagsSelect += delegate(object s, TagSelectData tags)
			{
				QuickAddView.OnTagsAdded(tags);
			};
			SetTagPopup.Child = _batchSetTagCtrl;
		}
		await Task.Delay(150);
		if (_tagPopupShow)
		{
			_batchSetTagCtrl.Init(new TagSelectData
			{
				OmniSelectTags = new List<string>(QuickAddView.GetSelectedTags())
			}, isFirst);
			SetTagPopup.IsOpen = true;
			_tagPopupTracker.Bind(SetTagPopup);
			TryCloseOtherPopups(SetTagPopup);
		}
	}

	private void OnBatchSetTagClosed(object sender, EventArgs e)
	{
		SetTagPopup.IsOpen = false;
	}

	private void TryShowAssignPopup()
	{
		if (SetAssigneePopup.IsOpen)
		{
			_assignPopupTracker.Mark();
		}
		if (!AssignBtn.IsMouseOver && !SetAssigneePopup.IsMouseOver)
		{
			_assignPopupShow = false;
			SetAssigneePopup.IsOpen = false;
		}
		else if (!_assignPopupShow)
		{
			_assignPopupShow = true;
			DelayShowAssignPopup();
		}
	}

	private async void DelayShowAssignPopup()
	{
		await Task.Delay(150);
		if (_assignPopupShow)
		{
			ShowAssignPopup();
		}
	}

	private void TryShowSetProjectPopup()
	{
		if (SetProjectPopup.IsOpen)
		{
			_projectPopupTracker.Mark();
		}
		bool flag = _projectPopup != null && _projectPopup.ChildMouseOver();
		if (!MoveButton.IsMouseOver && !SetProjectPopup.IsMouseOver && !flag)
		{
			_setProjectPopupShow = false;
			_projectPopup?.ClosePopup();
			SetProjectPopup.IsOpen = false;
		}
		else if (!_setProjectPopupShow)
		{
			_setProjectPopupShow = true;
			DelayShowSetProjectPopup();
		}
	}

	private async void DelayShowSetProjectPopup()
	{
		await Task.Delay(150);
		if (_setProjectPopupShow)
		{
			ShowSelectProjectPopup();
		}
	}

	private void ShowAssignPopup()
	{
		if (_assignDialog == null)
		{
			_assignDialog = new SetAssigneeDialog(_model.ProjectId, SetAssigneePopup, QuickAddView?.Avatar?.UserId);
			_assignDialog.AssigneeSelect += OnAssigneeSelect;
		}
		else
		{
			_assignDialog.SetItems(_model.ProjectId, QuickAddView?.Avatar?.UserId);
		}
		_assignDialog.Move(forward: false);
		_assignDialog.Show();
		_assignPopupTracker.Bind(SetAssigneePopup);
		TryCloseOtherPopups(SetAssigneePopup);
	}

	private void OnAssigneeSelect(object sender, AvatarInfo model)
	{
		SetAssigneePopup.IsOpen = false;
		QuickAddView.HideOptionDialog(stopOperate: true);
		QuickAddView.OnAssigneeSelect(model);
	}

	private void ShowSelectProjectPopup(bool enter = false)
	{
		if (_model.IsCalendar)
		{
			ShowSelectCalendarPopup(enter);
			return;
		}
		if (_projectPopup == null)
		{
			_projectPopup = new ProjectOrGroupPopup(SetProjectPopup, new ProjectExtra
			{
				ProjectIds = new List<string> { _model.ProjectId }
			}, new ProjectSelectorExtra
			{
				showAll = false,
				batchMode = false,
				canSelectGroup = false,
				onlyShowPermission = true,
				ShowColumn = true,
				ColumnId = _model.GetProjectColumnId()
			});
			_projectPopup.ItemSelect += OnProjectSelect;
		}
		else
		{
			_projectPopup.SetData(new ProjectExtra
			{
				ProjectIds = new List<string> { _model.ProjectId }
			}, _model.GetProjectColumnId());
		}
		if (enter)
		{
			_projectPopup.HoverSelectFirst();
		}
		_projectPopup.Show();
		_projectPopupTracker.Bind(SetProjectPopup);
		TryCloseOtherPopups(SetProjectPopup);
	}

	private void ShowSelectCalendarPopup(bool enter = false)
	{
		_projectPopup?.ClosePopup();
		_projectPopup = new ProjectOrGroupPopup(SetProjectPopup, new ProjectExtra
		{
			SubscribeCalendars = new List<string> { _model.CalendarId }
		}, new ProjectSelectorExtra
		{
			isCalendarMode = true,
			batchMode = false,
			accountId = (_model.IsOriginalCalendar ? _model.AccountId : string.Empty)
		});
		_projectPopup.ItemSelect += OnCalendarSelect;
		if (enter)
		{
			_projectPopup.HoverSelectFirst();
		}
		_projectPopup.Show();
		_projectPopupTracker.Bind(SetProjectPopup);
		TryCloseOtherPopups(SetProjectPopup);
	}

	private void OnCalendarSelect(object sender, SelectableItemViewModel e)
	{
		SetProjectPopup.IsOpen = false;
		QuickAddView.HideOptionDialog(stopOperate: true);
		QuickAddView.SelectCalendarFromOption(e.Title, e.Id);
	}

	private void OnProjectSelect(object sender, SelectableItemViewModel e)
	{
		SetProjectPopup.IsOpen = false;
		QuickAddView.HideOptionDialog(stopOperate: true);
		QuickAddView.OnProjectSelect(sender, e);
	}

	public bool HandleTab(bool shift)
	{
		if (SetTagPopup.IsOpen)
		{
			_batchSetTagCtrl?.HandleTab(shift);
			return false;
		}
		if (SetProjectPopup.IsOpen)
		{
			_projectPopup?.UpDownSelect(shift);
			return false;
		}
		if (SetAssigneePopup.IsOpen)
		{
			SetAssigneePopup.HandleTab(shift);
			return false;
		}
		int tabSelectedIndex = TabSelectedIndex;
		tabSelectedIndex += 8 + ((!shift) ? 1 : (-1));
		tabSelectedIndex %= 8;
		if (tabSelectedIndex < 4 && PriorityGrid.Visibility != Visibility.Visible)
		{
			TabSelectedIndex = tabSelectedIndex;
			return HandleTab(shift);
		}
		if (tabSelectedIndex == 5 && !AssignBtn.IsVisible)
		{
			TabSelectedIndex = tabSelectedIndex;
			return HandleTab(shift);
		}
		TabSelectedIndex = tabSelectedIndex;
		return true;
	}

	public bool HandleEnter()
	{
		if (TabSelectedIndex >= 0 && TabSelectedIndex <= 3)
		{
			QuickAddView.HideOptionDialog(stopOperate: true);
			QuickAddView.SetPriority((new int[4] { 5, 3, 1, 0 })[TabSelectedIndex]);
			return true;
		}
		if (TabSelectedIndex == 4 && !SetProjectPopup.IsOpen)
		{
			ShowSelectProjectPopup(enter: true);
		}
		if (TabSelectedIndex == 5)
		{
			if (SetAssigneePopup.IsOpen)
			{
				_assignDialog?.EnterSelect();
			}
			else
			{
				ShowAssignPopup();
			}
			return true;
		}
		if (TabSelectedIndex == 6 && !SetTagPopup.IsOpen)
		{
			_tagPopupShow = true;
			DelayShowTagPopup();
		}
		if (TabSelectedIndex == 7)
		{
			ShowAddFromTemplateDialog(_isNote ? TemplateKind.Note : TemplateKind.Task);
		}
		return true;
	}

	public bool HandleEsc()
	{
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		if (SetProjectPopup.IsOpen)
		{
			_projectPopup?.UpDownSelect(isUp);
			return true;
		}
		if (SetTagPopup.IsOpen)
		{
			return false;
		}
		if (SetAssigneePopup.IsOpen)
		{
			_assignDialog?.Move(isUp);
			return true;
		}
		return HandleTab(isUp);
	}

	public bool LeftRightSelect(bool isLeft)
	{
		if (TabSelectedIndex >= 0 && TabSelectedIndex <= 3)
		{
			return HandleTab(isLeft);
		}
		if (TabSelectedIndex >= 4 && TabSelectedIndex <= 6)
		{
			if (SetProjectPopup.IsOpen)
			{
				ProjectOrGroupPopup projectPopup = _projectPopup;
				if (projectPopup != null && projectPopup.LeftRightSelect(isLeft))
				{
					return true;
				}
				if (isLeft)
				{
					SetProjectPopup.IsOpen = false;
					return true;
				}
			}
			if (SetTagPopup.IsOpen)
			{
				return false;
			}
			if (!isLeft)
			{
				if (!SetAssigneePopup.IsOpen)
				{
					HandleEnter();
				}
			}
			else if (SetAssigneePopup.IsOpen)
			{
				SetAssigneePopup.IsOpen = false;
			}
		}
		return true;
	}

	private void OnAssignMouseEnter(object sender, MouseEventArgs e)
	{
	}

	private void OnInputStyleClick(object sender, EventArgs e)
	{
		UserActCollectUtils.AddClickEvent("add_task", "action_btn", "inputbox_settings");
		QuickAddStyleWindow quickAddStyleWindow = new QuickAddStyleWindow
		{
			Owner = Window.GetWindow(this)
		};
		DesktopQuickAdd desktopQuickAdd = LocalSettings.Settings.UserPreference.desktopQuickAdd;
		if (desktopQuickAdd != null)
		{
			quickAddStyleWindow.UseDetailStyle = desktopQuickAdd.style == 1;
		}
		quickAddStyleWindow.ShowDialog();
		quickAddStyleWindow.Activate();
		quickAddStyleWindow.Topmost = true;
	}

	private void OnSwitchNoteClick(object sender, EventArgs e)
	{
		UserActCollectUtils.AddClickEvent("add_task", "action_btn", "convert_to_note");
		this.SwitchNoteClick?.Invoke(this, null);
	}

	private void OnSwitchTaskClick(object sender, EventArgs e)
	{
		UserActCollectUtils.AddClickEvent("add_task", "action_btn", "convert_to_task");
		this.SwitchTaskClick?.Invoke(this, null);
	}

	private void OnSwitchEventClick(object sender, EventArgs e)
	{
		UserActCollectUtils.AddClickEvent("add_task", "action_btn", "convert_to_event");
		this.SwitchEventClick?.Invoke(this, null);
	}

	private void OnAttachmentClick(object sender, EventArgs e)
	{
		this.AttachmentClick?.Invoke(this, null);
	}

	private void OnSwitchKindClick(object sender, EventArgs e)
	{
		this.SwitchChecklistClick?.Invoke(this, null);
	}

	private void OnAICompleteClick(object sender, EventArgs e)
	{
		this.AICompleteClick?.Invoke(this, null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/quickadd/addoptiondialog.xaml", UriKind.Relative);
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
			Root = (AddOptionDialog)target;
			break;
		case 2:
			((StackPanel)target).MouseMove += ShowPopup;
			break;
		case 3:
			EntityPanel = (StackPanel)target;
			break;
		case 4:
			PriorityGrid = (Grid)target;
			break;
		case 5:
			((Grid)target).MouseLeftButtonUp += PriorityGridClick;
			break;
		case 6:
			((Grid)target).MouseLeftButtonUp += PriorityGridClick;
			break;
		case 7:
			((Grid)target).MouseLeftButtonUp += PriorityGridClick;
			break;
		case 8:
			((Grid)target).MouseLeftButtonUp += PriorityGridClick;
			break;
		case 9:
			MoveButton = (Grid)target;
			break;
		case 10:
			ProjectButton = (OptionCheckBox)target;
			break;
		case 11:
			SetProjectPopup = (EscPopup)target;
			break;
		case 12:
			AssignBtn = (Grid)target;
			AssignBtn.MouseEnter += OnAssignMouseEnter;
			break;
		case 13:
			SetAssigneePopup = (EscPopup)target;
			break;
		case 14:
			TagGrid = (Grid)target;
			break;
		case 15:
			SetTagPopup = (EscPopup)target;
			break;
		case 16:
			AttachmentButton = (OptionCheckBox)target;
			break;
		case 17:
			AddTemplateButton = (OptionCheckBox)target;
			break;
		case 18:
			ConvertToTaskSwitch = (OptionCheckBox)target;
			break;
		case 19:
			ConvertToNoteSwitch = (OptionCheckBox)target;
			break;
		case 20:
			ConvertToEventSwitch = (OptionCheckBox)target;
			break;
		case 21:
			MoreOptionSeparator = (Grid)target;
			break;
		case 22:
			QuickAddStyle = (OptionCheckBox)target;
			break;
		case 23:
			SwitchKind = (OptionCheckBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
