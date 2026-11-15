using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Emoji.Wpf;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Filter;

public class NormalFilterControl : UserControl, IComponentConnector
{
	private NormalFilterViewModel _viewModel;

	private ProjectOrGroupPopup _projectEditDialog;

	private DateEditDialog _dateEditDialog;

	private TagEditDialog _tagEditDialog;

	private AssigneeEditDialog _assignEditDialog;

	private bool _useInSearchFilter;

	private FrameworkElement _dropDownFieldMouseDownTarget;

	private const double PopupContentLeftInset = 5.0;

	private const double PopupContentRightInset = 10.0;

	private const double DefaultPopupWidth = 452.0;

	public static readonly DependencyProperty DropDownRowMarginProperty = DependencyProperty.Register("DropDownRowMargin", typeof(Thickness), typeof(NormalFilterControl), new PropertyMetadata(new Thickness(0.0, 5.0, 20.0, 5.0)));

	public static readonly DependencyProperty DropDownTextMarginProperty = DependencyProperty.Register("DropDownTextMargin", typeof(Thickness), typeof(NormalFilterControl), new PropertyMetadata(new Thickness(8.0, 0.0, 24.0, 0.0)));

	internal NormalFilterControl Root;

	internal Grid listField;

	internal EmjTextBlock listTextBox;

	internal EscPopup listPopup;

	internal DockPanel tagGrid;

	internal Grid tagField;

	internal EmjTextBlock tagTextBox;

	internal EscPopup tagPopup;

	internal Grid dateField;

	internal TextBlock dateTextBox;

	internal EscPopup datePopup;

	internal RadioButton rbPriority;

	internal DockPanel assigneeGrid;

	internal Grid AssignField;

	internal EmjTextBlock AssignTextBox;

	internal EscPopup AssignPopup;

	internal DockPanel KeywordsGrid;

	internal TextBox KeywordsText;

	internal Grid TaskTypeGrid;

	internal RadioButton AllType;

	private bool _contentLoaded;

	public NormalFilterViewModel ViewModel
	{
		set
		{
			_viewModel = value;
			base.DataContext = _viewModel;
		}
	}

	public bool CheckValid { get; set; }

	public Thickness DropDownRowMargin
	{
		get
		{
			return (Thickness)GetValue(DropDownRowMarginProperty);
		}
		set
		{
			SetValue(DropDownRowMarginProperty, value);
		}
	}

	public Thickness DropDownTextMargin
	{
		get
		{
			return (Thickness)GetValue(DropDownTextMarginProperty);
		}
		set
		{
			SetValue(DropDownTextMarginProperty, value);
		}
	}

	public bool PopupOpened
	{
		get
		{
			if (!AssignPopup.IsOpen && !listPopup.IsOpen && !tagPopup.IsOpen)
			{
				return datePopup.IsOpen;
			}
			return true;
		}
	}

	public event EventHandler NotifyInvalid;

	public event EventHandler RuleChanged;

	public NormalFilterControl()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
	}

	public async void Init(bool inMatrix = false)
	{
		_viewModel.DisplayProjectText = ProjectExtra.FormatDisplayText(ProjectExtra.Serialize(BuildProjectExtraForDisplay()));
		List<TagModel> tags = TagDataHelper.GetTags(sort: false);
		if ((tags == null || tags.Count == 0) && (_viewModel.Tags == null || _viewModel.Tags.Count == 0))
		{
			tagGrid.Visibility = Visibility.Collapsed;
		}
		bool flag = CacheManager.GetProjects().Any((ProjectModel p) => p.IsShareList());
		assigneeGrid.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		if (inMatrix)
		{
			KeywordsGrid.Visibility = Visibility.Collapsed;
			return;
		}
		TextBox keywordsText = KeywordsText;
		List<string> keywords = _viewModel.Keywords;
		keywordsText.Text = ((keywords != null && keywords.Count > 0) ? _viewModel.Keywords[0] : string.Empty);
	}

	private void OnDateClick(object sender, MouseButtonEventArgs e)
	{
		if (HandleDropDownFieldMouseUp(sender, e) && Editable() && !HandleDropDownToggleClick(datePopup, e) && !_useInSearchFilter)
		{
			CloseOtherDropDowns(datePopup);
			_dateEditDialog = new DateEditDialog(showAll: true, _viewModel.DueDates, LogicType.Or, _viewModel.Version, useDefaultMinWidth: false);
			_dateEditDialog.OnSelectedDateChanged += delegate(object s, List<string> duedates)
			{
				_viewModel.DueDates = duedates;
				this.RuleChanged?.Invoke(this, null);
			};
			datePopup.Closed -= DatePopup_Closed;
			datePopup.Closed += DatePopup_Closed;
			OnConditionClick(_dateEditDialog, datePopup, dateField);
		}
	}

	private void DatePopup_Closed(object sender, EventArgs e)
	{
		if (!_dateEditDialog.IsSave)
		{
			_dateEditDialog.Restore();
		}
		_dateEditDialog.IsSave = false;
	}

	private void OnAssigneeClick(object sender, MouseButtonEventArgs e)
	{
		if (HandleDropDownFieldMouseUp(sender, e) && Editable() && !HandleDropDownToggleClick(AssignPopup, e))
		{
			CloseOtherDropDowns(AssignPopup);
			_assignEditDialog = new AssigneeEditDialog(_viewModel.Assignees, showAll: true);
			_assignEditDialog.OnSelectedAssigneeChanged += delegate(object s, List<string> assignees)
			{
				_viewModel.Assignees = assignees;
				this.RuleChanged?.Invoke(this, null);
			};
			AssignPopup.Closed -= AssigneePopup_Closed;
			AssignPopup.Closed += AssigneePopup_Closed;
			OnConditionClick(_assignEditDialog, AssignPopup, AssignField);
		}
	}

	private void AssigneePopup_Closed(object sender, EventArgs e)
	{
		if (!_assignEditDialog.IsSave)
		{
			_assignEditDialog.Restore();
		}
		_assignEditDialog.IsSave = false;
	}

	private void OnTagClick(object sender, MouseButtonEventArgs e)
	{
		if (HandleDropDownFieldMouseUp(sender, e) && Editable() && !HandleDropDownToggleClick(tagPopup, e))
		{
			CloseOtherDropDowns(tagPopup);
			_tagEditDialog = new TagEditDialog(showAll: true, _viewModel.Tags);
			_tagEditDialog.OnSelectedTagChanged += delegate(object s, List<string> tags)
			{
				_viewModel.Tags = tags;
				this.RuleChanged?.Invoke(this, null);
			};
			tagPopup.Closed -= TagPopup_Closed;
			tagPopup.Closed += TagPopup_Closed;
			OnConditionClick(_tagEditDialog, tagPopup, tagField);
		}
	}

	private bool Editable()
	{
		if (CheckValid && !UserDao.IsUserValid())
		{
			this.NotifyInvalid?.Invoke(this, null);
			return false;
		}
		return true;
	}

	private void TagPopup_Closed(object sender, EventArgs e)
	{
		if (!_tagEditDialog.IsSave)
		{
			_tagEditDialog.Restore();
		}
		_tagEditDialog.IsSave = false;
	}

	private void OnListClick(object sender, MouseButtonEventArgs e)
	{
		if (HandleDropDownFieldMouseUp(sender, e) && Editable() && !HandleDropDownToggleClick(listPopup, e))
		{
			ProjectExtra data = BuildProjectExtraForPopup();
			CloseOtherDropDowns(listPopup);
			AlignPopupToField(listPopup, listField);
			ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(listPopup, data, new ProjectSelectorExtra
			{
				popupWidth = GetPopupOuterWidth(listField),
				showAll = false,
				showCalendars = true,
				showListGroup = true,
				useCircleCheckForSectionGroups = true
			});
			projectOrGroupPopup.Save += OnProjectSelect;
			projectOrGroupPopup.Show();
		}
	}

	private async void OnProjectSelect(object sender, ProjectExtra data)
	{
		List<string> list = data.ProjectIds.ToList();
		List<string> calendars = data.SubscribeCalendars.Where((string id) => id != "#allSubscribe").ToList();
		if (data.SubscribeCalendars.Contains("#allSubscribe") && !list.Contains("Calendar5959a2259161d16d23a4f272"))
		{
			list.Add("Calendar5959a2259161d16d23a4f272");
		}
		_viewModel.Projects = list;
		_viewModel.Groups = data.GroupIds.ToList();
		_viewModel.CalendarAccounts = data.BindAccounts.ToList();
		_viewModel.Calendars = calendars;
		this.RuleChanged?.Invoke(this, null);
		_viewModel.DisplayProjectText = ProjectExtra.FormatDisplayText(ProjectExtra.Serialize(data));
	}

	private ProjectExtra BuildProjectExtraForPopup()
	{
		ProjectExtra projectExtra = new ProjectExtra
		{
			ProjectIds = _viewModel.Projects.Where((string id) => id != "Calendar5959a2259161d16d23a4f272").ToList(),
			GroupIds = _viewModel.Groups.ToList()
		};
		if (_viewModel.CalendarAccounts.Any() || _viewModel.Calendars.Any())
		{
			projectExtra.BindAccounts = _viewModel.CalendarAccounts.ToList();
			projectExtra.SubscribeCalendars = _viewModel.Calendars.ToList();
			return projectExtra;
		}
		if (_viewModel.Projects.Contains("Calendar5959a2259161d16d23a4f272"))
		{
			projectExtra.SubscribeCalendars = new List<string> { "#allSubscribe" };
		}
		return projectExtra;
	}

	private ProjectExtra BuildProjectExtraForDisplay()
	{
		ProjectExtra projectExtra = new ProjectExtra
		{
			ProjectIds = _viewModel.Projects.Where((string id) => id != "Calendar5959a2259161d16d23a4f272").ToList(),
			GroupIds = _viewModel.Groups.ToList(),
			BindAccounts = _viewModel.CalendarAccounts.ToList(),
			SubscribeCalendars = _viewModel.Calendars.ToList()
		};
		if (_viewModel.Projects.Contains("Calendar5959a2259161d16d23a4f272") && !projectExtra.BindAccounts.Any() && !projectExtra.SubscribeCalendars.Any())
		{
			projectExtra.SubscribeCalendars = new List<string> { "#allSubscribe" };
		}
		return projectExtra;
	}

	private static double GetPopupOuterWidth(FrameworkElement field)
	{
		return ((field != null && field.ActualWidth > 0.0) ? field.ActualWidth : 452.0) + 5.0 + 10.0;
	}

	private static void AlignPopupToField(Popup popup, FrameworkElement field)
	{
		popup.Width = GetPopupOuterWidth(field);
		popup.HorizontalOffset = -5.0;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		Window window = Window.GetWindow(this);
		if (window != null)
		{
			window.PreviewMouseLeftButtonDown -= OnWindowPreviewMouseLeftButtonDown;
			window.PreviewMouseLeftButtonDown += OnWindowPreviewMouseLeftButtonDown;
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		Window window = Window.GetWindow(this);
		if (window != null)
		{
			window.PreviewMouseLeftButtonDown -= OnWindowPreviewMouseLeftButtonDown;
		}
	}

	private void OnWindowPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		DependencyObject source = e.OriginalSource as DependencyObject;
		if (!IsDropDownField(source) && !IsDropDownPopupContent(source) && !IsMouseOverAnyDropDownPopup())
		{
			CloseAllDropDowns();
		}
	}

	private void OnDropDownFieldMouseDown(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		_dropDownFieldMouseDownTarget = sender as FrameworkElement;
		e.Handled = true;
	}

	private bool HandleDropDownFieldMouseUp(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		bool result = _dropDownFieldMouseDownTarget == sender;
		_dropDownFieldMouseDownTarget = null;
		return result;
	}

	private bool HandleDropDownToggleClick(Popup popup, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (popup.IsOpen)
		{
			popup.IsOpen = false;
			return true;
		}
		return false;
	}

	private bool IsDropDownField(DependencyObject source)
	{
		while (source != null)
		{
			if (source == listField || source == tagField || source == dateField || source == AssignField)
			{
				return true;
			}
			source = GetParent(source);
		}
		return false;
	}

	private bool IsDropDownPopupContent(DependencyObject source)
	{
		if (source != null)
		{
			return (from popup in GetDropDownPopups()
				where popup.IsOpen
				select popup).Any((Popup popup) => IsDescendantOf(source, popup.Child) || IsDescendantOfOpenNestedPopup(source, popup.Child));
		}
		return false;
	}

	private bool IsMouseOverAnyDropDownPopup()
	{
		if (!(from popup in GetDropDownPopups()
			where popup.IsOpen
			select popup.Child as FrameworkElement).Any(IsMouseOverElement))
		{
			return (from popup in GetDropDownPopups()
				where popup.IsOpen
				select popup).Any((Popup popup) => IsMouseOverOpenNestedPopup(popup.Child));
		}
		return true;
	}

	private IEnumerable<Popup> GetDropDownPopups()
	{
		yield return listPopup;
		yield return tagPopup;
		yield return datePopup;
		yield return AssignPopup;
	}

	private static bool IsDescendantOf(DependencyObject source, DependencyObject ancestor)
	{
		for (DependencyObject dependencyObject = source; dependencyObject != null; dependencyObject = GetParent(dependencyObject))
		{
			if (dependencyObject == ancestor)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsMouseOverElement(FrameworkElement element)
	{
		if (element == null || !element.IsVisible || element.ActualWidth <= 0.0 || element.ActualHeight <= 0.0)
		{
			return false;
		}
		Point position = Mouse.GetPosition(element);
		if (position.X >= 0.0 && position.Y >= 0.0 && position.X <= element.ActualWidth)
		{
			return position.Y <= element.ActualHeight;
		}
		return false;
	}

	private static bool IsDescendantOfOpenNestedPopup(DependencyObject source, DependencyObject root)
	{
		return (from popup in FindDescendants<Popup>(root)
			where popup.IsOpen
			select popup).Any((Popup popup) => IsDescendantOf(source, popup.Child));
	}

	private static bool IsMouseOverOpenNestedPopup(DependencyObject root)
	{
		return (from popup in FindDescendants<Popup>(root)
			where popup.IsOpen
			select popup.Child as FrameworkElement).Any(IsMouseOverElement);
	}

	private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
	{
		if (root == null)
		{
			yield break;
		}
		HashSet<DependencyObject> visited = new HashSet<DependencyObject>();
		Queue<DependencyObject> pending = new Queue<DependencyObject>();
		EnqueueChildren(root, pending, visited);
		while (pending.Count > 0)
		{
			DependencyObject current = pending.Dequeue();
			if (current is T val)
			{
				yield return val;
			}
			EnqueueChildren(current, pending, visited);
		}
	}

	private static void EnqueueChildren(DependencyObject source, Queue<DependencyObject> pending, HashSet<DependencyObject> visited)
	{
		if (source == null)
		{
			return;
		}
		if (source is Popup { Child: not null } popup)
		{
			EnqueueChild(popup.Child, pending, visited);
		}
		if (source is Visual || source is Visual3D)
		{
			int childrenCount = VisualTreeHelper.GetChildrenCount(source);
			for (int i = 0; i < childrenCount; i++)
			{
				EnqueueChild(VisualTreeHelper.GetChild(source, i), pending, visited);
			}
		}
		foreach (object child2 in LogicalTreeHelper.GetChildren(source))
		{
			if (child2 is DependencyObject child)
			{
				EnqueueChild(child, pending, visited);
			}
		}
	}

	private static void EnqueueChild(DependencyObject child, Queue<DependencyObject> pending, HashSet<DependencyObject> visited)
	{
		if (child != null && visited.Add(child))
		{
			pending.Enqueue(child);
		}
	}

	private static DependencyObject GetParent(DependencyObject source)
	{
		if (source is Visual || source is Visual3D)
		{
			return VisualTreeHelper.GetParent(source);
		}
		if (source is FrameworkContentElement frameworkContentElement)
		{
			return frameworkContentElement.Parent;
		}
		if (!(source is ContentElement reference))
		{
			return LogicalTreeHelper.GetParent(source);
		}
		return ContentOperations.GetParent(reference);
	}

	private void CloseOtherDropDowns(Popup popup)
	{
		if (popup != listPopup)
		{
			listPopup.IsOpen = false;
		}
		if (popup != tagPopup)
		{
			tagPopup.IsOpen = false;
		}
		if (popup != datePopup)
		{
			datePopup.IsOpen = false;
		}
		if (popup != AssignPopup)
		{
			AssignPopup.IsOpen = false;
		}
	}

	private void CloseAllDropDowns()
	{
		listPopup.IsOpen = false;
		tagPopup.IsOpen = false;
		datePopup.IsOpen = false;
		AssignPopup.IsOpen = false;
	}

	private void OnConditionClick(ConditionEditDialog dialog, Popup popup, FrameworkElement field)
	{
		if (dialog != null)
		{
			dialog.OnCancel += delegate
			{
				popup.IsOpen = false;
			};
			dialog.OnSave += delegate
			{
				popup.IsOpen = false;
			};
			AlignPopupToField(popup, field);
			popup.Child = dialog;
			popup.IsOpen = true;
		}
	}

	private void PriorityAllClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (Editable() && _viewModel.Priorities.Count > 0)
		{
			_viewModel.Priorities.Clear();
			_viewModel.NotifyPriorityChanged();
			this.RuleChanged?.Invoke(this, null);
		}
	}

	private void TaskTypeAllClick(object sender, RoutedEventArgs e)
	{
		if (Editable() && _viewModel.TaskTypes.Count > 0)
		{
			_viewModel.TaskTypes.Clear();
			_viewModel.NotifyTaskTypeChanged();
			this.RuleChanged?.Invoke(this, null);
		}
	}

	private void AssigneeAllClick(object sender, RoutedEventArgs e)
	{
		if (Editable() && _viewModel.Assignees.Count > 0)
		{
			_viewModel.Assignees.Clear();
			_viewModel.NotifyAssigneeChanged();
			this.RuleChanged?.Invoke(this, null);
		}
	}

	private void TaskTypeCheckboxClick(object sender, RoutedEventArgs e)
	{
		if (!Editable())
		{
			return;
		}
		if (sender is CheckBox checkBox)
		{
			string item = checkBox.Tag.ToString();
			if (_viewModel.TaskTypes.Contains(item))
			{
				_viewModel.TaskTypes.Remove(item);
			}
			else
			{
				_viewModel.TaskTypes.Add(item);
			}
			if (_viewModel.TaskTypes.Count == 2)
			{
				_viewModel.TaskTypes.Clear();
			}
		}
		this.RuleChanged?.Invoke(this, null);
		_viewModel.NotifyTaskTypeChanged();
	}

	private void PriorityCheckboxClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (!Editable())
		{
			return;
		}
		if (sender is CheckBox checkBox)
		{
			int item = int.Parse(checkBox.Tag.ToString());
			if (_viewModel.Priorities.Contains(item))
			{
				_viewModel.Priorities.Remove(item);
			}
			else
			{
				_viewModel.Priorities.Add(item);
			}
			if (_viewModel.Priorities.Count == 4)
			{
				_viewModel.Priorities.Clear();
			}
		}
		this.RuleChanged?.Invoke(this, null);
		_viewModel.NotifyPriorityChanged();
	}

	private void AssigneeCheckboxClick(object sender, RoutedEventArgs e)
	{
		if (!Editable())
		{
			return;
		}
		if (sender is CheckBox checkBox)
		{
			string item = checkBox.Tag.ToString();
			if (_viewModel.Assignees.Contains(item))
			{
				_viewModel.Assignees.Remove(item);
			}
			else
			{
				_viewModel.Assignees.Add(item);
			}
		}
		_viewModel.NotifyAssigneeChanged();
		this.RuleChanged?.Invoke(this, null);
	}

	private void OnKeywordsChagned(object sender, TextChangedEventArgs e)
	{
		_viewModel.Keywords.Clear();
		string text = KeywordsText.Text.Trim();
		if (!string.IsNullOrEmpty(text))
		{
			_viewModel.Keywords.Add(text);
		}
	}

	public void SetUseInSearchFilter()
	{
		_useInSearchFilter = true;
		KeywordsGrid.Visibility = Visibility.Collapsed;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/filter/normalfiltercontrol.xaml", UriKind.Relative);
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
			Root = (NormalFilterControl)target;
			break;
		case 2:
			listField = (Grid)target;
			listField.PreviewMouseLeftButtonDown += OnDropDownFieldMouseDown;
			listField.PreviewMouseLeftButtonUp += OnListClick;
			break;
		case 3:
			listTextBox = (EmjTextBlock)target;
			break;
		case 4:
			listPopup = (EscPopup)target;
			break;
		case 5:
			tagGrid = (DockPanel)target;
			break;
		case 6:
			tagField = (Grid)target;
			tagField.PreviewMouseLeftButtonDown += OnDropDownFieldMouseDown;
			tagField.PreviewMouseLeftButtonUp += OnTagClick;
			break;
		case 7:
			tagTextBox = (EmjTextBlock)target;
			break;
		case 8:
			tagPopup = (EscPopup)target;
			break;
		case 9:
			dateField = (Grid)target;
			dateField.PreviewMouseLeftButtonDown += OnDropDownFieldMouseDown;
			dateField.PreviewMouseLeftButtonUp += OnDateClick;
			break;
		case 10:
			dateTextBox = (TextBlock)target;
			break;
		case 11:
			datePopup = (EscPopup)target;
			break;
		case 12:
			rbPriority = (RadioButton)target;
			rbPriority.PreviewMouseLeftButtonUp += PriorityAllClick;
			break;
		case 13:
			((CheckBox)target).PreviewMouseLeftButtonUp += PriorityCheckboxClick;
			break;
		case 14:
			((CheckBox)target).PreviewMouseLeftButtonUp += PriorityCheckboxClick;
			break;
		case 15:
			((CheckBox)target).PreviewMouseLeftButtonUp += PriorityCheckboxClick;
			break;
		case 16:
			((CheckBox)target).PreviewMouseLeftButtonUp += PriorityCheckboxClick;
			break;
		case 17:
			assigneeGrid = (DockPanel)target;
			break;
		case 18:
			AssignField = (Grid)target;
			AssignField.PreviewMouseLeftButtonDown += OnDropDownFieldMouseDown;
			AssignField.PreviewMouseLeftButtonUp += OnAssigneeClick;
			break;
		case 19:
			AssignTextBox = (EmjTextBlock)target;
			break;
		case 20:
			AssignPopup = (EscPopup)target;
			break;
		case 21:
			KeywordsGrid = (DockPanel)target;
			break;
		case 22:
			KeywordsText = (TextBox)target;
			KeywordsText.TextChanged += OnKeywordsChagned;
			break;
		case 23:
			TaskTypeGrid = (Grid)target;
			break;
		case 24:
			AllType = (RadioButton)target;
			AllType.Click += TaskTypeAllClick;
			break;
		case 25:
			((CheckBox)target).Click += TaskTypeCheckboxClick;
			break;
		case 26:
			((CheckBox)target).Click += TaskTypeCheckboxClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
