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
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views.Config;

public class TemplateControl : UserControl, IComponentConnector, IStyleConnector
{
	private TemplateKind _kind;

	public static readonly DependencyProperty UsedInSettingsProperty = DependencyProperty.Register("UsedInSettings", typeof(bool), typeof(TemplateControl), new PropertyMetadata(true, null));

	private List<TaskTemplateModel> _localTaskTemplate = new List<TaskTemplateModel>();

	private bool _pulled;

	private ObservableCollection<TemplateViewModel> _templateViewModels = new ObservableCollection<TemplateViewModel>();

	private AddTaskViewModel _addModel;

	private ListViewContainer _listView;

	private bool _inSettings;

	internal TemplateControl Root;

	internal Grid Container;

	internal Border SwitchBorder;

	internal TextBlock TitleText;

	internal EscPopup TitlePopup;

	internal ScrollViewer Scroller;

	internal ItemsControl MyTemplates;

	internal ItemsControl MyTemplatesInSetting;

	internal ContentControl EmptyGrid;

	internal Grid ToastGrid;

	internal TextBlock ToastTextBlock;

	private bool _contentLoaded;

	public bool UsedInSettings
	{
		get
		{
			return (bool)GetValue(UsedInSettingsProperty);
		}
		set
		{
			SetValue(UsedInSettingsProperty, value);
		}
	}

	public event EventHandler<TemplateViewModel> TemplateSelected;

	public TemplateControl()
	{
		InitializeComponent();
		base.Loaded += delegate
		{
			DataChangedNotifier.TemplateChanged += OnTemplateChanged;
			if (!UsedInSettings)
			{
				EmptyGrid.Margin = new Thickness(0.0, 84.0, 0.0, 0.0);
			}
		};
		base.Unloaded += delegate
		{
			DataChangedNotifier.TemplateChanged -= OnTemplateChanged;
		};
		InitEmptyView();
		DragSortBehavior.SetSortCompleted(MyTemplatesInSetting, OnSortCompleted);
		DragSortBehavior.SetSortCompleted(MyTemplates, OnSortCompleted);
	}

	private void InitEmptyView()
	{
		EmptyWithTextView emptyWithTextView = new EmptyWithTextView(148.0);
		emptyWithTextView.SetData(Utils.GetString("YouDoNotHaveAnyTemplates"), Utils.GetString("TemplateEmptyTaskText"), new EmptyIconsSourceModel("TemplateEmpty", new List<int> { 2, 4, 5 }), setIcon5Color: false);
		EmptyGrid.Content = emptyWithTextView;
	}

	private async void OnTemplateChanged(object sender, EventArgs e)
	{
		_localTaskTemplate = await TemplateDao.GetLocalTemplate();
		ShowLocalTemplate(_kind);
	}

	public TemplateControl(TemplateKind kind, AddTaskViewModel addModel = null)
	{
		InitializeComponent();
		Init(kind, addModel);
	}

	public async void Init(TemplateKind kind, AddTaskViewModel addModel = null, ListViewContainer listView = null)
	{
		_kind = kind;
		_localTaskTemplate = await TemplateDao.GetLocalTemplate();
		ShowLocalTemplate(kind);
		_addModel = addModel;
		_listView = listView;
		await Task.Delay(10);
		TryPullRemoteTemplate(kind);
	}

	private void ShowLocalTemplate(TemplateKind kind)
	{
		TitleText.Text = Utils.GetString((kind == TemplateKind.Note) ? "NoteTemplate" : "TaskTemplate");
		List<TemplateViewModel> list = TemplateViewModel.Build(_localTaskTemplate.Where((TaskTemplateModel t) => t.Kind == kind.ToString() || (kind == TemplateKind.Task && string.IsNullOrEmpty(t.Kind))).ToList());
		list.Sort(TemplateOrderHelper.Compare);
		_templateViewModels = new ObservableCollection<TemplateViewModel>(list);
		ItemsControl itemsControl = (_inSettings ? MyTemplatesInSetting : MyTemplates);
		itemsControl.ItemsSource = _templateViewModels;
		ObservableCollection<TemplateViewModel> templateViewModels = _templateViewModels;
		if (templateViewModels != null && templateViewModels.Count <= 0)
		{
			itemsControl.Visibility = Visibility.Collapsed;
			EmptyGrid.Visibility = Visibility.Visible;
			if (EmptyGrid.Content is EmptyWithTextView emptyWithTextView)
			{
				emptyWithTextView.SetContent(Utils.GetString((kind == TemplateKind.Note) ? "TemplateEmptyNoteText" : "TemplateEmptyTaskText"));
			}
		}
		else
		{
			itemsControl.Visibility = Visibility.Visible;
			EmptyGrid.Visibility = Visibility.Collapsed;
		}
	}

	private async void TryPullRemoteTemplate(TemplateKind kind)
	{
		if (!Utils.IsNetworkAvailable())
		{
			Toast(Utils.GetString("TemplateNoNetWork"));
			return;
		}
		TemplatesModel templates = await Communicator.PullTemplates();
		if (templates != null)
		{
			List<TaskTemplateModel> defaultTemplate = new List<TaskTemplateModel>();
			if (templates.noteTemplates == null && _localTaskTemplate.Count((TaskTemplateModel t) => t.Kind == TemplateKind.Note.ToString()) == 0)
			{
				List<TaskTemplateModel> defaultTemplates = TaskTemplateModel.GetDefaultNoteTemp();
				await TemplateDao.AddTemplates(defaultTemplates, isNote: true);
				defaultTemplate.AddRange(defaultTemplates);
			}
			if (templates.taskTemplates == null && _localTaskTemplate.Count((TaskTemplateModel t) => t.Kind == TemplateKind.Task.ToString() || string.IsNullOrEmpty(t.Kind)) == 0)
			{
				List<TaskTemplateModel> defaultTemplates = TaskTemplateModel.GetDefaultTaskTemp();
				await TemplateDao.AddTemplates(defaultTemplates);
				defaultTemplate.AddRange(defaultTemplates);
			}
			if (await TemplateDao.MergeTemplates(templates.taskTemplates, templates.noteTemplates, _localTaskTemplate))
			{
				_localTaskTemplate = await TemplateDao.GetLocalTemplate();
				ShowLocalTemplate(kind);
			}
			else if (defaultTemplate.Count > 0)
			{
				_localTaskTemplate.AddRange(defaultTemplate);
				ShowLocalTemplate(kind);
			}
		}
	}

	private void Toast(string toastText)
	{
		ToastTextBlock.Text = toastText;
		ToastGrid.Visibility = Visibility.Visible;
		((Storyboard)FindResource("ToastShowAndHide")).Begin();
	}

	private void HideToastGrid(object sender, EventArgs e)
	{
		ToastGrid.Visibility = Visibility.Collapsed;
	}

	private void TemplateOptionsClick(object sender, RoutedEventArgs routedEventArgs)
	{
		if (((Button)sender).DataContext is TemplateViewModel templateViewModel)
		{
			templateViewModel.OpenOption = true;
		}
	}

	private void OnApplyTemplateClick(object sender, RoutedEventArgs routedEventArgs)
	{
		if (sender is Button { DataContext: TemplateViewModel dataContext })
		{
			AddTask(dataContext);
			dataContext.OpenOption = false;
		}
	}

	private async void AddTask(TemplateViewModel model)
	{
		if (model == null)
		{
			return;
		}
		string projectId = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == _addModel?.ProjectId)?.id ?? Utils.GetInboxId();
		if (await ProChecker.CheckTaskLimit(projectId))
		{
			return;
		}
		TaskModel taskModel = await TaskService.AddTaskFromTemplate(model.Id, _addModel);
		if (taskModel != null)
		{
			if (_addModel != null)
			{
				_listView?.ReloadTaskListAndSelect(forceReload: true, taskModel.id, forceFocus: false);
				await Task.Delay(100);
				Utils.FindParent<SettingDialog>(this)?.Close();
			}
			else if (_listView?.GetSelectedProject() is NormalProjectIdentity normalProjectIdentity && normalProjectIdentity.Id == projectId)
			{
				_listView?.ReloadTaskListAndSelect(forceReload: true, taskModel.id, forceFocus: false);
			}
		}
	}

	private void OnRenameTemplateClick(object sender, RoutedEventArgs routedEventArgs)
	{
		if (!(sender is Button { DataContext: var dataContext }))
		{
			return;
		}
		TemplateViewModel model = dataContext as TemplateViewModel;
		if (model == null)
		{
			return;
		}
		EditTemplateWindow editTemplateWindow = new EditTemplateWindow(model.Title, _localTaskTemplate?.Select((TaskTemplateModel m) => m.Title).ToList());
		editTemplateWindow.Owner = Utils.FindParent<SettingDialog>(this);
		editTemplateWindow.Save += delegate(object obj, string title)
		{
			if (model.Title != title)
			{
				model.Title = title;
				TaskTemplateModel taskTemplateModel = _localTaskTemplate.FirstOrDefault((TaskTemplateModel m) => m.Id == model.Id);
				if (taskTemplateModel != null)
				{
					taskTemplateModel.Title = title;
					TemplateDao.UpdateTemplate(taskTemplateModel);
				}
			}
		};
		editTemplateWindow.ShowDialog();
	}

	private async void OnDeleteTemplateClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (sender is FrameworkElement { DataContext: var dataContext })
		{
			TemplateViewModel model = dataContext as TemplateViewModel;
			if (model != null && new CustomerDialog(Utils.GetString("DeleteTemplate"), string.Format(Utils.GetString("SureDeleteTemplate"), model.Title), MessageBoxButton.OKCancel).ShowDialog() == true)
			{
				_localTaskTemplate = await TemplateDao.GetLocalTemplate();
				TaskTemplateModel taskTemplateModel = _localTaskTemplate.FirstOrDefault((TaskTemplateModel m) => m.Id == model.Id);
				_templateViewModels.Remove(_templateViewModels.FirstOrDefault((TemplateViewModel m) => m.Id == model.Id));
				TemplateDao.DeleteTemplate(taskTemplateModel);
				_localTaskTemplate.Remove(taskTemplateModel);
			}
		}
		ObservableCollection<TemplateViewModel> templateViewModels = _templateViewModels;
		if (templateViewModels != null && templateViewModels.Count <= 0)
		{
			MyTemplates.Visibility = Visibility.Collapsed;
			MyTemplatesInSetting.Visibility = Visibility.Collapsed;
			EmptyGrid.Visibility = Visibility.Visible;
			if (EmptyGrid.Content is EmptyWithTextView emptyWithTextView)
			{
				emptyWithTextView.SetContent(Utils.GetString((_kind == TemplateKind.Note) ? "TemplateEmptyNoteText" : "TemplateEmptyTaskText"));
			}
		}
	}

	private void OnTemplateClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is Grid { DataContext: TemplateViewModel dataContext }))
		{
			return;
		}
		if (UsedInSettings)
		{
			PreviewTemplateWindow previewTemplateWindow = new PreviewTemplateWindow(dataContext);
			previewTemplateWindow.Owner = Utils.FindParent<SettingDialog>(this);
			previewTemplateWindow.AddTask += delegate(object obj, TemplateViewModel template)
			{
				AddTask(template);
			};
			previewTemplateWindow.ShowDialog();
		}
		else
		{
			this.TemplateSelected?.Invoke(this, dataContext);
		}
	}

	private async void ReloadData()
	{
		_localTaskTemplate = await TemplateDao.GetLocalTemplate();
		ShowLocalTemplate(_kind);
	}

	private void SwitchTemplateType(TemplateKind kind)
	{
		if (_kind != kind)
		{
			ShowLocalTemplate(kind);
			_kind = kind;
		}
	}

	private void TemplateGuideClick(object sender, MouseButtonEventArgs e)
	{
		new CustomerDialog(Utils.GetString("HowToCreateTemplate"), Utils.GetString("HowToCreateTemplateContent"), Utils.GetString("GotIt"), "", Window.GetWindow(this)).ShowDialog();
	}

	private async void ManageTemplateClick(object sender, MouseButtonEventArgs e)
	{
		Window.GetWindow(this);
		Window.GetWindow(this)?.Close();
		await Task.Delay(50);
		SettingDialog.ShowTemplateSettingDialog(null, _kind, _addModel);
	}

	public void SetSettingMode()
	{
		_inSettings = true;
		Scroller.Visibility = Visibility.Collapsed;
		MyTemplatesInSetting.Visibility = Visibility.Visible;
	}

	private void OnSwitchBorderClick(object sender, MouseButtonEventArgs e)
	{
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"Task", Utils.GetString("TaskTemplate"), (Geometry)null, "")
			{
				Selected = (_kind == TemplateKind.Task)
			},
			new CustomMenuItemViewModel((object)"Note", Utils.GetString("NoteTemplate"), (Geometry)null, "")
			{
				Selected = (_kind == TemplateKind.Note)
			}
		}, TitlePopup);
		customMenuList.Operated += OnActionSelected;
		customMenuList.Show();
	}

	private void OnActionSelected(object sender, object e)
	{
		if (e is string text)
		{
			SwitchTemplateType((!(text != "Note")) ? TemplateKind.Note : TemplateKind.Task);
		}
	}

	private void OnSortCompleted(object dragItem, int oldIndex, int newIndex)
	{
		if (TemplateOrderHelper.CountWithoutOrder(_templateViewModels) > 1)
		{
			TemplateOrderHelper.ResetOrders(_templateViewModels);
			TemplateViewModel.SaveSortOrders(_templateViewModels);
			return;
		}
		long? num = TemplateOrderHelper.CalculateInsertedOrder(_templateViewModels, newIndex);
		if (num.HasValue && dragItem is TemplateViewModel templateViewModel)
		{
			templateViewModel.SortOrder = num.Value;
			templateViewModel.HasOrder = true;
			templateViewModel.SaveSortOrder();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/templatecontrol.xaml", UriKind.Relative);
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
			Root = (TemplateControl)target;
			break;
		case 6:
			((Storyboard)target).Completed += HideToastGrid;
			break;
		case 7:
			Container = (Grid)target;
			break;
		case 8:
			SwitchBorder = (Border)target;
			SwitchBorder.MouseLeftButtonUp += OnSwitchBorderClick;
			break;
		case 9:
			TitleText = (TextBlock)target;
			break;
		case 10:
			TitlePopup = (EscPopup)target;
			break;
		case 11:
			Scroller = (ScrollViewer)target;
			break;
		case 12:
			MyTemplates = (ItemsControl)target;
			break;
		case 13:
			MyTemplatesInSetting = (ItemsControl)target;
			break;
		case 14:
			EmptyGrid = (ContentControl)target;
			break;
		case 15:
			ToastGrid = (Grid)target;
			break;
		case 16:
			ToastTextBlock = (TextBlock)target;
			break;
		case 17:
			((StackPanel)target).MouseLeftButtonUp += TemplateGuideClick;
			break;
		case 18:
			((TextBlock)target).MouseLeftButtonUp += ManageTemplateClick;
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
		case 2:
			((Grid)target).MouseLeftButtonUp += OnTemplateClick;
			break;
		case 3:
			((Button)target).Click += TemplateOptionsClick;
			break;
		case 4:
			((Button)target).Click += OnApplyTemplateClick;
			break;
		case 5:
			((Button)target).Click += OnRenameTemplateClick;
			break;
		}
	}
}
