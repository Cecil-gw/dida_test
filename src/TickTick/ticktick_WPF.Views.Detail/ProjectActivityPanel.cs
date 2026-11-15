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
using Newtonsoft.Json;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Activity;

namespace ticktick_WPF.Views.Detail;

public class ProjectActivityPanel : Grid, IComponentConnector
{
	internal Grid ProjectBackPathGrid;

	internal ProjectActivityControl ProjectActivityControl;

	private bool _contentLoaded;

	public event EventHandler HideDetail;

	public ProjectActivityPanel(bool showBack)
	{
		InitializeComponent();
		SetBachPathVisible(showBack);
	}

	private void OnBackClick(object sender, MouseButtonEventArgs e)
	{
		ProjectActivityControl.Items.ItemsSource = null;
		this.HideDetail?.Invoke(this, null);
	}

	public async Task Init(string projectId)
	{
		ProjectActivityControl.Items.ItemsSource = null;
		UserInfoModel userInfoModel = await UserManager.GetUserInfo();
		string userName = LocalSettings.Settings.LoginUserName;
		if (userInfoModel != null)
		{
			userName = (string.IsNullOrEmpty(userInfoModel.name) ? userInfoModel.username : userInfoModel.name);
		}
		string value = await Communicator.GetProjectActivities(projectId);
		try
		{
			List<ProjectModifyModel> items = JsonConvert.DeserializeObject<List<ProjectModifyModel>>(value)?.Where((ProjectModifyModel item) => !item.action.ToUpper().Contains("P_VIEW_MODE")).ToList();
			Dictionary<string, TaskDao.TitleModel> titles = (await TaskDao.GetTaskTitleInProject()).ToDictionary((TaskDao.TitleModel title) => title.TaskId, (TaskDao.TitleModel title) => title);
			if (items == null || items.Count <= 0)
			{
				return;
			}
			List<ProjectActivityViewModel> list = new List<ProjectActivityViewModel>();
			foreach (ProjectModifyModel item in items)
			{
				list.Add(new ProjectActivityViewModel(item, userName, titles));
			}
			IEnumerable<IGrouping<DateTime, ProjectActivityViewModel>> enumerable = from item in list
				orderby item.Date descending
				group item by item.Date.Date;
			List<ProjectActivityViewModel> list2 = new List<ProjectActivityViewModel>();
			foreach (IGrouping<DateTime, ProjectActivityViewModel> item2 in enumerable)
			{
				list2.Add(new ProjectActivityViewModel(DateUtils.FormatTimeDesc(item2.Key, isAllDay: true) + " " + DateUtils.FormatWeekDayName(item2.Key)));
				list2.AddRange(item2);
			}
			ProjectActivityControl.Items.ItemsSource = list2;
		}
		catch (Exception)
		{
		}
	}

	public void SetBachPathVisible(bool show)
	{
		ProjectBackPathGrid.Visibility = ((!show) ? Visibility.Collapsed : Visibility.Visible);
	}

	public void ClearEvent()
	{
		this.HideDetail = null;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/projectactivitypanel.xaml", UriKind.Relative);
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
			ProjectBackPathGrid = (Grid)target;
			ProjectBackPathGrid.MouseLeftButtonUp += OnBackClick;
			break;
		case 2:
			ProjectActivityControl = (ProjectActivityControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
