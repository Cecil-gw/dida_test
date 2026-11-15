using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using Emoji.Wpf;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar.Agenda;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Views.Calendar;

public class PomoDetailWindow : EscPopup, IComponentConnector, IStyleConnector
{
	public static bool PopupShowing;

	private PomoFilterControl _pomoFilter;

	private PomoDisplayViewModel _model;

	private CalendarDisplayViewModel _calModel;

	private CalendarAgendaItemViewModel _agendaModel;

	private readonly TaskBaseViewModel _source;

	internal PomoDetailWindow Root;

	internal ContentControl Control;

	internal Grid Container;

	internal ScrollViewer ScrollViewer;

	internal ItemsControl TaskPomoItems;

	internal EmojiEditor NoteEditor;

	internal TextBlock LengthText;

	internal Popup FilterPopup;

	internal Border DeleteBorder;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	internal Popup DeletePopup;

	private bool _contentLoaded;

	public PomoDetailWindow(PomoDisplayViewModel model, CalendarDisplayViewModel calModel)
	{
		InitializeComponent();
		_model = model;
		_calModel = calModel;
		_source = _calModel?.SourceViewModel;
		base.DataContext = _model;
		NoteEditor.Text = model.Note;
		NoteEditor.ReadOnly = !model.Enable;
		base.Closed += delegate
		{
			PopupShowing = false;
			PopupStateManager.OnViewPopupClosed();
			if (!Utils.IsEqualString(_model.Note, NoteEditor.Text))
			{
				PomoService.SaveNote(_model.Id, NoteEditor.Text);
			}
		};
	}

	public PomoDetailWindow(PomoDisplayViewModel model, CalendarAgendaItemViewModel agenda)
	{
		InitializeComponent();
		_model = model;
		_agendaModel = agenda;
		_source = agenda?.SourceViewModel;
		base.DataContext = _model;
		NoteEditor.Text = model.Note;
		NoteEditor.ReadOnly = !model.Enable;
		base.Closed += delegate
		{
			PopupStateManager.OnViewPopupClosed();
			if (!Utils.IsEqualString(_model.Note, NoteEditor.Text))
			{
				PomoService.SaveNote(_model.Id, NoteEditor.Text);
			}
		};
	}

	private async void OnDeleteMouseUp(object sender, MouseButtonEventArgs e)
	{
		bool? flag = new CustomerDialog(Utils.GetString("DeleteFocusRecord"), Utils.GetString("DeleteFocusRecordMsg"), Utils.GetString("Delete"), Utils.GetString("Cancel"))
		{
			Owner = App.Window
		}.ShowDialog();
		if (flag.HasValue && flag.Value && base.DataContext is PomoDisplayViewModel pomoDisplayViewModel)
		{
			await PomoService.DeleteById(pomoDisplayViewModel.Id);
			Close();
		}
	}

	private void OnShowLinkPopupMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PomoTaskDisplayViewModel dataContext } frameworkElement)
		{
			ShowLinkPopup(frameworkElement, dataContext);
		}
	}

	private void ShowLinkPopup(FrameworkElement element, PomoTaskDisplayViewModel pomoTask)
	{
		if (_pomoFilter == null)
		{
			_pomoFilter = new PomoFilterControl(FilterPopup, inFocus: false)
			{
				ShowComplete = true,
				DataContext = new FocusViewModel(pomoTask.GetId())
			};
			FilterPopup.Child = _pomoFilter;
		}
		_pomoFilter.ClearItemSelectedEvent();
		_pomoFilter.ItemSelected += async delegate(object obj, DisplayItemModel model)
		{
			FilterPopup.IsOpen = false;
			TimerModel timer = await TimerDao.GetTimerByIdOrObjId(model.Id);
			pomoTask.Title = (model.IsHabit ? model.Habit.Name : model.Title);
			pomoTask.HabitId = (model.IsHabit ? model.Id : ((timer != null && timer.ObjType == "habit") ? timer.ObjId : null));
			pomoTask.TaskId = (model.IsTaskOrNote ? model.Id : ((timer != null && timer.ObjType == "task") ? timer.ObjId : null));
			pomoTask.Bind = true;
			CheckTitleChanged(pomoTask);
			await Task.Delay(100);
			PomoService.RebindTaskIdOfPomoTask(pomoTask.PomoId, pomoTask.StartTime, pomoTask.TaskId, pomoTask.HabitId, timer);
		};
		FilterPopup.PlacementTarget = element;
		FilterPopup.IsOpen = true;
	}

	private void CheckTitleChanged(PomoTaskDisplayViewModel pomoTask)
	{
		if (!(base.DataContext is PomoDisplayViewModel pomoDisplayViewModel) || _source == null)
		{
			return;
		}
		TaskBaseViewModel taskBaseViewModel = null;
		if (pomoDisplayViewModel.PomoTaskModels.Count <= 1)
		{
			_source.Title = pomoTask.Title;
			taskBaseViewModel = TaskCache.GetTaskById(pomoTask.TaskId);
		}
		else
		{
			long maxDuration = pomoDisplayViewModel.PomoTaskModels.Max((PomoTaskDisplayViewModel m) => m.Duration);
			PomoTaskDisplayViewModel pomoTaskDisplayViewModel = pomoDisplayViewModel.PomoTaskModels.FirstOrDefault((PomoTaskDisplayViewModel p) => p.Duration == maxDuration);
			if (pomoTaskDisplayViewModel?.Title != pomoTask.Title)
			{
				return;
			}
			_source.Title = pomoTask.Title + "...";
			taskBaseViewModel = TaskCache.GetTaskById(pomoTaskDisplayViewModel?.TaskId);
		}
		TaskBaseViewModel taskBaseViewModel2 = new TaskBaseViewModel
		{
			Type = DisplayType.Pomo,
			Id = _source.Id,
			Title = _source.Title,
			IsAllDay = false,
			StartDate = _source.StartDate,
			DueDate = _source.DueDate,
			CompletedTime = _source.CompletedTime
		};
		_calModel?.SetSourceModel(taskBaseViewModel2, setEvent: true);
		_agendaModel?.SetSourceModel(taskBaseViewModel2, b: true);
		taskBaseViewModel2.SetDependenceModel(taskBaseViewModel, "Color", "Tag", "Priority");
		taskBaseViewModel2.Color = taskBaseViewModel?.Color;
		taskBaseViewModel2.Tag = taskBaseViewModel?.Tag;
		taskBaseViewModel2.Priority = taskBaseViewModel?.Priority ?? 0;
	}

	public async void Show(UIElement target, double targetWidth, bool showByMouse)
	{
		base.PlacementTarget = target;
		TaskPopupArgs popupLocation = PopupLocationCalculator.GetPopupLocation(target, targetWidth, 420.0, showByMouse, 0.0);
		if (!popupLocation.ByMouse)
		{
			base.Placement = (popupLocation.IsRight ? PlacementMode.Right : PlacementMode.Left);
			base.HorizontalOffset = ((!popupLocation.IsRight) ? 6 : 0);
			base.VerticalOffset = -4.0;
		}
		else
		{
			base.Placement = PlacementMode.Mouse;
		}
		await OpenAsync();
	}

	public async void ShowCentered(UIElement target)
	{
		base.PlacementTarget = target;
		base.Placement = PlacementMode.Center;
		base.HorizontalOffset = 0.0;
		base.VerticalOffset = 0.0;
		await OpenAsync();
	}

	private async Task OpenAsync()
	{
		PopupStateManager.OnViewPopupOpened();
		base.IsOpen = true;
		await Task.Delay(200);
		if (base.IsOpen)
		{
			PopupShowing = true;
			PopupStateManager.OnViewPopupOpened();
		}
	}

	private async Task ShowTaskDetail(FrameworkElement element, PomoTaskDisplayViewModel pomoTask)
	{
		if (!string.IsNullOrEmpty(pomoTask.TaskId) && await CheckTaskExist(pomoTask.TaskId) && PopupStateManager.LastTarget != element)
		{
			TaskDetailPopup obj = new TaskDetailPopup
			{
				IsCalendarContext = true,
				DependentWindow = null
			};
			base.StaysOpen = true;
			obj.Disappear -= OnDetailClosed;
			obj.Disappear += OnDetailClosed;
			obj.Show(pomoTask.TaskId, null, new TaskWindowDisplayArgs(element, 30.0, PopupLocationCalculator.GetMousePoint(isDefault: false), 0.0));
		}
	}

	private async void OnTitleClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: PomoTaskDisplayViewModel dataContext } frameworkElement))
		{
			return;
		}
		if (dataContext.Bind)
		{
			if (!string.IsNullOrEmpty(dataContext.TaskId))
			{
				await ShowTaskDetail(frameworkElement, dataContext);
			}
			else if (!string.IsNullOrEmpty(dataContext.HabitId))
			{
				await CheckHabitExist(dataContext.HabitId);
			}
		}
		else
		{
			ShowLinkPopup(frameworkElement, dataContext);
		}
	}

	private async Task<bool> CheckHabitExist(string habitId)
	{
		HabitModel habitModel = await HabitDao.GetHabitById(habitId);
		if (habitModel == null || habitModel.SyncStatus == -1)
		{
			Toast(Utils.GetString("HabitDeleted"));
			return false;
		}
		return true;
	}

	private async Task<bool> CheckTaskExist(string taskId)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel == null || taskModel.deleted != 0)
		{
			Toast(Utils.GetString("TaskDeleted"));
			return false;
		}
		return true;
	}

	private async void OnDetailClosed(object sender, string e)
	{
		if (base.PlacementTarget != null)
		{
			Window.GetWindow(base.PlacementTarget)?.Activate();
		}
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.Disappear -= OnDetailClosed;
		}
		await Task.Delay(150);
		base.StaysOpen = false;
		PopupStateManager.OnViewPopupOpened();
		Dictionary<string, string> dictionary = (await PomoDao.GetPomoTasksByPomoId(_model.Id)).Where((PomoTask x) => !string.IsNullOrEmpty(x.TaskId)).DistinctEx((PomoTask x) => x.TaskId).ToDictionary((PomoTask x) => x.TaskId, (PomoTask x) => x.Title);
		foreach (PomoTaskDisplayViewModel pomoTaskModel in _model.PomoTaskModels)
		{
			if (!string.IsNullOrEmpty(pomoTaskModel.TaskId) && dictionary.TryGetValue(pomoTaskModel.TaskId, out var value))
			{
				pomoTaskModel.Title = value;
			}
		}
	}

	public void Toast(string toastText)
	{
		ToastBorder.Visibility = Visibility.Visible;
		ToastText.Text = toastText;
		Storyboard storyboard = (Storyboard)FindResource("ShowToast");
		ToastBorder.BeginStoryboard(storyboard);
	}

	private void OnToasted(object sender, EventArgs e)
	{
		ToastBorder.Visibility = Visibility.Collapsed;
	}

	private void CancelBtnClick(object sender, RoutedEventArgs e)
	{
		DeletePopup.IsOpen = false;
	}

	private async void OkBtnClick(object sender, RoutedEventArgs e)
	{
		DeletePopup.IsOpen = false;
		if (base.DataContext is PomoDisplayViewModel pomoDisplayViewModel)
		{
			await PomoService.DeleteById(pomoDisplayViewModel.Id);
			Close();
		}
	}

	private void OnNoteChanged(object sender, EventArgs e)
	{
		if (NoteEditor.Text.Length >= 400)
		{
			LengthText.Visibility = Visibility.Visible;
			LengthText.Text = NoteEditor.Text.Length + "/500";
		}
		else
		{
			LengthText.Visibility = Visibility.Collapsed;
		}
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (!NoteEditor.IsMouseOver)
		{
			FocusManager.SetFocusedElement(this, this);
			Keyboard.Focus(this);
		}
	}

	public void SetCustomStyle()
	{
	}

	public void OnCancel()
	{
		base.IsOpen = false;
	}

	public void Ok()
	{
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (base.StaysOpen)
		{
			TaskDetailPopup.TryCloseWindow();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/pomodetailwindow.xaml", UriKind.Relative);
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
			Root = (PomoDetailWindow)target;
			break;
		case 2:
			((Storyboard)target).Completed += OnToasted;
			break;
		case 3:
			Control = (ContentControl)target;
			Control.MouseLeftButtonDown += OnMouseDown;
			break;
		case 4:
			((Grid)target).MouseLeftButtonUp += OnMouseUp;
			break;
		case 5:
			Container = (Grid)target;
			break;
		case 6:
			ScrollViewer = (ScrollViewer)target;
			break;
		case 7:
			TaskPomoItems = (ItemsControl)target;
			break;
		case 11:
			NoteEditor = (EmojiEditor)target;
			break;
		case 12:
			LengthText = (TextBlock)target;
			break;
		case 13:
			FilterPopup = (Popup)target;
			break;
		case 14:
			DeleteBorder = (Border)target;
			DeleteBorder.MouseLeftButtonUp += OnDeleteMouseUp;
			break;
		case 15:
			ToastBorder = (Border)target;
			break;
		case 16:
			ToastText = (TextBlock)target;
			break;
		case 17:
			DeletePopup = (Popup)target;
			break;
		case 18:
			((Button)target).Click += OkBtnClick;
			break;
		case 19:
			((Button)target).Click += CancelBtnClick;
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
		case 8:
			((EmjTextBlock)target).MouseLeftButtonUp += OnTitleClick;
			break;
		case 9:
			((TextBlock)target).MouseLeftButtonUp += OnTitleClick;
			break;
		case 10:
			((Border)target).MouseLeftButtonUp += OnShowLinkPopupMouseUp;
			break;
		}
	}
}
