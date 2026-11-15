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
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.Setting;

public class NewTaskDefault : UserControl, IComponentConnector
{
	private List<int> _durationValues;

	private bool _changed;

	internal CustomSimpleComboBox DefaultDateCombobox;

	internal Grid DateModePanel;

	internal CustomSimpleComboBox DateModeComboBox;

	internal CustomSimpleComboBox DurationUnitComboBox;

	internal Border TimeReminderBorder;

	internal TextBlock SelectTimeText;

	internal Path TimeReminderIcon;

	internal EscPopup SelectReminderPopup;

	internal Border AlldayReminderBorder;

	internal TextBlock SelectAlldayText;

	internal Path AllDayReminderIcon;

	internal EscPopup SelectAllDayReminderPopup;

	internal CustomSimpleComboBox DefaultPriorityComboBox;

	internal EmjTextBlock DefaultAddTagNameText;

	internal EscPopup DefaultAddTagPopup;

	internal Path ProjectArrow;

	internal EmjTextBlock DefaultAddProjectNameText;

	internal EscPopup DefaultAddProjectPopup;

	internal CustomSimpleComboBox AddToComboBox;

	internal CustomSimpleComboBox PosOfOverdueComboBox;

	internal TextBlock ResetDefault;

	private bool _contentLoaded;

	public NewTaskDefault()
	{
		InitializeComponent();
		InitData();
		base.Unloaded += OnUnloaded;
	}

	public async void InitData()
	{
		TaskDefaultModel model = await TaskDefaultDao.GetTaskDefaultWithDefault();
		TaskDefaultViewModel taskDefaultViewModel = new TaskDefaultViewModel(model);
		string emptyText = Utils.GetString("none");
		if (!string.IsNullOrEmpty(model.TimeReminders))
		{
			List<string> reminders = model.TimeReminders.Split(',').ToList();
			SelectTimeText.Text = GetDisplayText(reminders, isAllDay: false, emptyText);
			SelectTimeText.Tag = model.TimeReminders;
		}
		else
		{
			SelectTimeText.Text = Utils.GetString("none");
			SelectTimeText.Tag = string.Empty;
		}
		if (!string.IsNullOrEmpty(model.AllDayReminders))
		{
			List<string> reminders2 = model.AllDayReminders.Split(',').ToList();
			SelectAlldayText.Text = GetDisplayText(reminders2, isAllDay: true, emptyText);
			SelectAlldayText.Tag = model.AllDayReminders;
		}
		else
		{
			SelectAlldayText.Text = Utils.GetString("none");
			SelectAlldayText.Tag = string.Empty;
		}
		SetTagText(taskDefaultViewModel.Tags);
		if (LocalSettings.Settings.InServerBoxId.Equals(model.ProjectId))
		{
			SetDefaultProjectToInbox();
		}
		else
		{
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == model.ProjectId);
			if (projectModel != null)
			{
				DefaultAddProjectNameText.Text = projectModel.name;
				DefaultAddProjectNameText.Tag = projectModel.id;
			}
			else
			{
				SetDefaultProjectToInbox();
			}
		}
		DateModePanel.Visibility = ((!UserDao.IsPro()) ? Visibility.Collapsed : Visibility.Visible);
		base.DataContext = taskDefaultViewModel;
		InitDurationUnitComboBox(taskDefaultViewModel.Duration);
		InitDefaultDateComboBox(taskDefaultViewModel);
		void SetDefaultProjectToInbox()
		{
			DefaultAddProjectNameText.Text = Utils.GetString("Inbox");
			DefaultAddProjectNameText.Tag = LocalSettings.Settings.InServerBoxId;
		}
	}

	private void InitDefaultDateComboBox(TaskDefaultViewModel vm)
	{
		DefaultDateCombobox.ItemsSource = new List<string>
		{
			Utils.GetString("none"),
			Utils.GetString("Today"),
			Utils.GetString("Tomorrow"),
			Utils.GetString("DayAfterTomorrow"),
			Utils.GetString("NextWeek")
		};
		DefaultDateCombobox.SelectedIndex = vm.DateIndex;
		DateModeComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("date"),
			Utils.GetString("Duration")
		};
		DateModeComboBox.SelectedIndex = vm.DateMode;
		DefaultPriorityComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("PriorityHigh"),
			Utils.GetString("PriorityMedium"),
			Utils.GetString("PriorityLow"),
			Utils.GetString("PriorityNull")
		};
		DefaultPriorityComboBox.SelectedIndex = vm.PriorityIndex;
		AddToComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("TopOfList"),
			Utils.GetString("BottomOfList")
		};
		AddToComboBox.SelectedIndex = vm.AddTo;
		PosOfOverdueComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("TopOfList"),
			Utils.GetString("BottomOfList")
		};
		PosOfOverdueComboBox.SelectedIndex = LocalSettings.Settings.PosOfOverdue;
	}

	private void InitDurationUnitComboBox(int current)
	{
		List<int> list = new List<int> { 10, 15, 30, 45, 60, 90, 120, 150, 180 };
		if (current < 1440 && !list.Contains(current))
		{
			list.Add(current);
			list.Sort();
		}
		List<string> list2 = new List<string>();
		foreach (int item2 in list)
		{
			list2.Add(Utils.GetDurationString(item2 * 60, isShort: false, showZero: false, showSpan: true, isLong: true));
		}
		list2.Add(Utils.GetString("OneDay"));
		list.Add(1440);
		switch (current)
		{
		case 2880:
			list.Add(2880);
			list2.Add(Utils.GetString("TwoDays"));
			break;
		case 4320:
			list.Add(4320);
			list2.Add(Utils.GetString("ThreeDays"));
			break;
		}
		list2.Add(null);
		string item = Utils.GetString("Custom");
		list2.Add(item);
		DurationUnitComboBox.IgnoreValues.Add(item);
		_durationValues = list;
		int num = _durationValues.IndexOf(current);
		DurationUnitComboBox.SelectedIndex = ((num >= 0) ? num : 0);
		DurationUnitComboBox.ItemsSource = list2;
	}

	private void OnDefaultPrioritySelect(object sender, SimpleComboBoxViewModel e)
	{
		if (base.DataContext is TaskDefaultViewModel taskDefaultViewModel)
		{
			taskDefaultViewModel.PriorityIndex = DefaultPriorityComboBox.SelectedIndex;
		}
	}

	private void OnDefaultDateSelect(object sender, SimpleComboBoxViewModel e)
	{
		if (base.DataContext is TaskDefaultViewModel taskDefaultViewModel)
		{
			taskDefaultViewModel.DateIndex = DefaultDateCombobox.SelectedIndex;
		}
	}

	private void OnDurationUnitChanged(object sender, SimpleComboBoxViewModel e)
	{
		object dataContext = base.DataContext;
		TaskDefaultViewModel model = dataContext as TaskDefaultViewModel;
		if (model == null)
		{
			return;
		}
		if (e.Title == Utils.GetString("Custom"))
		{
			DurationSelectControl durationSelectControl = new DurationSelectControl(0, 30);
			EscPopup popup = new EscPopup
			{
				StaysOpen = false,
				PlacementTarget = DurationUnitComboBox,
				HorizontalOffset = -180.0
			};
			durationSelectControl.OnSave += delegate(object s, int min)
			{
				model.Duration = min;
				InitDurationUnitComboBox(model.Duration);
				popup.IsOpen = false;
			};
			durationSelectControl.OnCancel += delegate
			{
				popup.IsOpen = false;
			};
			popup.Child = durationSelectControl;
			popup.IsOpen = true;
		}
		else if (DurationUnitComboBox.SelectedIndex < _durationValues.Count)
		{
			model.Duration = _durationValues[DurationUnitComboBox.SelectedIndex];
			InitDurationUnitComboBox(model.Duration);
		}
	}

	private void OnAddToSelected(object sender, SimpleComboBoxViewModel e)
	{
		if (base.DataContext is TaskDefaultViewModel taskDefaultViewModel)
		{
			taskDefaultViewModel.AddTo = AddToComboBox.SelectedIndex;
		}
	}

	private void OnPosOfOverdueChanged(object sender, SimpleComboBoxViewModel e)
	{
		_changed = true;
		LocalSettings.Settings.PosOfOverdue = PosOfOverdueComboBox.SelectedIndex;
		LocalSettings.Settings.Save();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		SaveDefault();
	}

	public async Task SaveDefault()
	{
		if (base.DataContext != null && base.DataContext is TaskDefaultViewModel taskDefaultViewModel)
		{
			TaskDefaultModel taskDefaultModel = taskDefaultViewModel.ToModel();
			taskDefaultModel.TimeReminders = SelectTimeText.Tag?.ToString() ?? string.Empty;
			taskDefaultModel.AllDayReminders = SelectAlldayText.Tag?.ToString() ?? string.Empty;
			taskDefaultModel.ProjectId = DefaultAddProjectNameText.Tag?.ToString() ?? LocalSettings.Settings.InServerBoxId;
			taskDefaultModel.DateMode = DateModeComboBox.SelectedIndex;
			taskDefaultModel.AddTo = AddToComboBox.SelectedIndex;
			if (!TaskDefaultDao.GetDefaultSafely().Equal(taskDefaultModel))
			{
				await TaskDefaultDao.SaveTaskDefault(taskDefaultModel);
				_changed = true;
			}
			if (_changed)
			{
				_changed = false;
				DataChangedNotifier.NotifyTaskDefaultChanged();
				SettingsHelper.PushLocalSettings();
			}
		}
	}

	private void OnTimeReminderClick(object sender, MouseButtonEventArgs e)
	{
		SetReminderControl setReminderControl = new SetReminderControl(isAllDay: false, Utils.BuildReminders((IReadOnlyCollection<string>)(object)SelectTimeText.Tag.ToString().Split(',')), default(DateTime));
		setReminderControl.OnCancel += delegate
		{
			SelectReminderPopup.IsOpen = false;
		};
		setReminderControl.OnSelected += OnTimeReminderSelected;
		Mouse.Capture(null);
		SelectReminderPopup.Child = setReminderControl;
		SelectReminderPopup.IsOpen = true;
	}

	private void OnTimeReminderSelected(object sender, ReminderSelectedEventArgs e)
	{
		SelectReminderPopup.IsOpen = false;
		SelectTimeText.Tag = string.Join(",", e.Reminders);
		SelectTimeText.Text = GetDisplayText(e.Reminders, isAllDay: false, Utils.GetString("none"));
	}

	private void OnAllDayReminderClick(object sender, MouseButtonEventArgs e)
	{
		SetReminderControl setReminderControl = new SetReminderControl(isAllDay: true, Utils.BuildReminders((IReadOnlyCollection<string>)(object)SelectAlldayText.Tag.ToString().Split(',')), default(DateTime));
		setReminderControl.OnCancel += delegate
		{
			SelectAllDayReminderPopup.IsOpen = false;
		};
		setReminderControl.OnSelected += OnAllDayReminderSelected;
		Mouse.Capture(null);
		SelectAllDayReminderPopup.Child = setReminderControl;
		SelectAllDayReminderPopup.IsOpen = true;
	}

	private void OnAllDayReminderSelected(object sender, ReminderSelectedEventArgs e)
	{
		SelectAllDayReminderPopup.IsOpen = false;
		SelectAlldayText.Tag = string.Join(",", e.Reminders);
		SelectAlldayText.Text = GetDisplayText(e.Reminders, isAllDay: true, Utils.GetString("none"));
	}

	private static string GetDisplayText(IReadOnlyCollection<string> reminders, bool isAllDay, string emptyText = "")
	{
		return ReminderUtils.GetReminderListDisplayText(Utils.BuildReminders(reminders), isAllDay, emptyText);
	}

	private void OnDateModeChanged(object sender, SimpleComboBoxViewModel e)
	{
	}

	private void OnDefaultAddProjectClick(object sender, MouseButtonEventArgs e)
	{
		string item = LocalSettings.Settings.InServerBoxId;
		if (DefaultAddProjectNameText.Tag is string text)
		{
			item = text;
		}
		ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(DefaultAddProjectPopup, new ProjectExtra
		{
			ProjectIds = new List<string> { item }
		}, new ProjectSelectorExtra
		{
			showAll = false,
			batchMode = false,
			canSelectGroup = false,
			onlyShowPermission = true,
			showNoteProject = false,
			showSharedProject = true,
			CanSearch = true
		});
		projectOrGroupPopup.ItemSelect += OnDefaultProjectSelect;
		projectOrGroupPopup.Show();
	}

	private void OnDefaultTagClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		TaskDefaultViewModel vm = dataContext as TaskDefaultViewModel;
		if (vm != null)
		{
			BatchSetTagControl batchSetTagControl = new BatchSetTagControl();
			batchSetTagControl.Init(new TagSelectData
			{
				OmniSelectTags = (vm.Tags?.ToList() ?? new List<string>())
			}, firstInit: true);
			batchSetTagControl.TagsSelect += delegate(object s, TagSelectData tags)
			{
				vm.Tags = tags.OmniSelectTags;
				DefaultAddTagPopup.IsOpen = false;
				SetTagText(vm.Tags);
			};
			batchSetTagControl.Close += delegate
			{
				DefaultAddTagPopup.IsOpen = false;
			};
			DefaultAddTagPopup.Child = batchSetTagControl;
			DefaultAddTagPopup.IsOpen = true;
		}
	}

	private void SetTagText(List<string> tags)
	{
		if (tags == null || tags.Count == 0)
		{
			DefaultAddTagNameText.Text = Utils.GetString("none");
			return;
		}
		tags = tags.Select((string t) => CacheManager.GetTags().FirstOrDefault((TagModel ta) => ta.name == t.ToLower())?.label).ToList();
		tags.Remove(null);
		DefaultAddTagNameText.Text = tags.Join(",");
	}

	private void OnDefaultProjectSelect(object sender, SelectableItemViewModel e)
	{
		if (!(e is ProjectGroupViewModel))
		{
			DefaultAddProjectPopup.IsOpen = false;
			string projectId = e.Id;
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
			if (projectModel != null && (!projectModel.IsShareList() || new CustomerDialog(Utils.GetString("SetShareProjectAsDefault"), Utils.GetString("SetShareProjectAsDefaultDesc"), Utils.GetString("OK"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog() == true))
			{
				DefaultAddProjectNameText.Text = projectModel.name;
				DefaultAddProjectNameText.Tag = projectModel.id;
				SaveDefault();
			}
		}
	}

	private async void ResetDefaultClick(object sender, MouseButtonEventArgs e)
	{
		await TaskDefaultDao.SaveTaskDefault(TaskDefaultModel.BuildDefault());
		DataChangedNotifier.NotifyTaskDefaultChanged();
		SettingsHelper.PushLocalSettings();
		InitData();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/setting/newtaskdefault.xaml", UriKind.Relative);
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
			DefaultDateCombobox = (CustomSimpleComboBox)target;
			break;
		case 2:
			DateModePanel = (Grid)target;
			break;
		case 3:
			DateModeComboBox = (CustomSimpleComboBox)target;
			break;
		case 4:
			DurationUnitComboBox = (CustomSimpleComboBox)target;
			break;
		case 5:
			TimeReminderBorder = (Border)target;
			TimeReminderBorder.MouseLeftButtonUp += OnTimeReminderClick;
			break;
		case 6:
			SelectTimeText = (TextBlock)target;
			break;
		case 7:
			TimeReminderIcon = (Path)target;
			break;
		case 8:
			SelectReminderPopup = (EscPopup)target;
			break;
		case 9:
			AlldayReminderBorder = (Border)target;
			AlldayReminderBorder.MouseLeftButtonUp += OnAllDayReminderClick;
			break;
		case 10:
			SelectAlldayText = (TextBlock)target;
			break;
		case 11:
			AllDayReminderIcon = (Path)target;
			break;
		case 12:
			SelectAllDayReminderPopup = (EscPopup)target;
			break;
		case 13:
			DefaultPriorityComboBox = (CustomSimpleComboBox)target;
			break;
		case 14:
			((Border)target).MouseLeftButtonUp += OnDefaultTagClick;
			break;
		case 15:
			DefaultAddTagNameText = (EmjTextBlock)target;
			break;
		case 16:
			DefaultAddTagPopup = (EscPopup)target;
			break;
		case 17:
			ProjectArrow = (Path)target;
			break;
		case 18:
			((Border)target).MouseLeftButtonUp += OnDefaultAddProjectClick;
			break;
		case 19:
			DefaultAddProjectNameText = (EmjTextBlock)target;
			break;
		case 20:
			DefaultAddProjectPopup = (EscPopup)target;
			break;
		case 21:
			AddToComboBox = (CustomSimpleComboBox)target;
			break;
		case 22:
			PosOfOverdueComboBox = (CustomSimpleComboBox)target;
			break;
		case 23:
			ResetDefault = (TextBlock)target;
			ResetDefault.MouseLeftButtonUp += ResetDefaultClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
