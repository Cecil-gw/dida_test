using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DateParser;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CheckList;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.QuickAdd;

public class QuickAddView : System.Windows.Controls.UserControl, IComponentConnector, IStyleConnector
{
	public enum Scenario
	{
		TaskList,
		QuickAdd,
		Widget,
		Kanban
	}

	public static readonly DependencyProperty InputCornerRadiusProperty = DependencyProperty.Register("InputCornerRadius", typeof(CornerRadius), typeof(QuickAddView), new PropertyMetadata(new CornerRadius(8.0)));

	public static readonly DependencyProperty ScenarioProperty = DependencyProperty.Register("Scenario", typeof(Scenario), typeof(QuickAddView));

	private Scenario _scenario;

	public AvatarViewModel Avatar;

	private List<AvatarViewModel> _avatars = new List<AvatarViewModel>();

	private bool _canRender;

	private string _content;

	private string _dateTemplate;

	private bool _isPopupOpen;

	private bool _isInOperation;

	private bool _manualSelectedDate;

	private bool _needClearTag = true;

	private string _priorityTemplate;

	private string _projectTemplate;

	private bool _ctrlPressed;

	private DateTime? _quickSelectedDate;

	private SetAssigneeDialog _assignDialog;

	private DateTime? _tagAddTime;

	private bool _wronglyTimeSetToastShowed;

	private BatchSetTagControl _batchSetTagCtrl;

	public EscPopup OperationPopup = new EscPopup
	{
		Placement = PlacementMode.Bottom,
		VerticalOffset = -5.0,
		HorizontalOffset = 0.0,
		StaysOpen = false
	};

	public EventHandler<List<AvatarViewModel>> NotifyAvatarsChanged;

	private bool _adding;

	private IProjectTaskDefault _taskDefault;

	private ProjectShowExtra _quickSelectProjectExtra;

	private bool? _useInWidgetDark;

	private bool _needSendShiftEnter = true;

	private ProjectOrGroupPopup _projectOrGroupPopup;

	private bool? _projectPopupCalendarMode;

	private readonly QuickSetProjectPopupState _projectPopupState = new QuickSetProjectPopupState();

	private const string HintCtrlEnterType1 = "ctrl_enter_add_task";

	private const string HintEnterType2 = "enter_add_task";

	private const string HintShiftEnterType3 = "shift_enter_add_desc";

	private int _tabIndex;

	private string _uid;

	private bool _mouseDown;

	internal QuickAddView RootView;

	internal Grid AddTaskGrid;

	internal Border InputBackground;

	internal Border InputBackground1;

	internal Border ContentBorder;

	internal Border ContentBorderContent;

	internal Border InputBorder;

	internal Border InputBorderContent;

	internal ScrollViewer InputScrollViewer;

	internal Grid ScrollContent;

	internal Grid TaskKindIndicator;

	internal Grid TitleGrid;

	internal QuickAddText TitleText;

	internal Grid HintPanel;

	internal System.Windows.Shapes.Path AddPath;

	internal TextBlock HintTextBlock;

	internal Grid TextContentGrid;

	internal TextBlock ContentHint;

	internal MarkDownEditor ContentText;

	internal Grid ChecklistGrid;

	internal ChecklistControl Checklist;

	internal StackPanel OperationPanel;

	internal Border SetDateBorder;

	internal Border DateGrid;

	internal StackPanel DetailDateGrid;

	internal System.Windows.Shapes.Path SetDatePath;

	internal TextBlock DateText;

	internal Border SetCalBorder;

	internal EscPopup SetCalendarPopup;

	internal EscPopup SetProjectPopup;

	internal Border MoreGrid;

	internal Border AttachmentBorder;

	internal ScrollViewer FileScroller;

	internal Grid DetailOperationPanel;

	internal Grid DetailPanelContainer;

	internal StackPanel NormalActionPanel;

	internal Border SetDateBorder1;

	internal Border DateGrid1;

	internal StackPanel DetailDateGrid1;

	internal System.Windows.Shapes.Path SetDatePath1;

	internal TextBlock DateText1;

	internal EscPopup SetPriorityPopup;

	internal ContentControl TagGrid;

	internal EscPopup SetTagPopup;

	internal ContentControl AssignBtn;

	internal EscPopup SetAssigneePopup;

	internal Grid TaskDetailMoveGrid;

	internal Grid ProjectIconGrid;

	internal ContentControl MoreOptionGrid;

	internal Grid AddBtnContainer;

	internal System.Windows.Controls.Button SaveButton;

	private bool _contentLoaded;

	public CornerRadius InputCornerRadius
	{
		get
		{
			return (CornerRadius)GetValue(InputCornerRadiusProperty);
		}
		set
		{
			SetValue(InputCornerRadiusProperty, value);
		}
	}

	public Scenario ViewScenario
	{
		get
		{
			return (Scenario)GetValue(ScenarioProperty);
		}
		set
		{
			SetValue(ScenarioProperty, value);
		}
	}

	public bool IsInOperation
	{
		get
		{
			return _isInOperation;
		}
		set
		{
			UtilLog.Error("is_in_operation: " + value);
			if (value)
			{
				DelayActionHandlerCenter.RemoveAction(_uid);
				_isInOperation = true;
				return;
			}
			DelayActionHandlerCenter.TryDoAction(_uid, delegate
			{
				base.Dispatcher.Invoke(delegate
				{
					_isInOperation = false;
				});
			}, 100);
		}
	}

	public bool ManualSelectedDate
	{
		get
		{
			return _manualSelectedDate;
		}
		set
		{
			_manualSelectedDate = value;
			TitleText.ClearHighlightDate();
		}
	}

	public AddTaskViewModel Model { get; private set; }

	public event EventHandler<TaskModel> TaskAdded;

	public event EventHandler<List<TaskModel>> BatchTaskAdded;

	public event EventHandler<CalendarEventModel> CalendarEventAdded;

	public event EventHandler CalendarDetailOpened;

	public event EventHandler CalendarDetailRestored;

	public event EventHandler HideGuide;

	public event EventHandler InputPopupOpened;

	public event EventHandler InputPopupClosed;

	public event EventHandler TagsChanged;

	public QuickAddView(IProjectTaskDefault taskDefault, Scenario scenario = Scenario.TaskList, string themeId = "light", bool focus = false, Section section = null)
		: this(AddTaskViewModel.Build(taskDefault, section, scenario == Scenario.Kanban, allowUseDetailMode: true, scenario), scenario, themeId, focus)
	{
		_taskDefault = taskDefault;
		Checklist.IsNewAddTask = true;
	}

	public QuickAddView()
		: this(ProjectIdentity.GetDefaultProject())
	{
		InitializeComponent();
	}

	private QuickAddView(AddTaskViewModel model, Scenario scenario = Scenario.TaskList, string themeId = "light", bool focus = false)
	{
		if (scenario == Scenario.Widget)
		{
			ThemeUtil.SetTheme(themeId, this);
			if (themeId == "dark")
			{
				_useInWidgetDark = true;
			}
		}
		_uid = Utils.GetGuid();
		InitializeComponent();
		InitShortCut();
		Model = model;
		Model.AllowUseDetailMode = true;
		Model.Init(scenario);
		_scenario = scenario;
		_needClearTag = Model.Tags == null || Model.Tags.Count == 0;
		base.DataContext = Model;
		InitScenario(scenario);
		InitQuickAddText();
		InitCalendarSettings();
		UpdateContentHintText();
		if (focus)
		{
			FocusEnd();
		}
	}

	private void InitScenario(Scenario scenario)
	{
		switch (scenario)
		{
		case Scenario.QuickAdd:
			InitGlobalAddWindowStyle();
			break;
		case Scenario.Widget:
			InitWidgetStyle();
			break;
		case Scenario.Kanban:
			InitKanbanStyle();
			break;
		}
	}

	public void InitWindowStyle()
	{
		Model.UseDetailStyle = true;
		Model.BorderThickness = new Thickness(0.0, 0.0, 0.0, 0.0);
		ContentBorderContent.BorderBrush = new SolidColorBrush(Colors.Transparent);
		TextContentGrid.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		InputScrollViewer.MaxHeight = 340.0;
	}

	public void ResetView(IProjectTaskDefault taskDefault, bool focus = false, bool keepText = false)
	{
		_taskDefault = taskDefault;
		AddTaskViewModel addTaskViewModel = (Model = AddTaskViewModel.Build(taskDefault));
		Model.Init(addTaskViewModel.Scenario);
		_needClearTag = Model.Tags == null || Model.Tags.Count == 0;
		base.DataContext = Model;
		InitCalendarSettings();
		UpdateContentHintText();
		_wronglyTimeSetToastShowed = false;
		Avatar = null;
		_quickSelectedDate = null;
		ManualSelectedDate = false;
		TitleText.ResetTokens();
		if (!keepText)
		{
			TitleText.EditBox.Text = string.Empty;
			ContentText.EditBox.Text = string.Empty;
			TextContentGrid.Visibility = Visibility.Collapsed;
		}
		if (focus)
		{
			FocusEnd();
		}
		LoadAvatars();
	}

	public List<AvatarViewModel> GetAvatars()
	{
		return _avatars;
	}

	private bool IsCurrentProjectShared()
	{
		return CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == Model?.ProjectId)?.IsShareList() ?? false;
	}

	private void ClearAssignee()
	{
		Model.Assignee = null;
		Avatar = null;
		TitleText?.RemoveTokenByType(TokenType.Assignee);
	}

	public void Reset(AddTaskViewModel model, Scenario scenario = Scenario.TaskList, bool focus = false, bool keepText = false)
	{
		Model = model;
		_taskDefault = model.TaskDefault;
		Model.Init(scenario);
		_scenario = scenario;
		_needClearTag = Model.Tags == null || Model.Tags.Count == 0;
		base.DataContext = Model;
		UpdateContentHintText();
		_wronglyTimeSetToastShowed = false;
		Avatar = null;
		_quickSelectedDate = null;
		TitleText.ResetTokens();
		if (!keepText)
		{
			TitleText.EditBox.Text = string.Empty;
			ContentText.EditBox.Text = string.Empty;
		}
		if (scenario != Scenario.QuickAdd)
		{
			TextContentGrid.Visibility = Visibility.Collapsed;
		}
		ManualSelectedDate = false;
		if (focus)
		{
			FocusEnd();
		}
		LoadAvatars();
	}

	private void InitCalendarSettings()
	{
		if (Model.IsCalendar && !UserDao.IsUserValid())
		{
			TitleText.EditBox.IsReadOnly = true;
		}
	}

	private void UpdateContentHintText()
	{
		ContentHint.Text = Utils.GetString(Model.IsCalendar ? "Note" : "Description");
	}

	private void OnViewUnloaded(object sender, RoutedEventArgs e)
	{
		this.BatchTaskAdded = null;
		this.HideGuide = null;
		this.InputPopupOpened = null;
		this.InputPopupClosed = null;
		this.TagsChanged = null;
		NotifyAvatarsChanged = null;
		KeyBindingManager.ShortCutChanged -= OnShortCutChanged;
		GlobalEventManager.QuickAddStyleChanged -= OnQuickAddStyleChanged;
	}

	private async void OnViewLoaded(object sender, RoutedEventArgs e)
	{
		await LoadAvatars();
		KeyBindingManager.ShortCutChanged -= OnShortCutChanged;
		KeyBindingManager.ShortCutChanged += OnShortCutChanged;
		GlobalEventManager.QuickAddStyleChanged -= OnQuickAddStyleChanged;
		GlobalEventManager.QuickAddStyleChanged += OnQuickAddStyleChanged;
		ContentText.QuickPopupOpened += OnQuickPopupOpened;
		ContentText.QuickPopupClosed += OnQuickPopupClosed;
	}

	private void OnQuickPopupOpened(object sender, EventArgs e)
	{
		IsInOperation = true;
	}

	private void OnQuickPopupClosed(object sender, EventArgs e)
	{
		IsInOperation = false;
	}

	private void InitQuickAddText()
	{
		TitleText.TagSelected -= OnNewTagSelected;
		TitleText.TagSelected += OnNewTagSelected;
		TitleText.EnterText -= OnEnterText;
		TitleText.EnterText += OnEnterText;
		TitleText.SplitText -= OnSplitText;
		TitleText.SplitText += OnSplitText;
		TitleText.PrioritySelect -= OnQuickPrioritySelect;
		TitleText.PrioritySelect += OnQuickPrioritySelect;
		TitleText.ProjectSelect -= OnQuickProjectSelect;
		TitleText.ProjectSelect += OnQuickProjectSelect;
		TitleText.CalendarSelect -= OnQuickCalendarSelect;
		TitleText.CalendarSelect += OnQuickCalendarSelect;
		TitleText.DateSelect -= OnQuickDateSelect;
		TitleText.DateSelect += OnQuickDateSelect;
		TitleText.AssigneeSelect -= OnQuickAssignSelect;
		TitleText.AssigneeSelect += OnQuickAssignSelect;
		TitleText.EditBox.GotFocus -= TaskTextGotFocus;
		TitleText.EditBox.GotFocus += TaskTextGotFocus;
		TitleText.EditBox.LostFocus -= TaskTextLostFocus;
		TitleText.EditBox.LostFocus += TaskTextLostFocus;
		TitleText.TextChanged -= OnTitleTextChanged;
		TitleText.TextChanged += OnTitleTextChanged;
		TitleText.DateParsed -= OnDateParsed;
		TitleText.DateParsed += OnDateParsed;
		TitleText.TokenRemoved -= OnTokenRemoved;
		TitleText.TokenRemoved += OnTokenRemoved;
		TitleText.PopupOpenChanged -= PopupOpenChanged;
		TitleText.PopupOpenChanged += PopupOpenChanged;
		base.KeyUp -= OnKeyUp;
		base.KeyUp += OnKeyUp;
		TitleText.TextKeyDown -= OnTextKeyDown;
		TitleText.TextKeyDown += OnTextKeyDown;
		TitleText.EditBox.KeyUp -= OnTextKeyUp;
		TitleText.EditBox.KeyUp += OnTextKeyUp;
		TitleText.SplashCommand -= HandleSplashCommand;
		TitleText.SplashCommand += HandleSplashCommand;
		InitContentEvents();
		SetPriorityPopup.Closed -= OnPriorityClosed;
		SetPriorityPopup.Closed += OnPriorityClosed;
		TitleText.ShowProjectCommand -= HandleProjectCommand;
		TitleText.ShowProjectCommand += HandleProjectCommand;
		TitleText.HideProjectCommand -= HideProjectCommand;
		TitleText.HideProjectCommand += HideProjectCommand;
	}

	private void InitShortCut()
	{
		if (base.InputBindings.Count >= 9)
		{
			KeyBindingManager.SetKeyGesture("ClearDate", base.InputBindings[0] as KeyBinding);
			KeyBindingManager.SetKeyGesture("SetToday", base.InputBindings[1] as KeyBinding);
			KeyBindingManager.SetKeyGesture("SetTomorrow", base.InputBindings[2] as KeyBinding);
			KeyBindingManager.SetKeyGesture("SetNextWeek", base.InputBindings[3] as KeyBinding);
			KeyBindingManager.SetKeyGesture("SetDate", base.InputBindings[4] as KeyBinding);
			KeyBindingManager.SetKeyGesture("SetNoPriority", base.InputBindings[5] as KeyBinding);
			KeyBindingManager.SetKeyGesture("SetLowPriority", base.InputBindings[6] as KeyBinding);
			KeyBindingManager.SetKeyGesture("SetMediumPriority", base.InputBindings[7] as KeyBinding);
			KeyBindingManager.SetKeyGesture("SetHighPriority", base.InputBindings[8] as KeyBinding);
		}
	}

	private void OnShortCutChanged(object sender, string key)
	{
		if (key == null)
		{
			return;
		}
		switch (key.Length)
		{
		case 11:
			switch (key[3])
			{
			case 'T':
				if (key == "SetTomorrow")
				{
					KeyBindingManager.SetKeyGesture("SetTomorrow", base.InputBindings[2] as KeyBinding);
				}
				break;
			case 'N':
				if (key == "SetNextWeek")
				{
					KeyBindingManager.SetKeyGesture("SetNextWeek", base.InputBindings[3] as KeyBinding);
				}
				break;
			}
			break;
		case 9:
			if (key == "ClearDate")
			{
				KeyBindingManager.SetKeyGesture("ClearDate", base.InputBindings[0] as KeyBinding);
			}
			break;
		case 8:
			if (key == "SetToday")
			{
				KeyBindingManager.SetKeyGesture("SetToday", base.InputBindings[1] as KeyBinding);
			}
			break;
		case 7:
			if (key == "SetDate")
			{
				KeyBindingManager.SetKeyGesture("SetDate", base.InputBindings[4] as KeyBinding);
			}
			break;
		case 13:
			if (key == "SetNoPriority")
			{
				KeyBindingManager.SetKeyGesture("SetNoPriority", base.InputBindings[5] as KeyBinding);
			}
			break;
		case 14:
			if (key == "SetLowPriority")
			{
				KeyBindingManager.SetKeyGesture("SetLowPriority", base.InputBindings[6] as KeyBinding);
			}
			break;
		case 17:
			if (key == "SetMediumPriority")
			{
				KeyBindingManager.SetKeyGesture("SetMediumPriority", base.InputBindings[7] as KeyBinding);
			}
			break;
		case 15:
			if (key == "SetHighPriority")
			{
				KeyBindingManager.SetKeyGesture("SetHighPriority", base.InputBindings[8] as KeyBinding);
			}
			break;
		case 10:
		case 12:
		case 16:
			break;
		}
	}

	private void OnQuickAddStyleChanged(object sender, int style)
	{
		Model.UseDetailStyle = style == 1;
		Model.ResetHint();
	}

	private async void OnSplitText(object sender, string content)
	{
		if (!Model.UseDetailStyle)
		{
			Model.UseDetailStyle = true;
			Model.ResetHint();
		}
		SwitchViewCommand();
		ContentText.EditBox.Focus();
	}

	private void HideProjectCommand(object sender, EventArgs e)
	{
		SetProjectPopup.IsOpen = false;
		_quickSelectProjectExtra = null;
		_projectOrGroupPopup = null;
		_projectPopupCalendarMode = null;
	}

	private async void HandleProjectCommand(object sender, ProjectShowExtra extra)
	{
		_quickSelectProjectExtra = extra;
		if (SetProjectPopup.IsOpen && !ProjectPopupMatchesCurrentInputKind())
		{
			SetProjectPopup.IsOpen = false;
			_projectOrGroupPopup = null;
			_projectPopupCalendarMode = null;
		}
		if (!SetProjectPopup.IsOpen)
		{
			ShowSelectProjectPopup(enter: true, TitleText, extra, ignoreOperation: true, fromCommand: true);
			await Task.Delay(10);
			if (_quickSelectProjectExtra == extra)
			{
				_projectOrGroupPopup?.PerformSearch(extra.FilterContent);
			}
			return;
		}
		ProjectOrGroupPopup projectOrGroupPopup = _projectOrGroupPopup;
		if (projectOrGroupPopup != null && projectOrGroupPopup.PerformSearch(extra.FilterContent) > 0)
		{
			_projectPopupState.Restore(SetProjectPopup);
			IsInOperation = true;
			_projectOrGroupPopup.HoverSelectFirst(0);
		}
	}

	private bool ProjectPopupMatchesCurrentInputKind()
	{
		if (_projectOrGroupPopup != null)
		{
			return _projectPopupCalendarMode == Model.IsCalendar;
		}
		return false;
	}

	private void RefreshProjectPopupAfterKindSwitch()
	{
		if (SetProjectPopup.IsOpen && !ProjectPopupMatchesCurrentInputKind())
		{
			SetProjectPopup.IsOpen = false;
			_projectOrGroupPopup = null;
			_projectPopupCalendarMode = null;
			TitleText.RefreshQuickPopup();
		}
	}

	private void HandleSplashCommand(object sender, string command)
	{
		switch (command)
		{
		case "attachment":
			AddAttachment(null, null);
			break;
		case "switch_task":
			SwitchTask(null, null);
			break;
		case "switch_note":
			SwitchNote(null, null);
			break;
		case "switch_event":
			SwitchEvent(null, null);
			break;
		}
	}

	private void InitContentEvents()
	{
		ContentText.EditBox.TextChanged -= OnContentTextChanged;
		ContentText.EditBox.TextChanged += OnContentTextChanged;
		ContentText.EditBox.GotFocus -= TaskTextGotFocus;
		ContentText.EditBox.GotFocus += TaskTextGotFocus;
		ContentText.EditBox.GotFocus -= ContentTextGotFocus;
		ContentText.EditBox.GotFocus += ContentTextGotFocus;
		ContentText.EditBox.LostFocus -= TaskTextLostFocus;
		ContentText.EditBox.LostFocus += TaskTextLostFocus;
		ContentText.EditBox.LostFocus -= ContentTextLostFocus;
		ContentText.EditBox.LostFocus += ContentTextLostFocus;
		ContentText.QuickItemSelected -= OnQuickItemSelected;
		ContentText.QuickItemSelected += OnQuickItemSelected;
		ContentText.PopupOpenChanged -= ContentPopupOpenChanged;
		ContentText.PopupOpenChanged += ContentPopupOpenChanged;
		ContentText.LinkPopupOpened -= LinkPopupOpened;
		ContentText.LinkPopupOpened += LinkPopupOpened;
		ContentText.LinkPopupClosed -= LinkPopupClosed;
		ContentText.LinkPopupClosed += LinkPopupClosed;
		ContentText.RegisterInputHandler();
		ContentText.MoveUp -= OnContentMoveUp;
		ContentText.MoveUp += OnContentMoveUp;
		ContentText.EditBox.PreviewKeyDown -= OnTextKeyDown;
		ContentText.EditBox.PreviewKeyDown += OnTextKeyDown;
		ContentText.EditBox.KeyUp -= OnTextKeyUp;
		ContentText.EditBox.KeyUp += OnTextKeyUp;
		ContentText.EditBox.TextArea.Caret.PositionChanged -= OnContentCaretChanged;
		ContentText.EditBox.TextArea.Caret.PositionChanged += OnContentCaretChanged;
		TitleText.EditBox.TextArea.Caret.PositionChanged -= OnTitleCaretChanged;
		TitleText.EditBox.TextArea.Caret.PositionChanged += OnTitleCaretChanged;
		ContentText.SetMarkRegexText(unable: false, inDetail: false, Model.IsNote);
		ContentText.QuickSetAccountIdProvider = GetQuickSetCalendarAccountId;
		ContentText.QuickSetCalendarIdProvider = GetQuickSetCalendarId;
		ContentText.QuickSetCalendarSelectorAccountIdProvider = GetQuickSetCalendarSelectorAccountId;
		RefreshContentQuickSetContext();
	}

	private void RefreshContentQuickSetContext()
	{
		ContentText.QuickSetUsedInCalendar = Model.IsCalendar;
		ContentText.QuickSetAccountId = GetQuickSetCalendarAccountId();
		ContentText.QuickSetCalendarId = Model.CalendarId;
	}

	public string GetQuickSetCalendarAccountId()
	{
		if (!string.IsNullOrEmpty(Model.AccountId))
		{
			return Model.AccountId;
		}
		return EnsureQuickSetCalendarForSelector()?.AccountId ?? CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel calendar) => calendar.Id == Model.CalendarId)?.AccountId ?? string.Empty;
	}

	private string GetQuickSetCalendarId()
	{
		return EnsureQuickSetCalendarForSelector()?.Id ?? Model.CalendarId ?? string.Empty;
	}

	private string GetQuickSetCalendarSelectorAccountId()
	{
		if (!Model.IsOriginalCalendar)
		{
			return string.Empty;
		}
		return GetQuickSetCalendarAccountId();
	}

	private BindCalendarModel EnsureQuickSetCalendarForSelector()
	{
		if (!Model.IsCalendar)
		{
			return null;
		}
		BindCalendarModel defaultCalendarForNewEvent = GetDefaultCalendarForNewEvent(Model.AccountId, Model.CalendarId);
		if (defaultCalendarForNewEvent == null)
		{
			return null;
		}
		Model.CalendarId = defaultCalendarForNewEvent.Id;
		Model.CalendarName = defaultCalendarForNewEvent.Name;
		Model.AccountId = defaultCalendarForNewEvent.AccountId;
		return defaultCalendarForNewEvent;
	}

	private void ContentTextLostFocus(object sender, RoutedEventArgs e)
	{
		if (Model.Scenario == Scenario.QuickAdd && Model.Kind == "TEXT")
		{
			RefreshContentHintVisibility();
		}
	}

	private void ContentTextGotFocus(object sender, RoutedEventArgs e)
	{
		if (Model.Scenario == Scenario.QuickAdd && Model.Kind == "TEXT")
		{
			TextContentGrid.Visibility = Visibility.Visible;
			if (!ChecklistGrid.IsVisible)
			{
				EnlargeScrollContent(enlarge: true);
			}
			RefreshContentHintVisibility();
		}
	}

	private void RefreshContentHintVisibility()
	{
		ContentHint.Visibility = ((!ShouldShowContentHint()) ? Visibility.Collapsed : Visibility.Visible);
	}

	private bool ShouldShowContentHint()
	{
		if (!string.IsNullOrEmpty(ContentText.Text))
		{
			return false;
		}
		if (ChecklistGrid.IsVisible)
		{
			return true;
		}
		if (!TextContentGrid.IsVisible)
		{
			return false;
		}
		if (Model.Scenario == Scenario.QuickAdd && Model.Kind == "TEXT")
		{
			return TextContentGrid.MinHeight > 0.0;
		}
		return true;
	}

	public void TryEnlargeScrollContent()
	{
		if (string.IsNullOrEmpty(ContentText.Text))
		{
			EnlargeScrollContent(enlarge: false);
			RefreshContentHintVisibility();
		}
	}

	public void EnlargeScrollContent(bool enlarge)
	{
		ScrollContent.MinHeight = (enlarge ? 132 : 0);
		TextContentGrid.MinHeight = (enlarge ? 100 : 0);
	}

	private void OnTextKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Return && Utils.IfCtrlPressed())
		{
			string titleContent = TitleText.GetTitleContent();
			if (!string.IsNullOrEmpty(titleContent) || !string.IsNullOrEmpty(ContentText.EditBox.Text))
			{
				OnEnterText(null, titleContent);
				TitleText.FocusText();
			}
		}
		if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
		{
			DelayMuteCtrlPressed();
		}
	}

	private async Task DelayMuteCtrlPressed()
	{
		await Task.Delay(100);
		_ctrlPressed = false;
	}

	private void OnTitleCaretChanged(object sender, EventArgs e)
	{
		try
		{
			RelocateTitleLocation();
		}
		catch (Exception ex)
		{
			UtilLog.Error("RelocateTitleLocation" + ex.Message);
		}
	}

	private void RelocateTitleLocation()
	{
		TextView textView = TitleText.EditBox.TextArea.TextView;
		TextViewPosition position = new TextViewPosition(ContentText.EditBox.Document.GetLocation(TitleText.EditBox.CaretOffset));
		Point visualPosition = textView.GetVisualPosition(position, VisualYPosition.LineBottom);
		double num = InputScrollViewer.MaxHeight + InputScrollViewer.VerticalOffset;
		int num2 = 20;
		if (visualPosition.Y >= num)
		{
			InputScrollViewer.ScrollToVerticalOffset(visualPosition.Y);
		}
		else if (visualPosition.Y - (double)num2 < InputScrollViewer.VerticalOffset)
		{
			InputScrollViewer.ScrollToVerticalOffset(visualPosition.Y - (double)num2);
		}
	}

	private void OnContentCaretChanged(object sender, EventArgs e)
	{
		TextView textView = ContentText.EditBox.TextArea.TextView;
		TextViewPosition position = new TextViewPosition(ContentText.EditBox.Document.GetLocation(ContentText.EditBox.CaretOffset));
		Point visualPosition = textView.GetVisualPosition(position, VisualYPosition.LineBottom);
		double num = InputScrollViewer.MaxHeight + InputScrollViewer.VerticalOffset;
		int num2 = 18;
		double num3 = visualPosition.Y + TitleText.ActualHeight;
		if (num3 >= num)
		{
			InputScrollViewer.ScrollToVerticalOffset(num3);
		}
		else if (num3 - (double)num2 < InputScrollViewer.VerticalOffset)
		{
			InputScrollViewer.ScrollToVerticalOffset(num3 - (double)num2);
		}
	}

	private async void OnPriorityClosed(object sender, EventArgs e)
	{
		IsInOperation = false;
	}

	private void OnContentMoveUp(object sender, EventArgs e)
	{
		FocusEnd();
	}

	private void OnContentTextChanged(object sender, EventArgs e)
	{
		OnTaskContentChanged(ContentText.EditBox.Text);
	}

	private void OnTaskContentChanged(string text)
	{
		RefreshContentHintVisibility();
		Model.ContentText = text;
		RefreshHint();
		ResetSaveEnabled();
	}

	private void OnTextKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Tab)
		{
			if (_scenario == Scenario.QuickAdd)
			{
				FocusContentEnd();
			}
			else
			{
				TabSelectItem();
			}
		}
		if (e.Key == Key.V && Utils.IfCtrlPressed())
		{
			TryPasteAttachment(e);
		}
		if (e.Key == Key.Down && TitleText.Focused && !TitleText.IsQuickAddPopupOpened() && Model.UseDetailStyle)
		{
			ContentText.FocusEditBox();
			e.Handled = true;
		}
		if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
		{
			_ctrlPressed = true;
		}
	}

	public void TryPasteAttachment(System.Windows.Input.KeyEventArgs e)
	{
		try
		{
			BitmapSource pastImage = GetPastImage();
			List<string> list = ((pastImage == null) ? FileUtils.GetPastFiles() : null);
			if (Model.IsCalendar)
			{
				if (pastImage != null || (list != null && list.Any()))
				{
					e.Handled = true;
				}
			}
			else if (pastImage != null)
			{
				e.Handled = true;
				TryPasteImage(pastImage);
			}
			else if (list != null && list.Any())
			{
				e.Handled = true;
				TryPasteFile(list);
			}
		}
		catch (Exception)
		{
			Utils.Toast(Utils.GetString("FileNotFound"));
		}
	}

	public bool UseInNote()
	{
		return Model.IsNote;
	}

	private async Task<bool> TryPasteFile(IReadOnlyCollection<string> files)
	{
		if (Model.IsCalendar)
		{
			return true;
		}
		if (files == null || files.Count <= 0)
		{
			return false;
		}
		if (!(await CheckAttachmentLimit(files.Count)))
		{
			return true;
		}
		if (files.Count > 0)
		{
			foreach (string file in files)
			{
				string fileName = IOUtils.GetFileName(file);
				string path = FileUtils.SavePasteFile(file, fileName);
				try
				{
					if (File.Exists(path))
					{
						Model.AddFile(path);
					}
				}
				catch (Exception)
				{
					Utils.Toast(Utils.GetString("AddFailed"));
				}
			}
			return true;
		}
		return false;
	}

	private BitmapSource GetPastImage()
	{
		BitmapSource result = null;
		try
		{
			if (string.IsNullOrEmpty(System.Windows.Clipboard.GetText()))
			{
				result = System.Windows.Clipboard.GetImage();
			}
		}
		catch (Exception)
		{
		}
		return result;
	}

	private async Task TryPasteImage(BitmapSource image)
	{
		if (image != null && await CheckAttachmentLimit(1))
		{
			string name = Utils.GetGuid() + ".png";
			string path = ImageUtils.SavePasteImage(image, name);
			if (File.Exists(path))
			{
				Model.AddFile(path);
			}
		}
	}

	public async Task<bool> CheckAttachmentLimit(int fileNum)
	{
		fileNum += Model.Files.Count;
		Tuple<long, long> residueAttachmentCount = Utils.GetResidueAttachmentCount(fileNum);
		if (residueAttachmentCount.Item1 < fileNum)
		{
			_isInOperation = true;
			if (UserDao.IsPro())
			{
				CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("attachments_upload_limit_dialog_msg"), residueAttachmentCount.Item1, residueAttachmentCount.Item2), MessageBoxButton.OK);
				customerDialog.Owner = Window.GetWindow(this);
				customerDialog.ShowDialog();
			}
			else
			{
				ProChecker.ShowUpgradeDialog(ProType.MoreAttachments);
			}
			_isInOperation = false;
			return false;
		}
		long userLimit = Utils.GetUserLimit(Constants.LimitKind.TaskAttachmentNumber);
		if (fileNum > userLimit)
		{
			_isInOperation = true;
			CustomerDialog customerDialog2 = new CustomerDialog(Utils.GetString("LimitTips"), Utils.GetString("TaskAttachmentsLimitContent"), MessageBoxButton.OK);
			customerDialog2.Owner = Window.GetWindow(this);
			customerDialog2.ShowDialog();
			_isInOperation = false;
			return false;
		}
		return true;
	}

	private void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Escape:
			ClearTabSelect();
			break;
		case Key.Return:
			if (!Model.UseDetailStyle && Utils.IfShiftPressed())
			{
				Model.UseDetailStyle = true;
			}
			if (Model.UseDetailStyle && !ChecklistGrid.IsVisible && !IsInOperation && (Utils.IfShiftPressed() || string.IsNullOrEmpty(TitleText.EditBox.Text)) && !Utils.IfCtrlPressed())
			{
				SwitchViewCommand();
				ContentText.EditBox.Focus();
			}
			break;
		}
	}

	private async void SwitchViewCommand(bool focusContent = true)
	{
		TitleText.EditBox.LostFocus -= TaskTextLostFocus;
		if (Model.SaveHintKey == "shift_enter_add_desc")
		{
			LocalSettings.Settings.ExtraSettings.ShowTipCountType3++;
			LocalSettings.Settings.Save();
		}
		if (_needSendShiftEnter)
		{
			UserActCollectUtils.AddClickEvent("add_task", "description", "description_show");
			_needSendShiftEnter = false;
		}
		TextContentGrid.Visibility = Visibility.Visible;
		if (_scenario == Scenario.Widget)
		{
			DetailOperationPanel.Visibility = Visibility.Visible;
		}
		await Task.Delay(50);
		if (focusContent)
		{
			ContentText.FocusEditBox();
		}
		InputScrollViewer.ScrollToBottom();
		TitleText.EditBox.LostFocus += TaskTextLostFocus;
	}

	private void OnQuickCalendarSelect(object sender, string calendarId)
	{
		Model.CalendarId = calendarId;
		BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == calendarId);
		if (bindCalendarModel != null)
		{
			Model.CalendarName = bindCalendarModel.Name;
			Model.AccountId = bindCalendarModel.AccountId;
			RefreshContentQuickSetContext();
			Model.NotifyProjectChanged();
			Model.ResetHint();
		}
	}

	private void OnTokenRemoved(object sender, QuickAddToken token)
	{
		switch (token.TokenType)
		{
		case TokenType.Tag:
			Model.Tags.Remove("#" + token.Value.Replace("#", string.Empty).TrimEnd());
			Model.NotifyDisplayTagsChanged();
			break;
		case TokenType.Project:
			if (!TitleText.ExistToken(TokenType.Project))
			{
				Model.ResetProject();
			}
			break;
		case TokenType.Priority:
			if (!TitleText.ExistToken(TokenType.Priority))
			{
				Model.ResetPriority();
			}
			break;
		case TokenType.QuickDate:
			_quickSelectedDate = null;
			Model.StartDate = null;
			Model.DetailDayText = string.Empty;
			Model.ResetHint();
			break;
		case TokenType.Assignee:
			Model.Assignee = null;
			break;
		}
	}

	private void OnDateParsed(object sender, IPaserDueDate parseData)
	{
		GetValidDateTokens(parseData);
		base.Dispatcher.BeginInvoke(new Action(SetPanelLocation), DispatcherPriority.Loaded);
	}

	private void OnTitleTextChanged(object sender, string text)
	{
		RefreshHint();
		Model.DisplayTagCount = GetSelectedTags()?.Count ?? 0;
		if (Avatar != null)
		{
			Model.Assignee = Avatar.UserId;
		}
		Model.TitleText = TitleText.EditBox.Text;
		SetPanelLocation();
	}

	private void SetPanelLocation()
	{
		if (OperationPanel.IsVisible && !Model.UseDetailStyle)
		{
			TextViewPosition position = new TextViewPosition(TitleText.EditBox.Document.GetLocation(TitleText.EditBox.Text.Length));
			Point visualPosition = TitleText.EditBox.TextArea.TextView.GetVisualPosition(position, VisualYPosition.LineBottom);
			double actualWidth = OperationPanel.ActualWidth;
			double singleLineBottomThreshold = GetSingleLineBottomThreshold();
			if (visualPosition.X >= InputScrollViewer.ActualWidth - actualWidth || visualPosition.Y > singleLineBottomThreshold)
			{
				Grid.SetRow(OperationPanel, 3);
				OperationPanel.Margin = new Thickness(0.0, -4.0, 10.0, 8.0);
			}
			else
			{
				Grid.SetRow(OperationPanel, 0);
				OperationPanel.Margin = new Thickness(0.0, 6.0, 10.0, 6.0);
			}
		}
		else
		{
			Grid.SetRow(OperationPanel, 0);
			OperationPanel.Margin = new Thickness(0.0, 6.0, 10.0, 6.0);
		}
	}

	private double GetSingleLineBottomThreshold()
	{
		if (!(TryFindResource("Height24") is double num) || !(num > 0.0))
		{
			return 24.0;
		}
		return num;
	}

	private void OnQuickAssignSelect(object sender, AvatarViewModel model)
	{
		AvatarViewModel avatarViewModel = AvatarHelper.GetCacheProjectAvatars(Model.ProjectId).FirstOrDefault((AvatarViewModel avatar) => avatar.UserId == model.UserId);
		if (avatarViewModel != null)
		{
			Model.Assignee = avatarViewModel.UserId;
			Avatar = avatarViewModel;
		}
	}

	private void OnQuickDateSelect(object sender, DateTime date)
	{
		ticktick_WPF.Util.DateUtils.CheckIfTomorrowWronglySet(new TimeData
		{
			StartDate = date
		});
		TimeData timeData = TimeData.InitDefaultTime();
		_quickSelectedDate = date;
		timeData.StartDate = date;
		Model.TimeData = timeData;
	}

	private async void OnQuickProjectSelect(object sender, ProjectModel project)
	{
		if (project.id != Model.ProjectId)
		{
			Model.ProjectId = project.id;
			Model.AddToColumnId = null;
			Model.ProjectName = project.name;
			if (!Model.TaskKindSelected)
			{
				Model.IsNote = _scenario != Scenario.QuickAdd && project.IsNote;
			}
		}
		await LoadAvatars();
	}

	private bool HandleDetailEnter()
	{
		if ((ContentHint.IsVisible || !ContentText.Text.IsNullOrEmpty()) && !Utils.IfShiftPressed() && !_ctrlPressed)
		{
			FocusContentEnd();
			return true;
		}
		if (Utils.IfShiftPressed() && !_ctrlPressed)
		{
			SwitchViewCommand();
			ContentText.EditBox.Focus();
			return true;
		}
		return false;
	}

	private void FocusContentEnd()
	{
		ContentText.EditBox.Focus();
		ContentText.EditBox.CaretOffset = ContentText.EditBox.Text.Length;
	}

	private void OnQuickPrioritySelect(object sender, int priority)
	{
		Model.Priority = priority;
		Model.ManuallySelectedPriority = true;
	}

	private async void OnEnterText(object sender, string text)
	{
		if ((!Model.UseDetailStyle || !HandleDetailEnter()) && !_adding)
		{
			_adding = true;
			await OnEnterKeyUp(text);
			InputScrollViewer.ScrollToHome();
			_adding = false;
		}
	}

	private async Task OnEnterKeyUp(string text)
	{
		if (Utils.IfShiftPressed() && !Model.UseDetailStyle)
		{
			Model.UseDetailStyle = true;
		}
		if (Model.UseDetailStyle && HandleDetailEnter())
		{
			return;
		}
		if (_tabIndex == 1)
		{
			ShowSetDateDialog(showTab: true);
		}
		else if (_tabIndex == 2)
		{
			if (MoreGrid.IsVisible)
			{
				ShowMorePopup(isEnter: true, MoreGrid);
			}
			if (SetCalBorder.IsVisible)
			{
				IsInOperation = true;
				ShowSelectProjectPopup(enter: true);
			}
		}
		else
		{
			await DoAddTask(text);
		}
	}

	public virtual async Task DoAddTask(string text)
	{
		if (Model.IsCalendar)
		{
			await DoAddEvent(text);
		}
		else
		{
			if (string.IsNullOrEmpty(TitleText.EditBox.Text) && string.IsNullOrEmpty(ContentText.EditBox.Text) && Model.Files.Count == 0)
			{
				return;
			}
			if (TitleText.EditBox.Text.Length > 2048)
			{
				Utils.Toast(Utils.GetString("CharactersNumberLimit"));
				return;
			}
			string saveHintKey = Model.SaveHintKey;
			if (!(saveHintKey == "ctrl_enter_add_task"))
			{
				if (saveHintKey == "enter_add_task")
				{
					LocalSettings.Settings.ExtraSettings.ShowTipCountType2++;
					LocalSettings.Settings.Save();
				}
			}
			else
			{
				LocalSettings.Settings.ExtraSettings.ShowTipCountType1++;
				LocalSettings.Settings.Save();
			}
			if (LocalSettings.Settings.ExtraSettings.EnterTaskCount < 3)
			{
				LocalSettings.Settings.ExtraSettings.EnterTaskCount++;
				LocalSettings.Settings.Save();
			}
			Model.EnsureProjectForCreate();
			if (!string.IsNullOrEmpty(ContentText.EditBox.Text))
			{
				UserActCollectUtils.AddClickEvent("add_task", "description", "description_with_text");
			}
			if (!(await ProChecker.CheckTaskLimit(Model.ProjectId)))
			{
				List<string> selectedTags = TitleText.GetSelectedTags();
				AvatarViewModel selectedAvatar = GetSelectedAvatar();
				if (!TitleText.GetPriorityInTokens().HasValue)
				{
					Model.ResetPriority();
				}
				string assignId = ((!IsCurrentProjectShared()) ? string.Empty : (selectedAvatar?.UserId ?? Model.Assignee ?? string.Empty));
				if (await AddTask(text ?? string.Empty, selectedTags, assignId, GetPersistedDescription(ContentText)) != null)
				{
					ResetAfterAddTask();
				}
			}
		}
	}

	private async Task DoAddEvent(string text)
	{
		string eventContent = GetEventContent();
		if (string.IsNullOrEmpty(TitleText.EditBox.Text) && string.IsNullOrEmpty(eventContent))
		{
			return;
		}
		if (TitleText.EditBox.Text.Length > 2048)
		{
			Utils.Toast(Utils.GetString("CharactersNumberLimit"));
			return;
		}
		BindCalendarModel defaultCalendarForNewEvent = GetDefaultCalendarForNewEvent(Model.AccountId, Model.CalendarId);
		if (defaultCalendarForNewEvent != null)
		{
			EnsureEventTimeDefaults();
			if (await AddEvent(text ?? string.Empty, eventContent, defaultCalendarForNewEvent) != null)
			{
				Model.ClearFiles(needDelete: true);
				ResetAfterAddTask();
			}
		}
	}

	private string GetEventContent()
	{
		return TaskUtils.RemoveAttachmentMarkdown(GetEventSourceContent());
	}

	private string GetEventSourceContent()
	{
		string persistedDescription = GetPersistedDescription(ContentText);
		if (Checklist.IsVisible)
		{
			return Checklist.ToText(persistedDescription);
		}
		return persistedDescription;
	}

	private void OnNewTagSelected(object sender, string tag)
	{
		if (!Model.Tags.Contains(tag))
		{
			Model.Tags.Add(tag);
			Model.NotifyDisplayTagsChanged();
		}
	}

	public AddTaskViewModel GetTaskAddModel()
	{
		return Model;
	}

	private static string GetPersistedDescription(MarkDownEditor contentText)
	{
		return contentText?.GetPersistedText() ?? string.Empty;
	}

	public AddTaskExtraInfo GetExtra()
	{
		AddTaskExtraInfo obj = new AddTaskExtraInfo
		{
			Title = TitleText.GetTitleContent(),
			Content = GetPersistedDescription(ContentText),
			Tags = TitleText.GetSelectedTags()
		};
		AvatarViewModel selectedAvatar = GetSelectedAvatar();
		obj.AssigneeId = ((!IsCurrentProjectShared()) ? string.Empty : (selectedAvatar?.UserId ?? Model.Assignee ?? string.Empty));
		return obj;
	}

	private async Task LoadAvatars()
	{
		if (Model != null)
		{
			if (IsCurrentProjectShared())
			{
				_avatars = await AvatarHelper.GetProjectAvatars(Model.ProjectId);
				if (!_avatars.Any())
				{
					_avatars = await AvatarHelper.GetProjectAvatars(Model.ProjectId, showMyName: true, fetchRemote: true);
				}
			}
			else
			{
				_avatars = new List<AvatarViewModel>();
				ClearAssignee();
			}
		}
		else
		{
			_avatars = new List<AvatarViewModel>();
		}
		NotifyAvatarsChanged?.Invoke(null, _avatars);
	}

	public void SetAvatars(List<AvatarViewModel> avatars)
	{
		_avatars = avatars;
	}

	public List<AvatarViewModel> GetProjectAvatars()
	{
		return _avatars;
	}

	public void SetAvatar(AvatarViewModel avatar)
	{
		Model.Assignee = avatar.UserId;
		Avatar = avatar;
	}

	private void TryInitAddTag(List<string> tags)
	{
		if (tags != null && tags.Count > 0)
		{
			tags = tags.Select((string tag) => tag.Replace("#", string.Empty)).ToList();
			TitleText.InitTagTokens(tags);
		}
	}

	public void TryInitAvatar(string assignee)
	{
		AddTaskViewModel model = Model;
		if ((model == null || !model.IsCalendar) && !string.IsNullOrEmpty(assignee))
		{
			AvatarViewModel avatarViewModel = AvatarHelper.GetCacheProjectAvatars(Model.ProjectId)?.FirstOrDefault((AvatarViewModel avatar) => avatar.UserId == assignee);
			if (avatarViewModel != null)
			{
				Avatar = avatarViewModel;
				TitleText.OnTokenSelected(QuickAddToken.BuildAssignee("@" + avatarViewModel.Name), withMark: true);
				RefreshHint();
			}
		}
	}

	private void InitKanbanStyle()
	{
		DateGrid.Visibility = Visibility.Visible;
		DateGrid1.Visibility = Visibility.Visible;
		DetailDateGrid.Visibility = Visibility.Collapsed;
		DetailDateGrid1.Visibility = Visibility.Collapsed;
		AddTaskGrid.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
		Model.UseInKanban = true;
	}

	private void InitWidgetStyle()
	{
		TitleText.FontSize = 14.0;
		AddTaskGrid.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
		OperationPanel.Visibility = Visibility.Collapsed;
	}

	public async void ShowBatchAddWindow(string text)
	{
		bool closeClick = false;
		_isInOperation = true;
		CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("BatchAddTasks"), Utils.GetString("BatchAddTasksContent"), Utils.GetString("Add"), Utils.GetString("Cancel"));
		customerDialog.Owner = Window.GetWindow(this);
		customerDialog.CloseClick += delegate
		{
			closeClick = true;
		};
		bool? flag = customerDialog.ShowDialog();
		_isInOperation = false;
		if (!closeClick)
		{
			if (!flag.HasValue || !flag.Value)
			{
				text = text.Replace("\n", " ").Replace("\r", " ");
				int selectionStart = TitleText.EditBox.SelectionStart;
				string text2 = ((TitleText.EditBox.SelectionLength > 0) ? TitleText.EditBox.Text.Remove(selectionStart, TitleText.EditBox.SelectionLength) : TitleText.EditBox.Text);
				text2 = text2.Insert(selectionStart, text);
				TitleText.EditBox.Text = text2;
				TitleText.EditBox.CaretOffset = selectionStart + text.Length;
			}
			else
			{
				await BatchAddTask(text, GetPersistedDescription(ContentText));
			}
		}
	}

	private async Task BatchAddTask(string text, string content)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		string[] titles = text.Trim().Split('\n');
		if (titles.Any() && !(await ProChecker.CheckTaskLimit(Model.ProjectId, titles.Length)))
		{
			if (Model.TimeData != null)
			{
				Model.TimeData.Reminders = ((Model.TimeData.IsAllDay.HasValue && Model.TimeData.IsAllDay.Value) ? TimeData.GetDefaultAllDayReminders() : TimeData.GetDefaultTimeReminders());
			}
			List<TaskModel> list = await TaskService.BatchAddTasks(titles.ToList(), Model.ProjectId, Model.TimeData, (!Model.IsNote) ? Model.Priority : 0, TitleText.GetSelectedTags(), 0, "-1", Model.GetColumnIdForCreate(), null, Model.IsNote, Model.OriginalTimeData.StartDate, isPin: false, null, null, content);
			TaskChangeNotifier.NotifyTaskBatchAdded(list.Select((TaskModel model) => model.id).ToList());
			this.BatchTaskAdded?.Invoke(this, list);
			ResetAfterAddTask();
		}
	}

	public void InitGlobalAddWindowStyle()
	{
		base.FontSize = 18.0;
		Model.UseDetailStyle = true;
		Model.BorderThickness = new Thickness(0.0, 0.0, 0.0, 0.0);
		ContentBorderContent.BorderBrush = new SolidColorBrush(Colors.Transparent);
		ContentText.FontSize = 14.0;
		ContentHint.FontSize = 14.0;
		DetailPanelContainer.Height = 28.0;
		AddBtnContainer.Height = 28.0;
		SaveButton.Height = 28.0;
		SaveButton.FontSize = 14.0;
		SaveButton.MinWidth = 60.0;
		TextContentGrid.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		TextContentGrid.Visibility = Visibility.Visible;
		ContentHint.Visibility = Visibility.Collapsed;
	}

	private void SetDateClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		Mouse.Capture(null);
		if (_mouseDown)
		{
			_mouseDown = false;
			ShowSetDateDialog(showTab: false, sender as UIElement);
		}
	}

	private void ShowSetDateDialog(bool showTab = false, UIElement target = null)
	{
		IsInOperation = true;
		if (Model.TimeData == null)
		{
			Model.TimeData = new TimeData
			{
				IsDefault = false
			};
		}
		if (Model.IsCalendar && !Model.TimeData.DueDate.HasValue && !Model.TimeData.DueDate.HasValue)
		{
			Model.TimeData.DueDate = Model.TimeData.StartDate;
		}
		SetDateDialog dialog = SetDateDialog.GetDialog(showTab);
		dialog.ClearEventHandle();
		dialog.Save += delegate(object obj, TimeData data)
		{
			UserActCollectUtils.AddClickEvent("add_task", "action_btn", "date");
			TaskService.TryFixRepeatFlag(data);
			Model.TimeData = TimeData.Clone(data);
			Model.TimeData.IsDefault = false;
			ManualSelectedDate = true;
			if (!data.DueDate.HasValue)
			{
				ticktick_WPF.Util.DateUtils.CheckIfTomorrowWronglySet(data);
			}
			if (Model.IsCalendar && Model.TimeData.StartDate.HasValue)
			{
				TimeData timeData = Model.TimeData;
				if (timeData != null && timeData.IsAllDay.HasValue && Model.TimeData.IsAllDay.Value)
				{
					Model.TimeData.DueDate = Model.TimeData.DueDate ?? Model.TimeData.StartDate.Value;
				}
			}
		};
		dialog.Clear += delegate
		{
			if (!Model.IsCalendar)
			{
				Model.TimeData = null;
			}
			ManualSelectedDate = true;
			TitleText.ClearHighlightDate();
		};
		dialog.Hided += PopupClosed;
		bool showRemind = true;
		if (Model.IsCalendar)
		{
			showRemind = CacheManager.GetAccountCalById(Model.AccountId)?.Kind != "icloud";
		}
		dialog.Show(Model.TimeData, new SetDateDialogArgs(Model.IsCalendar, Model.IsNote, itemMode: false, target ?? (Model.UseDetailStyle ? SetDateBorder1 : SetDateBorder), -22.0, 24.0, PlacementMode.Relative, showQuickDate: true, canSkip: false, showRemind, !Model.IsCalendar));
		IsInOperation = true;
	}

	private async void LinkPopupOpened(object sender, EventArgs e)
	{
		IsInOperation = true;
	}

	private async void LinkPopupClosed(object sender, EventArgs e)
	{
		IsInOperation = false;
	}

	private async void ContentPopupOpenChanged(object sender, bool e)
	{
		if (!e)
		{
			Window window = Window.GetWindow(this);
			if (window != null && window.IsActive)
			{
				ContentText.Focus();
			}
		}
		else
		{
			ClearTabSelect();
		}
		IsInOperation = e;
	}

	private async void PopupOpenChanged(object sender, bool e)
	{
		if (!e)
		{
			Window window = Window.GetWindow(this);
			if (window != null && window.IsActive && !TitleText.FocusSuppressed)
			{
				TitleText.EditBox.Focus();
				TitleText.FocusText();
			}
		}
		else
		{
			ClearTabSelect();
		}
		IsInOperation = e;
	}

	private void PopupOpened(object sender, EventArgs e)
	{
		IsInOperation = true;
		PopupStateManager.OnViewPopupOpened();
	}

	private async void PopupClosed(object sender, EventArgs e)
	{
		Window window = Window.GetWindow(this);
		if (window != null && window.IsActive && !TitleText.FocusSuppressed && !ChecklistGrid.IsVisible)
		{
			TitleText.EditBox.Focus();
			TitleText.FocusText();
		}
		IsInOperation = false;
		PopupStateManager.OnViewPopupClosed();
	}

	internal bool CloseActionPopups()
	{
		if (IsActionPopupMouseOver())
		{
			return false;
		}
		if (!SetPriorityPopup.IsOpen && !SetTagPopup.IsOpen && !SetProjectPopup.IsOpen)
		{
			return false;
		}
		SetPriorityPopup.IsOpen = false;
		SetPriorityPopup.Child = null;
		SetTagPopup.IsOpen = false;
		SetProjectPopup.IsOpen = false;
		_projectOrGroupPopup = null;
		_projectPopupCalendarMode = null;
		_quickSelectProjectExtra = null;
		return true;
	}

	private bool IsActionPopupMouseOver()
	{
		if (!IsPopupMouseOver(SetPriorityPopup) && !IsPopupMouseOver(SetTagPopup))
		{
			return IsProjectPopupMouseOver();
		}
		return true;
	}

	private bool IsProjectPopupMouseOver()
	{
		if (!IsPopupMouseOver(SetProjectPopup))
		{
			if (SetProjectPopup.IsOpen)
			{
				return _projectOrGroupPopup?.ChildMouseOver() ?? false;
			}
			return false;
		}
		return true;
	}

	private static bool IsPopupMouseOver(Popup popup)
	{
		if (popup.IsOpen)
		{
			if (!popup.IsMouseOver)
			{
				return popup.Child?.IsMouseOver ?? false;
			}
			return true;
		}
		return false;
	}

	private void SetDetailProjectClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ShowSelectProjectPopup(enter: false, ProjectIconGrid);
	}

	private void SetProjectClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ShowSelectProjectPopup(enter: false, sender as UIElement);
	}

	public virtual void ShowSelectProjectPopup(bool enter = false, UIElement target = null, ProjectShowExtra extra = null, bool ignoreOperation = false, bool fromCommand = false)
	{
		if (!fromCommand)
		{
			_quickSelectProjectExtra = null;
		}
		if (IsInOperation && !ignoreOperation)
		{
			return;
		}
		if (Model.IsCalendar)
		{
			ShowSelectCalendarPopup(enter, target, extra, fromCommand);
			return;
		}
		ConfigureQuickSetProjectPopupFocus(fromCommand);
		_projectPopupCalendarMode = false;
		_projectOrGroupPopup = new ProjectOrGroupPopup(SetProjectPopup, new ProjectExtra
		{
			ProjectIds = (Model.SelectProject ? new List<string> { Model.ProjectId } : new List<string>())
		}, new ProjectSelectorExtra
		{
			showAll = false,
			batchMode = false,
			canSelectGroup = false,
			onlyShowPermission = true,
			ShowColumn = true,
			ColumnId = Model.GetProjectColumnId(),
			IgnoreMiniHeight = true
		});
		_projectOrGroupPopup.NoSearchResult -= OnNoSearchResult;
		_projectOrGroupPopup.NoSearchResult += OnNoSearchResult;
		SetProjectPopup.PlacementTarget = target ?? SetCalBorder;
		if (extra != null)
		{
			SetProjectPopup.HorizontalOffset = extra.Position.X - 16.0;
			SetProjectPopup.VerticalOffset = extra.Position.Y - 32.0;
			if (!string.IsNullOrEmpty(extra.FilterContent) && _projectOrGroupPopup.PerformSearch(extra.FilterContent) <= 0)
			{
				return;
			}
		}
		else
		{
			SetProjectPopup.HorizontalOffset = -18.0;
			SetProjectPopup.VerticalOffset = -5.0;
		}
		_projectOrGroupPopup.ItemSelect += delegate(object sender, SelectableItemViewModel e)
		{
			OnProjectSelect(sender, e, fromCommand);
		};
		_projectOrGroupPopup.Closed -= NotifyProjectPopupClosed;
		_projectOrGroupPopup.Closed += NotifyProjectPopupClosed;
		_projectOrGroupPopup.Show();
		if (enter)
		{
			_projectOrGroupPopup.HoverSelectFirst();
		}
		DelayNotifyInOperation();
	}

	private void ShowSelectCalendarPopup(bool enter = false, UIElement target = null, ProjectShowExtra extra = null, bool fromCommand = false)
	{
		BindCalendarModel defaultCalendarForNewEvent = GetDefaultCalendarForNewEvent(Model.AccountId, Model.CalendarId);
		if (defaultCalendarForNewEvent == null)
		{
			return;
		}
		Model.CalendarId = defaultCalendarForNewEvent.Id;
		Model.CalendarName = defaultCalendarForNewEvent.Name;
		Model.AccountId = defaultCalendarForNewEvent.AccountId;
		ConfigureQuickSetProjectPopupFocus(fromCommand);
		_projectPopupCalendarMode = true;
		_projectOrGroupPopup = new ProjectOrGroupPopup(SetProjectPopup, new ProjectExtra
		{
			SubscribeCalendars = new List<string> { Model.CalendarId }
		}, new ProjectSelectorExtra
		{
			isCalendarMode = true,
			batchMode = false,
			accountId = (Model.IsOriginalCalendar ? Model.AccountId : string.Empty)
		});
		SetProjectPopup.PlacementTarget = target ?? SetCalBorder;
		if (extra != null)
		{
			SetProjectPopup.HorizontalOffset = extra.Position.X - 16.0;
			SetProjectPopup.VerticalOffset = extra.Position.Y - 32.0;
			if (!string.IsNullOrEmpty(extra.FilterContent) && _projectOrGroupPopup.PerformSearch(extra.FilterContent) <= 0)
			{
				return;
			}
		}
		else
		{
			SetProjectPopup.HorizontalOffset = -18.0;
			SetProjectPopup.VerticalOffset = -5.0;
		}
		_projectOrGroupPopup.ItemSelect += OnCalendarSelect;
		_projectOrGroupPopup.Closed -= NotifyProjectPopupClosed;
		_projectOrGroupPopup.Closed += NotifyProjectPopupClosed;
		_projectOrGroupPopup.Show();
		if (enter)
		{
			_projectOrGroupPopup.HoverSelectFirst();
		}
		DelayNotifyInOperation();
	}

	private void OnCalendarSelect(object sender, SelectableItemViewModel model)
	{
		bool num = _quickSelectProjectExtra != null;
		int caretOffset = TitleText.EditBox.CaretOffset;
		SetProjectPopup.IsOpen = false;
		CalendarSelect(model.Title, model.Id);
		if (num)
		{
			TitleText.RemoveQuickMark(caretOffset, "^", "~");
		}
		_quickSelectProjectExtra = null;
	}

	private async void DelayNotifyInOperation()
	{
		ProjectOrGroupPopup popup = _projectOrGroupPopup;
		IsInOperation = true;
		await Task.Delay(150);
		if (IsInSetProject() && _projectOrGroupPopup == popup)
		{
			IsInOperation = true;
		}
	}

	private void NotifyProjectPopupClosed(object sender, EventArgs eventArgs)
	{
		IsInOperation = false;
	}

	private void OnNoSearchResult(object sender, EventArgs e)
	{
		if (IsQuickSetImeCompositionActive())
		{
			TitleText?.SuppressCurrentProjectQuickPopup();
			_projectPopupState.HideUntil(SetProjectPopup, IsQuickSetImeCompositionActive, delegate
			{
				IsInOperation = false;
			});
		}
		else
		{
			SetProjectPopup.IsOpen = false;
		}
	}

	private void ConfigureQuickSetProjectPopupFocus(bool keepInputFocus)
	{
		_projectPopupState.Restore(SetProjectPopup);
		SetProjectPopup.NeedFocus = !keepInputFocus;
		SetProjectPopup.RestoreFocus = !keepInputFocus;
	}

	private bool IsQuickSetImeCompositionActive()
	{
		QuickAddText titleText = TitleText;
		if (titleText == null || titleText.EditBox?.TextArea?.IsImeCompositionActive != true)
		{
			MarkDownEditor contentText = ContentText;
			if (contentText == null)
			{
				return false;
			}
			return contentText.EditBox?.TextArea?.IsImeCompositionActive == true;
		}
		return true;
	}

	public void OnProjectSelect(object sender, SelectableItemViewModel e, bool removeMark = false)
	{
		(string, string, string) projectAndColumnId = e.GetProjectAndColumnId();
		string projectId = projectAndColumnId.Item1;
		string item = projectAndColumnId.Item2;
		string item2 = projectAndColumnId.Item3;
		bool flag = e is ProjectColumnViewModel;
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (projectModel != null)
		{
			OnProjectSelect(projectModel, flag ? item2 : null, removeMark);
			Model.AddToColumnId = item;
		}
	}

	private async void OnProjectSelect(ProjectModel project, string columnName = null, bool removeMark = false)
	{
		IsInOperation = false;
		UserActCollectUtils.AddClickEvent("add_task", "action_btn", "list");
		TitleText.EditBox.Focus();
		SetProjectPopup.IsOpen = false;
		Model.ProjectId = project.id;
		Model.ProjectName = project.name ?? Utils.GetString("Inbox");
		if (!Model.TaskKindSelected)
		{
			Model.IsNote = project.IsNote;
		}
		string text = "^";
		if (_quickSelectProjectExtra != null)
		{
			text = _quickSelectProjectExtra.TriggerMark;
		}
		int caretOffset = TitleText.EditBox.CaretOffset;
		TitleText.OnTokenSelected(QuickAddToken.BuildProject(text + project.name + ((!string.IsNullOrEmpty(columnName)) ? ("/" + columnName) : string.Empty)), withMark: true);
		if (removeMark)
		{
			TitleText.RemoveQuickMark1(caretOffset, "^", "~");
		}
		_quickSelectProjectExtra = null;
		RefreshHint();
		ResetHint();
		await LoadAvatars();
	}

	public AvatarViewModel GetSelectedAvatar()
	{
		if (!IsCurrentProjectShared())
		{
			return null;
		}
		return Avatar;
	}

	protected void ResetAfterAddTask(bool keepKindConvertCache = false, bool focusAfterReset = true)
	{
		_needSendShiftEnter = true;
		ManualSelectedDate = false;
		DesktopQuickAdd desktopQuickAdd = LocalSettings.Settings.UserPreference.desktopQuickAdd;
		if (desktopQuickAdd != null)
		{
			Model.UseDetailStyle = desktopQuickAdd.style == 1 && _scenario != Scenario.Widget;
		}
		Model.Reset(_taskDefault, keepKindConvertCache);
		UpdateContentHintText();
		Model.ResetHint();
		if (_needClearTag)
		{
			Model.OriginalTags = new List<string>();
			Model.Tags = new List<string>();
		}
		_wronglyTimeSetToastShowed = false;
		Avatar = null;
		_quickSelectedDate = null;
		TitleText.ResetTokens();
		TitleText.TextChanged -= OnTitleTextChanged;
		TitleText.RemoveTextChangedEvent();
		TitleText.EditBox.Text = string.Empty;
		TitleText.AddTextChangedEvent();
		TitleText.TextChanged += OnTitleTextChanged;
		RefreshHint();
		TryInitAddTag(Model.Tags);
		TryInitAvatar(Model.Assignee);
		ContentText.EditBox.Text = string.Empty;
		if (focusAfterReset)
		{
			FocusText();
		}
	}

	public void ResetHint()
	{
		Model.ResetHint();
	}

	private async Task<TaskModel> AddTask(string title, List<string> tags, string assignId = "", string content = "")
	{
		title = title.Trim();
		TaskModel task = new TaskModel
		{
			id = Utils.GetGuid(),
			kind = ((Model.Kind == "CHECKLIST") ? "CHECKLIST" : (Model.IsNote ? "NOTE" : "TEXT")),
			title = title,
			content = content,
			priority = ((!Model.IsNote) ? Model.Priority : 0),
			projectId = Model.ProjectId,
			columnId = Model.GetColumnIdForCreate(),
			tag = TagSerializer.ToJsonContent(tags),
			assignee = assignId
		};
		TagService.CheckTagsExist(tags);
		if (Model.TimeData != null)
		{
			task.startDate = Model.TimeData.StartDate;
			task.dueDate = (Model.IsNote ? ((DateTime?)null) : Model.TimeData.DueDate);
			task.isAllDay = Model.TimeData.IsAllDay;
			task.repeatFrom = (Model.IsNote ? null : Model.TimeData.RepeatFrom);
			task.repeatFlag = (Model.IsNote ? null : Model.TimeData.RepeatFlag);
			task.annoyingAlert = Model.TimeData.AnnoyingAlert;
			task.isFloating = Model.TimeData.TimeZone.IsFloat;
			task.timeZone = Model.TimeData.TimeZone.TimeZoneName;
			if (Model.TimeData.Reminders != null && Model.TimeData.Reminders.Count > 0)
			{
				task.reminders = Model.TimeData.Reminders.ToArray();
				TaskService.PrepareTaskForSave(task);
				task.reminderCount = (task.reminders?.Length).Value;
				TaskReminderModel[] reminders = task.reminders;
				foreach (TaskReminderModel obj in reminders)
				{
					obj.id = Utils.GetGuid();
					obj.taskserverid = task.id;
					await TaskReminderDao.SaveReminders(obj);
				}
			}
		}
		if (Checklist.IsVisible)
		{
			task.desc = GetPersistedDescription(ContentText);
			List<CheckItemViewModel> items = Checklist.ChecklistItems?.ToList();
			if (items != null && (items.Count > 1 || (items.Count == 1 && !string.IsNullOrEmpty(items[0].Title))))
			{
				foreach (CheckItemViewModel item in items)
				{
					await TaskDetailItemDao.InsertChecklistItem(new TaskDetailItemModel
					{
						id = Utils.GetGuid(),
						TaskId = 0,
						TaskServerId = task.id,
						title = item.Title,
						sortOrder = item.SortOrder,
						completedTime = item.CompletedTime,
						startDate = item.StartDate,
						isAllDay = item.IsAllDay,
						snoozeReminderTime = item.SnoozeReminderTime,
						status = item.Status
					});
				}
				if (items.All((CheckItemViewModel checkItemViewModel) => checkItemViewModel.Status == 1))
				{
					task.status = 2;
					task.completedTime = DateTime.Now;
				}
				task.kind = "CHECKLIST";
			}
		}
		AvatarViewModel selectedAvatar = GetSelectedAvatar();
		task.assignee = ((selectedAvatar == null) ? string.Empty : selectedAvatar.UserId);
		if (selectedAvatar == null)
		{
			task.assignee = assignId;
		}
		task.createdTime = DateTime.Now;
		task.sortOrder = ProjectSortOrderDao.GetNewTaskSortOrderInProject(task.projectId, string.Empty);
		if (_taskDefault is ProjectIdentity projectIdentity)
		{
			await TaskSortOrderService.NewTaskAdded(task?.id, projectIdentity.CatId, projectIdentity.SortOption);
		}
		if (Model.IsPin)
		{
			task.pinnedTime = DateTime.Now.ToString(UtcDateTimeConverter.GetConverterValue(DateTime.Now));
		}
		if (Model.IsComplete)
		{
			task.status = 2;
			task.completedTime = DateTime.Now;
			task.completedUserId = LocalSettings.Settings.LoginUserId;
		}
		List<AttachmentModel> list = await AttachmentProvider.AddAttachmentModels(task.id, Model.ClearAndGetFiles());
		if (list.Count > 0)
		{
			AttachmentProvider.AppendTaskContent(task, list);
			AttachmentCache.SetTodayAttachmentCount(AttachmentCache.TodayAttachmentCount() + list.Count);
		}
		await TaskService.AddTask(task, setSortOrder: true, notify: true, this);
		this.TaskAdded?.Invoke(this, task);
		TaskUtils.TryGetTaskLinkTitle(task);
		if (_scenario != Scenario.Widget && _scenario != Scenario.QuickAdd && Model.ProjectId != Model.OriginalProjectId)
		{
			Utils.GetToastWindow()?.ToastMoveProjectControl(task.projectId, task.title, MoveToastType.Add);
		}
		return task;
	}

	private async Task<CalendarEventModel> AddEvent(string title, string content, BindCalendarModel calendar)
	{
		BindCalendarAccountModel accountCalById = CacheManager.GetAccountCalById(calendar.AccountId);
		string text = ((accountCalById?.IsCalDav() ?? false) ? Guid.NewGuid().ToString() : Utils.GetGuid());
		CalendarEventModel calEvent = new CalendarEventModel
		{
			UserId = Utils.GetCurrentUserIdInt().ToString(),
			Id = text + "@" + calendar.Id,
			Uid = text,
			Title = title.Trim(),
			Content = content,
			EventId = text,
			CalendarId = calendar.Id,
			Type = 0,
			TimeZone = (Model.TimeData?.TimeZone?.TimeZoneName ?? Utils.GetLocalTimeZone())
		};
		TimeData timeData = Model.TimeData ?? TimeData.InitDefaultDuration();
		if (!timeData.StartDate.HasValue)
		{
			timeData = TimeData.InitDefaultDuration();
		}
		calEvent.IsAllDay = timeData.IsAllDay == true;
		if (calEvent.IsAllDay)
		{
			calEvent.DueStart = timeData.StartDate.Value.Date;
			calEvent.DueEnd = (timeData.DueDate ?? timeData.StartDate.Value.Date).Date;
			if (calEvent.DueStart.Value == calEvent.DueEnd.Value)
			{
				calEvent.DueEnd = calEvent.DueEnd.Value.AddDays(1.0);
			}
		}
		else
		{
			int num = TaskDefaultDao.GetDefaultSafely()?.Duration ?? 60;
			calEvent.DueStart = timeData.StartDate.Value;
			calEvent.DueEnd = timeData.DueDate ?? timeData.StartDate.Value.AddMinutes(num);
		}
		if (accountCalById?.Kind != "icloud")
		{
			List<int> list = (from trigger in timeData.Reminders?.Select((TaskReminderModel reminder) => reminder.trigger)
				where !string.IsNullOrEmpty(trigger)
				select trigger).Select(TriggerUtils.TriggerToReminder).ToList() ?? new List<int>();
			if (list.Any())
			{
				calEvent.ReminderList = list;
				calEvent.Reminders = JsonConvert.SerializeObject(list);
			}
			else if (timeData.IsDefault)
			{
				TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
				string text2 = ((!calEvent.IsAllDay) ? defaultSafely?.TimeReminders : defaultSafely?.AllDayReminders);
				if (!string.IsNullOrEmpty(text2))
				{
					list = (calEvent.ReminderList = text2.Split(',').Select(TriggerUtils.TriggerToReminder).ToList());
					calEvent.Reminders = JsonConvert.SerializeObject(list);
				}
			}
		}
		calEvent.RepeatFlag = timeData.RepeatFlag;
		await CalendarService.AddEvent(calEvent);
		Model.CalendarId = calendar.Id;
		Model.CalendarName = calendar.Name;
		Model.AccountId = calendar.AccountId;
		Model.OriginalCalendarId = calendar.Id;
		Model.OriginalCalendarName = calendar.Name;
		Model.OriginalAccountId = calendar.AccountId;
		DataChangedNotifier.NotifyCalendarChanged(200);
		SyncManager.TryDelaySync();
		this.CalendarEventAdded?.Invoke(this, calEvent);
		return calEvent;
	}

	private void TaskTextGotFocus(object sender, RoutedEventArgs e)
	{
		if (_scenario != Scenario.QuickAdd && _scenario != Scenario.Widget && _scenario != Scenario.QuickAdd)
		{
			OperationPanel.Visibility = Visibility.Visible;
			Model.TitleFocused = true;
			Model.ResetHint();
			if (Model.UseDetailStyle && !string.IsNullOrEmpty(ContentText.Text))
			{
				TextContentGrid.Visibility = Visibility.Visible;
			}
		}
		SetPanelLocation();
		RefreshHint();
	}

	private void TaskTextLostFocus(object sender, RoutedEventArgs e)
	{
		RefreshHint();
		if (!base.IsMouseOver && !IsInOperation && !TitleText.EditBox.IsFocused && !TitleText.EditBox.TextArea.IsKeyboardFocused && !ContentText.EditBox.IsFocused && !ContentText.EditBox.TextArea.IsKeyboardFocused && _scenario != Scenario.QuickAdd)
		{
			OperationPanel.Visibility = Visibility.Collapsed;
			Model.TitleFocused = false;
			ClearTabSelect();
			Model.ResetHint();
			Model.ShowSaveHint = false;
			if (string.IsNullOrEmpty(ContentText.Text) && string.IsNullOrEmpty(TitleText.EditBox.Text))
			{
				TextContentGrid.Visibility = Visibility.Collapsed;
				if (Model.UseDetailStyle)
				{
					DesktopQuickAdd desktopQuickAdd = LocalSettings.Settings.UserPreference.desktopQuickAdd;
					if (desktopQuickAdd != null && desktopQuickAdd.style == 0)
					{
						Model.UseDetailStyle = false;
					}
				}
			}
		}
		if (TitleText.EditBox.SelectionLength > 0)
		{
			TitleText.EditBox.Select(TitleText.EditBox.SelectionStart, 0);
		}
		if (ContentText.EditBox.SelectionLength > 0)
		{
			ContentText.EditBox.Select(ContentText.EditBox.SelectionStart, 0);
		}
		if (OperationPanel.IsVisible)
		{
			return;
		}
		List<string> tags = Model.Tags;
		if (((tags == null || tags.Count <= 0) && string.IsNullOrEmpty(Model.Assignee)) || TitleText.EditBox.TextArea.IsKeyboardFocused || Model.UseDetailStyle)
		{
			return;
		}
		string text = TitleText.EditBox.Text ?? string.Empty;
		List<string> tags2 = Model.Tags;
		if (tags2 != null && tags2.Count > 0)
		{
			List<string> list = Model.Tags.ToList();
			list.Sort((string a, string b) => b.Length.CompareTo(a.Length));
			text = list.Aggregate(text, (string current, string tag) => current.Replace("#" + tag + " ", ""));
		}
		if (!string.IsNullOrEmpty(Model.Assignee))
		{
			text = text.Replace("@" + Avatar?.Name + " ", "");
		}
		if (string.IsNullOrEmpty(text))
		{
			TitleText.EditBox.Text = "";
			Model.AutoAddTags = true;
		}
	}

	protected void RefreshHint()
	{
		string text = TitleText.EditBox.Text;
		HintPanel.Visibility = ((!string.IsNullOrEmpty(text)) ? Visibility.Collapsed : Visibility.Visible);
		ResetSaveEnabled();
		CheckNeedShowEnterHint(text);
	}

	private static bool IsInputMark(string text)
	{
		return new Regex("^(/|、|#|@|~|^|!|！|\\\\*)$").Match(text).Success;
	}

	private void CheckNeedShowEnterHint(string text)
	{
		Model.ShowSaveHint = false;
		Model.SaveHint = string.Empty;
		Model.SaveTooltip = (Utils.GetString((!ContentText.Text.IsNullOrEmpty()) ? "ctrl_enter_add_task" : "enter_add_task") + "\n" + Utils.GetString("shift_enter_add_desc") + "\n" + Utils.GetString("ctrl_shift_enter_add_and_create_new")).Replace("↩\ufe0e", "↩ ");
		string text2 = string.Empty;
		if (ContentText.IsVisible)
		{
			text2 = "ctrl_enter_add_task";
		}
		else if (!string.IsNullOrEmpty(text) && !IsInputMark(text))
		{
			text2 = ((text.Length > 20) ? "shift_enter_add_desc" : "enter_add_task");
		}
		if (!string.IsNullOrEmpty(text2) && _scenario != Scenario.QuickAdd)
		{
			ExtraSettings extraSettings = LocalSettings.Settings.ExtraSettings;
			CommonSettings common = LocalSettings.Settings.Common;
			if (text2 == "ctrl_enter_add_task" && extraSettings.ShowTipCountType1 < 3)
			{
				Model.ShowSaveHint = true;
				Model.SaveHintKey = text2;
				Model.SaveHint = Utils.GetString(text2);
			}
			if (text2 == "enter_add_task" && common.FirstInstall && LocalSettings.Settings.ExtraSettings.ShowTipCountType2 < 3 && Model.UseDetailStyle)
			{
				Model.ShowSaveHint = true;
				Model.SaveHintKey = text2;
				Model.SaveHint = Utils.GetString(text2);
			}
			if (text2 == "shift_enter_add_desc" && LocalSettings.Settings.ExtraSettings.ShowTipCountType3 < 3)
			{
				Model.ShowSaveHint = true;
				Model.SaveHintKey = text2;
				Model.SaveHint = Utils.GetString(text2);
			}
		}
	}

	private void ResetSaveEnabled()
	{
		Model.SaveEnabled = !string.IsNullOrEmpty(TitleText.EditBox.Text) || !string.IsNullOrEmpty(ContentText.EditBox.Text) || Checklist.IsVisible;
	}

	public bool IsContentEmpty()
	{
		return string.IsNullOrEmpty(TitleText.EditBox.Text);
	}

	protected virtual void CalendarSelect(string title, string id)
	{
		Model.CalendarName = title;
		Model.CalendarId = id;
		BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == id);
		if (bindCalendarModel != null)
		{
			Model.AccountId = bindCalendarModel.AccountId;
		}
		RefreshContentQuickSetContext();
		Model.NotifyProjectChanged();
		Model.ResetHint();
		RefreshHint();
	}

	public void SelectCalendarFromOption(string title, string id)
	{
		CalendarSelect(title, id);
	}

	public bool CanConvertToEvent()
	{
		if (UserDao.IsPro())
		{
			return GetDefaultCalendarForNewEvent(Model.AccountId, Model.CalendarId) != null;
		}
		return false;
	}

	private BindCalendarModel GetDefaultCalendarForNewEvent(string accountId = null, string calendarId = null)
	{
		Dictionary<string, BindCalendarAccountModel> accounts = (from account in CacheManager.GetBindCalendarAccounts()
			where account != null && !account.Expired && !string.IsNullOrEmpty(account.Id)
			select account).ToDictionary((BindCalendarAccountModel account) => account.Id, (BindCalendarAccountModel account) => account);
		List<BindCalendarModel> source = (from calendar in CacheManager.GetBindCalendars()
			where calendar != null && !string.IsNullOrEmpty(calendar.AccountId) && accounts.ContainsKey(calendar.AccountId) && calendar.Accessible
			select calendar).ToList();
		if (!source.Any())
		{
			return null;
		}
		if (!string.IsNullOrEmpty(calendarId))
		{
			BindCalendarModel bindCalendarModel = source.FirstOrDefault((BindCalendarModel calendar) => calendar.Id == calendarId);
			if (bindCalendarModel != null)
			{
				return bindCalendarModel;
			}
		}
		if (!string.IsNullOrEmpty(accountId))
		{
			BindCalendarModel bindCalendarModel2 = (from calendar in source
				where calendar.AccountId == accountId
				orderby calendar.Show == "hidden", calendar.sortOrder
				select calendar).FirstOrDefault();
			if (bindCalendarModel2 != null)
			{
				return bindCalendarModel2;
			}
		}
		return (from calendar in source
			orderby calendar.Show == "hidden", calendar.sortOrder
			select calendar).First();
	}

	public void OnAssigneeSelect(AvatarInfo model)
	{
		UserActCollectUtils.AddClickEvent("add_task", "action_btn", "assign");
		TitleText.EditBox.Focus();
		AvatarViewModel avatarViewModel = AvatarHelper.GetCacheProjectAvatars(Model.ProjectId).FirstOrDefault((AvatarViewModel avatar) => avatar.UserId == model.UserId);
		if (avatarViewModel != null)
		{
			Model.Assignee = avatarViewModel.UserId;
			Avatar = avatarViewModel;
			TitleText.OnTokenSelected(QuickAddToken.BuildAssignee("@" + avatarViewModel.Name), withMark: true);
		}
		else
		{
			Model.Assignee = null;
			Avatar = null;
			TitleText.RemoveTokenByType(TokenType.Assignee);
		}
		TitleText.FocusText();
	}

	public void OnTagAdded(string tag)
	{
		if (tag.StartsWith("#") || tag.StartsWith("＃"))
		{
			tag = tag.Substring(1);
		}
		if (Model.Tags.Contains(tag))
		{
			Model.Tags.Add(tag);
		}
		Model.NotifyDisplayTagsChanged();
	}

	public async void OnTagsAdded(TagSelectData data)
	{
		UserActCollectUtils.AddClickEvent("add_task", "action_btn", "tag");
		TitleText.OnTagsAdded(data);
		if (!OperationPopup.IsOpen)
		{
			TitleText.FocusText();
		}
		else
		{
			Window.GetWindow(this)?.Activate();
			OperationPopup.IsOpen = false;
		}
		Model.Tags = GetSelectedTags();
		Model.NotifyDisplayTagsChanged();
	}

	public void FocusEnd()
	{
		TitleText.FocusText(focusEnd: true);
	}

	public async Task<bool> FocusEndAsync()
	{
		TitleText.AllowFocus();
		await base.Dispatcher.InvokeAsync(delegate
		{
		}, DispatcherPriority.Loaded);
		if (TitleText.FocusSuppressed || !TitleText.IsVisible)
		{
			return false;
		}
		TextArea textArea = TitleText.EditBox.TextArea;
		Keyboard.Focus(TitleText.EditBox);
		Keyboard.Focus(textArea);
		if (!textArea.IsKeyboardFocused)
		{
			await base.Dispatcher.InvokeAsync(delegate
			{
			}, DispatcherPriority.Input);
			Keyboard.Focus(TitleText.EditBox);
			Keyboard.Focus(textArea);
		}
		if (textArea.IsKeyboardFocused)
		{
			TitleText.EditBox.CaretOffset = TitleText.EditBox.Text.Length;
			return true;
		}
		return false;
	}

	private void GetValidDateTokens(IPaserDueDate parsedData)
	{
		if (ManualSelectedDate)
		{
			return;
		}
		string text = TitleText.EditBox.Text;
		if (parsedData?.GetRecognizeStrings() != null && parsedData.IsTimeSeted() && parsedData.GetRecognizeStrings().Count > 0)
		{
			Model.TimeData = parsedData.ToTimeData(!string.IsNullOrEmpty(text) && text.Contains(Utils.GetString("reminder")));
			if (Model.IsNote)
			{
				Model.TimeData.DueDate = null;
			}
			if (!_wronglyTimeSetToastShowed)
			{
				ticktick_WPF.Util.DateUtils.CheckIfTomorrowWronglySet(Model.TimeData);
				_wronglyTimeSetToastShowed = true;
			}
		}
		else if (!Utils.IsEmptyDate(_quickSelectedDate))
		{
			TimeData timeData = TimeData.InitDefaultTime();
			timeData.StartDate = _quickSelectedDate;
			Model.TimeData = timeData;
		}
		else
		{
			Model.ResetTimeData(_taskDefault);
		}
	}

	public void FocusText()
	{
		if (!TitleText.FocusSuppressed)
		{
			TitleText.EditBox.Focus();
		}
	}

	public async void SetPriority(int priority)
	{
		Model.Priority = priority;
		Model.ManuallySelectedPriority = true;
		TitleText.OnTokenSelected(QuickAddToken.BuildPriority("!" + Utils.GetPriorityName(priority)), withMark: true, handleText: true, closePopup: true, forceFocus: false);
		RefreshHint();
	}

	public void SetDate(string key)
	{
		DateTime value = DateTime.Today;
		switch (key)
		{
		case "today":
			value = DateTime.Today;
			break;
		case "tomorrow":
			value = DateTime.Today.AddDays(1.0);
			break;
		case "nextweek":
			value = DateTime.Today.AddDays(7.0);
			break;
		}
		ManualSelectedDate = true;
		TimeData timeData = TimeData.InitDefaultTime();
		timeData.StartDate = value;
		Model.TimeData = timeData;
	}

	public void ClearDate()
	{
		ManualSelectedDate = true;
		Model.TimeData = null;
	}

	public void SelectDate()
	{
		ShowSetDateDialog();
	}

	private void OnMoreClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		Mouse.Capture(null);
		if (_mouseDown)
		{
			_mouseDown = false;
			ShowMorePopup(isEnter: false, sender as UIElement, !(sender is ContentControl));
		}
	}

	private void ShowMorePopup(bool isEnter, UIElement target, bool showEntity = true)
	{
		OperationPopup.Closed -= PopupClosed;
		OperationPopup.Closed += PopupClosed;
		OperationPopup.Opened -= PopupOpened;
		OperationPopup.Opened += PopupOpened;
		OperationPopup.PlacementTarget = target;
		OperationPopup.HorizontalOffset = -18.0;
		if (!TitleText.GetPriorityInTokens().HasValue)
		{
			Model.ResetPriority();
		}
		AddOptionDialog addOptionDialog = new AddOptionDialog();
		addOptionDialog.Init(Model, isEnter, showEntity, _scenario != Scenario.QuickAdd, Model.Kind, CanConvertToEvent());
		addOptionDialog.QuickAddView = this;
		addOptionDialog.AttachmentClick -= AddAttachment;
		addOptionDialog.AttachmentClick += AddAttachment;
		addOptionDialog.SwitchNoteClick -= SwitchNote;
		addOptionDialog.SwitchNoteClick += SwitchNote;
		addOptionDialog.SwitchTaskClick -= SwitchTask;
		addOptionDialog.SwitchTaskClick += SwitchTask;
		addOptionDialog.SwitchEventClick -= SwitchEvent;
		addOptionDialog.SwitchEventClick += SwitchEvent;
		addOptionDialog.SwitchChecklistClick -= SwitchChecklist;
		addOptionDialog.SwitchChecklistClick += SwitchChecklist;
		addOptionDialog.AICompleteClick -= ShowAIComplete;
		addOptionDialog.AICompleteClick += ShowAIComplete;
		OperationPopup.Child = addOptionDialog;
		OperationPopup.IsOpen = true;
		this.HideGuide?.Invoke(this, null);
	}

	private void ShowAIComplete(object sender, EventArgs e)
	{
		OperationPopup.IsOpen = false;
		if (!UserDao.IsPro())
		{
			ProChecker.ShowUpgradeDialog(ProType.AICompleteSuggest);
			return;
		}
		string text = TitleText.EditBox.Text;
		if (string.IsNullOrEmpty(text))
		{
			Utils.Toast(Utils.GetString("请输入标题"));
			return;
		}
		string persistedDescription = GetPersistedDescription(ContentText);
		List<string> selectedTags = TitleText.GetSelectedTags();
		AvatarViewModel selectedAvatar = GetSelectedAvatar();
		string assignee = ((selectedAvatar == null) ? string.Empty : selectedAvatar.UserId);
		TaskModel taskModel = new TaskModel
		{
			id = Utils.GetGuid(),
			kind = ((Model.Kind == "CHECKLIST") ? "CHECKLIST" : (Model.IsNote ? "NOTE" : "TEXT")),
			title = text,
			content = persistedDescription,
			priority = ((!Model.IsNote) ? Model.Priority : 0),
			projectId = Model.ProjectId,
			columnId = Model.GetColumnIdForCreate(),
			tag = TagSerializer.ToJsonContent(selectedTags),
			assignee = assignee
		};
		if (Model.TimeData != null)
		{
			taskModel.startDate = Model.TimeData.StartDate;
			taskModel.dueDate = (Model.IsNote ? ((DateTime?)null) : Model.TimeData.DueDate);
			taskModel.isAllDay = Model.TimeData.IsAllDay;
			taskModel.repeatFrom = (Model.IsNote ? null : Model.TimeData.RepeatFrom);
			taskModel.repeatFlag = (Model.IsNote ? null : Model.TimeData.RepeatFlag);
			taskModel.isFloating = Model.TimeData.TimeZone.IsFloat;
			taskModel.timeZone = Model.TimeData.TimeZone.TimeZoneName;
		}
		TaskBaseViewModel task = new TaskBaseViewModel(taskModel);
		if (AICompleteWindow.Window == null)
		{
			AICompleteWindow.Window = new AICompleteWindow(task)
			{
				Owner = App.Window
			};
			AICompleteWindow.Window.Show();
		}
		else
		{
			Utils.Toast(Utils.GetString("AIAnotherTaskGenerating"));
		}
	}

	private void SwitchChecklist(object sender, EventArgs e)
	{
		OperationPopup.IsOpen = false;
		SwitchToTextOrList();
	}

	private void SwitchToTextOrList()
	{
		Model.Kind = ((Model.Kind == "TEXT") ? "CHECKLIST" : "TEXT");
		string kind = Model.Kind;
		if (!(kind == "TEXT"))
		{
			if (kind == "CHECKLIST")
			{
				SwitchToList();
			}
		}
		else
		{
			SwitchToText();
		}
	}

	private void SwitchToText()
	{
		TextContentGrid.Visibility = Visibility.Visible;
		ChecklistGrid.Visibility = Visibility.Collapsed;
		string text = Checklist.ToText(GetPersistedDescription(ContentText));
		ContentText.Text = text;
		Model.Kind = "TEXT";
		EnlargeScrollContent(enlarge: true);
		RefreshContentHintVisibility();
		ContentText.EditBox.Focus();
	}

	private void EnsureTextContentMode()
	{
		if (ChecklistGrid.IsVisible)
		{
			SwitchToText();
		}
		else
		{
			Model.Kind = "TEXT";
		}
	}

	private bool HasDetailContent()
	{
		return !string.IsNullOrWhiteSpace(ContentText.Text);
	}

	private bool IsDetailContentExpanded()
	{
		return TextContentGrid.IsVisible;
	}

	private void EnterTemporaryDetailMode(bool expandWhenEmpty = true, bool keepDetailLayout = true)
	{
		bool flag = IsDetailContentExpanded();
		Model.AllowUseDetailMode = true;
		Model.UseDetailStyle = keepDetailLayout;
		if (expandWhenEmpty || HasDetailContent() || flag)
		{
			SwitchViewCommand(focusContent: false);
		}
		else
		{
			TextContentGrid.Visibility = Visibility.Collapsed;
		}
	}

	private void SwitchNote(object sender, EventArgs e)
	{
		OperationPopup.IsOpen = false;
		SwitchTaskOrNote(isNote: true);
	}

	private void SwitchTask(object sender, EventArgs e)
	{
		OperationPopup.IsOpen = false;
		SwitchTaskOrNote(isNote: false);
	}

	private void SwitchTaskOrNote(bool isNote, bool keepKindConvertCache = false)
	{
		bool isCalendar = Model.IsCalendar;
		bool isNote2 = Model.IsNote;
		bool num = IsDetailContentExpanded();
		if (!keepKindConvertCache && isCalendar)
		{
			Model.CacheTaskFieldsBeforeKindConvert(keepExisting: true, null, null, null, null, TitleText.GetProjectTokenValue());
		}
		if (!keepKindConvertCache && !isCalendar && !isNote2 && isNote)
		{
			Model.CacheTaskFieldsBeforeKindConvert(keepExisting: false, null, null, null, null, TitleText.GetProjectTokenValue());
		}
		if (isNote)
		{
			EnsureTextContentMode();
			Model.ClearRepeatForNote();
		}
		Model.IsCalendar = false;
		Model.IsNote = isNote;
		Model.TaskKindSelected = true;
		RefreshContentQuickSetContext();
		bool keepDetailLayout = true;
		bool expandWhenEmpty = num;
		EnterTemporaryDetailMode(expandWhenEmpty, keepDetailLayout);
		TitleText.RemoveTokenByType(TokenType.Priority);
		if (isCalendar)
		{
			if (isNote)
			{
				Model.RestoreFilesAfterKindConvert(includeTags: true);
			}
			else
			{
				Model.RestoreTaskFieldsAfterKindConvert(includeTags: true);
				RestorePriorityToken();
			}
			Model.EnsureTaskProjectAfterCalendarConvert(Model.IsOriginalCalendar);
			RestoreProjectToken();
			RestoreAssigneeToken();
			if (!keepKindConvertCache)
			{
				TitleText.OnTagsAdded(new TagSelectData
				{
					OmniSelectTags = (Model.Tags?.ToList() ?? new List<string>())
				});
				Model.Tags = GetSelectedTags();
				Model.DisplayTagCount = Model.Tags.Count;
			}
		}
		else if (isNote2 && !isNote)
		{
			Model.RestoreTaskFieldsAfterKindConvert(includeTags: false, includeSharedFields: false);
			RestorePriorityToken();
		}
		Model.IsCalendar = false;
		Model.IsNote = isNote;
		Model.TaskKindSelected = true;
		RefreshContentQuickSetContext();
		ResetNonCalendarTimeZoneWhenDisabled();
		UpdateContentHintText();
		Model.RefreshDayText(Model.TimeData);
		Model.ResetHint();
		SetPanelLocation();
		RefreshProjectPopupAfterKindSwitch();
	}

	protected void ResetNonCalendarTimeZoneWhenDisabled()
	{
		if (!Model.IsCalendar && !LocalSettings.Settings.EnableTimeZone && Model.TimeData != null)
		{
			Model.TimeData.TimeZone = TimeZoneData.LocalTimeZoneModel;
		}
	}

	private void RestorePriorityToken()
	{
		if (Model.Priority != 0)
		{
			TitleText.AddTokenWithoutParsing(QuickAddToken.BuildPriority("!" + Utils.GetPriorityName(Model.Priority)));
		}
	}

	private void RestoreProjectToken()
	{
		string kindConvertProjectTokenValue = Model.KindConvertProjectTokenValue;
		if (!string.IsNullOrEmpty(kindConvertProjectTokenValue) && string.Equals(Model.KindConvertProjectId, Model.ProjectId, StringComparison.Ordinal))
		{
			string text = ((!string.IsNullOrEmpty(Model.KindConvertAddToColumnId)) ? Model.KindConvertAddToColumnId : Model.KindConvertColumnId);
			string b = ((!string.IsNullOrEmpty(Model.AddToColumnId)) ? Model.AddToColumnId : Model.ColumnId);
			if (!ProjectTokenHasColumn(kindConvertProjectTokenValue) || string.IsNullOrEmpty(text) || string.Equals(text, b, StringComparison.Ordinal))
			{
				TitleText.AddTokenWithoutParsing(QuickAddToken.BuildProject(kindConvertProjectTokenValue));
			}
		}
	}

	private static bool ProjectTokenHasColumn(string tokenValue)
	{
		if (string.IsNullOrEmpty(tokenValue))
		{
			return false;
		}
		string text = tokenValue.Trim();
		if (text.StartsWith("^") || text.StartsWith("~"))
		{
			text = text.Substring(1);
		}
		return text.Contains("/");
	}

	private void RestoreAssigneeToken()
	{
		if (!string.IsNullOrEmpty(Model.Assignee))
		{
			AvatarViewModel avatarViewModel = AvatarHelper.GetCacheProjectAvatars(Model.ProjectId)?.FirstOrDefault((AvatarViewModel avatar) => avatar.UserId == Model.Assignee);
			if (avatarViewModel != null)
			{
				Avatar = avatarViewModel;
				TitleText.AddTokenWithoutParsing(QuickAddToken.BuildAssignee("@" + avatarViewModel.Name));
			}
		}
	}

	private void SwitchEvent(object sender, EventArgs e)
	{
		OperationPopup.IsOpen = false;
		if (!CanConvertToEvent())
		{
			return;
		}
		if (!Model.IsCalendar)
		{
			Model.RestoreCalendarFieldsAfterKindConvert();
		}
		string kindConvertLocation = Model.KindConvertLocation;
		BindCalendarModel defaultCalendarForNewEvent = GetDefaultCalendarForNewEvent(Model.AccountId, Model.CalendarId);
		if (defaultCalendarForNewEvent != null)
		{
			bool expandWhenEmpty = IsDetailContentExpanded();
			if (!Model.IsCalendar)
			{
				Model.Tags = GetSelectedTags();
				Model.CacheTaskFieldsBeforeKindConvert(Model.IsNote, null, null, null, kindConvertLocation, TitleText.GetProjectTokenValue());
			}
			EnsureEventTimeDefaults();
			Model.RestoreRepeatForCalendar();
			ClearCalendarUnsupportedRepeat(Model.TimeData);
			EnsureTextContentMode();
			Model.IsCalendar = true;
			Model.IsNote = false;
			Model.CalendarId = defaultCalendarForNewEvent.Id;
			Model.CalendarName = defaultCalendarForNewEvent.Name;
			Model.AccountId = defaultCalendarForNewEvent.AccountId;
			Model.TaskKindSelected = true;
			RefreshContentQuickSetContext();
			TitleText.RemoveTokenByType(TokenType.Priority);
			TitleText.RemoveTokenByType(TokenType.Project);
			TitleText.RemoveTokenByType(TokenType.Tag);
			ClearAssignee();
			Model.Tags = new List<string>();
			Model.DisplayTagCount = 0;
			Model.ClearFiles(needDelete: false);
			UpdateContentHintText();
			Model.NotifyProjectChanged();
			Model.RefreshDayText(Model.TimeData);
			Model.ResetHint();
			SetPanelLocation();
			EnterTemporaryDetailMode(expandWhenEmpty);
			RefreshProjectPopupAfterKindSwitch();
		}
	}

	private void EnsureEventTimeDefaults()
	{
		if (Model.TimeData == null || !Model.TimeData.StartDate.HasValue)
		{
			Model.TimeData = TimeData.InitDefaultDuration();
		}
		if (!Model.TimeData.IsAllDay.HasValue)
		{
			Model.TimeData.IsAllDay = false;
		}
		if (Model.TimeData.StartDate.HasValue && Model.TimeData.IsAllDay == false && !Model.TimeData.DueDate.HasValue)
		{
			int num = TaskDefaultDao.GetDefaultSafely()?.Duration ?? 60;
			Model.TimeData.DueDate = Model.TimeData.StartDate.Value.AddMinutes(num);
		}
		if (Model.TimeData.StartDate.HasValue)
		{
			Model.TimeData.IsDefault = false;
			Model.StartDate = Model.TimeData.StartDate;
		}
	}

	private static void ClearCalendarUnsupportedRepeat(TimeData timeData)
	{
		if (timeData != null && !string.IsNullOrWhiteSpace(timeData.RepeatFlag) && !RepeatUtils.IsStandardRRule(timeData.RepeatFlag))
		{
			timeData.RepeatFlag = null;
			timeData.RepeatFrom = "2";
			timeData.ExDates = new List<string>();
		}
	}

	private CalendarDetailViewModel BuildCalendarDetailModel(BindCalendarModel calendar)
	{
		string text = GetEventSourceContent() ?? string.Empty;
		string text2 = TaskUtils.RemoveAttachmentMarkdown(text);
		Model.CacheAttachmentContentForCalendar(text, text2);
		CalendarDetailViewModel calendarDetailViewModel = new CalendarDetailViewModel(calendar)
		{
			Title = (TitleText.EditBox.Text ?? string.Empty),
			Content = text2,
			Location = (Model.KindConvertLocation ?? string.Empty),
			ShowQuickAddConvertOptions = true
		};
		TimeData timeData = Model.TimeData ?? TimeData.InitDefaultDuration();
		if (!timeData.StartDate.HasValue)
		{
			timeData = TimeData.InitDefaultDuration();
		}
		calendarDetailViewModel.IsAllDay = timeData.IsAllDay == true;
		if (calendarDetailViewModel.IsAllDay)
		{
			calendarDetailViewModel.StartDate = timeData.StartDate.Value.Date;
			calendarDetailViewModel.DueDate = (timeData.DueDate ?? timeData.StartDate.Value.Date).Date;
			if (calendarDetailViewModel.StartDate == calendarDetailViewModel.DueDate)
			{
				calendarDetailViewModel.DueDate = calendarDetailViewModel.DueDate.Value.AddDays(1.0);
			}
		}
		else
		{
			int num = TaskDefaultDao.GetDefaultSafely()?.Duration ?? 60;
			calendarDetailViewModel.StartDate = timeData.StartDate.Value;
			calendarDetailViewModel.DueDate = timeData.DueDate ?? timeData.StartDate.Value.AddMinutes(num);
		}
		calendarDetailViewModel.RepeatFlag = timeData.RepeatFlag;
		calendarDetailViewModel.RepeatText = RRuleUtils.RRule2String(string.Empty, calendarDetailViewModel.RepeatFlag, calendarDetailViewModel.StartDate);
		calendarDetailViewModel.TimeZone = timeData.TimeZone?.TimeZoneName ?? Utils.GetLocalTimeZone();
		calendarDetailViewModel.Reminders = GetEventReminderMinutes(timeData, calendarDetailViewModel.IsAllDay, calendar.AccountId);
		return calendarDetailViewModel;
	}

	private List<int> GetEventReminderMinutes(TimeData timeData, bool isAllDay, string accountId)
	{
		if (CacheManager.GetAccountCalById(accountId)?.Kind == "icloud")
		{
			return new List<int>();
		}
		List<int> list = (from trigger in timeData.Reminders?.Select((TaskReminderModel reminder) => reminder.trigger)
			where !string.IsNullOrEmpty(trigger)
			select trigger).Select(TriggerUtils.TriggerToReminder).ToList() ?? new List<int>();
		if (list.Any())
		{
			return list;
		}
		if (timeData.IsDefault)
		{
			TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
			string text = ((!isAllDay) ? defaultSafely?.TimeReminders : defaultSafely?.AllDayReminders);
			if (!string.IsNullOrEmpty(text))
			{
				return text.Split(',').Select(TriggerUtils.TriggerToReminder).ToList();
			}
		}
		return new List<int>();
	}

	private void OpenCalendarDetailWindow(CalendarDetailViewModel detailModel)
	{
		Window window = Window.GetWindow(this);
		Window window2 = window?.Owner ?? System.Windows.Application.Current?.MainWindow ?? window;
		UIElement target = (UIElement)(((object)window2) ?? ((object)this));
		double num = ((window2 != null && window2.ActualWidth > 0.0) ? window2.ActualWidth : SystemParameters.WorkArea.Width);
		double num2 = ((window2 != null && window2.ActualHeight > 0.0) ? window2.ActualHeight : SystemParameters.WorkArea.Height);
		Point point = new Point(Math.Max(0.0, (num - 400.0) / 2.0), Math.Max(0.0, (num2 - 450.0) / 2.0));
		CalendarDetailWindow window3 = new CalendarDetailWindow
		{
			StaysOpen = true
		};
		window3.EventCreated += OnCalendarDetailEventCreated;
		window3.ConvertNewEventToTaskKind += OnCalendarDetailConvertToTaskKind;
		window3.Show(target, num, 0.0, byMouse: false, detailModel, point);
		window3.Dispatcher.BeginInvoke((Action)delegate
		{
			if (window3.IsOpen)
			{
				HwndHelper.SetFocus(window3, getKeyFocus: true);
				window3.FocusTitleEnd();
			}
		}, DispatcherPriority.Input);
		ReleaseCalendarDetailInitialStaysOpen(window3);
	}

	private async void ReleaseCalendarDetailInitialStaysOpen(CalendarDetailWindow window)
	{
		await Task.Delay(300);
		if (window?.IsOpen ?? false)
		{
			window.StaysOpen = false;
		}
	}

	private void OnCalendarDetailEventCreated(object sender, CalendarEventModel e)
	{
		this.CalendarEventAdded?.Invoke(this, e);
	}

	private void OnCalendarDetailConvertToTaskKind(object sender, CalendarDetailConvertEventArgs e)
	{
		if (e?.Model != null)
		{
			RestoreFromCalendarDetail(e.Model, e.IsNote, e.ManualTimeSelected);
		}
	}

	private void RestoreFromCalendarDetail(CalendarDetailViewModel detailModel, bool isNote, bool manualTimeSelected)
	{
		TitleText.AllowFocus();
		ManualSelectedDate |= manualTimeSelected;
		BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel item) => item != null && item.Id == detailModel.CalendarId);
		Model.CacheTaskFieldsBeforeKindConvert(keepExisting: true, detailModel.CalendarId, detailModel.CalendarName, bindCalendarModel?.AccountId, detailModel.Location, TitleText.GetProjectTokenValue());
		SwitchTaskOrNote(isNote, keepKindConvertCache: true);
		string text = detailModel.Title ?? string.Empty;
		string text2 = Model.RestoreAttachmentMarkdownAfterKindConvert(detailModel.Content ?? string.Empty);
		TitleText.SetTextWithoutTokenParsing(text, resetTokens: true);
		ContentText.EditBox.Text = text2;
		Model.ContentText = text2;
		TextContentGrid.Visibility = ((string.IsNullOrEmpty(text2) && !Model.UseDetailStyle) ? Visibility.Collapsed : Visibility.Visible);
		RefreshContentHintVisibility();
		Model.TimeData = BuildTimeDataFromCalendarDetail(detailModel);
		if (isNote)
		{
			Model.ClearRepeatForNote();
		}
		if (!isNote)
		{
			Model.RestoreTaskFieldsAfterKindConvert(includeTags: true);
			Model.EnsureTaskProjectAfterCalendarConvert(Model.IsOriginalCalendar);
			RestoreProjectToken();
			RestorePriorityToken();
			RestoreAssigneeToken();
		}
		else
		{
			Model.RestoreFilesAfterKindConvert(includeTags: true);
			Model.EnsureTaskProjectAfterCalendarConvert(Model.IsOriginalCalendar);
			RestoreProjectToken();
			RestoreAssigneeToken();
		}
		TitleText.OnTagsAdded(new TagSelectData
		{
			OmniSelectTags = (Model.Tags?.ToList() ?? new List<string>())
		});
		Model.Tags = GetSelectedTags();
		Model.DisplayTagCount = Model.Tags.Count;
		Model.IsCalendar = false;
		Model.IsNote = isNote;
		Model.TaskKindSelected = true;
		ResetNonCalendarTimeZoneWhenDisabled();
		Model.RefreshDayText(Model.TimeData);
		Model.ResetHint();
		RefreshHint();
		SetPanelLocation();
		EventHandler eventHandler = this.CalendarDetailRestored;
		if (eventHandler != null)
		{
			eventHandler(this, EventArgs.Empty);
		}
		else
		{
			FocusText();
		}
	}

	private TimeData BuildTimeDataFromCalendarDetail(CalendarDetailViewModel detailModel)
	{
		if (!detailModel.StartDate.HasValue)
		{
			return null;
		}
		return new TimeData
		{
			StartDate = detailModel.StartDate,
			DueDate = detailModel.DueDate,
			IsAllDay = detailModel.IsAllDay,
			RepeatFlag = detailModel.RepeatFlag,
			IsDefault = false,
			TimeZone = new TimeZoneViewModel(isFloat: false, detailModel.TimeZone ?? Utils.GetLocalTimeZone()),
			Reminders = (detailModel.Reminders?.Select((int minute) => new TaskReminderModel
			{
				trigger = TriggerUtils.ReminderToTrigger(minute)
			}).ToList() ?? new List<TaskReminderModel>())
		};
	}

	private async void AddAttachment(object sender, EventArgs e)
	{
		if (Model.IsCalendar)
		{
			return;
		}
		if (sender != null)
		{
			UserActCollectUtils.AddClickEvent("add_task", "action_btn", "file");
		}
		try
		{
			IsInOperation = true;
			await PerformAddAttachment();
		}
		finally
		{
			IsInOperation = false;
		}
	}

	private async Task PerformAddAttachment()
	{
		OperationPopup.IsOpen = false;
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Multiselect = UserDao.IsPro()
		};
		if (openFileDialog.ShowDialog() != DialogResult.OK)
		{
			return;
		}
		int fileNum = openFileDialog.FileNames.Cast<object>().Count((object item) => File.Exists(item.ToString()));
		if (!(await CheckAttachmentLimit(fileNum)))
		{
			return;
		}
		string[] fileNames = openFileDialog.FileNames;
		foreach (string text in fileNames)
		{
			if (!File.Exists(text))
			{
				continue;
			}
			string fileName = text.Split('\\').Last();
			fileName = FileUtils.TrimFileName(fileName, 240 - AppPaths.ImageDir.Length);
			string text2 = AppPaths.ImageDir + fileName;
			if (File.Exists(text2))
			{
				int num2 = fileName.LastIndexOf('.');
				if (num2 < 0)
				{
					num2 = fileName.Length;
				}
				int num3 = 1;
				do
				{
					num3++;
					text2 = AppPaths.ImageDir + fileName.Insert(num2, $"({num3})");
				}
				while (File.Exists(text2));
			}
			try
			{
				File.Copy(text, text2);
				Model.AddFile(text2);
			}
			catch (Exception ex)
			{
				UtilLog.Warn(ExceptionUtils.BuildExceptionMessage(ex));
			}
		}
	}

	public void NotifyAddTask(TaskModel task)
	{
		if (task != null)
		{
			ResetAfterAddTask();
			RefreshHint();
			this.TaskAdded?.Invoke(this, task);
		}
	}

	public async Task HideOptionDialog(bool stopOperate = false)
	{
		if (stopOperate)
		{
			IsInOperation = false;
		}
		OperationPopup.Closed -= PopupClosed;
		OperationPopup.IsOpen = false;
		PopupStateManager.OnViewPopupClosed();
		await Task.Delay(10);
		if (!TitleText.FocusSuppressed)
		{
			TitleText.EditBox.Focus();
			TitleText.FocusText();
		}
	}

	public void OnAddTask(TaskModel task)
	{
		this.TaskAdded?.Invoke(this, task);
	}

	public List<string> GetSelectedTags()
	{
		return TitleText.GetSelectedTags();
	}

	private void TitleTextOnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		e.Handled = true;
	}

	private void OnAddViewMouseLeftUp(object sender, MouseButtonEventArgs e)
	{
	}

	public bool TabSelectItem()
	{
		if (OperationPopup.IsOpen || IsInOperation)
		{
			return false;
		}
		if (!TitleText.Focused)
		{
			TitleText.FocusText(focusEnd: true);
			ClearTabSelect();
		}
		else
		{
			_tabIndex += 3 + ((!Utils.IfShiftPressed()) ? 1 : (-1));
			_tabIndex %= 3;
			switch (_tabIndex)
			{
			case 0:
				ClearTabSelect();
				break;
			case 1:
				SetDateBorder.SetResourceReference(System.Windows.Controls.Control.BorderBrushProperty, "PrimaryColor");
				SetCalBorder.BorderBrush = Brushes.Transparent;
				MoreGrid.BorderBrush = Brushes.Transparent;
				break;
			case 2:
				SetDateBorder.BorderBrush = Brushes.Transparent;
				SetCalBorder.SetResourceReference(System.Windows.Controls.Control.BorderBrushProperty, "PrimaryColor");
				MoreGrid.SetResourceReference(System.Windows.Controls.Control.BorderBrushProperty, "PrimaryColor");
				break;
			}
		}
		return true;
	}

	public void ClearTabSelect()
	{
		_tabIndex = 0;
		SetDateBorder.BorderBrush = Brushes.Transparent;
		SetCalBorder.BorderBrush = Brushes.Transparent;
		MoreGrid.BorderBrush = Brushes.Transparent;
	}

	public async Task TryAddTask()
	{
		string titleContent = TitleText.GetTitleContent();
		await DoAddTask(titleContent);
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!_isInOperation)
		{
			_mouseDown = true;
		}
	}

	private void OnPopupClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	public bool IsLostFocus()
	{
		return OperationPanel.Visibility != Visibility.Visible;
	}

	public async Task TryAddTaskOnLostFocus()
	{
		if (!string.IsNullOrWhiteSpace(TitleText.GetTitleContent(forceRemove: true)))
		{
			await TryAddTask();
		}
	}

	public void TryFocus()
	{
		if (!TitleText.FocusSuppressed)
		{
			TitleText.EditBox.Focus();
			TitleText.FocusText();
		}
	}

	private void DeleteFile(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: AddTaskAttachmentInfo dataContext })
		{
			Model.Remove(dataContext);
		}
	}

	public void OnFileDrop(object sender, System.Windows.DragEventArgs e)
	{
		e.Handled = true;
		if (!Model.IsCalendar)
		{
			TryPasteFile(GetDropFiles(e));
		}
	}

	private static List<string> GetDropFiles(System.Windows.DragEventArgs e)
	{
		Array array = (Array)e.Data.GetData(System.Windows.DataFormats.FileDrop);
		List<string> list = new List<string>();
		if (array != null)
		{
			foreach (object item in array)
			{
				list.Add(item.ToString());
			}
		}
		return list;
	}

	private void OnFileScrollerMouseWheel(object sender, MouseWheelEventArgs e)
	{
		FileScroller.ScrollToHorizontalOffset(FileScroller.HorizontalOffset - (double)((e.Delta > 0) ? 56 : (-56)));
	}

	private void OnAttachmentClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: AddTaskAttachmentInfo dataContext })
		{
			dataContext.OnAttachmentClick();
		}
	}

	private void SetPriorityMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is AddTaskViewModel addTaskViewModel && !IsInOperation)
		{
			SetPriorityDialog setPriorityDialog = new SetPriorityDialog(addTaskViewModel.Priority, lineStyle: true);
			SetPriorityPopup.Child = setPriorityDialog;
			SetPriorityPopup.IsOpen = true;
			IsInOperation = true;
			setPriorityDialog.PrioritySelect += OnPrioritySelected;
			SetPriorityPopup.Closed -= OnPriorityClosed;
			SetPriorityPopup.Closed += OnPriorityClosed;
		}
	}

	private async void OnPrioritySelected(object o, int priority)
	{
		UserActCollectUtils.AddClickEvent("add_task", "action_btn", "priority");
		SetPriorityPopup.IsOpen = false;
		SetPriorityPopup.Child = null;
		SetPriority(priority);
		await Task.Delay(100);
		FocusText();
	}

	private void SetAssignMouseUp(object sender, MouseButtonEventArgs e)
	{
		TryShowAssignPopup();
	}

	private void TryShowAssignPopup()
	{
		if (!IsInOperation)
		{
			if (_assignDialog == null)
			{
				_assignDialog = new SetAssigneeDialog(Model.ProjectId, SetAssigneePopup, Avatar?.UserId);
				_assignDialog.AssigneeSelect += OnAssigneeSelect;
				Popup popup = _assignDialog.GetPopup();
				popup.HorizontalOffset = -45.0;
				popup.VerticalOffset = 18.0;
				popup.StaysOpen = false;
				popup.PlacementTarget = AssignBtn;
				popup.Closed -= OnAssignClosed;
				popup.Closed += OnAssignClosed;
			}
			else
			{
				_assignDialog.SetItems(Model.ProjectId, Avatar?.UserId);
			}
			_assignDialog.Move(forward: false);
			_assignDialog.Show();
			IsInOperation = true;
		}
	}

	private void OnAssignClosed(object sender, EventArgs e)
	{
		IsInOperation = false;
	}

	private void OnAssigneeSelect(object sender, AvatarInfo e)
	{
		OnAssigneeSelect(e);
	}

	private void SetTagMouseUp(object sender, MouseButtonEventArgs e)
	{
		TryShowSelectTagPopup();
	}

	private async void TryShowSelectTagPopup()
	{
		if (IsInOperation)
		{
			return;
		}
		bool flag = _batchSetTagCtrl == null;
		if (flag)
		{
			_batchSetTagCtrl = new BatchSetTagControl();
			_batchSetTagCtrl.Close += OnBatchSetTagClosed;
			_batchSetTagCtrl.TagsSelect += delegate(object s, TagSelectData tags)
			{
				OnTagsAdded(tags);
			};
			SetTagPopup.Child = _batchSetTagCtrl;
			SetTagPopup.Closed -= OnSetTagClosed;
			SetTagPopup.Closed += OnSetTagClosed;
			IsInOperation = true;
		}
		_batchSetTagCtrl.Init(new TagSelectData
		{
			OmniSelectTags = new List<string>(GetSelectedTags())
		}, flag);
		IsInOperation = true;
		SetTagPopup.IsOpen = true;
	}

	private void OnSetTagClosed(object sender, EventArgs e)
	{
		IsInOperation = false;
	}

	private void OnBatchSetTagClosed(object sender, EventArgs e)
	{
		SetTagPopup.IsOpen = false;
	}

	private async void OnInsertClick(object sender, RoutedEventArgs e)
	{
		await DoAddTask(TitleText.GetTitleContent());
	}

	private void OnContentHintClick(object sender, MouseButtonEventArgs e)
	{
		if (TextContentGrid.IsVisible)
		{
			ContentText.Focus();
		}
	}

	private void EditorOnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		e.Handled = true;
	}

	private void OnSelectDate(object sender, EventArgs e)
	{
		ShowSetDateDialog();
	}

	private async void OnContentKeyup(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (_scenario == Scenario.QuickAdd && Utils.IfShiftPressed() && e.Key == Key.Tab)
		{
			FocusText();
		}
	}

	private void OnQuickAddClick(object sender, MouseButtonEventArgs e)
	{
	}

	private void OnTitleMouseScroll(object sender, MouseWheelEventArgs e)
	{
		if (e != null)
		{
			InputScrollViewer.ScrollToVerticalOffset(InputScrollViewer.VerticalOffset - (double)((float)e.Delta / 2f));
		}
	}

	private void OnMouseScroll(object sender, MouseWheelEventArgs e)
	{
		if (e != null)
		{
			InputScrollViewer.ScrollToVerticalOffset(InputScrollViewer.VerticalOffset - (double)((float)e.Delta / 2f));
		}
	}

	public bool HandleMoveUp()
	{
		if (IsInSetProject())
		{
			_projectOrGroupPopup.UpDownSelect(isUp: true);
			return true;
		}
		return false;
	}

	public bool IsInSetProject()
	{
		if (SetProjectPopup.IsOpen && !_projectPopupState.IsHidden)
		{
			return _projectOrGroupPopup != null;
		}
		return false;
	}

	public bool HandleKey(Key key)
	{
		if (IsInSetProject())
		{
			switch (key)
			{
			case Key.Up:
				_projectOrGroupPopup.UpDownSelect(isUp: true);
				break;
			case Key.Down:
				_projectOrGroupPopup.UpDownSelect(isUp: false);
				break;
			case Key.Left:
				_projectOrGroupPopup.LeftRightSelect(isLeft: true);
				break;
			case Key.Right:
				_projectOrGroupPopup.LeftRightSelect(isLeft: false);
				break;
			case Key.Return:
				_projectOrGroupPopup.EnterSelect();
				break;
			case Key.Space:
				return _projectOrGroupPopup.SpaceSelect();
			}
			return true;
		}
		return false;
	}

	public bool HandleMoveDown()
	{
		if (IsInSetProject())
		{
			_projectOrGroupPopup.UpDownSelect(isUp: false);
			return true;
		}
		return false;
	}

	private void OnQuickAddSizeChanged(object sender, SizeChangedEventArgs e)
	{
		SetPanelLocation();
	}

	private void OnInputGotFocus(object sender, RoutedEventArgs e)
	{
	}

	private void OnListCaretMoveUp(object sender, EventArgs e)
	{
	}

	private async void SwitchToList()
	{
		ContentHint.Visibility = Visibility.Visible;
		TextContentGrid.Visibility = Visibility.Visible;
		ChecklistGrid.Visibility = Visibility.Visible;
		EnlargeScrollContent(enlarge: false);
		ResetSaveEnabled();
		Checklist.SetData(new List<CheckItemViewModel>());
		string persistedText = ContentText.GetPersistedText();
		if (!string.IsNullOrEmpty(persistedText))
		{
			ChecklistExtra checklistExtra = ChecklistUtils.Text2Items(persistedText);
			ContentText.Text = checklistExtra.Description;
			if (!string.IsNullOrEmpty(checklistExtra.Description))
			{
				ContentHint.Visibility = Visibility.Collapsed;
			}
			if (checklistExtra.ChecklistItems.Count > 0)
			{
				List<TaskDetailItemModel> models = ChecklistUtils.BuildChecklist(0, null, checklistExtra.ChecklistItems);
				InitChecklistView(new List<CheckItemViewModel>(CheckItemViewModel.BuildListFromModels(models, null)));
			}
		}
		else
		{
			TaskDetailItemModel item = new TaskDetailItemModel
			{
				id = Utils.GetGuid(),
				TaskId = 0,
				TaskServerId = null,
				title = string.Empty,
				status = 0,
				sortOrder = 0L
			};
			List<TaskDetailItemModel> models2 = new List<TaskDetailItemModel> { item };
			InitChecklistView(new List<CheckItemViewModel>(CheckItemViewModel.BuildListFromModels(models2, null)));
		}
		Checklist.FocusFirstItem();
	}

	private void InitChecklistView(List<CheckItemViewModel> source)
	{
		if (source != null && source.Count == 1)
		{
			source[0].ShowAddHint = true;
		}
		Checklist.SetData(source);
		Checklist.CheckItemsDeleted -= OnCheckItemsDeleted;
		Checklist.CheckItemsDeleted += OnCheckItemsDeleted;
		Checklist.QuickItemSelected -= OnQuickItemSelected;
		Checklist.QuickItemSelected += OnQuickItemSelected;
		Checklist.QuickAddTask -= OnQuickAddTask;
		Checklist.QuickAddTask += OnQuickAddTask;
		Checklist.PopOpened -= CheckListPopupOpened;
		Checklist.PopOpened += CheckListPopupOpened;
		Checklist.PopClosed -= CheckListPopupClosed;
		Checklist.PopClosed += CheckListPopupClosed;
		Checklist.DatePopOpened -= DatePopupOpened;
		Checklist.DatePopOpened += DatePopupOpened;
		Checklist.DatePopClosed -= DatePopupClosed;
		Checklist.DatePopClosed += DatePopupClosed;
	}

	private void CheckListPopupClosed(object sender, bool e)
	{
		IsInOperation = false;
	}

	private void CheckListPopupOpened(object sender, bool e)
	{
		IsInOperation = true;
	}

	private void OnQuickItemSelected(object sender, QuickSetModel e)
	{
		switch (e.Type)
		{
		case QuickSetType.Priority:
			Model.ManuallySelectedPriority = true;
			SetPriority(e.Priority);
			break;
		case QuickSetType.Project:
			OnProjectSelect(e.Project, e.ColumnName);
			Model.AddToColumnId = e.ColumnId;
			break;
		case QuickSetType.Tag:
			TitleText.OnQuickItemSelect(sender, e, withTagPrefix: true);
			break;
		case QuickSetType.Date:
		{
			TimeData timeData = TimeData.InitDefaultTime();
			timeData.StartDate = e.Date;
			Model.TimeData = timeData;
			break;
		}
		case QuickSetType.CalId:
			CalendarSelect(e.Title, e.CalId);
			break;
		case QuickSetType.Assign:
			break;
		}
	}

	private void DatePopupClosed(object sender, EventArgs e)
	{
		IsInOperation = false;
	}

	private void DatePopupOpened(object sender, EventArgs e)
	{
		IsInOperation = true;
	}

	private void OnCheckItemsDeleted(object sender, TaskDetailItemModel e)
	{
		if (Checklist.ChecklistItems.Count == 0)
		{
			SwitchToText();
		}
	}

	private void OnQuickAddTask(object sender, EventArgs e)
	{
		TryAddTask();
	}

	public bool? UseInWidgetDark()
	{
		return _useInWidgetDark;
	}

	public void ResetDate()
	{
		if (!_manualSelectedDate)
		{
			Model.ResetTimeData(_taskDefault);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/quickadd/quickaddview.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			RootView = (QuickAddView)target;
			RootView.Loaded += OnViewLoaded;
			RootView.Unloaded += OnViewUnloaded;
			RootView.MouseLeftButtonUp += OnAddViewMouseLeftUp;
			RootView.PreviewMouseLeftButtonDown += OnQuickAddClick;
			RootView.SizeChanged += OnQuickAddSizeChanged;
			break;
		case 2:
			AddTaskGrid = (Grid)target;
			AddTaskGrid.Drop += OnFileDrop;
			break;
		case 3:
			InputBackground = (Border)target;
			break;
		case 4:
			InputBackground1 = (Border)target;
			break;
		case 5:
			ContentBorder = (Border)target;
			break;
		case 6:
			ContentBorderContent = (Border)target;
			break;
		case 7:
			InputBorder = (Border)target;
			break;
		case 8:
			InputBorderContent = (Border)target;
			break;
		case 9:
			InputScrollViewer = (ScrollViewer)target;
			InputScrollViewer.PreviewMouseWheel += OnTitleMouseScroll;
			break;
		case 10:
			ScrollContent = (Grid)target;
			break;
		case 11:
			TaskKindIndicator = (Grid)target;
			break;
		case 12:
			TitleGrid = (Grid)target;
			break;
		case 13:
			TitleText = (QuickAddText)target;
			break;
		case 14:
			HintPanel = (Grid)target;
			break;
		case 15:
			AddPath = (System.Windows.Shapes.Path)target;
			break;
		case 16:
			HintTextBlock = (TextBlock)target;
			break;
		case 17:
			TextContentGrid = (Grid)target;
			break;
		case 18:
			ContentHint = (TextBlock)target;
			ContentHint.MouseLeftButtonUp += OnContentHintClick;
			break;
		case 19:
			ContentText = (MarkDownEditor)target;
			break;
		case 20:
			ChecklistGrid = (Grid)target;
			break;
		case 21:
			Checklist = (ChecklistControl)target;
			break;
		case 22:
			OperationPanel = (StackPanel)target;
			break;
		case 23:
			SetDateBorder = (Border)target;
			break;
		case 24:
			((Grid)target).MouseLeftButtonDown += OnMouseDown;
			((Grid)target).PreviewMouseLeftButtonUp += SetDateClick;
			break;
		case 25:
			DateGrid = (Border)target;
			break;
		case 26:
			DetailDateGrid = (StackPanel)target;
			break;
		case 27:
			SetDatePath = (System.Windows.Shapes.Path)target;
			break;
		case 28:
			DateText = (TextBlock)target;
			break;
		case 29:
			SetCalBorder = (Border)target;
			break;
		case 30:
			((Grid)target).MouseLeftButtonUp += SetProjectClick;
			break;
		case 31:
			SetCalendarPopup = (EscPopup)target;
			break;
		case 32:
			SetProjectPopup = (EscPopup)target;
			break;
		case 33:
			MoreGrid = (Border)target;
			break;
		case 34:
			((Grid)target).MouseLeftButtonDown += OnMouseDown;
			((Grid)target).PreviewMouseLeftButtonUp += OnMoreClick;
			break;
		case 35:
			AttachmentBorder = (Border)target;
			break;
		case 36:
			FileScroller = (ScrollViewer)target;
			FileScroller.PreviewMouseWheel += OnFileScrollerMouseWheel;
			break;
		case 39:
			DetailOperationPanel = (Grid)target;
			break;
		case 40:
			DetailPanelContainer = (Grid)target;
			break;
		case 41:
			NormalActionPanel = (StackPanel)target;
			break;
		case 42:
			SetDateBorder1 = (Border)target;
			break;
		case 43:
			((Grid)target).MouseLeftButtonDown += OnMouseDown;
			((Grid)target).PreviewMouseLeftButtonUp += SetDateClick;
			break;
		case 44:
			DateGrid1 = (Border)target;
			break;
		case 45:
			DetailDateGrid1 = (StackPanel)target;
			break;
		case 46:
			SetDatePath1 = (System.Windows.Shapes.Path)target;
			break;
		case 47:
			DateText1 = (TextBlock)target;
			break;
		case 48:
			((ContentControl)target).PreviewMouseLeftButtonUp += SetPriorityMouseUp;
			break;
		case 49:
			SetPriorityPopup = (EscPopup)target;
			break;
		case 50:
			TagGrid = (ContentControl)target;
			TagGrid.PreviewMouseLeftButtonUp += SetTagMouseUp;
			break;
		case 51:
			SetTagPopup = (EscPopup)target;
			break;
		case 52:
			AssignBtn = (ContentControl)target;
			AssignBtn.PreviewMouseLeftButtonUp += SetAssignMouseUp;
			break;
		case 53:
			SetAssigneePopup = (EscPopup)target;
			break;
		case 54:
			((Border)target).PreviewMouseLeftButtonUp += SetDetailProjectClick;
			break;
		case 55:
			TaskDetailMoveGrid = (Grid)target;
			break;
		case 56:
			ProjectIconGrid = (Grid)target;
			break;
		case 57:
			MoreOptionGrid = (ContentControl)target;
			MoreOptionGrid.MouseLeftButtonDown += OnMouseDown;
			MoreOptionGrid.MouseLeftButtonUp += OnMoreClick;
			break;
		case 58:
			AddBtnContainer = (Grid)target;
			break;
		case 59:
			SaveButton = (System.Windows.Controls.Button)target;
			SaveButton.Click += OnInsertClick;
			break;
		case 60:
			((Border)target).MouseLeftButtonUp += OnInsertClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 37:
			((Border)target).MouseLeftButtonUp += OnAttachmentClick;
			break;
		case 38:
			((Grid)target).MouseLeftButtonUp += DeleteFile;
			break;
		}
	}
}
