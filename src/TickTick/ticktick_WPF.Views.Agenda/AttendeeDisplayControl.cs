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
using System.Windows.Markup;
using Newtonsoft.Json;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Agenda;

public class AttendeeDisplayControl : UserControl, IComponentConnector
{
	internal ItemsControl AttendeePanel;

	internal TextBlock ErrorHint;

	private bool _contentLoaded;

	private List<ProfileModel> AttendModels { get; set; } = new List<ProfileModel>();

	public AttendeeDisplayControl()
	{
		InitializeComponent();
	}

	public async Task<bool> WaitingForAttend(AgendaHelper.IAgenda agenda)
	{
		return AgendaHelper.CanAccessAgenda(agenda) && AttendModels.Count == 1;
	}

	public async Task LoadData(double panelWidth, string attendId, string taskId)
	{
		UserInfoModel myself = await UserDao.GetUserByName(LocalSettings.Settings.LoginUserName);
		TaskAttendModel model = await AgendaHelper.GetAgendaAttendModel(attendId, taskId);
		LoadData(panelWidth, myself, model);
		TaskAttendModel taskAttendModel = await AgendaHelper.GetRemoteModel(attendId, taskId);
		string text = JsonConvert.SerializeObject(model);
		string text2 = JsonConvert.SerializeObject(taskAttendModel);
		if (text != text2)
		{
			LoadData(panelWidth, myself, taskAttendModel);
		}
	}

	private void LoadData(double panelWidth, UserInfoModel myself, TaskAttendModel model)
	{
		AttendeePanel.ItemsSource = null;
		AttendModels.Clear();
		if (model != null)
		{
			if (model.organizer != null)
			{
				AssembleDisplayModels(model, myself);
				NotifyWidthChanged(panelWidth);
			}
			else
			{
				DisplayError();
			}
		}
	}

	private void DisplayError()
	{
		AttendeePanel.Visibility = Visibility.Collapsed;
		ErrorHint.Visibility = Visibility.Visible;
	}

	private void AssembleDisplayModels(TaskAttendModel model, UserInfoModel myself)
	{
		AttendeePanel.Visibility = Visibility.Visible;
		ErrorHint.Visibility = Visibility.Collapsed;
		if (model == null)
		{
			return;
		}
		if (model.organizer != null)
		{
			if (model.organizer.isMyself)
			{
				AttendModels.Add(new ProfileModel
				{
					name = myself.name,
					email = myself.email,
					avatarUrl = myself.picture,
					isMyself = true,
					isOwner = true
				});
			}
			else
			{
				model.organizer.isOwner = true;
				AttendModels.Add(model.organizer);
			}
		}
		if (model.attendees != null && model.attendees.Any())
		{
			foreach (AttendeeModel attendee in model.attendees)
			{
				if (attendee.user.isMyself)
				{
					AttendModels.Add(new ProfileModel
					{
						name = myself.name,
						email = myself.email,
						avatarUrl = myself.picture,
						isOwner = false,
						isMyself = true
					});
				}
				else
				{
					AttendModels.Add(attendee.user);
				}
			}
		}
		if (AttendModels != null && AttendModels.Any())
		{
			AttendModels = (from m in AttendModels
				orderby m.isOwner descending, m.isMyself descending
				select m).ToList();
		}
	}

	public void NotifyWidthChanged(double width)
	{
		ObservableCollection<BaseViewModel> observableCollection = new ObservableCollection<BaseViewModel>();
		int displayCount = GetDisplayCount(width);
		if (AttendModels == null || !AttendModels.Any())
		{
			return;
		}
		List<ProfileModel> list = AttendModels.Take(displayCount - 1).ToList();
		foreach (ProfileModel item in list)
		{
			observableCollection.Add(new UserProfileViewModel(item));
		}
		if (AttendModels.Count > displayCount)
		{
			int num = AttendModels.Count - list.Count;
			observableCollection.Add(new AttendeeExtraViewModel
			{
				Title = "+" + num
			});
		}
		else
		{
			observableCollection.Add(new AttendeeExtraViewModel
			{
				Title = string.Format(Utils.GetString("PersonCount"), AttendModels.Count)
			});
		}
		AttendeePanel.ItemsSource = observableCollection;
	}

	private static int GetDisplayCount(double panelWidth)
	{
		return (int)(panelWidth / 28.0) - 1;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/agenda/attendeedisplaycontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			AttendeePanel = (ItemsControl)target;
			break;
		case 2:
			ErrorHint = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
