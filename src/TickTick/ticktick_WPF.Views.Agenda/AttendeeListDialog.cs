using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Agenda;

public class AttendeeListDialog : Window, IComponentConnector, IStyleConnector
{
	private readonly string _projectId;

	private readonly string _attendId;

	private readonly string _taskId;

	private readonly TaskAttendModel _model;

	internal ItemsControl AttendeeItems;

	private bool _contentLoaded;

	public AttendeeListDialog(string projectId, string attendId, string taskId, TaskAttendModel model)
	{
		InitializeComponent();
		_attendId = attendId;
		_projectId = projectId;
		_taskId = taskId;
		_model = model;
		LoadData();
	}

	private async Task LoadData()
	{
		if (_model == null)
		{
			return;
		}
		bool canDelete = _model.organizer.isMyself;
		TaskModel taskModel = await TaskDao.GetThinTaskById(_taskId);
		if (taskModel != null)
		{
			ProjectModel projectById = CacheManager.GetProjectById(taskModel.projectId);
			if (projectById != null && projectById.IsShareList())
			{
				canDelete = true;
			}
		}
		UserInfoModel user = await UserDao.GetUserByName(LocalSettings.Settings.LoginUserName);
		ObservableCollection<UserProfileViewModel> observableCollection = new ObservableCollection<UserProfileViewModel>();
		if (_model.organizer != null)
		{
			observableCollection.Add(_model.organizer.isMyself ? new UserProfileViewModel(user, isMyself: true)
			{
				IsOwner = true
			} : new UserProfileViewModel(_model.organizer)
			{
				IsOwner = true
			});
		}
		if (_model.attendees != null && _model.attendees.Any())
		{
			foreach (AttendeeModel attendee in _model.attendees)
			{
				observableCollection.Add(new UserProfileViewModel(attendee)
				{
					ShowDelete = canDelete
				});
			}
			observableCollection = new ObservableCollection<UserProfileViewModel>((from m in observableCollection.ToList()
				orderby m.IsOwner descending, m.IsMySelf descending
				select m).ToList());
		}
		AttendeeItems.ItemsSource = observableCollection;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async void OnDeleteClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is TextBlock { Tag: var tag }))
		{
			return;
		}
		string userCode = tag as string;
		if (userCode != null)
		{
			await Communicator.RemoveAttendee(_projectId, _attendId, userCode);
			ObservableCollection<UserProfileViewModel> observableCollection = (ObservableCollection<UserProfileViewModel>)AttendeeItems.ItemsSource;
			UserProfileViewModel userProfileViewModel = observableCollection.FirstOrDefault((UserProfileViewModel m) => m.UserCode == userCode);
			if (userProfileViewModel != null)
			{
				observableCollection.Remove(userProfileViewModel);
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/agenda/attendeelistdialog.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			AttendeeItems = (ItemsControl)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			((TextBlock)target).MouseLeftButtonUp += OnDeleteClick;
		}
	}
}
